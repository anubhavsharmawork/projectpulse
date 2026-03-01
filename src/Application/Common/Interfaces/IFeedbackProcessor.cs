namespace Application.Common.Interfaces
{
    public interface IFeedbackProcessor
    {
        Task ProcessFeedbackAsync(Guid feedbackId);
        Task RunDailyMaintenanceAsync();
    }
}
