using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Pedidos.Domain.Entities;

namespace Pedidos.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(u => u.Name).HasColumnName("name").HasMaxLength(200).IsRequired();
        builder.Property(u => u.Email).HasColumnName("email").HasMaxLength(256).IsRequired();
        builder.Property(u => u.PasswordHash).HasColumnName("password_hash").HasMaxLength(512).IsRequired();
        builder.Property(u => u.Role).HasColumnName("role").HasMaxLength(50).IsRequired();
        builder.Property(u => u.CreatedAt).HasColumnName("created_at");

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
