using Application.Common.Interfaces;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration;

namespace Infrastructure
{
    public class S3StorageService : IStorageService
    {
        private readonly IAmazonS3 _s3;
        private readonly string _bucket;
        private readonly string _regionName;

        public S3StorageService(IConfiguration config)
        {
            // Prefer hierarchical keys, then common env var names used on Heroku and AWS
            _bucket =
                config["S3:Bucket"] ??
                config["S3__Bucket"] ??
                config["S3_BUCKET"] ??
                config["S3_BUCKET_NAME"] ??
                Environment.GetEnvironmentVariable("S3_BUCKET_NAME") ??
                string.Empty;

            _regionName =
                config["S3:Region"] ??
                config["S3__Region"] ??
                config["AWS_REGION"] ??
                config["AWS_DEFAULT_REGION"] ??
                Environment.GetEnvironmentVariable("AWS_REGION") ??
                Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION") ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(_bucket))
                throw new InvalidOperationException("S3 bucket is not configured (S3:Bucket / S3_BUCKET_NAME)");
            if (string.IsNullOrWhiteSpace(_regionName))
                throw new InvalidOperationException("S3 region is not configured (S3:Region / AWS_REGION)");

            // Fail fast if placeholders are used
            if (string.Equals(_bucket, "your-bucket", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(_regionName, "your-aws-region", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Invalid S3 configuration. Please set S3:Bucket and S3:Region to real values (not placeholders).");
            }

            var cfg = new AmazonS3Config
            {
                RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(_regionName)
            };
            // Reduce retries and timeouts to avoid long H12 timeouts on bad configuration/DNS
            cfg.Timeout = TimeSpan.FromSeconds(10);
            cfg.ReadWriteTimeout = TimeSpan.FromSeconds(10);
            cfg.MaxErrorRetry = 1;

            _s3 = new AmazonS3Client(cfg);
        }

        public async Task<string> UploadAsync(string key, Stream content, string contentType, CancellationToken ct = default)
        {
            // Enforce 40KB (40960 bytes) max upload size
            const int MaxBytes = 40 * 1024;
            await using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            if (ms.Length > MaxBytes)
                throw new InvalidOperationException("File too large. Max 40KB.");
            ms.Position = 0;

            if (string.IsNullOrWhiteSpace(contentType))
                contentType = "application/octet-stream";

            var put = new PutObjectRequest
            {
                BucketName = _bucket,
                Key = key,
                InputStream = ms,
                ContentType = contentType,
                ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256
            };

            await _s3.PutObjectAsync(put, ct);

            // Return a public HTTPS object URL (assuming bucket/object ACL allows read)
            var httpsUrl = $"https://{_bucket}.s3.{_regionName}.amazonaws.com/{Uri.EscapeDataString(key)}";
            return httpsUrl;
        }
    }
}
