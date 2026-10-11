using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PortfolioApi.Models.Dtos;

namespace PortfolioApi.Services
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _uploadDir;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly long _maxFileSizeBytes;
        private readonly string[] _allowedExt = new[] { ".jpeg", ".jpg", ".png", ".gif", ".webp" };

        public LocalFileStorage(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;
            _uploadDir = config["Uploads:Directory"] ?? Path.Combine(_env.ContentRootPath, "uploads");
            if (!Directory.Exists(_uploadDir)) Directory.CreateDirectory(_uploadDir);
            _maxFileSizeBytes = long.TryParse(config["Uploads:MaxFileSizeBytes"], out var v) ? v : 5 * 1024 * 1024;
        }

        public async Task<FileUploadResultDto> SaveFileAsync(IFormFile file)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            if (file.Length == 0) throw new ArgumentException("Empty file");
            if (file.Length > _maxFileSizeBytes) throw new ArgumentException("File too large");

            var originalFileName = Path.GetFileName(file.FileName);
            var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
            if (!_allowedExt.Contains(ext)) throw new ArgumentException("Only image files are allowed");

            // Prevent path traversal in original filename by using only the file name
            // and not including any provided path segments in OriginalName return value.
            var safeOriginalName = originalFileName;

            var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "-" + Guid.NewGuid().ToString("N");
            var filename = "image-" + unique + ext;
            var path = Path.Combine(_uploadDir, filename);

            // Write to temp file then move to avoid partial files
            var tempPath = path + ".tmp";
            await using (var fs = new FileStream(tempPath, FileMode.CreateNew))
            {
                await file.CopyToAsync(fs);
                await fs.FlushAsync();
            }
            System.IO.File.Move(tempPath, path);

            return new FileUploadResultDto
            {
                FileName = filename,
                OriginalName = safeOriginalName,
                Size = file.Length,
                Url = $"/uploads/{filename}"
            };
        }

        public Task<bool> DeleteFileAsync(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return Task.FromResult(false);
            // Prevent path traversal by only allowing simple file names
            var name = Path.GetFileName(filename);
            if (string.IsNullOrEmpty(name)) return Task.FromResult(false);
            var path = Path.Combine(_uploadDir, name);
            if (!File.Exists(path)) return Task.FromResult(false);
            File.Delete(path);
            return Task.FromResult(true);
        }

        public Task<IEnumerable<UploadedFileInfoDto>> ListFilesAsync()
        {
            var files = Directory.Exists(_uploadDir) ? Directory.GetFiles(_uploadDir) : Array.Empty<string>();
            var infos = files.Select(f => new UploadedFileInfoDto
            {
                Name = Path.GetFileName(f),
                Size = new FileInfo(f).Length,
                Created = File.GetCreationTimeUtc(f),
                Url = $"/uploads/{Path.GetFileName(f)}"
            });

            return Task.FromResult(infos);
        }
    }
}
