using Auth.Domain.Entities;
using Auth.Domain.Enums;
using Auth.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Seed;

public static class DbSeeder
{
    public static async Task SeedAdminAsync(AuthDbContext db)
    {
        // Check if admin already exists
        var adminExists = await db.Users.AnyAsync(u => u.Role == UserRole.Admin);
        if (adminExists) return;

        // Create the one fixed admin
        var admin = new User
        {
            FullName     = "Super Admin",
            Email        = "admin@parkease.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            Phone        = "9999999999",
            Role         = UserRole.Admin,
            IsActive     = true
        };

        await db.Users.AddAsync(admin);
        await db.SaveChangesAsync();

        Console.WriteLine("Admin seeded: admin@parkease.com / Admin@123");
    }
}