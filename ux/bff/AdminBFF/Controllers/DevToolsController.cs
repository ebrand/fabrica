using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace AdminBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevToolsController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DevToolsController> _logger;
    private readonly IConfiguration _configuration;

    // Script runner runs on host machine, accessible via host.docker.internal from Docker
    private string ScriptRunnerUrl => _configuration["SCRIPT_RUNNER_URL"] ?? "http://host.docker.internal:3800";
    private string ScriptRunnerApiKey => _configuration["SCRIPT_RUNNER_API_KEY"] ?? "fabrica-dev-key";

    public DevToolsController(
        IHttpClientFactory httpClientFactory,
        ILogger<DevToolsController> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.Timeout = TimeSpan.FromMinutes(10); // Long timeout for builds
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Check if the script runner service is available
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult> GetHealth()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ScriptRunnerUrl}/health");
            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadFromJsonAsync<object>();
                return Ok(new
                {
                    scriptRunnerAvailable = true,
                    scriptRunnerUrl = ScriptRunnerUrl,
                    details = content
                });
            }

            return Ok(new
            {
                scriptRunnerAvailable = false,
                scriptRunnerUrl = ScriptRunnerUrl,
                error = "Script runner returned non-success status"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Script runner not available at {Url}", ScriptRunnerUrl);
            return Ok(new
            {
                scriptRunnerAvailable = false,
                scriptRunnerUrl = ScriptRunnerUrl,
                error = ex.Message,
                hint = "Make sure the script runner service is running on your local machine. Run: cd infrastructure/local-services/script-runner && ./start.sh"
            });
        }
    }

    /// <summary>
    /// Get list of components that can be redeployed
    /// </summary>
    [HttpGet("components")]
    public async Task<ActionResult> GetComponents()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{ScriptRunnerUrl}/components");
            request.Headers.Add("X-API-Key", ScriptRunnerApiKey);

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, new { error = "Failed to fetch components from script runner" });
            }

            var content = await response.Content.ReadFromJsonAsync<object>();
            return Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching components from script runner");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Redeploy a component - proxies to script runner and streams output via SSE
    /// </summary>
    [HttpPost("redeploy/{component}")]
    public async Task Redeploy(string component, [FromBody] RedeployRequest? request)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("Connection", "keep-alive");
        Response.Headers.Append("X-Accel-Buffering", "no");

        try
        {
            _logger.LogInformation("Starting redeploy for component: {Component}", component);

            // Create a new HttpClient for this request to avoid connection reuse issues
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);

            // Create request to script runner
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{ScriptRunnerUrl}/redeploy/{component}");
            httpRequest.Headers.Add("X-API-Key", ScriptRunnerApiKey);

            if (request != null)
            {
                httpRequest.Content = JsonContent.Create(new
                {
                    noCache = request.NoCache,
                    killPorts = request.KillPorts
                });
            }

            // Send request WITHOUT tying to browser cancellation - let the script run to completion
            using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                await WriteSSEEvent("error", new { message = $"Script runner error: {error}" });
                return;
            }

            // Stream the SSE events from script runner to client
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (!string.IsNullOrEmpty(line))
                {
                    try
                    {
                        await Response.WriteAsync(line + "\n");
                        await Response.Body.FlushAsync();
                    }
                    catch (Exception)
                    {
                        // Client disconnected, but let script continue
                        _logger.LogInformation("Client disconnected during redeploy of {Component}", component);
                    }
                }
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Redeploy cancelled by client for component: {Component}", component);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during redeploy of {Component}", component);
            await WriteSSEEvent("error", new { message = ex.Message });
        }
    }

    private async Task WriteSSEEvent(string eventType, object data)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new { type = eventType, data });
        await Response.WriteAsync($"data: {json}\n\n");
        await Response.Body.FlushAsync();
    }
}

public class RedeployRequest
{
    public bool NoCache { get; set; } = false;
    public bool KillPorts { get; set; } = false;
}
