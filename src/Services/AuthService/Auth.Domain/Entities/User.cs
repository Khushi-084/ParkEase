using Auth.Domain.Enums;

namespace Auth.Domain.Entities;

public class User
{
    public Guid     UserId        { get; set; } = Guid.NewGuid();
    public string   FullName      { get; set; } = string.Empty;
    public string   Email         { get; set; } = string.Empty;
    public string   PasswordHash  { get; set; } = string.Empty;
    public string   Phone         { get; set; } = string.Empty;
    public UserRole Role          { get; set; } = UserRole.Driver;
    public bool     IsActive      { get; set; } = true;

    // ✅ NEW — only relevant for LotManager role
    // true  = admin has approved this LotManager
    // false = pending approval (LotManager cannot access lot management endpoints)
    // null  = not applicable (Driver or Admin)
    public bool?    IsApproved    { get; set; } = null;

    public string?  ProfilePicUrl { get; set; }
    public DateTime CreatedAt     { get; set; } = DateTime.UtcNow;
}