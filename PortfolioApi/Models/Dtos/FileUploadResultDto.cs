using System;

namespace PortfolioApi.Models.Dtos
{
    public class FileUploadResultDto
    {
        public string Url { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string OriginalName { get; set; } = string.Empty;
        public long Size { get; set; }
    }
}
