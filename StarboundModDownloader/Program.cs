using CommandLine;
using StarboundModDownloader.Downloader;
using System;
using System.IO;

namespace StarboundModDownloader
{
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "Mod URL. For PlayStarbound, use the link to the mod overview. For GitHub, use the GitHub repository link.")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file location. Directories leading up to the file must exist.")]
        public string OutputFile { get; set; }

        [Option('c', "cookie", Required = false, HelpText = "PlayStarbound session cookie (xf2_session), used to download from community.playstarbound.com.")]
        public string Cookie { get; set; }

        [Option("overwrite", Required = false, HelpText = "Overwrite output file, if the file already exists.")]
        public bool Overwrite { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(opts => Run(opts));
        }

        static void Run(Options options)
        {
            if (!ValidFileLocation(options.OutputFile))
            {
                Console.WriteLine("Output file is not a valid file path, or the directories leading to the file do not exist.");
                Environment.Exit(1);
                return;
            }

            if (!options.Overwrite && File.Exists(options.OutputFile))
            {
                Console.WriteLine("File already exists and flag '--overwrite' is not set.");
                Environment.Exit(1);
                return;
            }

            try
            {
                DownloadType type = LinkHelper.GetDownloadType(options.Input);
                Uri uri = new Uri(options.Input);

                Console.WriteLine("Downloading from {0}...", type);

                IModDownloader downloader = GetDownloader(type, options);
                DownloadResult result = Download(downloader, uri);
                WarnFileType(options.OutputFile, result.FileType);
                Save(result, options.OutputFile);
            }
            catch (ArgumentException exc)
            {
                Console.WriteLine(exc.Message);
                Environment.Exit(1);
            }
            catch (FileTypeException exc)
            {
                Console.WriteLine(exc.Message);
                Environment.Exit(1);
            }
            catch (Exception exc)
            {
                Console.WriteLine(exc.ToString());
                Environment.Exit(1);
            }
        }

        static IModDownloader GetDownloader(DownloadType type, Options options)
        {
            switch (type)
            {
                case DownloadType.GitHub:
                    return new GitHubDownloader();
                default:
                case DownloadType.PlayStarbound:
                    if (string.IsNullOrWhiteSpace(options.Cookie))
                    {
                        throw new ArgumentException("Flag 'c' not defined. Use --help for more information.");
                    }

                    return new PlayStarboundDownloader(options.Cookie);
            }
        }

        /// <summary>
        /// Downloads a mod using the configured downloader.
        /// </summary>
        /// <param name="downloader">Configured downloader.</param>
        /// <param name="uri">Download location.</param>
        static DownloadResult Download(IModDownloader downloader, Uri uri)
        {
            var task = downloader.Download(uri);
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Saves the file to disk.
        /// </summary>
        /// <param name="result">Download result from the IModDownloader.</param>
        /// <param name="outputPath">Path to save file to.</param>
        static void Save(DownloadResult result, string outputPath)
        {
            MemoryStream s = result.MemoryStream;
            using (var fs = File.Create(outputPath))
            {
                s.Position = 0;
                s.CopyTo(fs);
            }
        }

        static bool ValidFileLocation(string path)
        {
            if (File.Exists(path)) return true; // If the path is an existing file, we know it's valid right away.
            string dir = Path.GetDirectoryName(path);
            if (dir == "") dir = Directory.GetCurrentDirectory();
            if (!Directory.Exists(dir)) return false; // If the parent directory does not exist, we know it's invalid.
            if (Directory.Exists(path)) return false; // If the path is a directory itself, we know it's invalid.
            return true; // Parent directory exists and path is not a directory itself, it should be valid.
        }

        static void WarnFileType(string outputPath, string fileType)
        {
            switch (fileType.ToLowerInvariant())
            {
                case "application/octet-stream":
                    if (!outputPath.ToLowerInvariant().EndsWith(".pak"))
                        Console.WriteLine("Warning: Downloaded file is a binary file, but output file does not end with '.pak'!");
                    break;
                case "application/zip":
                    if (!outputPath.ToLowerInvariant().EndsWith(".zip"))
                        Console.WriteLine("Warning: Downloaded file is a zip file, but output file does not end with '.zip'!");
                        break;
            }
        }
    }
}
