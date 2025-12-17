using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Asp.Versioning;

namespace API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class FilesController : ControllerBase
    {
        private readonly IStorageService _storage;
        private readonly ILogger<FilesController> _logger;

        // OWASP A04: Secure Design - Whitelist of allowed file extensions
        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg",  // Images
            ".pdf", ".txt", ".md",                               // Documents
            ".json", ".xml"                                      // Data files
        };

        // OWASP A04: Secure Design - Whitelist of allowed MIME types
        private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/gif", "image/webp", "image/svg+xml",
            "application/pdf", "text/plain", "text/markdown",
            "application/json", "application/xml", "text/xml"
        };

        public FilesController(IStorageService storage, ILogger<FilesController> logger)
        {
            _storage = storage;
            _logger = logger;
        }

        [HttpPost("upload")]
        [RequestSizeLimit(40960)] // 40KB
        public async Task<IActionResult> Upload([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0) return BadRequest(new { error = "Empty file" });
            if (file.Length > 40960) return BadRequest(new { error = "File too large. Max 40KB." });

            // OWASP A04: Validate file extension
            var extension = Path.GetExtension(file.FileName);
            if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("Upload rejected: invalid extension {Extension} for file {FileName}", extension, file.FileName);
                return BadRequest(new { error = $"File type not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}" });
            }

            // OWASP A04: Validate MIME type
            if (string.IsNullOrEmpty(file.ContentType) || !AllowedMimeTypes.Contains(file.ContentType))
            {
                _logger.LogWarning("Upload rejected: invalid MIME type {ContentType} for file {FileName}", file.ContentType, file.FileName);
                return BadRequest(new { error = "File content type not allowed." });
            }

            try
            {
                await using var stream = file.OpenReadStream();
                var url = await _storage.UploadAsync(file.FileName, stream, file.ContentType);
                return Ok(new { url });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed for {FileName}", file.FileName);
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
