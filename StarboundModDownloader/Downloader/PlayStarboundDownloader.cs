using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace StarboundModDownloader.Downloader
{
    /// <summary>
    /// Downloads a mod from a PlayStarbound resource page.
    /// The downloader supports zip and binary files (pak), and will even process a redirect (but only one).
    /// If the download button redirects to a html page, FileTypeException will be thrown (unless it has been added to SupportedFileTypes, in which case the html will be downloaded).
    /// </summary>
    public class PlayStarboundDownloader : IModDownloader
    {
        /// <summary>
        /// Supported file types.
        /// </summary>
        public ISet<string> SupportedFileTypes { get; set; } = new HashSet<string>()
        {
            "application/zip", // Zipped release (can contain mod folder or pak).
            "application/octet-stream" // Binary (we'll have to assume it's a valid .pak).
        };

        /// <summary>
        /// xf2_session id, used to access the download (button).
        /// </summary>
        private string sessionCookie;

        /// <summary>
        /// Instantiates a new downloader for PlayStarbound resources.
        /// </summary>
        /// <param name="sessionCookie">xf2_session cookie, used to access download link.</param>
        public PlayStarboundDownloader(string sessionCookie)
        {
            this.sessionCookie = sessionCookie;
        }

        /// <summary>
        /// Downloads the resource from a mod page.
        /// </summary>
        /// <param name="uri">Page to the mod resource (not the download link).</param>
        /// <returns>MemoryStream containing the downloaded file.</returns>
        public async Task<DownloadResult> Download(Uri uri)
        {
            Uri baseUri = new Uri(uri.GetLeftPart(UriPartial.Authority));

            // Get download link from download button.
            // XPath is really confusing and //label[@class='downloadButton']/a did not work.
            // TODO: Move this to a method since it can also be used to fetch the version number (which could be useful to poll for version changes for automated fetching).
            string html = DownloadHtml(uri, sessionCookie);
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            string downloadLocation = doc.DocumentNode
                .SelectNodes("//label/a")
                .Where((a) =>
                {
                    string s = a.Attributes["href"]?.Value;
                    if (s == null) return false;
                    return s.StartsWith("resources/");
                })
                .First()
                .Attributes["href"].Value;

            // Resource download location.
            var downloadUri = new Uri(baseUri, downloadLocation);
            
            // Attempt to download resource.
            try
            {
                CookieContainer cookies = new CookieContainer();
                cookies.Add(downloadUri, new Cookie("xf2_session", sessionCookie));
                return await DownloadToMemoryAsync(downloadUri, cookies);
            }
            catch (WebException exc)
            {
                // Possibly a redirect (why this is an exception, who knows).
                // Get redirect from response and attempt download once more.
                var response = exc.Response as HttpWebResponse;
                string location = response.GetResponseHeader("Location");

                // Not a redirect.
                if (string.IsNullOrWhiteSpace(location))
                {
                    throw exc;
                }

                Uri newUri = new Uri(location);
                return await DownloadToMemoryAsync(newUri);
            }
        }

        /// <summary>
        /// Downloads the Html page and returns it as a string.
        /// </summary>
        /// <param name="uri">Html Uri.</param>
        /// <param name="sessionCookie">Xenforo session cookie. Necessary to find the download link.</param>
        /// <returns></returns>
        private string DownloadHtml(Uri uri, string sessionCookie)
        {
            using (WebClient client = new WebClient())
            {
                client.Headers.Add(HttpRequestHeader.Cookie, "xf2_session=" + sessionCookie);
                return client.DownloadString(uri);
            }
        }


        /// <summary>
        /// Downloads a file and returns a memory stream containing said file. Checks for <see cref="SupportedFileTypes"/>.
        /// </summary>
        /// <param name="uri">Download Uri.</param>
        /// <param name="cookies">CookieContainer with necessary cookies to download the file.</param>
        /// <returns></returns>
        /// <exception cref="FileTypeException">File is not in <see cref="SupportedFileTypes"/>.</exception>
        /// <exception cref="WebException">Failed to download file.</exception>
        private async Task<DownloadResult> DownloadToMemoryAsync(Uri uri, CookieContainer cookies = null)
        {
            HttpWebRequest request = WebRequest.Create(uri) as HttpWebRequest;
            request.CookieContainer = cookies;

            // GetResponseAsync can throw WebException if the request redirects.
            using (var response = await request.GetResponseAsync() as HttpWebResponse)
            {
                // Check if response file type matches expectation.
                if (!SupportedFileTypes.Contains(response.ContentType))
                {
                    throw new FileTypeException($"Content Type {response.ContentType} not supported by PlaystarboundDownloader.");
                }

                // Read file to memory.
                byte[] buffer = new byte[4096];

                MemoryStream memoryStream = new MemoryStream();
                using (var responseStream = response.GetResponseStream())
                {
                    int read;
                    do
                    {
                        read = responseStream.Read(buffer, 0, buffer.Length);
                        memoryStream.Write(buffer, 0, read);
                    } while (read > 0);
                }

                return new DownloadResult() { MemoryStream = memoryStream, FileType = response.ContentType };
            }
        }
    }
}
