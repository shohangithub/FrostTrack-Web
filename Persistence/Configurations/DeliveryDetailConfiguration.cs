using Domain.Entitites;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Configurations;

public class DeliveryDetailConfiguration : IEntityTypeConfiguration<DeliveryDetail>
{
    public void Configure(EntityTypeBuilder<DeliveryDetail> builder)
    {
        builder.HasOne(d => d.Delivery)
            .WithMany(p => p.DeliveryDetails)
            .HasForeignKey(d => d.DeliveryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
