using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public class UserRepository(AuthDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email) =>
        db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower().Trim());

    public Task<User?> FindByUserIdAsync(Guid userId) =>
        db.Users.FirstOrDefaultAsync(u => u.UserId == userId);

    public Task<bool> ExistsByEmailAsync(string email) =>
        db.Users.AnyAsync(u => u.Email == email.ToLower().Trim());

    //  NEW — get all users with optional role and active filters
    public async Task<IEnumerable<User>> GetAllUsersAsync(UserRole? role, bool? isActive)
    {
        var query = db.Users.AsQueryable();

        if (role.HasValue)
            query = query.Where(u => u.Role == role.Value);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        return await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
    }

    public async Task AddAsync(User user) =>
        await db.Users.AddAsync(user);

    public async Task DeleteAsync(User user)
    {
        db.Users.Remove(user);
        await db.SaveChangesAsync();
    }

    public Task SaveChangesAsync() => db.SaveChangesAsync();
}