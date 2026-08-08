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
    public DbSet<ProductFilament> ProductFilaments => Set<ProductFilament>();
    public DbSet<UserSettings> UserSettings => Set<UserSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PrintFlowDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
