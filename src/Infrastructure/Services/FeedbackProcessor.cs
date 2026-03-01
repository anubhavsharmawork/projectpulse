using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Infrastructure.Services
{
    public class FeedbackProcessor : IFeedbackProcessor
    {
        private readonly IAppDbContext _db;
        private readonly ILogger<FeedbackProcessor> _logger;
        private readonly IConfiguration _configuration;

        public FeedbackProcessor(IAppDbContext db, ILogger<FeedbackProcessor> logger, IConfiguration configuration)
        {
            _db = db;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task ProcessFeedbackAsync(Guid feedbackId)
        {
            var feedback = await _db.Feedbacks
                .FirstOrDefaultAsync(f => f.Id == feedbackId);

            if (feedback is null)
            {
                _logger.LogWarning("Feedback {FeedbackId} not found for processing", feedbackId);
                return;
            }

            if (feedback.ProcessedAt.HasValue)
            {
                _logger.LogInformation("Feedback {FeedbackId} already processed at {ProcessedAt}", feedbackId, feedback.ProcessedAt);
                return;
            }

            _logger.LogInformation(
                "Processing feedback {FeedbackId} from {UserName} ({UserEmail}): {MessagePreview}",
                feedbackId,
                feedback.UserDisplayName ?? "Unknown",
                feedback.UserEmail ?? "N/A",
                feedback.Message.Length > 80 ? feedback.Message[..80] + "..." : feedback.Message);

            await SendNotificationEmailAsync(feedback);

            feedback.ProcessedAt = DateTime.UtcNow;
            feedback.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Feedback {FeedbackId} processed successfully", feedbackId);
        }

        public async Task RunDailyMaintenanceAsync()
        {
            var unprocessedCount = await _db.Feedbacks
                .CountAsync(f => f.ProcessedAt == null && f.IsActive);

            var totalToday = await _db.Feedbacks
                .CountAsync(f => f.CreatedAt >= DateTime.UtcNow.Date && f.IsActive);

            _logger.LogInformation(
                "Feedback daily maintenance: {UnprocessedCount} unprocessed, {TotalToday} received today",
                unprocessedCount, totalToday);

            // Mark feedback older than 90 days as inactive for cleanup
            var cutoff = DateTime.UtcNow.AddDays(-90);
            var stale = await _db.Feedbacks
                .Where(f => f.CreatedAt < cutoff && f.IsActive && f.ProcessedAt.HasValue)
                .ToListAsync();

            foreach (var item in stale)
            {
                item.IsActive = false;
                item.UpdatedAt = DateTime.UtcNow;
            }

            if (stale.Count > 0)
            {
                await _db.SaveChangesAsync();
                _logger.LogInformation("Deactivated {Count} stale feedback items older than 90 days", stale.Count);
            }
        }

        private async Task SendNotificationEmailAsync(Domain.Entities.Feedback feedback)
        {
            var smtpHost = _configuration["Smtp:Host"];
            var smtpPortStr = _configuration["Smtp:Port"];
            var smtpUser = _configuration["Smtp:Username"];
            var smtpPass = _configuration["Smtp:Password"];
            var recipientEmail = _configuration["Feedback:NotificationEmail"] ?? "anubhav.sharma.work@outlook.com";
            var senderEmail = _configuration["Smtp:From"] ?? smtpUser;

            if (string.IsNullOrWhiteSpace(smtpHost) || string.IsNullOrWhiteSpace(smtpUser))
            {
                _logger.LogWarning(
                    "SMTP not configured — skipping email notification for feedback {FeedbackId}. " +
                    "Set Smtp:Host, Smtp:Port, Smtp:Username, Smtp:Password in configuration.",
                    feedback.Id);
                return;
            }

            if (!int.TryParse(smtpPortStr, out var smtpPort))
                smtpPort = 587;

            try
            {
                using var message = new MailMessage();
                message.From = new MailAddress(senderEmail!, "Project Pulse Feedback");
                message.To.Add(new MailAddress(recipientEmail));
                message.Subject = $"New Feedback from {feedback.UserDisplayName ?? "a user"}";
                message.IsBodyHtml = true;
                message.Body = $"""
                    <h3>New Product Feedback</h3>
                    <p><strong>From:</strong> {WebUtility.HtmlEncode(feedback.UserDisplayName ?? "Unknown")} ({WebUtility.HtmlEncode(feedback.UserEmail ?? "N/A")})</p>
                    <p><strong>Date:</strong> {feedback.CreatedAt:yyyy-MM-dd HH:mm} UTC</p>
                    <hr/>
                    <p>{WebUtility.HtmlEncode(feedback.Message)}</p>
                    """;

                using var client = new SmtpClient(smtpHost, smtpPort);
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
                client.EnableSsl = true;
                client.Timeout = 15_000;

                await client.SendMailAsync(message);
                _logger.LogInformation("Feedback notification email sent for {FeedbackId}", feedback.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send feedback notification email for {FeedbackId}", feedback.Id);
                // Don't rethrow — we still mark the feedback as processed
            }
        }
    }
}
