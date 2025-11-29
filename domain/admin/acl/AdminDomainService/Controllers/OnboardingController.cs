using System.Text.RegularExpressions;
using AdminDomainService.Data;
using AdminDomainService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AdminDomainService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OnboardingController : ControllerBase
{
    private readonly AdminDbContext _context;
    private readonly ILogger<OnboardingController> _logger;

    public OnboardingController(AdminDbContext context, ILogger<OnboardingController> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Gets the caller's user ID from the X-User-ID header
    /// </summary>
    private Guid? GetCallerUserId()
    {
        if (Request.Headers.TryGetValue("X-User-ID", out var value))
        {
            var userIdString = value.FirstOrDefault();
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                return userId;
            }
        }
        return null;
    }

    /// <summary>
    /// Get available subscription plans
    /// </summary>
    [HttpGet("plans")]
    public async Task<ActionResult<List<SubscriptionPlanResponseDto>>> GetPlans()
    {
        try
        {
            var plans = await _context.SubscriptionPlans
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .Select(p => new SubscriptionPlanResponseDto
                {
                    PlanId = p.PlanId,
                    Name = p.Name,
                    Description = p.Description,
                    PriceCents = p.PriceCents,
                    BillingInterval = p.BillingInterval,
                    MaxUsers = p.MaxUsers,
                    MaxProducts = p.MaxProducts,
                    DisplayOrder = p.DisplayOrder
                })
                .ToListAsync();

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
    public async Task<ActionResult<OnboardingStatusResponseDto>> GetStatus()
    {
        try
        {
            var userId = GetCallerUserId();
            if (!userId.HasValue)
            {
                return BadRequest(new { error = "User ID is required" });
            }

            // Check if user has any active tenants
            var userTenants = await _context.UserTenants
                .Where(ut => ut.UserId == userId.Value && ut.IsActive)
                .Include(ut => ut.Tenant)
                .ToListAsync();

            // Check if user has pending invitations
            var user = await _context.Users.FindAsync(userId.Value);
            var hasPendingInvitations = user != null && await _context.Invitations
                .AnyAsync(i => i.Email.ToLower() == user.Email.ToLower()
                    && i.Status == "pending"
                    && i.ExpiresAt > DateTime.UtcNow);

            // If user has tenants or pending invitations, they don't need onboarding
            if (userTenants.Any() || hasPendingInvitations)
            {
                return Ok(new OnboardingStatusResponseDto
                {
                    RequiresOnboarding = false,
                    CurrentStep = 0,
                    TenantId = userTenants.FirstOrDefault()?.TenantId,
                    TenantName = userTenants.FirstOrDefault()?.Tenant?.Name
                });
            }

            // Check if there's an in-progress onboarding tenant for this user
            var onboardingTenant = await _context.Tenants
                .Where(t => t.OwnerUserId == userId.Value && !t.OnboardingCompleted)
                .Include(t => t.UserTenants)
                .FirstOrDefaultAsync();

            if (onboardingTenant != null)
            {
                var subscription = await _context.TenantSubscriptions
                    .Where(s => s.TenantId == onboardingTenant.TenantId)
                    .Include(s => s.Plan)
                    .FirstOrDefaultAsync();

                return Ok(new OnboardingStatusResponseDto
                {
                    RequiresOnboarding = true,
                    CurrentStep = onboardingTenant.OnboardingStep,
                    TenantId = onboardingTenant.TenantId,
                    TenantName = onboardingTenant.Name,
                    PlanId = subscription?.PlanId,
                    PlanName = subscription?.Plan?.Name
                });
            }

            // User needs to start fresh onboarding
            return Ok(new OnboardingStatusResponseDto
            {
                RequiresOnboarding = true,
                CurrentStep = 0
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting onboarding status");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Step 1: Create tenant during onboarding
    /// </summary>
    [HttpPost("tenant")]
    public async Task<ActionResult<OnboardingTenantResponseDto>> CreateTenant([FromBody] CreateOnboardingTenantDto dto)
    {
        try
        {
            var userId = GetCallerUserId();
            if (!userId.HasValue)
            {
                return BadRequest(new { error = "User ID is required" });
            }

            // Validate plan exists
            var plan = await _context.SubscriptionPlans.FindAsync(dto.PlanId);
            if (plan == null || !plan.IsActive)
            {
                return BadRequest(new { error = "Invalid subscription plan" });
            }

            // Check if user already has an in-progress onboarding tenant
            var existingTenant = await _context.Tenants
                .FirstOrDefaultAsync(t => t.OwnerUserId == userId.Value && !t.OnboardingCompleted);

            if (existingTenant != null)
            {
                // Update existing tenant
                existingTenant.Name = dto.Name;
                existingTenant.Description = dto.Description;
                existingTenant.Slug = await EnsureUniqueSlugAsync(GenerateSlug(dto.Name));
                existingTenant.OnboardingStep = 1;
                existingTenant.UpdatedAt = DateTime.UtcNow;

                // Update or create subscription
                var existingSubscription = await _context.TenantSubscriptions
                    .FirstOrDefaultAsync(s => s.TenantId == existingTenant.TenantId);

                if (existingSubscription != null)
                {
                    existingSubscription.PlanId = dto.PlanId;
                    existingSubscription.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    var subscription = new TenantSubscription
                    {
                        TenantId = existingTenant.TenantId,
                        PlanId = dto.PlanId,
                        Status = "pending",
                        CurrentPeriodStart = DateTime.UtcNow
                    };
                    _context.TenantSubscriptions.Add(subscription);
                }

                await _context.SaveChangesAsync();

                return Ok(new OnboardingTenantResponseDto
                {
                    TenantId = existingTenant.TenantId,
                    Name = existingTenant.Name,
                    Slug = existingTenant.Slug,
                    Description = existingTenant.Description,
                    OnboardingStep = existingTenant.OnboardingStep
                });
            }

            // Create new tenant
            var tenant = new Tenant
            {
                TenantId = Guid.NewGuid(),
                Name = dto.Name,
                Slug = await EnsureUniqueSlugAsync(GenerateSlug(dto.Name)),
                Description = dto.Description,
                IsActive = true,
                IsPersonal = false,
                OwnerUserId = userId.Value,
                OnboardingCompleted = false,
                OnboardingStep = 1,
                CreatedBy = userId.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Tenants.Add(tenant);

            // Link user to tenant as owner
            var userTenant = new UserTenant
            {
                UserTenantId = Guid.NewGuid(),
                UserId = userId.Value,
                TenantId = tenant.TenantId,
                Role = "owner",
                IsActive = true,
                GrantedBy = userId.Value,
                GrantedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.UserTenants.Add(userTenant);

            // Create subscription record
            var newSubscription = new TenantSubscription
            {
                TenantId = tenant.TenantId,
                PlanId = dto.PlanId,
                Status = "pending",
                CurrentPeriodStart = DateTime.UtcNow
            };

            _context.TenantSubscriptions.Add(newSubscription);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created onboarding tenant {TenantId} for user {UserId}", tenant.TenantId, userId.Value);

            return Ok(new OnboardingTenantResponseDto
            {
                TenantId = tenant.TenantId,
                Name = tenant.Name,
                Slug = tenant.Slug,
                Description = tenant.Description,
                OnboardingStep = tenant.OnboardingStep
            });
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
    public async Task<ActionResult<OnboardingInvitationsResponseDto>> CreateInvitations([FromBody] CreateOnboardingInvitationsDto dto)
    {
        try
        {
            var userId = GetCallerUserId();
            if (!userId.HasValue)
            {
                return BadRequest(new { error = "User ID is required" });
            }

            // Verify tenant belongs to user
            var tenant = await _context.Tenants.FindAsync(dto.TenantId);
            if (tenant == null || tenant.OwnerUserId != userId.Value)
            {
                return BadRequest(new { error = "Invalid tenant" });
            }

            var user = await _context.Users.FindAsync(userId.Value);
            var createdEmails = new List<string>();
            var failedEmails = new List<string>();

            foreach (var email in dto.Emails.Take(10)) // Limit to 10 invitations
            {
                var normalizedEmail = email.Trim().ToLower();

                // Skip self-invite
                if (user != null && normalizedEmail == user.Email.ToLower())
                {
                    failedEmails.Add(email);
                    continue;
                }

                // Skip if pending invitation exists
                var existingInvitation = await _context.Invitations
                    .FirstOrDefaultAsync(i => i.Email.ToLower() == normalizedEmail
                        && i.TenantId == dto.TenantId
                        && i.Status == "pending");

                if (existingInvitation != null)
                {
                    failedEmails.Add(email);
                    continue;
                }

                var invitation = new Invitation
                {
                    InvitationId = Guid.NewGuid(),
                    Email = normalizedEmail,
                    TenantId = dto.TenantId,
                    InvitedBy = userId.Value,
                    Status = "pending",
                    ExpiresAt = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Invitations.Add(invitation);
                createdEmails.Add(email);
            }

            // Update onboarding step
            tenant.OnboardingStep = 2;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} invitations for tenant {TenantId}",
                createdEmails.Count, dto.TenantId);

            return Ok(new OnboardingInvitationsResponseDto
            {
                InvitationsCreated = createdEmails.Count,
                Emails = createdEmails,
                FailedEmails = failedEmails
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating onboarding invitations");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Step 3: Save payment method during onboarding
    /// </summary>
    [HttpPost("payment")]
    public async Task<ActionResult> SavePayment([FromBody] SavePaymentMethodDto dto)
    {
        try
        {
            var userId = GetCallerUserId();
            if (!userId.HasValue)
            {
                return BadRequest(new { error = "User ID is required" });
            }

            // Verify tenant belongs to user
            var tenant = await _context.Tenants.FindAsync(dto.TenantId);
            if (tenant == null || tenant.OwnerUserId != userId.Value)
            {
                return BadRequest(new { error = "Invalid tenant" });
            }

            // Update subscription with payment info
            var subscription = await _context.TenantSubscriptions
                .FirstOrDefaultAsync(s => s.TenantId == dto.TenantId);

            if (subscription == null)
            {
                return BadRequest(new { error = "Subscription not found" });
            }

            subscription.StripeCustomerId = dto.StripeCustomerId;
            subscription.StripePaymentMethodId = dto.StripePaymentMethodId;
            subscription.BillingEmail = dto.BillingEmail;
            subscription.Status = "active";
            subscription.UpdatedAt = DateTime.UtcNow;

            // Update onboarding step
            tenant.OnboardingStep = 3;
            tenant.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Saved payment method for tenant {TenantId}", dto.TenantId);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving payment method");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Step 4: Complete onboarding
    /// </summary>
    [HttpPost("complete")]
    public async Task<ActionResult> CompleteOnboarding([FromBody] CompleteOnboardingDto dto)
    {
        try
        {
            var userId = GetCallerUserId();
            if (!userId.HasValue)
            {
                return BadRequest(new { error = "User ID is required" });
            }

            // Verify tenant belongs to user
            var tenant = await _context.Tenants.FindAsync(dto.TenantId);
            if (tenant == null || tenant.OwnerUserId != userId.Value)
            {
                return BadRequest(new { error = "Invalid tenant" });
            }

            // Verify subscription has payment method
            var subscription = await _context.TenantSubscriptions
                .FirstOrDefaultAsync(s => s.TenantId == dto.TenantId);

            if (subscription == null || string.IsNullOrEmpty(subscription.StripePaymentMethodId))
            {
                return BadRequest(new { error = "Payment method is required" });
            }

            // Mark onboarding as complete
            tenant.OnboardingCompleted = true;
            tenant.OnboardingStep = 4;
            tenant.UpdatedAt = DateTime.UtcNow;

            // Set subscription period
            subscription.CurrentPeriodStart = DateTime.UtcNow;
            subscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);
            subscription.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Completed onboarding for tenant {TenantId}", dto.TenantId);

            return Ok(new { success = true, tenantId = tenant.TenantId, tenantSlug = tenant.Slug });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing onboarding");
            return BadRequest(new { error = ex.Message });
        }
    }

    private static string GenerateSlug(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "workspace";

        var slug = Regex.Replace(input.ToLowerInvariant(), @"[^a-z0-9]+", "-");
        slug = slug.Trim('-');
        if (slug.Length > 50)
            slug = slug.Substring(0, 50).TrimEnd('-');

        return string.IsNullOrEmpty(slug) ? "workspace" : slug;
    }

    private async Task<string> EnsureUniqueSlugAsync(string baseSlug)
    {
        var slug = baseSlug;
        var counter = 0;

        while (await _context.Tenants.AnyAsync(t => t.Slug == slug))
        {
            counter++;
            slug = $"{baseSlug}-{counter}";
        }

        return slug;
    }
}
