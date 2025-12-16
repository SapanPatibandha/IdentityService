using Microsoft.EntityFrameworkCore;
using IdentityService.Core.Interfaces;
using IdentityService.Infrastructure.Data;

namespace IdentityService.Infrastructure.Repositories;

public abstract class BaseRepository<T> where T : class
{
    protected readonly IdentityDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected BaseRepository(IdentityDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    protected async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
