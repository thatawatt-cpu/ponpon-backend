namespace PonPon.Modules.Catalog.Application.Features.Products.UploadProductImage;

public sealed record UploadProductImageCommand(Guid ProductId, Stream FileStream, string FileName, string ContentType);
