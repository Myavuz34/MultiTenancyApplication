using Microsoft.EntityFrameworkCore;
using MultiTenant.Core.Contracts;
using MultiTenant.Core.Entities;
using MultiTenant.Core.Interfaces;

namespace Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private string TenantId { get; set; }
    private readonly ITenantService _tenantService;
    public ApplicationDbContext(DbContextOptions options, ITenantService tenantService) : base(options)
    {
        _tenantService = tenantService;
        TenantId = _tenantService.GetTenant().Tıd;
    }
    public DbSet<Product> Products { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<Product>().HasQueryFilter(a => a.TenantId == TenantId);
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var tenantConnectionString = _tenantService.GetConnectionString();
        if (!string.IsNullOrEmpty(tenantConnectionString))
        {
            var dbProvider = _tenantService.GetDatabaseProvider();
            if (dbProvider.ToLower() == "postgres")
            {
                optionsBuilder.UseNpgsql(_tenantService.GetConnectionString());
            }
        }
    }
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                case EntityState.Modified:
                    entry.Entity.TenantId = TenantId;
                    break;
                case EntityState.Detached:
                    break;
                case EntityState.Unchanged:
                    break;
                case EntityState.Deleted:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        var result = await base.SaveChangesAsync(cancellationToken);
        return result;
    }
}