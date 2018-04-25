using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StarboundModDownloader.Downloader
{
    public class GitHubDownloader : IModDownloader
    {
        public string User { get; set; }

        public string Repository { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the source code is downloaded.
        /// If false, uses the asset pattern instead.
        /// </summary>
        public bool DownloadSourceCode { get; set; } = false;

        /// <summary>
        /// Regex for asset names.
        /// </summary>
        public Regex AssetPattern { get; set; }

        /// <summary>
        /// Download from GitHub repository.
        /// </summary>
        /// <param name="uri">GitHub repository.</param>
        /// <returns>Download result.</returns>
        public async Task<DownloadResult> Download()
        {
            if (!DownloadSourceCode && AssetPattern == null)
            {
                throw new ArgumentNullException("Asset pattern is empty for GitHub Downloader.");
            }

            string endpoint = string.Format("https://api.github.com/repos/{0}/{1}/releases/latest", User, Repository);

            string json;
            using (WebClient client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.UserAgent, "StarboundModDownloader");
                json = await client.DownloadStringTaskAsync(endpoint);
            }

            JObject j = JObject.Parse(json);

            if (DownloadSourceCode)
            {
                string source = FindSource(j);
                return await Download(source);
            }
            else
            {
                string asset = FindAsset(j, AssetPattern);
                return await Download(asset);
            }
        }

        private async Task<DownloadResult> Download(string url)
        {
            Uri uri = new Uri(url);
            MemoryStream ms;
            using (WebClient client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.UserAgent, "StarboundModDownloader");
                byte[] data = await client.DownloadDataTaskAsync(uri);
                ms = new MemoryStream(data);
            }
            return new DownloadResult()
            {
                MemoryStream = ms,
                FileType = Path.GetExtension(url)
            };
        }

        /// <summary>
        /// Finds the source code download.
        /// </summary>
        /// <param name="latestRelease"></param>
        /// <returns></returns>
        private string FindSource(JObject latestRelease)
        {
            return latestRelease["zipball_url"].Value<string>();
        }

        /// <summary>
        /// Finds a downloadable asset matching the pattern, or null if no assets exist or match the pattern.
        /// </summary>
        /// <param name="latestRelease">Latest release response.</param>
        /// <returns></returns>
        private string FindAsset(JObject latestRelease, Regex pattern)
        {
            foreach (var item in latestRelease["assets"])
            {
                string fileName = item["name"].Value<string>();
                if (AssetPattern.IsMatch(fileName))
                {
                    return item["browser_download_url"].Value<string>();
                }
            }

            return null;
        }
    }
}
