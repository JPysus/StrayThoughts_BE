using StrayThoughts_BE.Models;
using Microsoft.EntityFrameworkCore;

namespace StrayThoughts_BE.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostAuditLog> PostAuditLogs => Set<PostAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(post => post.Id);
            entity.Property(post => post.Title).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(post => post.Body).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(post => post.BadgeEmoji).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired(false);
            entity.Property(post => post.Theme).HasColumnType("varchar(50)").HasMaxLength(50).IsRequired();
            entity.Property(post => post.AdminHideDatetime).IsRequired(false);
            entity.Property(post => post.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<PostAuditLog>(entity =>
        {
            entity.HasKey(log => log.Id);
            entity.Property(log => log.Action).HasMaxLength(50).IsRequired();
            entity.Property(log => log.PerformedBy).HasMaxLength(100).IsRequired();
            entity.Property(log => log.Timestamp).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(log => log.Details).HasColumnType("nvarchar(max)");
            entity.Property(log => log.IpAddress).HasMaxLength(45);
        });
    }
}
