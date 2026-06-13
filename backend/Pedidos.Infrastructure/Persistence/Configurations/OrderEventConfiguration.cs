using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Persistence.Configurations;

public class OrderEventConfiguration : IEntityTypeConfiguration<OrderEvent>
{
    public void Configure(EntityTypeBuilder<OrderEvent> builder)
    {
        builder.ToTable("order_events");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.OrderId).HasColumnName("order_id");
        builder.Property(e => e.Type).HasColumnName("type").HasConversion<int>();
        builder.Property(e => e.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
        builder.Property(e => e.Source).HasColumnName("source").HasMaxLength(20).IsRequired();
        builder.Property(e => e.OccurredAt).HasColumnName("occurred_at");

        builder.HasIndex(e => e.OrderId);
    }
}
