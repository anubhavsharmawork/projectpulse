namespace Application.Common.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateToken(Guid userId, string email, string role);
        string GenerateToken(Guid userId, string email, string role, string? systemRole, IEnumerable<string>? permissions);
        string GenerateToken(Guid userId, Guid tenantId, string email, string role, string? systemRole, IEnumerable<string>? permissions, bool isDemo = false);
    }
}
