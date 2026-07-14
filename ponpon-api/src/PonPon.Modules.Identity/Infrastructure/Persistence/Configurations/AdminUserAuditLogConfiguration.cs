using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Identity.Domain.Users;

namespace PonPon.Modules.Identity.Infrastructure.Persistence.Configurations;

public sealed class AdminUserAuditLogConfiguration : IEntityTypeConfiguration<AdminUserAuditLog>
{
    public void Configure(EntityTypeBuilder<AdminUserAuditLog> builder)
    {
        builder.ToTable("admin_user_audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Action).HasMaxLength(128).IsRequired();
        builder.Property(x => x.DetailsJson).HasColumnType("jsonb").IsRequired();
        builder.HasIndex(x => x.TargetUserId);
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.CreatedAtUtc);
    }
}
