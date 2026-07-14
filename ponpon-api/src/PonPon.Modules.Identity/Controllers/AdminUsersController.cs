using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Identity.Application.AdminUsers;
using PonPon.Modules.Identity.Domain.Users;

namespace PonPon.Modules.Identity.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize]
public sealed class AdminUsersController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminUserListResponse>> GetUsers(
        [FromQuery] string? search,
        [FromQuery] UserStatus? status,
        [FromQuery] string? role,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromServices] AdminUserManagementService service,
        CancellationToken cancellationToken)
    {
        return Ok(await service.GetUsersAsync(search, status, role, page, pageSize, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateAdminUserRequest request,
        [FromServices] AdminUserManagementService service,
        CancellationToken cancellationToken)
    {
        var id = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetUsers), new { id }, id);
    }

    [HttpPatch("{userId:guid}")]
    public async Task<IActionResult> Update(
        Guid userId,
        [FromBody] UpdateAdminUserRequest request,
        [FromServices] AdminUserManagementService service,
        CancellationToken cancellationToken)
    {
        await service.UpdateAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{userId:guid}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        Guid userId,
        [FromBody] ResetAdminUserPasswordRequest request,
        [FromServices] AdminUserManagementService service,
        CancellationToken cancellationToken)
    {
        await service.ResetPasswordAsync(userId, request, cancellationToken);
        return NoContent();
    }
}
