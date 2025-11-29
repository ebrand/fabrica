using Stripe;

namespace AdminBFF.Services;

public class StripeService
{
    private readonly ILogger<StripeService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private string _publishableKey = "";
    private bool _isInitialized = false;

    public StripeService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<StripeService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = config;
        _logger = logger;
    }

    /// <summary>
    /// Initialize Stripe with keys from Vault
    /// </summary>
    private async Task EnsureInitializedAsync()
    {
        if (_isInitialized) return;

        try
        {
            var adminServiceUrl = _configuration["ADMIN_SERVICE_URL"] ?? "http://acl-admin:3600";
            var httpClient = _httpClientFactory.CreateClient();

            var response = await httpClient.GetAsync($"{adminServiceUrl}/api/vault/stripe");

            if (response.IsSuccessStatusCode)
            {
                var config = await response.Content.ReadFromJsonAsync<StripeConfig>();
                if (config != null)
                {
                    StripeConfiguration.ApiKey = config.SecretKey;
                    _publishableKey = config.PublishableKey;
                    _logger.LogInformation("Stripe configuration loaded from Vault");
                }
            }
            else
            {
                _logger.LogWarning("Failed to fetch Stripe config from Vault, using fallback");
                LoadFallbackConfig();
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error fetching Stripe config from Vault, using fallback");
            LoadFallbackConfig();
        }

        _isInitialized = true;
    }

    private void LoadFallbackConfig()
    {
        var secretKey = _configuration["Stripe:SecretKey"] ??
                        Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");
        var publishableKey = _configuration["Stripe:PublishableKey"] ??
                             Environment.GetEnvironmentVariable("STRIPE_PUBLISHABLE_KEY");

        if (!string.IsNullOrEmpty(secretKey) && !secretKey.StartsWith("${"))
        {
            StripeConfiguration.ApiKey = secretKey;
        }

        if (!string.IsNullOrEmpty(publishableKey) && !publishableKey.StartsWith("${"))
        {
            _publishableKey = publishableKey;
        }
    }

    public async Task<string> GetPublishableKeyAsync()
    {
        await EnsureInitializedAsync();
        return _publishableKey;
    }

    /// <summary>
    /// Create a Stripe customer for the tenant
    /// </summary>
    public async Task<Customer> CreateCustomerAsync(string email, string name)
    {
        await EnsureInitializedAsync();

        var options = new CustomerCreateOptions
        {
            Email = email,
            Name = name,
            Description = $"Fabrica tenant: {name}"
        };

        var service = new CustomerService();
        var customer = await service.CreateAsync(options);

        _logger.LogInformation("Created Stripe customer {CustomerId} for {Email}", customer.Id, email);

        return customer;
    }

    /// <summary>
    /// Create a SetupIntent for collecting payment method
    /// </summary>
    public async Task<SetupIntent> CreateSetupIntentAsync(string customerId)
    {
        await EnsureInitializedAsync();

        var options = new SetupIntentCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string> { "card" },
            Usage = "off_session" // For future charges without customer present
        };

        var service = new SetupIntentService();
        var setupIntent = await service.CreateAsync(options);

        _logger.LogInformation("Created SetupIntent {SetupIntentId} for customer {CustomerId}",
            setupIntent.Id, customerId);

        return setupIntent;
    }

    /// <summary>
    /// Attach a payment method to a customer and set as default
    /// </summary>
    public async Task<PaymentMethod> AttachPaymentMethodAsync(string customerId, string paymentMethodId)
    {
        await EnsureInitializedAsync();

        // Attach payment method to customer
        var pmService = new PaymentMethodService();
        var paymentMethod = await pmService.AttachAsync(
            paymentMethodId,
            new PaymentMethodAttachOptions { Customer = customerId }
        );

        // Set as default payment method
        var customerService = new CustomerService();
        await customerService.UpdateAsync(customerId, new CustomerUpdateOptions
        {
            InvoiceSettings = new CustomerInvoiceSettingsOptions
            {
                DefaultPaymentMethod = paymentMethodId
            }
        });

        _logger.LogInformation("Attached payment method {PaymentMethodId} to customer {CustomerId}",
            paymentMethodId, customerId);

        return paymentMethod;
    }

    /// <summary>
    /// Get customer details
    /// </summary>
    public async Task<Customer?> GetCustomerAsync(string customerId)
    {
        await EnsureInitializedAsync();

        try
        {
            var service = new CustomerService();
            return await service.GetAsync(customerId);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Failed to get Stripe customer {CustomerId}", customerId);
            return null;
        }
    }

    private class StripeConfig
    {
        public string SecretKey { get; set; } = "";
        public string PublishableKey { get; set; } = "";
    }
}
