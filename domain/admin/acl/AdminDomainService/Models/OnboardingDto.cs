namespace AdminDomainService.Models;

/// <summary>
/// Request to create a tenant during onboarding
/// </summary>
public class CreateOnboardingTenantDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid PlanId { get; set; }
}

/// <summary>
/// Request to create invitations during onboarding
/// </summary>
public class CreateOnboardingInvitationsDto
{
    public Guid TenantId { get; set; }
    public List<string> Emails { get; set; } = new();
}

/// <summary>
/// Request to save payment method during onboarding
/// </summary>
public class SavePaymentMethodDto
{
    public Guid TenantId { get; set; }
    public string StripeCustomerId { get; set; } = string.Empty;
    public string StripePaymentMethodId { get; set; } = string.Empty;
    public string BillingEmail { get; set; } = string.Empty;
}

/// <summary>
/// Request to complete onboarding
/// </summary>
public class CompleteOnboardingDto
{
    public Guid TenantId { get; set; }
}

/// <summary>
/// Response for subscription plan
/// </summary>
public class SubscriptionPlanResponseDto
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
/// Response for onboarding status
/// </summary>
public class OnboardingStatusResponseDto
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
public class OnboardingTenantResponseDto
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
public class OnboardingInvitationsResponseDto
{
    public int InvitationsCreated { get; set; }
    public List<string> Emails { get; set; } = new();
    public List<string> FailedEmails { get; set; } = new();
}
