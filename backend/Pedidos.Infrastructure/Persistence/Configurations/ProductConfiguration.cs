using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(p => p.Sku).HasColumnName("sku").HasMaxLength(50).IsRequired();
        builder.Property(p => p.UnitPrice).HasColumnName("unit_price").HasColumnType("numeric(12,2)");

        builder.HasIndex(p => p.Sku).IsUnique();
    }
}
