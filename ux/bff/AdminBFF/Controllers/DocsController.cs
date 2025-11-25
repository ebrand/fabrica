using AdminBFF.Models;
using AdminBFF.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocsController : ControllerBase
{
    private readonly ServicesRegistry _servicesRegistry;
    private readonly HttpClient _httpClient;
    private readonly ILogger<DocsController> _logger;

    public DocsController(
        ServicesRegistry servicesRegistry,
        IHttpClientFactory httpClientFactory,
        ILogger<DocsController> logger)
    {
        _servicesRegistry = servicesRegistry;
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
    }

    /// <summary>
    /// Get service registry
    /// Returns list of available domain services for API documentation
    /// </summary>
    [HttpGet("services")]
    public ActionResult<List<ServiceDto>> GetServices()
    {
        try
        {
            var services = _servicesRegistry.GetServiceRegistry();
            return Ok(services);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching service registry");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Proxy Swagger JSON from domain services
    /// Route: GET /api/docs/swagger/{serviceId}
    /// </summary>
    [HttpGet("swagger/{serviceId}")]
    public async Task<ActionResult> GetSwagger(string serviceId)
    {
        try
        {
            var service = _servicesRegistry.GetServiceById(serviceId);

            if (service == null)
            {
                return NotFound(new
                {
                    error = "Service not found",
                    serviceId,
                    availableServices = _servicesRegistry.GetServiceRegistry().Select(s => s.Id).ToList()
                });
            }

            var swaggerUrl = $"{service.BaseUrl}{service.SwaggerPath}";
            _logger.LogInformation("Proxying Swagger JSON from: {SwaggerUrl}", swaggerUrl);

            var response = await _httpClient.GetAsync(swaggerUrl);

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    return StatusCode(503, new
                    {
                        error = "Service unavailable",
                        message = $"Cannot connect to {serviceId} service"
                    });
                }

                return StatusCode((int)response.StatusCode, new
                {
                    error = "Failed to fetch Swagger documentation",
                    message = response.ReasonPhrase
                });
            }

            var swaggerJson = await response.Content.ReadFromJsonAsync<object>();
            return Ok(swaggerJson);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching Swagger JSON for {ServiceId}", serviceId);
            return StatusCode(503, new
            {
                error = "Service unavailable",
                message = $"Cannot connect to {serviceId} service",
                details = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Swagger JSON for {ServiceId}", serviceId);
            return BadRequest(new { error = ex.Message });
        }
    }
}
