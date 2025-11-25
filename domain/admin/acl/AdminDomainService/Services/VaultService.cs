using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

namespace AdminDomainService.Services;

public class VaultService
{
    private readonly IVaultClient _vaultClient;
    private readonly ILogger<VaultService> _logger;

    public VaultService(IConfiguration configuration, ILogger<VaultService> logger)
    {
        _logger = logger;

        var vaultUrl = configuration["Vault:Address"] ?? "http://vault:8200";
        var vaultToken = configuration["Vault:Token"] ?? "fabrica_dev_root_token";

        var authMethod = new TokenAuthMethodInfo(vaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultUrl, authMethod);

        _vaultClient = new VaultClient(vaultClientSettings);

        _logger.LogInformation("VaultService initialized. Vault Address: {VaultUrl}", vaultUrl);
    }

    public async Task<Secret<SecretData>> GetSecretAsync(string path)
    {
        try
        {
            _logger.LogInformation("Fetching secret from Vault: {Path}", path);
            var secret = await _vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(path, mountPoint: "fabrica");
            _logger.LogInformation("Successfully fetched secret from: {Path}", path);
            return secret;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch secret from Vault: {Path}", path);
            throw;
        }
    }

    public async Task<T?> GetSecretValueAsync<T>(string path, string key)
    {
        var secret = await GetSecretAsync(path);
        if (secret.Data.Data.TryGetValue(key, out var value))
        {
            return (T?)Convert.ChangeType(value, typeof(T));
        }
        return default;
    }

    public async Task<IDictionary<string, object>> GetSecretDataAsync(string path)
    {
        var secret = await GetSecretAsync(path);
        return secret.Data.Data;
    }
}
