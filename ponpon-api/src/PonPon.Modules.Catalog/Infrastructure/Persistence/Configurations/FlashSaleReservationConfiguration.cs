using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PonPon.Modules.Catalog.Domain.FlashSales;

namespace PonPon.Modules.Catalog.Infrastructure.Persistence.Configurations;

public sealed class FlashSaleReservationConfiguration : IEntityTypeConfiguration<FlashSaleReservation>
{
    public void Configure(EntityTypeBuilder<FlashSaleReservation> builder)
    {
        builder.ToTable("flash_sale_reservations");
        builder.HasKey(x => new { x.OrderId, x.FlashSaleId, x.ProductId });
        builder.HasIndex(x => new { x.FlashSaleId, x.ProductId, x.IsReleased });
    }
}
