using Microsoft.EntityFrameworkCore;
using Pedidos.Application.Auth.Dtos;
using Pedidos.Application.Auth.Interfaces;
using Pedidos.Infrastructure.Persistence;

namespace Pedidos.Infrastructure.Repositories;

/// <summary>Leitura de usuários via EF Core + LINQ (busca direta por e-mail).</summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<UserCredentialsDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return await _db.Users
            .AsNoTracking()
            .Where(u => u.Email.ToLower() == normalized)
            .Select(u => new UserCredentialsDto
            {
                Id = u.Id,
                Name = u.Name,
                Email = u.Email,
                Role = u.Role,
                PasswordHash = u.PasswordHash
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
