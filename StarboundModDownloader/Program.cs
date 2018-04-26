using CommandLine;
using StarboundModDownloader.Downloader;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StarboundModDownloader
{
    [Verb("github", HelpText = "Download asset or source code from GitHub.")]
    public class GitHubOptions
    {
        #region Generic

        [Option('i', "input", Required = true, HelpText = "Resource URL. For example, https://community.playstarbound.com/resources/wardrobe.3704/")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file location. Directories leading up to the file must exist.")]
        public string OutputFile { get; set; }

        [Option("overwrite", Required = false, HelpText = "Overwrite output file, if the file already exists.")]
        public bool Overwrite { get; set; }

        #endregion

        #region GitHub-specific

        [Option('p', "pattern", Required = false, HelpText = "Pattern for GitHub release assets. Use when omitting flag -s.")]
        public string Pattern { get; set; }

        [Option('s', "source", Required = false, HelpText = "Download source code instead of asset matching pattern.")]
        public bool Source { get; set; }

        [Option('v', "previousversion", Required = false, HelpText = "If set, aborts the download if the latest version matches this value.")]
        public string PreviousVersion { get; set; }

        #endregion
    }

    [Verb("psb", HelpText = "Download a mod from PlayStarbound.")]
    public class PSBOptions
    {
        #region Generic

        [Option('i', "input", Required = true, HelpText = "Resource URL. For example, https://community.playstarbound.com/resources/wardrobe.3704/")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file location. Directories leading up to the file must exist.")]
        public string OutputFile { get; set; }

        [Option("overwrite", Required = false, HelpText = "Overwrite output file, if the file already exists.")]
        public bool Overwrite { get; set; }

        #endregion

        #region PlayStarbound-specific 

        [Option('s', "session", Required = true, HelpText = "Session cookie (xf2_session), used to access the resource.")]
        public string Cookie { get; set; }

        [Option('v', "previousversion", Required = false, HelpText = "If set, aborts the download if the latest version matches this value.")]
        public string PreviousVersion { get; set; }

        #endregion
    }

    /// <summary>
    /// Exit codes:
    /// 1: Invalid arguments.
    /// 2: Latest mod version matches known version.
    /// 3: Download failed.
    /// 4: Saving file failed.
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<GitHubOptions, PSBOptions>(args)
                .WithParsed<GitHubOptions>(DownloadFromGitHub)
                .WithParsed<PSBOptions>(DownloadFromPSB);
        }

        /// <summary>
        /// Parses input and attempts to download an asset from GitHub.
        /// </summary>
        /// <param name="options">Command-line arguments</param>
        static void DownloadFromGitHub(GitHubOptions options)
        {
            Logger.LogInfo("= Starbound Mod Downloader");
            Logger.LogInfo("       URL: {0}", options.Input);
            Logger.LogInfo("    Target: {0}", options.Source ? "Source Code" : "Pattern " + options.Pattern);
            Logger.LogInfo("    Output: {0}", options.OutputFile);
            Logger.LogInfo(" Overwrite: {0}", options.Overwrite);
            Logger.LogInfo("Downloading...");

            // Check arguments.
            ConfirmOutputFile(options.OutputFile, options.Overwrite);

            // Get GitHub repository
            string user, repo;
            try
            {
                (user, repo) = GitHubDownloader.GetRepositoryName(options.Input);
            }
            catch (ArgumentException exc)
            {
                Logger.LogError(exc.Message);
                Debug.WriteLine(exc.ToString());
                Environment.Exit(1);
                return;
            }

            // Configure downloader
            GitHubDownloader downloader = new GitHubDownloader()
            {
                User = user,
                Repository = repo,
                DownloadSourceCode = options.Source,
                PreviousVersion = options.PreviousVersion
            };

            if (!options.Source)
            {
                try
                {
                    downloader.Pattern = new Regex(options.Pattern);
                }
                catch (Exception exc)
                {
                    Logger.LogError("Incorrect pattern: {0}", options.Pattern);
                    Debug.WriteLine(exc.ToString());
                    Environment.Exit(1);
                    return;
                }
            }

            downloader.OnDownloadProgressChanged += DownloadProgressed;

            // Start download
            DownloadResult result = Download(downloader);

            // Handle result
            WarnFileType(options.OutputFile, result.FileType);
            Save(result, options.OutputFile);
        }

        /// <summary>
        /// Parses input and attempts to download a mod from PlayStarbound.
        /// </summary>
        /// <param name="options">Command-line arguments</param>
        static void DownloadFromPSB(PSBOptions options)
        {
            Logger.LogInfo("= Starbound Mod Downloader");
            Logger.LogInfo("       URL: {0}", options.Input);
            Logger.LogInfo("    Output: {0}", options.OutputFile);
            Logger.LogInfo(" Overwrite: {0}", options.Overwrite);
            Logger.LogInfo("Downloading...");

            // Check arguments.
            ConfirmOutputFile(options.OutputFile, options.Overwrite);

            PlayStarboundDownloader downloader;
            try
            {
                downloader = new PlayStarboundDownloader()
                {
                    ResourceLink = options.Input,
                    SessionCookie = options.Cookie,
                    PreviousVersion = options.PreviousVersion
                };
            }
            catch (ArgumentException exc)
            {
                Logger.LogError(exc.Message);
                Environment.Exit(1);
                return;
            }

            downloader.OnDownloadProgressChanged += DownloadProgressed;

            // Start download
            DownloadResult result = Download(downloader);

            // Handle result
            WarnFileType(options.OutputFile, result.FileType);
            Save(result, options.OutputFile);
        }

        // Counter for DownloadProgressed
        static int counter = 0;

        /// <summary>
        /// Logs progress by writing the downloaded bytes to the console as KB.
        /// Progress is updated every 1024 calls (due to the speed at which WebClients report progress).
        /// Conflicts if other threads write to the console.
        /// </summary>
        /// <param name="bytes">Total bytes downloaded.</param>
        static void DownloadProgressed(long bytes)
        {
            if (counter++ % 1024 == 0)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write($"Downloaded: {bytes / 1024} KB");
            }
        }

        /// <summary>
        /// Checks if the output file is (or seems) valid.
        /// If the output file is invalid, logs the error and terminates the application.
        /// </summary>
        /// <param name="outputFile">Destination file path.</param>
        /// <param name="canOverwrite">May the file be overwritten?</param>
        static void ConfirmOutputFile(string outputFile, bool canOverwrite)
        {
            if (!canOverwrite && File.Exists(outputFile))
            {
                Logger.LogError("Output file already exists, and flag --overwrite is not set.");
                Environment.Exit(1);
            }

            if (!ValidFileLocation(outputFile, canOverwrite))
            {
                Logger.LogError("The given output file path is not valid, or could not be written to.");
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Downloads the resource.
        /// If the download fails, logs the error and terminates the application.
        /// </summary>
        /// <param name="downloader">Configured downloader.</param>
        /// <returns>Download result.</returns>
        static DownloadResult Download(IModDownloader downloader)
        {
            Console.CursorVisible = false;
            try
            {
                Task<DownloadResult> task = downloader.Download();
                task.Wait();
                DownloadResult result = task.Result;

                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine($"Downloaded: {result.TotalBytes / 1024} KB"); // Line break after written progress.
                Logger.LogInfo("Download complete.");

                Console.CursorVisible = true;
                return result;
            }
            catch (ArgumentException exc)
            {
                Console.CursorVisible = true;
                Logger.LogError("Failed to download file. Error: {0}", exc.Message);
                Debug.WriteLine(exc.ToString());
                Environment.Exit(1);
                return null;
            }
            catch (Exception exc)
            {
                Console.CursorVisible = true;
                Logger.LogError("Failed to download file. Error: {0}", exc.Message);
                Debug.WriteLine(exc.ToString());
                Environment.Exit(3);
                return null;
            }
        }

        /// <summary>
        /// Saves the result from memory to disk.
        /// If saving fails, logs the error and terminates the application.
        /// </summary>
        /// <param name="result">Download result from the IModDownloader.</param>
        /// <param name="outputPath">Path to save file to.</param>
        static void Save(DownloadResult result, string outputPath)
        {
            try
            {
                MemoryStream s = result.MemoryStream;
                using (var fs = File.Create(outputPath))
                {
                    s.Position = 0;
                    s.CopyTo(fs);
                }
                Logger.LogInfo("File saved to: {0}", outputPath);
            }
            catch (Exception exc)
            {
                Logger.LogError("Failed to save downloaded file. Error: {0}", exc.ToString());
                Debug.WriteLine(exc.ToString());
                Environment.Exit(4);
            }
        }

        /// <summary>
        /// Checks if the given path is a valid location to save a file to.
        /// </summary>
        /// <param name="path">Output file path.</param>
        /// <param name="overwrite">Can the file be overwritten to test for access?</param>
        /// <returns>True if a file can be written to the target path.</returns>
        static bool ValidFileLocation(string path, bool overwrite = false)
        {
            if (File.Exists(path)) return overwrite; // If the path is an existing file, we know it's valid right away.
            string dir = Path.GetDirectoryName(path);
            if (dir == "") dir = Directory.GetCurrentDirectory();
            if (!Directory.Exists(dir)) return false; // If the parent directory does not exist, we know it's invalid.
            if (Directory.Exists(path)) return false; // If the path is a directory itself, we know it's invalid.
            return true; // Parent directory exists and path is not a directory itself, it should be valid.
        }

        /// <summary>
        /// Warns if the downloaded file type does not match the extension in the output path.
        /// </summary>
        /// <param name="outputPath">Output file path.</param>
        /// <param name="fileType">Downloaded file type / content type.</param>
        static void WarnFileType(string outputPath, string fileType)
        {
            switch (fileType.ToLowerInvariant())
            {
                case "application/octet-stream":
                case "pak":
                case ".pak":
                    if (!outputPath.ToLowerInvariant().EndsWith(".pak"))
                        Logger.LogWarning("Downloaded a binary file, but output file does not end with '.pak'!");
                    break;
                case "application/zip":
                case "zip":
                case ".zip":
                    if (!outputPath.ToLowerInvariant().EndsWith(".zip"))
                        Logger.LogWarning("Downloaded a zip file, but output file does not end with '.zip'!");
                    break;
            }
        }
    }
}
