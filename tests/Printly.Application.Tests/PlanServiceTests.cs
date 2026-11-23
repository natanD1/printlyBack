using System.Text.Json;
using Printly.Application.DTOs.Auth;
using Printly.Application.DTOs.Plans;
using Printly.Application.Services;
using Printly.Domain.Entities;
using Printly.Domain.Exceptions;

namespace Printly.Application.Tests;

public class PlanServiceTests
{
    private sealed class Context
    {
        public FakeUserRepository Users { get; } = new();
        public FakePlanRepository Plans { get; } = new();
        public PlanService Service { get; }
        public Plan Free { get; }

        public Context()
        {
            Plans.Users = Users;
            Free = TestPlans.Create("Free", isDefault: true);
            Plans.Items.Add(Free);
            Service = new PlanService(Plans, Users, new FakeUnitOfWork());
        }

        public User AddUser(Plan plan)
        {
            var user = new User("Fulano", $"{Guid.NewGuid():N}@b.com", "hash", Guid.NewGuid(), plan.Id);
            Users.Items.Add(user);
            return user;
        }
    }

    private static SavePlanRequest Request(
        string name = "Pro",
        decimal price = 29.9m,
        bool isActive = true,
        bool isDefault = false,
        int sortOrder = 1,
        List<PlanEntitlementDto>? entitlements = null,
        bool isPublic = true) =>
        new(name, "Plano pago", price, isActive, isDefault, sortOrder, entitlements ?? new List<PlanEntitlementDto>(), isPublic);

    [Fact]
    public async Task Create_SavesPlanWithEntitlements_NormalizingByType()
    {
        var ctx = new Context();

        var dto = await ctx.Service.CreateAsync(Request(entitlements: new()
        {
            new("MaxPrinters", 3, true),
            new("ProductHistoryMonths", null, null),
            new("CompanyLogoInQuote", 5, true),
            new("RemoveBrandingFooter", null, null),
        }));

        Assert.Equal("Pro", dto.Name);
        Assert.Equal(0, dto.UserCount);
        Assert.False(dto.IsDefault);
        Assert.Contains(dto.Entitlements, e => e is { Key: "MaxPrinters", IntValue: 3, BoolValue: null });
        Assert.Contains(dto.Entitlements, e => e is { Key: "ProductHistoryMonths", IntValue: null, BoolValue: null });
        Assert.Contains(dto.Entitlements, e => e is { Key: "CompanyLogoInQuote", IntValue: null, BoolValue: true });
        Assert.Contains(dto.Entitlements, e => e is { Key: "RemoveBrandingFooter", IntValue: null, BoolValue: false });
    }

    [Fact]
    public async Task Create_AsDefault_UnmarksPreviousDefault()
    {
        var ctx = new Context();

        var dto = await ctx.Service.CreateAsync(Request(isDefault: true));

        Assert.True(dto.IsDefault);
        Assert.False(ctx.Free.IsDefault);
        Assert.Single(ctx.Plans.Items, p => p.IsDefault);
    }

    [Fact]
    public async Task Update_MarkingAnotherAsDefault_UnmarksPreviousDefault()
    {
        var ctx = new Context();
        var pro = await ctx.Service.CreateAsync(Request());

        await ctx.Service.UpdateAsync(pro.Id, Request(isDefault: true));

        Assert.False(ctx.Free.IsDefault);
        Assert.Equal(pro.Id, Assert.Single(ctx.Plans.Items, p => p.IsDefault).Id);
    }

    [Fact]
    public async Task Update_UnmarkingDefaultDirectly_Throws()
    {
        var ctx = new Context();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => ctx.Service.UpdateAsync(ctx.Free.Id, Request(name: "Free", price: 0, isDefault: false)));

        Assert.Equal("Para trocar o plano padrao, marque outro plano como padrao.", ex.Message);
        Assert.True(ctx.Free.IsDefault);
    }

    [Fact]
    public async Task Update_DeactivatingDefault_Throws()
    {
        var ctx = new Context();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => ctx.Service.UpdateAsync(ctx.Free.Id, Request(name: "Free", price: 0, isDefault: true, isActive: false)));

        Assert.Equal("O plano padrao nao pode ser desativado.", ex.Message);
        Assert.True(ctx.Free.IsActive);
    }

    [Fact]
    public async Task Create_InactiveAsDefault_Throws()
    {
        var ctx = new Context();

        await Assert.ThrowsAsync<DomainException>(() => ctx.Service.CreateAsync(Request(isDefault: true, isActive: false)));

        Assert.True(ctx.Free.IsDefault);
        Assert.Single(ctx.Plans.Items);
    }

    // ---------- Plano oculto ----------

    [Fact]
    public async Task Create_Hidden_SavesAsNotPublic()
    {
        var ctx = new Context();

        var dto = await ctx.Service.CreateAsync(Request(isPublic: false));

        Assert.False(dto.IsPublic);
        Assert.False(ctx.Plans.Items.Single(p => p.Id == dto.Id).IsPublic);
    }

    [Fact]
    public async Task Create_HiddenAsDefault_Throws()
    {
        var ctx = new Context();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => ctx.Service.CreateAsync(Request(isDefault: true, isPublic: false)));

        Assert.Equal("O plano padrao precisa estar visivel.", ex.Message);
        Assert.True(ctx.Free.IsDefault);
        Assert.Single(ctx.Plans.Items);
    }

    [Fact]
    public async Task Update_HidingDefault_Throws()
    {
        var ctx = new Context();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => ctx.Service.UpdateAsync(ctx.Free.Id, Request(name: "Free", price: 0, isDefault: true, isPublic: false)));

        Assert.Equal("O plano padrao nao pode ser oculto.", ex.Message);
        Assert.True(ctx.Free.IsPublic);
    }

    [Fact]
    public async Task Update_MarkingHiddenPlanAsDefault_Throws()
    {
        var ctx = new Context();
        var hidden = await ctx.Service.CreateAsync(Request(isPublic: false));

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => ctx.Service.UpdateAsync(hidden.Id, Request(isDefault: true, isPublic: false)));

        Assert.Equal("O plano padrao precisa estar visivel.", ex.Message);
        Assert.True(ctx.Free.IsDefault);
        Assert.Equal(ctx.Free.Id, Assert.Single(ctx.Plans.Items, p => p.IsDefault).Id);
    }

    [Fact]
    public async Task SavePlanRequest_WithoutIsPublic_CreatesPublicPlan()
    {
        var ctx = new Context();
        const string json = """
            {"name":"Pro","description":null,"monthlyPrice":29.9,"isActive":true,"isDefault":false,"sortOrder":1,"entitlements":[]}
            """;

        // Mesmas opcoes da API (camelCase, sem diferenciar maiusculas).
        var request = JsonSerializer.Deserialize<SavePlanRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var dto = await ctx.Service.CreateAsync(request);

        Assert.Null(request.IsPublic);
        Assert.True(dto.IsPublic);
        Assert.True(ctx.Plans.Items.Single(p => p.Id == dto.Id).IsPublic);
    }

    [Fact]
    public async Task Update_WithoutIsPublic_KeepsHiddenPlanHidden()
    {
        var ctx = new Context();
        var hidden = await ctx.Service.CreateAsync(Request(name: "Parceiro", isPublic: false));
        const string json = """
            {"name":"Parceiro","description":null,"monthlyPrice":0,"isActive":true,"isDefault":false,"sortOrder":1,"entitlements":[]}
            """;

        var request = JsonSerializer.Deserialize<SavePlanRequest>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        var dto = await ctx.Service.UpdateAsync(hidden.Id, request);

        Assert.False(dto.IsPublic);
    }

    [Fact]
    public async Task Update_ReplacesEntitlementSet()
    {
        var ctx = new Context();
        var pro = await ctx.Service.CreateAsync(Request(entitlements: new()
        {
            new("MaxPrinters", 3, null),
            new("CompanyLogoInQuote", null, true),
        }));

        var updated = await ctx.Service.UpdateAsync(pro.Id, Request(entitlements: new()
        {
            new("MaxPrinters", 5, null),
            new("MaxFilaments", 10, null),
        }));

        Assert.Equal(2, updated.Entitlements.Count);
        Assert.Contains(updated.Entitlements, e => e is { Key: "MaxPrinters", IntValue: 5 });
        Assert.Contains(updated.Entitlements, e => e is { Key: "MaxFilaments", IntValue: 10 });
        Assert.DoesNotContain(updated.Entitlements, e => e.Key == "CompanyLogoInQuote");
    }

    [Fact]
    public async Task Delete_DefaultPlan_Throws()
    {
        var ctx = new Context();

        var ex = await Assert.ThrowsAsync<DomainException>(() => ctx.Service.DeleteAsync(ctx.Free.Id));

        Assert.Equal("O plano padrao nao pode ser excluido.", ex.Message);
        Assert.Contains(ctx.Free, ctx.Plans.Items);
    }

    [Fact]
    public async Task Delete_PlanWithUsers_Throws_ButCanBeDeactivated()
    {
        var ctx = new Context();
        var pro = await ctx.Service.CreateAsync(Request());
        ctx.AddUser(ctx.Plans.Items.Single(p => p.Id == pro.Id));

        await Assert.ThrowsAsync<DomainException>(() => ctx.Service.DeleteAsync(pro.Id));

        var deactivated = await ctx.Service.UpdateAsync(pro.Id, Request(isActive: false));
        Assert.False(deactivated.IsActive);
        Assert.Equal(1, deactivated.UserCount);
    }

    [Fact]
    public async Task Delete_PlanWithoutUsers_Removes()
    {
        var ctx = new Context();
        var pro = await ctx.Service.CreateAsync(Request());

        await ctx.Service.DeleteAsync(pro.Id);

        Assert.DoesNotContain(ctx.Plans.Items, p => p.Id == pro.Id);
    }

    [Theory]
    [InlineData("free")]
    [InlineData("FREE")]
    [InlineData("  Free  ")]
    public async Task Name_MustBeUnique_CaseInsensitive(string name)
    {
        var ctx = new Context();

        var ex = await Assert.ThrowsAsync<DomainException>(() => ctx.Service.CreateAsync(Request(name: name)));

        Assert.Equal("Ja existe um plano com este nome.", ex.Message);
    }

    [Fact]
    public async Task Name_SamePlanCanKeepItsName()
    {
        var ctx = new Context();

        var updated = await ctx.Service.UpdateAsync(ctx.Free.Id, Request(name: "FREE", price: 0, isDefault: true));

        Assert.Equal("FREE", updated.Name);
    }

    [Fact]
    public async Task NegativePrice_Throws()
    {
        var ctx = new Context();

        await Assert.ThrowsAsync<DomainException>(() => ctx.Service.CreateAsync(Request(price: -1m)));
    }

    [Fact]
    public async Task PriceAboveColumnPrecision_Throws()
    {
        var ctx = new Context();

        await Assert.ThrowsAsync<DomainException>(() => ctx.Service.CreateAsync(Request(price: 100_000_000m)));
    }

    [Fact]
    public async Task NegativeLimit_Throws()
    {
        var ctx = new Context();

        await Assert.ThrowsAsync<DomainException>(
            () => ctx.Service.CreateAsync(Request(entitlements: new() { new("MaxPrinters", -1, null) })));
        Assert.Single(ctx.Plans.Items);
    }

    [Theory]
    [InlineData("MaxUnicorns")]
    [InlineData("maxprinters")]
    [InlineData("0")]
    [InlineData("")]
    [InlineData(null)]
    public async Task UnknownKey_ThrowsInvalidRule(string? key)
    {
        var ctx = new Context();

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => ctx.Service.CreateAsync(Request(entitlements: new() { new(key!, 1, null) })));

        Assert.Equal("Regra de plano invalida.", ex.Message);
    }

    [Fact]
    public async Task DuplicatedKey_Throws()
    {
        var ctx = new Context();

        await Assert.ThrowsAsync<DomainException>(() => ctx.Service.CreateAsync(Request(entitlements: new()
        {
            new("MaxPrinters", 1, null),
            new("MaxPrinters", 2, null),
        })));
    }

    [Fact]
    public async Task GetAll_OrdersBySortOrderThenName_WithUserCount()
    {
        var ctx = new Context();
        await ctx.Service.CreateAsync(Request(name: "Zeta", sortOrder: 1));
        await ctx.Service.CreateAsync(Request(name: "Alfa", sortOrder: 1));
        ctx.AddUser(ctx.Free);
        ctx.AddUser(ctx.Free);

        var plans = await ctx.Service.GetAllAsync();

        Assert.Equal(new[] { "Free", "Alfa", "Zeta" }, plans.Select(p => p.Name));
        Assert.Equal(2, plans[0].UserCount);
        Assert.Equal(0, plans[1].UserCount);
    }

    [Fact]
    public void Catalog_ExposesTypesInLowercase()
    {
        var catalog = new Context().Service.GetCatalog();

        Assert.Equal(PlanEntitlementCatalog.All.Count, catalog.Count);
        Assert.Contains(catalog, c => c is { Key: "MaxPrinters", Type: "limit", Label: "Impressoras", Group: "Cadastros" });
        Assert.Contains(catalog, c => c is { Key: "OptionalCostsEnabled", Type: "feature" });
    }

    [Fact]
    public void Catalog_LimitsHaveDisplayUnits_FeaturesDoNot()
    {
        var catalog = new Context().Service.GetCatalog();

        foreach (var limit in catalog.Where(c => c.Type == "limit"))
        {
            Assert.False(string.IsNullOrWhiteSpace(limit.Unit), limit.Key);
            Assert.False(string.IsNullOrWhiteSpace(limit.UnitSingular), limit.Key);
            Assert.Equal(limit.Key == "ProductHistoryMonths", limit.ZeroLabel is not null);
        }

        foreach (var feature in catalog.Where(c => c.Type == "feature"))
        {
            Assert.Null(feature.Unit);
            Assert.Null(feature.UnitSingular);
            Assert.Null(feature.ZeroLabel);
        }

        Assert.Contains(catalog, c => c is { Key: "MaxPrinters", Unit: "impressoras", UnitSingular: "impressora", ZeroLabel: null });
        Assert.Contains(catalog, c => c is { Key: "MaxProductsPerMonth", Unit: "produtos por mês", UnitSingular: "produto por mês" });
        Assert.Contains(catalog, c => c is
        {
            Key: "ProductHistoryMonths",
            Unit: "meses anteriores ao atual",
            UnitSingular: "mês anterior ao atual",
            ZeroLabel: "Só o mês atual",
        });
    }

    // ---------- Planos disponiveis (GET /plans/available) ----------

    private static Plan Entity(Context ctx, PlanDto dto) => ctx.Plans.Items.Single(p => p.Id == dto.Id);

    [Fact]
    public async Task Available_ReturnsActivePublic_PlusCurrentHidden_OrderedBySortOrderThenName()
    {
        var ctx = new Context();
        await ctx.Service.CreateAsync(Request(name: "Zeta", sortOrder: 1));
        await ctx.Service.CreateAsync(Request(name: "Alfa", sortOrder: 1));
        await ctx.Service.CreateAsync(Request(name: "Inativo", sortOrder: 1, isActive: false));
        await ctx.Service.CreateAsync(Request(name: "Oculto", sortOrder: 1, isPublic: false));
        var secret = await ctx.Service.CreateAsync(Request(name: "Secreto", sortOrder: 1, isPublic: false));
        var user = ctx.AddUser(Entity(ctx, secret));

        var available = await ctx.Service.GetAvailablePlansAsync(user.Id);

        Assert.Equal(new[] { "Free", "Alfa", "Secreto", "Zeta" }, available.Plans.Select(p => p.Name));
        Assert.Equal(secret.Id, Assert.Single(available.Plans, p => p.IsCurrent).Id);
    }

    [Fact]
    public async Task Available_IncludesCurrentPlan_EvenIfInactive()
    {
        var ctx = new Context();
        var inactive = await ctx.Service.CreateAsync(Request(name: "Antigo", isActive: false));
        var user = ctx.AddUser(Entity(ctx, inactive));

        var available = await ctx.Service.GetAvailablePlansAsync(user.Id);

        Assert.Equal(new[] { "Free", "Antigo" }, available.Plans.Select(p => p.Name));
        Assert.True(available.Plans.Single(p => p.Id == inactive.Id).IsCurrent);
        Assert.False(available.Plans.Single(p => p.Id == ctx.Free.Id).IsCurrent);
    }

    [Fact]
    public async Task Available_CurrentPublicPlan_AppearsOnce()
    {
        var ctx = new Context();
        await ctx.Service.CreateAsync(Request(name: "Pro"));
        var user = ctx.AddUser(ctx.Free);

        var available = await ctx.Service.GetAvailablePlansAsync(user.Id);

        Assert.Equal(new[] { "Free", "Pro" }, available.Plans.Select(p => p.Name));
        Assert.True(Assert.Single(available.Plans, p => p.Id == ctx.Free.Id).IsCurrent);
        Assert.False(available.Plans.Single(p => p.Name == "Pro").IsCurrent);
    }

    [Fact]
    public async Task Available_HasPlanFieldsEntitlementsAndCatalogRules()
    {
        var ctx = new Context();
        var pro = await ctx.Service.CreateAsync(Request(name: "Pro", price: 29.9m, sortOrder: 2, entitlements: new()
        {
            new("MaxPrinters", 3, null),
            new("CompanyLogoInQuote", null, true),
        }));
        var user = ctx.AddUser(ctx.Free);

        var available = await ctx.Service.GetAvailablePlansAsync(user.Id);

        var plan = available.Plans.Single(p => p.Id == pro.Id);
        Assert.Equal("Pro", plan.Name);
        Assert.Equal("Plano pago", plan.Description);
        Assert.Equal(29.9m, plan.MonthlyPrice);
        Assert.Equal(2, plan.SortOrder);
        Assert.Contains(plan.Entitlements, e => e is { Key: "MaxPrinters", IntValue: 3, BoolValue: null });
        Assert.Contains(plan.Entitlements, e => e is { Key: "CompanyLogoInQuote", IntValue: null, BoolValue: true });
        Assert.Equal(ctx.Service.GetCatalog(), available.Rules);
    }

    [Fact]
    public async Task Available_UnknownUser_Throws()
    {
        var ctx = new Context();

        var ex = await Assert.ThrowsAsync<DomainException>(() => ctx.Service.GetAvailablePlansAsync(Guid.NewGuid()));

        Assert.Equal("Usuario nao encontrado.", ex.Message);
    }

    [Fact]
    public void AvailablePlanDto_DoesNotExposeAdminFields()
    {
        var properties = typeof(AvailablePlanDto).GetProperties().Select(p => p.Name).ToList();

        Assert.Equal(
            new[] { "Description", "Entitlements", "Id", "IsCurrent", "MonthlyPrice", "Name", "SortOrder" },
            properties.OrderBy(p => p, StringComparer.Ordinal));
        Assert.DoesNotContain("UserCount", properties);
        Assert.DoesNotContain("IsDefault", properties);
        Assert.DoesNotContain("IsActive", properties);
        Assert.DoesNotContain("IsPublic", properties);
    }

    // ---------- Troca de plano do usuario ----------

    [Fact]
    public async Task ChangeUserPlan_ChangesAndReturnsManagedUser()
    {
        var ctx = new Context();
        var user = ctx.AddUser(ctx.Free);
        var pro = await ctx.Service.CreateAsync(Request());

        var dto = await ctx.Service.ChangeUserPlanAsync(user.Id, pro.Id);

        Assert.Equal(pro.Id, user.PlanId);
        Assert.Equal(pro.Id, dto.PlanId);
        Assert.Equal("Pro", dto.PlanName);
        Assert.Equal(user.Email, dto.Email);
    }

    [Fact]
    public async Task ChangeUserPlan_PlanNotFound_Throws()
    {
        var ctx = new Context();
        var user = ctx.AddUser(ctx.Free);

        var ex = await Assert.ThrowsAsync<DomainException>(() => ctx.Service.ChangeUserPlanAsync(user.Id, Guid.NewGuid()));

        Assert.Equal("Plano nao encontrado.", ex.Message);
        Assert.Equal(ctx.Free.Id, user.PlanId);
    }

    [Fact]
    public async Task ChangeUserPlan_InactivePlan_Throws()
    {
        var ctx = new Context();
        var user = ctx.AddUser(ctx.Free);
        var inactive = await ctx.Service.CreateAsync(Request(isActive: false));

        var ex = await Assert.ThrowsAsync<DomainException>(() => ctx.Service.ChangeUserPlanAsync(user.Id, inactive.Id));

        Assert.Equal("Nao e possivel atribuir um plano inativo.", ex.Message);
        Assert.Equal(ctx.Free.Id, user.PlanId);
    }

    [Fact]
    public async Task ChangeUserPlan_UserNotFound_Throws()
    {
        var ctx = new Context();

        await Assert.ThrowsAsync<DomainException>(() => ctx.Service.ChangeUserPlanAsync(Guid.NewGuid(), ctx.Free.Id));
    }

    [Fact]
    public async Task UserManagement_ListsUsersWithPlanName()
    {
        var ctx = new Context();
        ctx.AddUser(ctx.Free);
        var service = new UserManagementService(ctx.Users, ctx.Plans);

        var users = await service.GetAllAsync();

        var user = Assert.Single(users);
        Assert.Equal(ctx.Free.Id, user.PlanId);
        Assert.Equal("Free", user.PlanName);
    }

    // ---------- Cadastro ----------

    [Fact]
    public async Task Register_AssignsDefaultPlan_ByMarkNotName()
    {
        var users = new FakeUserRepository();
        var invites = new FakeInviteCodeRepository();
        var plans = new FakePlanRepository();
        var free = TestPlans.Create("Free");
        var standard = TestPlans.Create("Padrao da casa", isDefault: true);
        plans.Items.Add(free);
        plans.Items.Add(standard);
        invites.Items.Add(new InviteCode("ABC123", null));
        var service = new AuthService(users, invites, new FakePasswordHasher(), new FakeTokenService(), new FakeRefreshTokenRepository(), plans, new FakeUnitOfWork(), new FakeTimeProvider());

        await service.RegisterAsync(new RegisterRequest("Fulano", "a@b.com", "Abcdef1!", "ABC123"));

        Assert.Equal(standard.Id, users.Items.Single().PlanId);
    }

    [Fact]
    public async Task Register_WithoutDefaultPlan_ThrowsClearError()
    {
        var users = new FakeUserRepository();
        var invites = new FakeInviteCodeRepository();
        invites.Items.Add(new InviteCode("ABC123", null));
        var service = new AuthService(users, invites, new FakePasswordHasher(), new FakeTokenService(), new FakeRefreshTokenRepository(), new FakePlanRepository(), new FakeUnitOfWork(), new FakeTimeProvider());

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.RegisterAsync(new RegisterRequest("Fulano", "a@b.com", "Abcdef1!", "ABC123")));

        Assert.Equal("Nenhum plano padrao configurado. Entre em contato com o suporte.", ex.Message);
        Assert.Empty(users.Items);
    }
}
