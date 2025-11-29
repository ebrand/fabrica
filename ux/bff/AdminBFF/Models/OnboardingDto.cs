namespace AdminBFF.Models;

/// <summary>
/// Subscription plan for display
/// </summary>
public class SubscriptionPlanDto
{
    public Guid PlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int PriceCents { get; set; }
    public string BillingInterval { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int MaxProducts { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Request to create tenant during onboarding
/// </summary>
public class CreateOnboardingTenantRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid PlanId { get; set; }
}

/// <summary>
/// Request to create invitations during onboarding
/// </summary>
public class CreateOnboardingInvitationsRequest
{
    public Guid TenantId { get; set; }
    public List<string> Emails { get; set; } = new();
}

/// <summary>
/// Request to save payment method during onboarding
/// </summary>
public class SavePaymentRequest
{
    public Guid TenantId { get; set; }
    public string PaymentMethodId { get; set; } = string.Empty;
    public string BillingEmail { get; set; } = string.Empty;
}

/// <summary>
/// Request to complete onboarding
/// </summary>
public class CompleteOnboardingRequest
{
    public Guid TenantId { get; set; }
}

/// <summary>
/// Response for onboarding status
/// </summary>
public class OnboardingStatusDto
{
    public bool RequiresOnboarding { get; set; }
    public int CurrentStep { get; set; }
    public Guid? TenantId { get; set; }
    public string? TenantName { get; set; }
    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }
}

/// <summary>
/// Response for created tenant during onboarding
/// </summary>
public class OnboardingTenantDto
{
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OnboardingStep { get; set; }
}

/// <summary>
/// Response for created invitations during onboarding
/// </summary>
public class OnboardingInvitationsDto
{
    public int InvitationsCreated { get; set; }
    public List<string> Emails { get; set; } = new();
    public List<string> FailedEmails { get; set; } = new();
}

/// <summary>
/// Response for Stripe SetupIntent
/// </summary>
public class SetupIntentResponse
{
    public string ClientSecret { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string PublishableKey { get; set; } = string.Empty;
}

/// <summary>
/// Request to create SetupIntent
/// </summary>
public class CreateSetupIntentRequest
{
    public Guid TenantId { get; set; }
    public string BillingEmail { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
}
