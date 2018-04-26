using System.IO;

namespace StarboundModDownloader.Downloader
{
    public class DownloadResult
    {
        public MemoryStream MemoryStream { get; set; }
        public string FileType { get; set; }

        public long TotalBytes { get; set; }
    }
}
