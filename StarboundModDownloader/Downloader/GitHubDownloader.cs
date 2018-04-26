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
        /// <summary>
        /// Used to validate a GitHub URL.
        /// Group 1: User/Organization
        /// Group 2: Repository
        /// </summary>
        public static readonly Regex REPOSITORY_REGEX = new Regex("(?:https:\\/\\/)?github\\.com\\/([\\w-]+)\\/([\\w-]+)");

        public event ProgressChangedHandler OnDownloadProgressChanged;

        /// <summary>
        /// Username or organization name.
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// Repository name.
        /// </summary>
        public string Repository { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the source code should be downloaded.
        /// If false, uses the <see cref="Pattern"/> instead.
        /// </summary>
        public bool DownloadSourceCode { get; set; } = false;

        /// <summary>
        /// Regex pattern used when determining the release asset to download.
        /// For example, ".*\.pak" to download the first asset ending with `.pak`.
        /// </summary>
        public Regex Pattern { get; set; }
        
        /// <summary>
        /// Download from GitHub repository.
        /// </summary>
        /// <param name="uri">GitHub repository.</param>
        /// <returns>Download result.</returns>
        public async Task<DownloadResult> Download()
        {
            if (!DownloadSourceCode && Pattern == null)
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
                string asset = FindAsset(j, Pattern);
                if (string.IsNullOrEmpty(asset))
                {
                    throw new ArgumentException($"No asset found matching the given asset pattern: {Pattern}");
                }
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
                client.DownloadProgressChanged += (e, d) =>
                {
                    OnDownloadProgressChanged?.Invoke(d.BytesReceived);
                };

                byte[] data = await client.DownloadDataTaskAsync(uri);
                ms = new MemoryStream(data);
            }

            string fileType = Path.GetExtension(url);
            if (!string.IsNullOrWhiteSpace(fileType))
                fileType = fileType.Substring(1).ToLowerInvariant();

            return new DownloadResult()
            {
                MemoryStream = ms,
                FileType = fileType,
                TotalBytes = ms.Length
            };
        }

        /// <summary>
        /// Finds the source code download.
        /// </summary>
        /// <param name="latestRelease"></param>
        /// <returns></returns>
        public static string FindSource(JObject latestRelease)
        {
            return latestRelease["zipball_url"].Value<string>();
        }

        /// <summary>
        /// Finds a downloadable asset matching the pattern, or null if no assets exist or match the pattern.
        /// </summary>
        /// <param name="latestRelease">Latest release response.</param>
        /// <returns></returns>
        public static string FindAsset(JObject latestRelease, Regex pattern)
        {
            foreach (var item in latestRelease["assets"])
            {
                string fileName = item["name"].Value<string>();
                if (pattern.IsMatch(fileName))
                {
                    return item["browser_download_url"].Value<string>();
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the user/organization and repository name from a GitHub repository URL.
        /// </summary>
        /// <param name="url">GitHub repository url.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static (string user, string repository) GetRepositoryName(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new ArgumentNullException("URL is empty or null.");
            }

            Match match = REPOSITORY_REGEX.Match(url);

            if (!match.Success)
            {
                throw new ArgumentException("URL is not a supported GitHub repository link.");
            }

            return (match.Groups[1].Value, match.Groups[2].Value);
        }
    }
}
