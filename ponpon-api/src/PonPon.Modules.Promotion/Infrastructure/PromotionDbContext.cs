using Microsoft.EntityFrameworkCore;
using PonPon.Modules.Promotion.Domain;

namespace PonPon.Modules.Promotion.Infrastructure;

public sealed class PromotionDbContext : DbContext
{
    public const string Schema = "promotion";

    public PromotionDbContext(DbContextOptions<PromotionDbContext> options) : base(options) { }

    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<CouponCampaign> CouponCampaigns => Set<CouponCampaign>();
    public DbSet<CouponClaim> CouponClaims => Set<CouponClaim>();
    public DbSet<CouponUsage> CouponUsages => Set<CouponUsage>();
    public DbSet<CouponAuditLog> CouponAuditLogs => Set<CouponAuditLog>();
    public DbSet<CouponBulkGenerationJob> CouponBulkGenerationJobs => Set<CouponBulkGenerationJob>();
    public DbSet<global::PonPon.Modules.Promotion.Domain.Promotion> Promotions
        => Set<global::PonPon.Modules.Promotion.Domain.Promotion>();
    public DbSet<PromotionUsage> PromotionUsages => Set<PromotionUsage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<CouponCampaign>(b =>
        {
            b.ToTable("coupon_campaigns");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.HasIndex(x => x.Name);
            b.HasIndex(x => new { x.IsActive, x.StartsAtUtc, x.EndsAtUtc });
        });
        modelBuilder.Entity<Coupon>(b =>
        {
            b.ToTable("coupons");
            b.HasKey(x => x.Id);
            b.Property(x => x.Code).HasMaxLength(64).IsRequired();
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Type).HasMaxLength(32).IsRequired();
            b.Property(x => x.Value).HasPrecision(18, 2);
            b.Property(x => x.MinimumSubtotal).HasPrecision(18, 2);
            b.Property(x => x.MaximumDiscount).HasPrecision(18, 2);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.HasIndex(x => x.Code).IsUnique();
            b.HasIndex(x => x.CampaignId);
            b.HasIndex(x => x.IsDeleted);
            b.HasOne<CouponCampaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.SetNull);
            b.HasMany(x => x.Scopes).WithOne().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.CustomerScopes).WithOne().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Conditions).WithOne().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.Scopes).AutoInclude();
            b.Navigation(x => x.CustomerScopes).AutoInclude();
            b.Navigation(x => x.Conditions).AutoInclude();
        });
        modelBuilder.Entity<CouponScope>(b =>
        {
            b.ToTable("coupon_scopes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasMaxLength(32).IsRequired();
            b.Property(x => x.Sku).HasMaxLength(128);
            b.Property(x => x.CategoryName).HasMaxLength(512);
            b.HasIndex(x => x.CouponId);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.VariantId);
            b.HasIndex(x => x.Sku);
            b.HasIndex(x => x.ZortCategoryId);
        });
        modelBuilder.Entity<CouponClaim>(b =>
        {
            b.ToTable("coupon_claims");
            b.HasKey(x => x.Id);
            b.HasIndex(x => new { x.CouponId, x.CustomerId }).IsUnique();
            b.HasIndex(x => x.CustomerId);
            b.HasIndex(x => x.ClaimedAtUtc);
            b.HasOne<Coupon>().WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<CouponCustomerScope>(b =>
        {
            b.ToTable("coupon_customer_scopes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasMaxLength(32).IsRequired();
            b.HasIndex(x => x.CouponId);
            b.HasIndex(x => x.CustomerId);
        });
        modelBuilder.Entity<CouponCondition>(b =>
        {
            b.ToTable("coupon_conditions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasMaxLength(32).IsRequired();
            b.Property(x => x.Value).HasMaxLength(128).IsRequired();
            b.HasIndex(x => x.CouponId);
            b.HasIndex(x => new { x.Type, x.Value });
        });
        modelBuilder.Entity<CouponUsage>(b =>
        {
            b.ToTable("coupon_usages");
            b.HasKey(x => x.Id);
            b.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            b.HasIndex(x => new { x.OrderId, x.CouponId }).IsUnique();
            b.HasIndex(x => new { x.CouponId, x.CustomerId, x.IsReleased });
            b.HasOne<Coupon>().WithMany().HasForeignKey(x => x.CouponId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CouponAuditLog>(b =>
        {
            b.ToTable("coupon_audit_logs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Action).HasMaxLength(64).IsRequired();
            b.Property(x => x.ActorUserType).HasMaxLength(32);
            b.Property(x => x.BeforeJson).HasColumnType("jsonb");
            b.Property(x => x.AfterJson).HasColumnType("jsonb");
            b.HasIndex(x => x.CouponId);
            b.HasIndex(x => x.BatchId);
            b.HasIndex(x => x.CreatedAtUtc);
        });
        modelBuilder.Entity<CouponBulkGenerationJob>(b =>
        {
            b.ToTable("coupon_bulk_generation_jobs");
            b.HasKey(x => x.Id);
            b.Property(x => x.Prefix).HasMaxLength(32).IsRequired();
            b.Property(x => x.InputJson).HasColumnType("jsonb").IsRequired();
            b.Property(x => x.Status).HasMaxLength(32).IsRequired();
            b.Property(x => x.BackgroundJobId).HasMaxLength(128);
            b.Property(x => x.Error).HasMaxLength(2000);
            b.Property(x => x.RequestedByUserType).HasMaxLength(32);
            b.HasIndex(x => x.BatchId).IsUnique();
            b.HasIndex(x => x.CampaignId);
            b.HasIndex(x => new { x.Status, x.RequestedAtUtc });
            b.HasOne<CouponCampaign>().WithMany().HasForeignKey(x => x.CampaignId)
                .OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<global::PonPon.Modules.Promotion.Domain.Promotion>(b =>
        {
            b.ToTable("promotions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(256).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.Type).HasMaxLength(32).IsRequired();
            b.Property(x => x.DiscountType).HasMaxLength(32).IsRequired();
            b.Property(x => x.DiscountValue).HasPrecision(18, 2);
            b.Property(x => x.MinimumSubtotal).HasPrecision(18, 2);
            b.Property(x => x.MaximumDiscount).HasPrecision(18, 2);
            b.Property(x => x.Timezone).HasMaxLength(64).IsRequired();
            b.HasIndex(x => x.CampaignId);
            b.HasIndex(x => new { x.IsActive, x.StartsAtUtc, x.EndsAtUtc, x.Priority });
            b.HasOne<CouponCampaign>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.SetNull);
            b.HasMany(x => x.ScheduleRules).WithOne().HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Scopes).WithOne().HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.CustomerScopes).WithOne().HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Cascade);
            b.HasMany(x => x.Conditions).WithOne().HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Cascade);
            b.Navigation(x => x.ScheduleRules).AutoInclude();
            b.Navigation(x => x.Scopes).AutoInclude();
            b.Navigation(x => x.CustomerScopes).AutoInclude();
            b.Navigation(x => x.Conditions).AutoInclude();
        });
        modelBuilder.Entity<PromotionScheduleRule>(b =>
        {
            b.ToTable("promotion_schedule_rules");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasMaxLength(32).IsRequired();
            b.HasIndex(x => x.PromotionId);
            b.HasIndex(x => new { x.Type, x.DayOfWeek, x.DayOfMonth });
        });
        modelBuilder.Entity<PromotionScope>(b =>
        {
            b.ToTable("promotion_scopes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasMaxLength(32).IsRequired();
            b.Property(x => x.Sku).HasMaxLength(128);
            b.Property(x => x.CategoryName).HasMaxLength(512);
            b.HasIndex(x => x.PromotionId);
            b.HasIndex(x => x.ProductId);
            b.HasIndex(x => x.VariantId);
            b.HasIndex(x => x.Sku);
        });
        modelBuilder.Entity<PromotionCustomerScope>(b =>
        {
            b.ToTable("promotion_customer_scopes");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasMaxLength(32).IsRequired();
            b.HasIndex(x => x.PromotionId);
            b.HasIndex(x => x.CustomerId);
        });
        modelBuilder.Entity<PromotionCondition>(b =>
        {
            b.ToTable("promotion_conditions");
            b.HasKey(x => x.Id);
            b.Property(x => x.Type).HasMaxLength(32).IsRequired();
            b.Property(x => x.Value).HasMaxLength(128).IsRequired();
            b.HasIndex(x => x.PromotionId);
            b.HasIndex(x => new { x.Type, x.Value });
        });
        modelBuilder.Entity<PromotionUsage>(b =>
        {
            b.ToTable("promotion_usages");
            b.HasKey(x => x.Id);
            b.Property(x => x.DiscountAmount).HasPrecision(18, 2);
            b.HasIndex(x => new { x.OrderId, x.PromotionId }).IsUnique();
            b.HasIndex(x => new { x.PromotionId, x.CustomerId, x.IsReleased });
            b.HasOne<global::PonPon.Modules.Promotion.Domain.Promotion>().WithMany().HasForeignKey(x => x.PromotionId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
