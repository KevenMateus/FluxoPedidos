using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(o => o.CustomerId).HasColumnName("customer_id");
        builder.Property(o => o.CreatedAt).HasColumnName("created_at");
        builder.Property(o => o.Status).HasColumnName("status").HasConversion<int>();
        builder.Property(o => o.PaymentMethod).HasColumnName("payment_method").HasConversion<int>();
        builder.Property(o => o.Notes).HasColumnName("notes").HasMaxLength(1000);

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Metadata.FindNavigation(nameof(Order.Items))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata.FindNavigation(nameof(Order.Events))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(o => o.Events)
            .WithOne()
            .HasForeignKey(e => e.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(o => o.Total);

        builder.HasIndex(o => o.CreatedAt);
        builder.HasIndex(o => o.CustomerId);
    }
}
