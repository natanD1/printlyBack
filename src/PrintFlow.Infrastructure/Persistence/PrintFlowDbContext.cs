using Microsoft.EntityFrameworkCore;
using PrintFlow.Domain.Entities;

namespace PrintFlow.Infrastructure.Persistence;

public class PrintFlowDbContext : DbContext
{
    public PrintFlowDbContext(DbContextOptions<PrintFlowDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<InviteCode> InviteCodes => Set<InviteCode>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Filament> Filaments => Set<Filament>();
    public DbSet<FilamentBrand> FilamentsBrands => Set<FilamentBrand>();
    public DbSet<FilamentType> FilamentsType => Set<FilamentType>();
    public DbSet<ProductFilament> ProductFilaments => Set<ProductFilament>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();
    public DbSet<Feedback> Feedbacks => Set<Feedback>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrintFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
