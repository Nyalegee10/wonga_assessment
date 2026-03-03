using Microsoft.EntityFrameworkCore;
using UserAuth.Domain.Entities;

namespace UserAuth.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var isRelational = Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory";

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            if (isRelational)
                entity.Property(e => e.Id).UseIdentityAlwaysColumn();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.PasswordHash).IsRequired();
            if (isRelational)
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("NOW()");
        });
    }
}
