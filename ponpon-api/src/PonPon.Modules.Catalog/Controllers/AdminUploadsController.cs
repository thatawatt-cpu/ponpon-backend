using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PonPon.Modules.Catalog.Application.Features.Uploads;
using PonPon.Shared.Application.Exceptions;

namespace PonPon.Modules.Catalog.Controllers;

[ApiController]
[Route("api/admin/uploads")]
[Authorize(Roles = "Admin")]
public sealed class AdminUploadsController : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<UploadAdminFileResponse>> Upload(IFormFile file, [FromServices] UploadAdminFileHandler handler, CancellationToken cancellationToken)
    {
        if (file.Length == 0)
            throw new BadRequestException("File is required.");

        await using var stream = file.OpenReadStream();
        return Ok(await handler.HandleAsync(new UploadAdminFileCommand(stream, file.FileName, file.ContentType), cancellationToken));
    }
}
