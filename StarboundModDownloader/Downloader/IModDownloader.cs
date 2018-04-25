using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace StarboundModDownloader.Downloader
{
    public interface IModDownloader
    {
        Task<DownloadResult> Download(Uri uri);
    }
}
