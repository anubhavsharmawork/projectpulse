using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Application.Users.Commands;

public record UpdateTimezoneCommand(string TimeZoneId, int TimeZoneOffset) : IRequest<Unit>;

public class UpdateTimezoneHandler : IRequestHandler<UpdateTimezoneCommand, Unit>
{
    private readonly IAppDbContext _db;
    private readonly IHttpContextAccessor _http;

    public UpdateTimezoneHandler(IAppDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<Unit> Handle(UpdateTimezoneCommand request, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var user = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User not found");

        user.TimeZoneId = request.TimeZoneId;
        user.TimeZoneOffset = request.TimeZoneOffset;
        await _db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }

    private Guid GetCurrentUserId()
    {
        var claim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : throw new UnauthorizedAccessException();
    }
}
