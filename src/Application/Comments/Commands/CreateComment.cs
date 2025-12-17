using Application.Common.Interfaces;
using Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Application.Comments.Commands
{
    public record CreateCommentCommand(Guid WorkItemId, string Body) : IRequest<CreateCommentResult>;
    public record CreateCommentResult(Guid CommentId);

    public class CreateCommentHandler : IRequestHandler<CreateCommentCommand, CreateCommentResult>
    {
        private readonly IAppDbContext _db;
        private readonly IHttpContextAccessor _http;
        private readonly IRealTimeNotificationService? _notificationService;
        
        // Regex to match @mentions - matches @username or @"Display Name"
        private static readonly Regex MentionPattern = new(@"@(\w+)|@""([^""]+)""", RegexOptions.Compiled);

        public CreateCommentHandler(IAppDbContext db, IHttpContextAccessor http, IRealTimeNotificationService? notificationService = null)
        {
            _db = db; 
            _http = http;
            _notificationService = notificationService;
        }

        public async Task<CreateCommentResult> Handle(CreateCommentCommand request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Body))
                throw new ArgumentException("Comment body is required", nameof(request.Body));

            var userIdClaim = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var authorId = Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;

            // Extract mentioned usernames from comment body
            var mentionedUserIds = await ExtractMentionedUserIds(request.Body, cancellationToken);

            var entity = new Comment
            {
                Id = Guid.NewGuid(),
                WorkItemId = request.WorkItemId,
                AuthorId = authorId,
                Body = request.Body.Trim(),
                MentionedUserIds = mentionedUserIds
            };
            _db.Comments.Add(entity);

            // Create notifications for mentioned users (only if there are mentions)
            if (mentionedUserIds.Count > 0)
            {
                var workItem = await _db.WorkItems
                    .AsNoTracking()
                    .Where(w => w.Id == request.WorkItemId)
                    .Select(w => new { w.Title })
                    .FirstOrDefaultAsync(cancellationToken);

                var author = await _db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == authorId)
                    .Select(u => new { u.DisplayName })
                    .FirstOrDefaultAsync(cancellationToken);

                var notifiedUserIds = new List<Guid>();
                
                foreach (var mentionedUserId in mentionedUserIds.Where(uid => uid != authorId))
                {
                    var notification = new MentionNotification
                    {
                        Id = Guid.NewGuid(),
                        UserId = mentionedUserId,
                        CommentId = entity.Id,
                        WorkItemId = request.WorkItemId,
                        MentionedByUserId = authorId,
                        CommentBody = request.Body.Trim().Length > 100 
                            ? request.Body.Trim().Substring(0, 100) + "..." 
                            : request.Body.Trim(),
                        WorkItemTitle = workItem?.Title ?? "Unknown",
                        MentionedByName = author?.DisplayName ?? "Someone"
                    };
                    _db.MentionNotifications.Add(notification);
                    notifiedUserIds.Add(mentionedUserId);
                }

                await _db.SaveChangesAsync(cancellationToken);
                
                // Send real-time SignalR notifications to mentioned users
                if (_notificationService != null)
                {
                    var workItemTitle = workItem?.Title ?? "Unknown";
                    var mentionedByName = author?.DisplayName ?? "Someone";
                    
                    foreach (var userId in notifiedUserIds)
                    {
                        await _notificationService.SendMentionNotificationAsync(
                            userId, 
                            request.WorkItemId, 
                            workItemTitle, 
                            mentionedByName, 
                            cancellationToken);
                    }
                }
            }
            else
            {
                await _db.SaveChangesAsync(cancellationToken);
            }
            
            return new CreateCommentResult(entity.Id);
        }

        private async Task<List<Guid>> ExtractMentionedUserIds(string body, CancellationToken cancellationToken)
        {
            var matches = MentionPattern.Matches(body);
            if (matches.Count == 0) return new List<Guid>();

            var mentionTerms = matches
                .Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct()
                .Select(t => t.ToLower())
                .ToList();

            if (mentionTerms.Count == 0) return new List<Guid>();

            // Fetch all users and filter in memory to avoid EF Core translation issues
            var allUsers = await _db.Users
                .AsNoTracking()
                .Select(u => new { u.Id, DisplayName = u.DisplayName.ToLower(), Email = u.Email.ToLower() })
                .ToListAsync(cancellationToken);

            var matchedUserIds = allUsers
                .Where(u => mentionTerms.Any(term => 
                    u.DisplayName == term || 
                    u.Email.StartsWith(term)))
                .Select(u => u.Id)
                .ToList();

            return matchedUserIds;
        }
    }
}
