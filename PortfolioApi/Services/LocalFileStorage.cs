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

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExt.Contains(ext)) throw new ArgumentException("Only image files are allowed");

            var unique = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "-" + Guid.NewGuid().ToString("N");
            var filename = "image-" + unique + ext;
            var path = Path.Combine(_uploadDir, filename);

            await using (var fs = new FileStream(path, FileMode.CreateNew))
            {
                await file.CopyToAsync(fs);
            }

            return new FileUploadResultDto
            {
                FileName = filename,
                OriginalName = file.FileName,
                Size = file.Length,
                Url = $"/uploads/{filename}"
            };
        }

        public Task<bool> DeleteFileAsync(string filename)
        {
            if (string.IsNullOrEmpty(filename)) return Task.FromResult(false);
            var path = Path.Combine(_uploadDir, filename);
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
