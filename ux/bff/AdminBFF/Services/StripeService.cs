using Stripe;

namespace AdminBFF.Services;

public class StripeService
{
    private readonly ILogger<StripeService> _logger;
    private readonly string _publishableKey;

    public StripeService(IConfiguration config, ILogger<StripeService> logger)
    {
        _logger = logger;
        var secretKey = config["Stripe:SecretKey"];
        _publishableKey = config["Stripe:PublishableKey"] ?? "";

        if (string.IsNullOrEmpty(secretKey))
        {
            _logger.LogWarning("Stripe SecretKey is not configured");
        }
        else
        {
            StripeConfiguration.ApiKey = secretKey;
        }
    }

    public string GetPublishableKey() => _publishableKey;

    /// <summary>
    /// Create a Stripe customer for the tenant
    /// </summary>
    public async Task<Customer> CreateCustomerAsync(string email, string name)
    {
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
}
