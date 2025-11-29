using AdminBFF.Models;
using AdminBFF.Services;
using Microsoft.AspNetCore.Mvc;

namespace AdminBFF.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OnboardingController : ControllerBase
{
    private readonly AdminServiceClient _adminClient;
    private readonly ContentServiceClient _contentClient;
    private readonly StripeService _stripeService;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(
        AdminServiceClient adminClient,
        ContentServiceClient contentClient,
        StripeService stripeService,
        ILogger<OnboardingController> logger)
    {
        _adminClient = adminClient;
        _contentClient = contentClient;
        _stripeService = stripeService;
        _logger = logger;
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    [HttpGet("plans")]
    public async Task<ActionResult<List<SubscriptionPlanDto>>> GetPlans()
    {
        try
        {
            var plans = await _adminClient.GetSubscriptionPlansAsync();
            return Ok(plans);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching subscription plans");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get onboarding status for current user
    /// </summary>
    [HttpGet("status")]
    public async Task<ActionResult<OnboardingStatusDto>> GetStatus()
    {
        try
        {
            var status = await _adminClient.GetOnboardingStatusAsync();
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching onboarding status");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Step 1: Create tenant during onboarding
    /// Also provisions default languages (en-US, es-ES, fr-FR) for the new tenant
    /// </summary>
    [HttpPost("tenant")]
    public async Task<ActionResult<OnboardingTenantDto>> CreateTenant([FromBody] CreateOnboardingTenantRequest request)
    {
        try
        {
            var tenant = await _adminClient.CreateOnboardingTenantAsync(request);

            // Provision default languages for the new tenant
            // This is a cross-domain operation (admin -> content)
            try
            {
                var languagesCreated = await _contentClient.ProvisionDefaultLanguagesAsync(tenant.TenantId.ToString());
                _logger.LogInformation("Provisioned {Count} default languages for new tenant {TenantId}",
                    languagesCreated, tenant.TenantId);
            }
            catch (Exception langEx)
            {
                // Log but don't fail tenant creation if language provisioning fails
                // Languages can be added manually later
                _logger.LogWarning(langEx, "Failed to provision default languages for tenant {TenantId}. Languages can be added manually.",
                    tenant.TenantId);
            }

            return Ok(tenant);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating onboarding tenant");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Step 2: Create invitations during onboarding
    /// </summary>
    [HttpPost("invitations")]
    public async Task<ActionResult<OnboardingInvitationsDto>> CreateInvitations([FromBody] CreateOnboardingInvitationsRequest request)
    {
        try
        {
            var result = await _adminClient.CreateOnboardingInvitationsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating onboarding invitations");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Step 3a: Create Stripe SetupIntent for collecting payment method
    /// </summary>
    [HttpPost("setup-intent")]
    public async Task<ActionResult<SetupIntentResponse>> CreateSetupIntent([FromBody] CreateSetupIntentRequest request)
    {
        try
        {
            // Create Stripe customer
            var customer = await _stripeService.CreateCustomerAsync(request.BillingEmail, request.TenantName);

            // Create SetupIntent for collecting payment method
            var setupIntent = await _stripeService.CreateSetupIntentAsync(customer.Id);

            return Ok(new SetupIntentResponse
            {
                ClientSecret = setupIntent.ClientSecret,
                CustomerId = customer.Id,
                PublishableKey = await _stripeService.GetPublishableKeyAsync()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Stripe SetupIntent");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Step 3b: Save payment method after successful card collection
    /// </summary>
    [HttpPost("payment")]
    public async Task<ActionResult> SavePayment([FromBody] SavePaymentRequest request)
    {
        try
        {
            // Get customer ID from the request or create a new one
            // In the flow, the frontend will have the customer ID from the setup-intent call
            // We'll need to pass it through. For now, let's assume it comes with the payment method

            // Save to ACL - the actual Stripe attachment was done client-side via SetupIntent
            // We just need to record the IDs in our database
            await _adminClient.SaveOnboardingPaymentAsync(
                request.TenantId,
                request.PaymentMethodId, // We'll use this to lookup/attach if needed
                request.PaymentMethodId,
                request.BillingEmail
            );

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payment method");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Step 3b: Save payment method with customer ID (after SetupIntent confirmation)
    /// </summary>
    [HttpPost("payment-confirm")]
    public async Task<ActionResult> ConfirmPayment([FromBody] ConfirmPaymentRequest request)
    {
        try
        {
            // Attach payment method to customer and set as default
            await _stripeService.AttachPaymentMethodAsync(request.CustomerId, request.PaymentMethodId);

            // Save to ACL database
            await _adminClient.SaveOnboardingPaymentAsync(
                request.TenantId,
                request.CustomerId,
                request.PaymentMethodId,
                request.BillingEmail
            );

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming payment method");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Step 4: Complete onboarding
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult> CompleteOnboarding([FromBody] CompleteOnboardingRequest request)
    {
        try
        {
            await _adminClient.CompleteOnboardingAsync(request.TenantId);
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding");
            return BadRequest(new { error = ex.Message });
        }
    }
}

/// <summary>
/// Request for confirming payment with customer ID
/// </summary>
public class ConfirmPaymentRequest
{
    public Guid TenantId { get; set; }
    public string CustomerId { get; set; } = string.Empty;
    public string PaymentMethodId { get; set; } = string.Empty;
    public string BillingEmail { get; set; } = string.Empty;
}
