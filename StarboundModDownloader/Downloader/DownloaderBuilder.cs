using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace StarboundModDownloader.Downloader
{
    public static class DownloaderBuilder
    {
        public static GitHubDownloader BuildGitHubDownloader(string user, string repository, Regex pattern = null)
        {
            var d = new GitHubDownloader()
            {
                User = user,
                Repository = repository
            };

            d.DownloadSourceCode = pattern == null ? true : false;
            d.AssetPattern = pattern;

            return d;
        }

        public static PlayStarboundDownloader BuildPSBDownloader(Uri resourceUri, string sessionCookie)
        {
            return new PlayStarboundDownloader()
            {
                ResourceUri = resourceUri,
                SessionCookie = sessionCookie
            };
        }
    }
}
