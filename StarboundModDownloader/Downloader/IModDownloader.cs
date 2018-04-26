using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace StarboundModDownloader.Downloader
{
    public delegate void ProgressChangedHandler(long totalBytes);

    public interface IModDownloader
    {
        event ProgressChangedHandler OnDownloadProgressChanged;
        Task<DownloadResult> Download();
    }
}
