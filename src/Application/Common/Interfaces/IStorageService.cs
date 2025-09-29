namespace Application.Common.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default);
    }
}
