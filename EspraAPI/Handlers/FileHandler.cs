using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EspraAPI.Service;

namespace EspraAPI.Handlers
{
    public static class FileHandler
    {
        static FileHandler()
        {
            // Init
        }

        [Authorize(Roles = "Admin")]
        public static async Task<object> PostDocument(string group, HttpRequest request, FileService fileService, CancellationToken token)
        {
            if (!request.HasFormContentType)
                return Results.NoContent();

            var form = await request.ReadFormAsync(cancellationToken: token);

            var document = form.Files.First(i => i != null && i.Length > 0);

            if (document is null)
                return Results.BadRequest("Empty request");

            var documentExtension = Path.GetExtension(document.FileName);

            if (!Util.DOCUMENT_EXTENSIONS.Contains(documentExtension))
                return Results.BadRequest("Unsupported file");

            using var stream = new MemoryStream();

            document.CopyTo(stream);

            return await fileService.AddAsync(group, documentExtension, stream.ToArray(), token) ? Results.Ok() : Results.BadRequest();
        }

        [Authorize(Roles = "Admin")]
        public static async Task<object> GetDocumentById(string id, FileService fileService, CancellationToken token)
        {
            var documentData = await fileService.GetDocumentByIdAsync(id, token);

            return Results.File(documentData.Item1, documentData.Item2);
        }

        [Authorize(Roles = "Admin")]
        public static async Task<object> PostImage(string group, HttpRequest request, FileService fileService, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            if (!request.HasFormContentType)
                return Results.NoContent();

            var form = await request.ReadFormAsync(cancellationToken: token);

            var image = form.Files.First(i => i != null && i.Length > 0);

            if (image is null)
                return Results.BadRequest("Empty request");

            var imageExtension = Path.GetExtension(image.FileName);

            if (!Util.IMAGE_EXTENSIONS.Contains(imageExtension))
                return Results.BadRequest("Unsupported file");

            using var stream = new MemoryStream();

            image.CopyTo(stream);

            return await fileService.AddAsync(group, imageExtension, stream.ToArray(), token) ? Results.Ok() : Results.BadRequest();
        }

        [Authorize(Roles = "Admin")]
        public static async Task<object> GetImageById(string id, FileService fileService, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            var documentData = await fileService.GetImageByIdAsync(id, token);

            return Results.File(documentData.Item1, documentData.Item2);
        }
    }
}
