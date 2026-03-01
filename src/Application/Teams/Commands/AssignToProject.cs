using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Application.Teams.Commands
{
    public record AssignToProjectCommand(
        Guid ProjectId,
        string Username,
        string Role,
        string? DomainExpertise,
        string? Skills,
        decimal AvailabilityHoursPerWeek,
        decimal CostRate) : IRequest<AssignToProjectResult>;

    public record AssignToProjectResult(Guid TeamMemberId, Guid TeamId);

    public class AssignToProjectHandler : IRequestHandler<AssignToProjectCommand, AssignToProjectResult>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;

        public AssignToProjectHandler(IAppDbContext db, IHttpContextAccessor http)
        {
            _db = db;
            _http = http;
        }

        public async Task<AssignToProjectResult> Handle(AssignToProjectCommand request, CancellationToken cancellationToken)
        {
            var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);
            if (project is null)
                throw new InvalidOperationException("Project not found");

            // Resolve user by username instead of userId
            var normalizedUsername = request.Username.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == normalizedUsername, cancellationToken);
            if (user is null)
                throw new InvalidOperationException($"No user found with username '{request.Username}'");

            // Find or create the default team for this project
            var team = await _db.Teams.FirstOrDefaultAsync(t => t.ProjectId == request.ProjectId, cancellationToken);
            if (team is null)
            {
                var createdBy = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
                team = new Domain.Entities.Team
                {
                    Id = Guid.NewGuid(),
                    Name = $"{project.Name} Team",
                    ProjectId = project.Id,
                    CreatedBy = createdBy
                };
                _db.Teams.Add(team);
            }

            // Check if already assigned
            var exists = await _db.TeamMembers.AnyAsync(
                tm => tm.TeamId == team.Id && tm.UserId == user.Id, cancellationToken);
            if (exists)
                throw new InvalidOperationException("User is already assigned to this project");

            var member = new Domain.Entities.TeamMember
            {
                Id = Guid.NewGuid(),
                TeamId = team.Id,
                UserId = user.Id,
                Role = request.Role,
                DomainExpertise = request.DomainExpertise,
                Skills = request.Skills,
                AvailabilityHoursPerWeek = request.AvailabilityHoursPerWeek,
                CostRate = request.CostRate,
                CreatedBy = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? string.Empty
            };

            _db.TeamMembers.Add(member);
            await _db.SaveChangesAsync(cancellationToken);
            return new AssignToProjectResult(member.Id, team.Id);
        }
    }
}
