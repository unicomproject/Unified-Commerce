using System.Security.Claims;
using E_POS.Api.Models;
using E_POS.Application.Common.Models;
using E_POS.Application.Modules.Platform.Subscription.Contracts;
using E_POS.Application.Modules.Platform.Subscription.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace E_POS.Api.Controllers;

[ApiController]
[Authorize(Policy = "PlatformOnly")]
[Route("api/v1/platform/subscription-plans")]
public sealed class PlatformSubscriptionPlansController : ControllerBase
{
    private readonly IPlatformSubscriptionPlanService _subscriptionPlanService;

    public PlatformSubscriptionPlansController(IPlatformSubscriptionPlanService subscriptionPlanService)
    {
        _subscriptionPlanService = subscriptionPlanService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanListResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPlans(
        [FromQuery] SubscriptionPlanListQuery query,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.GetPlansAsync(query, platformUserId, cancellationToken);
        return ToActionResult(result, "Subscription plans loaded successfully.");
    }

    [HttpGet("catalog")]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanCatalogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCatalog(CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.GetCatalogAsync(platformUserId, cancellationToken);
        return ToActionResult(result, "Subscription plan catalog loaded successfully.");
    }

    [HttpGet("{planId:guid}")]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanDetailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlanById(Guid planId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.GetPlanDetailAsync(planId, platformUserId, cancellationToken);
        return ToActionResult(result, "Subscription plan loaded successfully.");
    }

    [HttpPost]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanMutationResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePlan(
        [FromBody] CreateSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.CreateDraftAsync(request, platformUserId, cancellationToken);

        if (result.IsSuccess && result.Value is not null)
        {
            return StatusCode(
                StatusCodes.Status201Created,
                LegacyApiResponse<SubscriptionPlanMutationResponse>.Ok(
                    "Subscription plan created successfully.",
                    result.Value));
        }

        return MapError(result.Error);
    }

    [HttpPatch("{planId:guid}/pricing")]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanMutationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdatePricing(
        Guid planId,
        [FromBody] UpdateSubscriptionPlanPricingRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.UpdatePricingAsync(
            planId,
            request,
            platformUserId,
            cancellationToken);

        return ToMutationActionResult(result, "Subscription plan pricing updated successfully.");
    }

    [HttpPatch("{planId:guid}/limits")]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanMutationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateLimits(
        Guid planId,
        [FromBody] UpdateSubscriptionPlanLimitsRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.UpdateLimitsAsync(
            planId,
            request,
            platformUserId,
            cancellationToken);

        return ToMutationActionResult(result, "Subscription plan limits updated successfully.");
    }

    [HttpPatch("{planId:guid}/features")]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanMutationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateFeatures(
        Guid planId,
        [FromBody] UpdateSubscriptionPlanFeaturesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.UpdateFeaturesAsync(
            planId,
            request,
            platformUserId,
            cancellationToken);

        return ToMutationActionResult(result, "Subscription plan features updated successfully.");
    }

    [HttpPut("{planId:guid}")]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanMutationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateDraft(
        Guid planId,
        [FromBody] UpdateSubscriptionPlanRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.UpdateDraftAsync(
            planId,
            request,
            platformUserId,
            cancellationToken);

        return ToMutationActionResult(result, "Subscription plan updated successfully.");
    }

    [HttpPost("{planId:guid}/publish")]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanMutationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishPlan(Guid planId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.PublishAsync(planId, platformUserId, cancellationToken);
        return ToMutationActionResult(result, "Subscription plan published successfully.");
    }

    [HttpPost("{planId:guid}/duplicate")]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanMutationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DuplicatePlan(Guid planId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.DuplicateAsync(planId, platformUserId, cancellationToken);
        return ToMutationActionResult(result, "Subscription plan duplicated successfully.");
    }

    [HttpPost("{planId:guid}/archive")]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanMutationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ArchivePlan(Guid planId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.ArchiveAsync(planId, platformUserId, cancellationToken);
        return ToMutationActionResult(result, "Subscription plan archived successfully.");
    }

    [HttpPost("{planId:guid}/reactivate")]
    [ProducesResponseType(typeof(LegacyApiResponse<SubscriptionPlanMutationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReactivatePlan(Guid planId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.ReactivateAsync(planId, platformUserId, cancellationToken);
        return ToMutationActionResult(result, "Subscription plan reactivated successfully.");
    }

    [HttpDelete("{planId:guid}")]
    [ProducesResponseType(typeof(LegacyApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteDraftPlan(Guid planId, CancellationToken cancellationToken)
    {
        if (!TryGetPlatformUserId(out var platformUserId))
        {
            return Unauthorized(CreateLegacyError(
                "platform_auth.invalid_session",
                "Invalid platform session."));
        }

        var result = await _subscriptionPlanService.DeleteDraftAsync(planId, platformUserId, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(LegacyApiResponse<object>.Ok(
                "Subscription plan deleted successfully.",
                new { deleted = true }));
        }

        return MapError(result.Error);
    }

    private IActionResult ToActionResult<T>(ApplicationResult<T> result, string successMessage)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(LegacyApiResponse<T>.Ok(successMessage, result.Value));
        }

        return MapError(result.Error);
    }

    private IActionResult ToMutationActionResult(
        ApplicationResult<SubscriptionPlanMutationResponse> result,
        string successMessage)
    {
        if (result.IsSuccess && result.Value is not null)
        {
            return Ok(LegacyApiResponse<SubscriptionPlanMutationResponse>.Ok(successMessage, result.Value));
        }

        return MapError(result.Error);
    }

    private IActionResult MapError(ApplicationError error)
    {
        return error.Code switch
        {
            "platform_subscription_plans.not_found" => NotFound(CreateLegacyError(error.Code, error.Message)),
            "platform_subscription_plans.validation_failed" => BadRequest(CreateLegacyError(error.Code, error.Message)),
            "platform_subscription_plans.conflict" or "platform_subscription_plans.invalid_transition" or "platform_subscription_plans.in_use" =>
                StatusCode(StatusCodes.Status409Conflict, CreateLegacyError(error.Code, error.Message)),
            _ => StatusCode(StatusCodes.Status403Forbidden, CreateLegacyError(error.Code, error.Message))
        };
    }

    private bool TryGetPlatformUserId(out Guid platformUserId)
    {
        var platformUserIdValue = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(platformUserIdValue, out platformUserId);
    }

    private object CreateLegacyError(string errorCode, string message)
    {
        return new
        {
            success = false,
            message,
            errorCode,
            errors = Array.Empty<object>(),
            traceId = HttpContext.TraceIdentifier
        };
    }
}


