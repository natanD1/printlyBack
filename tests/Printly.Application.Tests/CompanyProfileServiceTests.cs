using Printly.Application.DTOs.CompanyProfile;
using Printly.Application.Services;
using Printly.Domain.Entities;
using Printly.Domain.Exceptions;
using Printly.Domain.Interfaces;

namespace Printly.Application.Tests;

public class CompanyProfileServiceTests
{
    private static readonly byte[] PngHeader = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    private static readonly byte[] JpgHeader = { 0xFF, 0xD8, 0xFF, 0xE0 };

    private static string Base64(byte[] header, int totalBytes = 0)
    {
        var bytes = new byte[Math.Max(header.Length, totalBytes)];
        header.CopyTo(bytes, 0);
        return Convert.ToBase64String(bytes);
    }

    private static string PngLogo => Base64(PngHeader);

    private static SaveCompanyProfileRequest Request(
        string name = "Minha Empresa",
        string? logo = null,
        bool removeLogo = false,
        string? phone = null,
        string? email = null,
        string? whatsApp = null,
        string? instagram = null,
        string? website = null) =>
        new(name, logo, removeLogo, phone, email, whatsApp, instagram, website);

    private static (CompanyProfileService Service, FakeCompanyProfileRepository Repository) CreateService()
    {
        var repository = new FakeCompanyProfileRepository();
        return (new CompanyProfileService(repository, new AllowAllEntitlementService(), new FakeShowcaseRepository(new FakeProductRepository()), new FakeUnitOfWork()), repository);
    }

    [Fact]
    public async Task Save_CreatesProfile_WhenNoneExists()
    {
        var (service, repository) = CreateService();
        var userId = Guid.NewGuid();

        var dto = await service.SaveAsync(userId, Request(logo: PngLogo, phone: "11999999999", email: "a@b.com"));

        Assert.Single(repository.Items);
        Assert.Equal("Minha Empresa", dto.Name);
        Assert.Equal(PngLogo, dto.LogoBase64);
        Assert.Equal("11999999999", dto.Phone);
        Assert.Equal("a@b.com", dto.Email);
        Assert.Null(dto.UpdatedAt);
        Assert.Equal(userId, repository.Items[0].UserId);
    }

    [Fact]
    public async Task Save_UpdatesExistingProfile_KeepingSameId()
    {
        var (service, repository) = CreateService();
        var userId = Guid.NewGuid();

        var created = await service.SaveAsync(userId, Request(name: "Antiga"));
        var updated = await service.SaveAsync(userId, Request(name: "Nova", website: "https://x.com"));

        Assert.Single(repository.Items);
        Assert.Equal(created.Id, updated.Id);
        Assert.Equal("Nova", updated.Name);
        Assert.Equal("https://x.com", updated.Website);
        Assert.NotNull(updated.UpdatedAt);
    }

    [Fact]
    public async Task Save_KeepsLogo_WhenNoLogoAndRemoveLogoFalse()
    {
        var (service, _) = CreateService();
        var userId = Guid.NewGuid();

        await service.SaveAsync(userId, Request(logo: PngLogo));
        var updated = await service.SaveAsync(userId, Request(name: "Outra", logo: null, removeLogo: false));

        Assert.Equal(PngLogo, updated.LogoBase64);
    }

    [Fact]
    public async Task Save_RemovesLogo_WhenRemoveLogoTrue()
    {
        var (service, _) = CreateService();
        var userId = Guid.NewGuid();

        await service.SaveAsync(userId, Request(logo: PngLogo));
        var updated = await service.SaveAsync(userId, Request(logo: null, removeLogo: true));

        Assert.Null(updated.LogoBase64);
    }

    [Fact]
    public async Task Save_ReplacesLogo_WhenLogoSentEvenIfRemoveLogoTrue()
    {
        var (service, _) = CreateService();
        var userId = Guid.NewGuid();
        var jpgLogo = Base64(JpgHeader);

        await service.SaveAsync(userId, Request(logo: PngLogo));
        var updated = await service.SaveAsync(userId, Request(logo: jpgLogo, removeLogo: true));

        Assert.Equal(jpgLogo, updated.LogoBase64);
    }

    [Fact]
    public async Task Save_AcceptsJpgLogo()
    {
        var (service, _) = CreateService();

        var dto = await service.SaveAsync(Guid.NewGuid(), Request(logo: Base64(JpgHeader)));

        Assert.NotNull(dto.LogoBase64);
    }

    [Fact]
    public async Task Save_Throws_WhenLogoFormatInvalid()
    {
        var (service, repository) = CreateService();
        var gif = Base64(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 });

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.SaveAsync(Guid.NewGuid(), Request(logo: gif)));

        Assert.Equal("Logo deve ser PNG ou JPG.", ex.Message);
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Save_Throws_WhenLogoTooLarge()
    {
        var (service, _) = CreateService();
        var big = Base64(PngHeader, CompanyProfile.MaxLogoBytes + 1);

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.SaveAsync(Guid.NewGuid(), Request(logo: big)));

        Assert.Equal("Logo deve ter no maximo 500 KB.", ex.Message);
    }

    [Fact]
    public async Task Save_Accepts_LogoExactlyAtLimit()
    {
        var (service, _) = CreateService();
        var exact = Base64(PngHeader, CompanyProfile.MaxLogoBytes);

        var dto = await service.SaveAsync(Guid.NewGuid(), Request(logo: exact));

        Assert.Equal(exact, dto.LogoBase64);
    }

    [Fact]
    public async Task Save_Throws_WhenLogoBase64Invalid()
    {
        var (service, _) = CreateService();

        var ex = await Assert.ThrowsAsync<DomainException>(() => service.SaveAsync(Guid.NewGuid(), Request(logo: "isso nao e base64!!")));

        Assert.Equal("Logo invalido.", ex.Message);
    }

    [Fact]
    public async Task Save_DoesNotChangeExisting_WhenUpdateFails()
    {
        var (service, _) = CreateService();
        var userId = Guid.NewGuid();

        await service.SaveAsync(userId, Request(name: "Original", logo: PngLogo));
        await Assert.ThrowsAsync<DomainException>(() => service.SaveAsync(userId, Request(name: "Mudou", logo: "@@@")));

        var current = await service.GetAsync(userId);
        Assert.Equal("Original", current!.Name);
        Assert.Equal(PngLogo, current.LogoBase64);
    }

    [Fact]
    public async Task Get_ReturnsNull_WhenNoProfile_AndDoesNotCreate()
    {
        var (service, repository) = CreateService();

        var dto = await service.GetAsync(Guid.NewGuid());

        Assert.Null(dto);
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task UserIsolation_OtherUserGetsNull_AndSaveDoesNotTouchFirstProfile()
    {
        var (service, repository) = CreateService();
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var a = await service.SaveAsync(userA, Request(name: "Empresa A", logo: PngLogo));

        Assert.Null(await service.GetAsync(userB));

        var b = await service.SaveAsync(userB, Request(name: "Empresa B", removeLogo: true));

        Assert.NotEqual(a.Id, b.Id);
        Assert.Equal(2, repository.Items.Count);

        var aAfter = await service.GetAsync(userA);
        Assert.Equal("Empresa A", aAfter!.Name);
        Assert.Equal(PngLogo, aAfter.LogoBase64);
        Assert.Null(aAfter.UpdatedAt);
    }

    [Fact]
    public async Task Save_ConvertsEmptyOptionalStringsToNull_AndTrims()
    {
        var (service, _) = CreateService();

        var dto = await service.SaveAsync(Guid.NewGuid(), Request(
            name: "  Empresa  ",
            logo: "   ",
            phone: "",
            email: "   ",
            whatsApp: null,
            instagram: "  @loja  ",
            website: ""));

        Assert.Equal("Empresa", dto.Name);
        Assert.Null(dto.LogoBase64);
        Assert.Null(dto.Phone);
        Assert.Null(dto.Email);
        Assert.Null(dto.WhatsApp);
        Assert.Equal("@loja", dto.Instagram);
        Assert.Null(dto.Website);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Save_Throws_WhenNameEmpty(string name)
    {
        var (service, repository) = CreateService();

        await Assert.ThrowsAsync<DomainException>(() => service.SaveAsync(Guid.NewGuid(), Request(name: name)));

        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Save_Throws_WhenNameTooLong()
    {
        var (service, _) = CreateService();

        await Assert.ThrowsAsync<DomainException>(() => service.SaveAsync(Guid.NewGuid(), Request(name: new string('a', 121))));
    }

    private sealed class FakeCompanyProfileRepository : ICompanyProfileRepository
    {
        public List<CompanyProfile> Items { get; } = new();

        public Task<CompanyProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Items.FirstOrDefault(c => c.UserId == userId));

        public Task AddAsync(CompanyProfile companyProfile, CancellationToken cancellationToken = default)
        {
            Items.Add(companyProfile);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }
}
