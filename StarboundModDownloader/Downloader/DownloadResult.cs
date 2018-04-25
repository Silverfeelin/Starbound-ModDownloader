using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace StarboundModDownloader.Downloader
{
    public class DownloadResult
    {
        public MemoryStream MemoryStream { get; set; }
        public string FileType { get; set; }
    }
}
