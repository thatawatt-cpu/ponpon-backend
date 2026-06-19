namespace PonPon.Modules.Catalog.Application.Features.Uploads;

public sealed record UploadAdminFileCommand(Stream FileStream, string FileName, string ContentType);
