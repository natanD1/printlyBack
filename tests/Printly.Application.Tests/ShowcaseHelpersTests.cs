using Printly.Application.Services;

namespace Printly.Application.Tests;

public class PublicContactSanitizerTests
{
    [Theory]
    [InlineData("(11) 99999-8888", "5511999998888")]
    [InlineData("1140028922", "551140028922")]
    [InlineData("+55 11 99999-8888", "5511999998888")]
    [InlineData("551140028922", "551140028922")]
    [InlineData("123456789", null)]
    [InlineData("441140028922", null)]
    [InlineData("12345678901234", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void WhatsApp(string? input, string? expected) =>
        Assert.Equal(expected, PublicContactSanitizer.WhatsApp(input));

    [Theory]
    [InlineData("@atelie3d", "atelie3d")]
    [InlineData("atelie.3d_", "atelie.3d_")]
    [InlineData("instagram.com/atelie3d", "atelie3d")]
    [InlineData("https://www.instagram.com/atelie3d/?hl=pt", "atelie3d")]
    [InlineData("https://evil.com/atelie3d", null)]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("@atelie 3d", null)]
    [InlineData("@", null)]
    [InlineData("<script>", null)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", null)]
    public void Instagram(string input, string? expected) =>
        Assert.Equal(expected, PublicContactSanitizer.Instagram(input));

    [Theory]
    [InlineData("https://atelie3d.com.br", "https://atelie3d.com.br/")]
    [InlineData("http://atelie3d.com.br/loja", "http://atelie3d.com.br/loja")]
    [InlineData("atelie3d.com.br", "https://atelie3d.com.br/")]
    [InlineData("javascript:alert(1)", null)]
    [InlineData("JavaScript:alert(1)", null)]
    [InlineData("data:text/html;base64,PHNjcmlwdD4=", null)]
    [InlineData("ftp://atelie3d.com.br", null)]
    [InlineData("/etc/passwd", null)]
    [InlineData("", null)]
    public void Website(string input, string? expected) =>
        Assert.Equal(expected, PublicContactSanitizer.Website(input));

    [Theory]
    [InlineData("contato@atelie.com", "contato@atelie.com")]
    [InlineData("Ana <ana@x.com>", null)]
    [InlineData("ana @x.com", null)]
    [InlineData("nao-e-email", null)]
    public void Email(string input, string? expected) =>
        Assert.Equal(expected, PublicContactSanitizer.Email(input));

    [Theory]
    [InlineData("(11) 4002-8922", "(11) 4002-8922")]
    [InlineData("+55 11 4002-8922", "+55 11 4002-8922")]
    [InlineData("4002-892", null)]
    [InlineData("ligue 40028922", null)]
    [InlineData("((((((((", null)]
    public void Phone(string input, string? expected) =>
        Assert.Equal(expected, PublicContactSanitizer.Phone(input));
}

public class ImageContentTests
{
    [Fact]
    public void Png_Jpeg_Gif_Webp_Detected()
    {
        Assert.Equal("image/png", ImageContent.TryDecode(TestImages.Png)!.ContentType);
        Assert.Equal("image/jpeg", ImageContent.TryDecode(TestImages.Jpeg)!.ContentType);
        Assert.Equal("image/gif", ImageContent.TryDecode(Convert.ToBase64String("GIF89a...."u8.ToArray()))!.ContentType);
        Assert.Equal("image/webp", ImageContent.TryDecode(Convert.ToBase64String("RIFF\0\0\0\0WEBPVP8 "u8.ToArray()))!.ContentType);
    }

    [Fact]
    public void DataUriPrefix_Accepted_BytesExact()
    {
        var image = ImageContent.TryDecode("data:image/png;base64," + TestImages.Png);

        Assert.Equal("image/png", image!.ContentType);
        Assert.Equal(TestImages.PngBytes, image.Bytes);
    }

    [Fact]
    public void PrefixClaimingPng_ButSvgBytes_Rejected() =>
        Assert.Null(ImageContent.TryDecode("data:image/png;base64," + TestImages.Svg));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nao base64!!")]
    [InlineData("data:image/png,naobase64")]
    public void Invalid_ReturnsNull(string? input) => Assert.Null(ImageContent.TryDecode(input));

    [Fact]
    public void Svg_And_UnknownMagicBytes_Rejected()
    {
        Assert.Null(ImageContent.TryDecode(TestImages.Svg));
        Assert.Null(ImageContent.TryDecode(TestImages.Garbage));
        Assert.Null(ImageContent.TryDecode(Convert.ToBase64String("RIFF\0\0\0\0WAVE"u8.ToArray())));
    }
}
