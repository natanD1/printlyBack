namespace Printly.Application.Services;

/// <summary>
/// Imagem decodificada de base64 com o tipo detectado pelos magic bytes (nunca pela extensao ou pelo prefixo).
/// Aceita PNG, JPEG, GIF e WEBP; qualquer outro (SVG incluso) e recusado.
/// </summary>
public sealed record ImageContent(byte[] Bytes, string ContentType)
{
    private static readonly byte[] Png = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] Jpeg = { 0xFF, 0xD8, 0xFF };
    private static readonly byte[] Gif87 = "GIF87a"u8.ToArray();
    private static readonly byte[] Gif89 = "GIF89a"u8.ToArray();
    private static readonly byte[] Riff = "RIFF"u8.ToArray();
    private static readonly byte[] Webp = "WEBP"u8.ToArray();

    /// <summary>Remove prefixo <c>data:...;base64,</c> se houver. Base64 invalido ou tipo fora da lista = nulo.</summary>
    public static ImageContent? TryDecode(string? base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
            return null;

        var payload = base64.Trim();

        if (payload.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = payload.IndexOf(',');

            if (comma < 0 || !payload[..comma].EndsWith(";base64", StringComparison.OrdinalIgnoreCase))
                return null;

            payload = payload[(comma + 1)..];
        }

        var buffer = new byte[(payload.Length * 3 / 4) + 3];

        if (!Convert.TryFromBase64String(payload, buffer, out var written) || written == 0)
            return null;

        var bytes = buffer.AsSpan(0, written);
        var contentType = DetectContentType(bytes);

        return contentType is null ? null : new ImageContent(bytes.ToArray(), contentType);
    }

    public static string? DetectContentType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.StartsWith(Png))
            return "image/png";

        if (bytes.StartsWith(Jpeg))
            return "image/jpeg";

        if (bytes.StartsWith(Gif87) || bytes.StartsWith(Gif89))
            return "image/gif";

        if (bytes.Length >= 12 && bytes.StartsWith(Riff) && bytes.Slice(8, 4).SequenceEqual(Webp))
            return "image/webp";

        return null;
    }
}
