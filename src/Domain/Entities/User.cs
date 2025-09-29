namespace Domain.Entities
{
    public enum Role
    {
        Member = 0,
        Admin = 1
    }

    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty; // placeholder for identity provider
        public string DisplayName { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.Member;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
