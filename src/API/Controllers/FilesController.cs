using Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "MemberPolicy")]
    public class FilesController : ControllerBase
    {
        private readonly IStorageService _storage;
        private readonly ILogger<FilesController> _logger;
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
