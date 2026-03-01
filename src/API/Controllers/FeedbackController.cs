using Application.Common.Interfaces;
using Asp.Versioning;
using Domain.Entities;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class FeedbackController : ControllerBase
    {
        private readonly IAppDbContext _db;

        public FeedbackController(IAppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Submit product feedback. A background job processes it asynchronously.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitFeedbackRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = Guid.TryParse(userIdClaim, out var id) ? id : (Guid?)null;

            string? email = null;
            string? displayName = null;

            if (userId.HasValue)
            {
                var user = await _db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == userId.Value);

                email = user?.Email;
                displayName = user?.DisplayName ?? user?.UserName;
            }

            var feedback = new Feedback
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                UserEmail = email,
                UserDisplayName = displayName,
                Message = request.Message.Trim(),
                CreatedBy = userId?.ToString() ?? "anonymous"
            };

            _db.Feedbacks.Add(feedback);
            await _db.SaveChangesAsync();

            BackgroundJob.Enqueue<IFeedbackProcessor>(p => p.ProcessFeedbackAsync(feedback.Id));

            return Ok(new { feedbackId = feedback.Id });
        }

        /// <summary>
        /// List feedback (admin only). Supports simple pagination.
        /// </summary>
        [HttpGet]
        [Authorize(Policy = "AdminPolicy")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var query = _db.Feedbacks
                .AsNoTracking()
                .Where(f => f.IsActive)
                .OrderByDescending(f => f.CreatedAt);

            var total = await query.CountAsync();
            var items = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(f => new FeedbackItemDto(
                    f.Id,
                    f.UserDisplayName,
                    f.UserEmail,
                    f.Message,
                    f.CreatedAt,
                    f.ProcessedAt))
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }
    }

    public record SubmitFeedbackRequest(
        [Required]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Feedback must be between 10 and 2000 characters.")]
        string Message);

    public record FeedbackItemDto(
        Guid Id,
        string? UserDisplayName,
        string? UserEmail,
        string Message,
        DateTime CreatedAt,
        DateTime? ProcessedAt);
}
