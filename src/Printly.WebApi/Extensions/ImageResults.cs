using Microsoft.AspNetCore.Mvc;
using Printly.Application.Services;

namespace Printly.WebApi.Extensions;

/// <summary>Resposta de imagem: Content-Type pelos magic bytes, nosniff e Cache-Control informado. Nulo = 404.</summary>
public static class ImageResults
{
    public static IActionResult From(ControllerBase controller, ImageContent? image, string cacheControl)
    {
        if (image is null)
            return controller.NotFound(new { message = "Imagem nao encontrada." });

        var headers = controller.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.CacheControl = cacheControl;

        return controller.File(image.Bytes, image.ContentType);
    }
}
