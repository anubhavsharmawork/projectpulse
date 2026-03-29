using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Application.UnitTests.TestHelpers;
using Domain.Entities;
using Xunit;

namespace Application.UnitTests.Infrastructure;

public class FeedbackProcessorTests
{
    [Fact]
    public async Task ProcessFeedbackAsync_FeedbackExists_ProcessesEvenWhenSmtpNotConfigured()
    {
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Feedbacks.Add(new Feedback
            {
                Id = Guid.NewGuid(),
                Message = "Hello",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });
        });

        var logger = new Mock<ILogger<FeedbackProcessor>>();
        var configMock = new Mock<IConfiguration>();
        var processor = new FeedbackProcessor(db, logger.Object, configMock.Object);

        var feedbackId = db.Feedbacks.First().Id;

        await processor.ProcessFeedbackAsync(feedbackId);

        var f = db.Feedbacks.First();
        f.ProcessedAt.Should().NotBeNull();
        f.UpdatedAt.Should().BeAfter(f.CreatedAt);
    }

    [Fact]
    public async Task RunDailyMaintenanceAsync_DeactivatesStaleProcessedFeedback()
    {
        var oldDate = DateTime.UtcNow.AddDays(-120);
        using var db = TestDbContextFactory.CreateWithData(ctx =>
        {
            ctx.Feedbacks.Add(new Feedback
            {
                Id = Guid.NewGuid(),
                Message = "Old processed",
                CreatedAt = oldDate,
                ProcessedAt = oldDate,
                IsActive = true
            });

            ctx.Feedbacks.Add(new Feedback
            {
                Id = Guid.NewGuid(),
                Message = "Recent processed",
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                ProcessedAt = DateTime.UtcNow.AddDays(-5),
                IsActive = true
            });

            ctx.Feedbacks.Add(new Feedback
            {
                Id = Guid.NewGuid(),
                Message = "Unprocessed",
                CreatedAt = DateTime.UtcNow.AddDays(-200),
                ProcessedAt = null,
                IsActive = true
            });
        });

        var logger = new Mock<ILogger<FeedbackProcessor>>();
        var configMock = new Mock<IConfiguration>();

        var processor = new FeedbackProcessor(db, logger.Object, configMock.Object);

        await processor.RunDailyMaintenanceAsync();

        var all = db.Feedbacks.ToList();
        all.Count(f => !f.IsActive).Should().Be(1);
        all.Count(f => f.IsActive).Should().Be(2);
    }
}
