using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AvailabilityService.Api.Exceptions;
using AvailabilityService.Api.Services.Interfaces;

namespace AvailabilityService.Api.Controllers;

[ApiController]
[Authorize]
[Produces("application/json")]
public abstract class BaseApiController(IUserContextService userContextService) : ControllerBase
{
    protected Guid GetUserId() => userContextService.GetUserId();

    protected Guid? GetTenantId() => userContextService.GetTenantId();

    protected bool IsCustomer() => userContextService.IsCustomer();

    protected string GetUserRole() => userContextService.GetRole();

    protected void ValidateCustomerAccess() => userContextService.ValidateCustomerAccess();

    protected void ValidateTenantAccess(Guid tenantId) => userContextService.ValidateTenantAccess(tenantId, "resource");

    protected void ValidateProviderAccess() => userContextService.ValidateProviderAccess();
}