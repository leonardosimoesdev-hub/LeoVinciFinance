using Auth.Application.Abstractions;
using Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

public class UsuarioRepository : IUsuarioReadRepository
{
    private readonly AuthDbContext _dbContext;

    public UsuarioRepository(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Usuario?> GetByUsernameAsync(string username, CancellationToken cancellationToken) =>
        _dbContext.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

    public Task<Usuario?> GetByIdAsync(long id, CancellationToken cancellationToken) =>
        _dbContext.Usuarios.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
}
