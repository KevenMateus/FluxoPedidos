using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(i => i.OrderId).HasColumnName("order_id");
        builder.Property(i => i.ProductId).HasColumnName("product_id");
        builder.Property(i => i.ProductName).HasColumnName("product_name").HasMaxLength(200).IsRequired();
        builder.Property(i => i.Quantity).HasColumnName("quantity");
        builder.Property(i => i.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(12,2)");

        builder.Ignore(i => i.LineTotal);

        builder.HasIndex(i => i.OrderId);

        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
