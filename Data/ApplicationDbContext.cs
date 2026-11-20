using StrayThoughts_BE.Models;
using Microsoft.EntityFrameworkCore;

namespace StrayThoughts_BE.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Post> Posts => Set<Post>();
    public DbSet<PostAuditLog> PostAuditLogs => Set<PostAuditLog>();
    public DbSet<Theme> Themes => Set<Theme>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Post>(entity =>
        {
            entity.HasKey(post => post.Id);
            entity.Property(post => post.Title).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(post => post.Body).HasColumnType("nvarchar(max)").IsRequired();
            entity.Property(post => post.BadgeEmoji).HasColumnType("nvarchar(32)").HasMaxLength(32).IsRequired(false);
            entity.Property(post => post.ThemeId).IsRequired();
            entity.HasOne(post => post.Theme)
                .WithMany(theme => theme.Posts)
                .HasForeignKey(post => post.ThemeId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.Property(post => post.AdminHideDatetime).IsRequired(false);
            entity.Property(post => post.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        });

        modelBuilder.Entity<Theme>(entity =>
        {
            entity.HasKey(theme => theme.Id);
            entity.Property(theme => theme.Key).HasMaxLength(100).IsRequired();
            entity.Property(theme => theme.DisplayName).HasMaxLength(100).IsRequired();
            entity.Property(theme => theme.LightBg).HasColumnType("varchar(7)").HasMaxLength(7).IsRequired();
            entity.Property(theme => theme.LightBorder).HasColumnType("varchar(7)").HasMaxLength(7).IsRequired();
            entity.Property(theme => theme.LightText).HasColumnType("varchar(7)").HasMaxLength(7).IsRequired();
            entity.Property(theme => theme.DarkBg).HasColumnType("varchar(7)").HasMaxLength(7).IsRequired();
            entity.Property(theme => theme.DarkBorder).HasColumnType("varchar(7)").HasMaxLength(7).IsRequired();
            entity.Property(theme => theme.DarkText).HasColumnType("varchar(7)").HasMaxLength(7).IsRequired();
            entity.Property(theme => theme.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(theme => theme.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");
            entity.Property(theme => theme.DeletedAt).IsRequired(false);
            entity.HasIndex(theme => theme.Key).IsUnique();
            entity.HasIndex(theme => theme.DisplayName).IsUnique();
            entity.HasCheckConstraint("CK_Themes_Key_Lowercase", "[Key] = LOWER([Key])");
            entity.HasCheckConstraint("CK_Themes_DisplayName_Lowercase", "[DisplayName] = LOWER([DisplayName])");
        });

        //modelBuilder.Ignore<PostAuditLog>();
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
