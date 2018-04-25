using StarboundModDownloader.Downloader;
using System;
using System.Collections.Generic;
using System.Text;

namespace StarboundModDownloader
{
    public static class LinkHelper
    {
        public static DownloadType GetDownloadType(string url)
        {
            url = url.Replace("https://", "").Replace("http://", "").Replace("www.", "").ToLowerInvariant();
            if (url.StartsWith("community.playstarbound.com/resources/"))
            {
                return DownloadType.PlayStarbound;
            }
            else if(url.StartsWith("github.com"))
            {
                return DownloadType.GitHub;
            }
            else
            {
                throw new ArgumentException($"URL {url} not supported.");
            }
        }
    }
}
