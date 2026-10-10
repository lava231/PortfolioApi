using System;

namespace PortfolioApi.Models.Dtos
{
    public class UploadedFileInfoDto
    {
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public DateTime Created { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}
