using Auth.Application.Interfaces;
using Auth.Domain.Entities;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Repositories;

public class UserRepository(AuthDbContext db) : IUserRepository
{
    public Task<User?> FindByEmailAsync(string email)   => db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());
    public Task<User?> FindByUserIdAsync(Guid userId)   => db.Users.FirstOrDefaultAsync(u => u.UserId == userId);
    public Task<bool>  ExistsByEmailAsync(string email) => db.Users.AnyAsync(u => u.Email == email.ToLower());
    public async Task  AddAsync(User user)              => await db.Users.AddAsync(user);
    public Task        SaveChangesAsync()               => db.SaveChangesAsync();
}