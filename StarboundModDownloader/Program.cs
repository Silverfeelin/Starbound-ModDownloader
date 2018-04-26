﻿using CommandLine;
using StarboundModDownloader.Downloader;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StarboundModDownloader
{
    [Verb("github", HelpText = "Download asset or source code from GitHub.")]
    public class GitHubOptions
    {
        [Option('i', "input", Required = true, HelpText = "Resource URL. For example, https://community.playstarbound.com/resources/wardrobe.3704/")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file location. Directories leading up to the file must exist.")]
        public string OutputFile { get; set; }

        [Option("overwrite", Required = false, HelpText = "Overwrite output file, if the file already exists.")]
        public bool Overwrite { get; set; }

        [Option('p', "pattern", Required = false, HelpText = "Pattern for GitHub release assets. Use when omitting flag -s.")]
        public string Pattern { get; set; }

        [Option('s', "source", Required = false, HelpText = "Download source code instead of asset matching pattern.")]
        public bool Source { get; set; }
    }

    [Verb("psb", HelpText = "Download a mod from PlayStarbound.")]
    public class PSBOptions
    {
        [Option('i', "input", Required = true, HelpText = "Resource URL. For example, https://community.playstarbound.com/resources/wardrobe.3704/")]
        public string Input { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output file location. Directories leading up to the file must exist.")]
        public string OutputFile { get; set; }

        [Option("overwrite", Required = false, HelpText = "Overwrite output file, if the file already exists.")]
        public bool Overwrite { get; set; }

        [Option('s', "session", Required = true, HelpText = "Session cookie (xf2_session), used to access the resource.")]
        public string Cookie { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<GitHubOptions, PSBOptions>(args)
                .WithParsed<GitHubOptions>(gh => { })
                .WithParsed<PSBOptions>(psb => { });
        }

        static void DownloadFromGitHub(GitHubOptions options)
        {
            string url = options.Input;
            GitHubDownloader downloader = new GitHubDownloader()
            {
                
            };
        }

        static void DownloadFromPSB(PSBOptions options)
        {
            PlayStarboundDownloader downloader = new PlayStarboundDownloader()
            {
                ResourceUri = new Uri(options.Input),
                SessionCookie = options.Cookie
            };

            Task<DownloadResult> task = downloader.Download();
            task.Wait();
            DownloadResult result = task.Result;
        }

        static void Run()
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

                Console.WriteLine("Downloading from {0}. This may take a while...", type);
                IModDownloader downloader = GetDownloader(type, options);
                DownloadResult result = Download(downloader, uri);

                Console.WriteLine("Saving to {0}", options.OutputFile);
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
                    Regex repoRegex = new Regex("(?:https:\\/\\/)?github\\.com\\/([\\w-]+)\\/([\\w-]+)");
                    Match match = repoRegex.Match(options.Input);
                    if (!match.Success)
                    {
                        throw new ArgumentException($"GitHub repository could not be parsed from '{options.Input}'.");
                    }
                    var b = DownloaderBuilder.BuildGitHubDownloader(
                        match.Groups[1].Value,
                        match.Groups[2].Value,
                        !options.Source ? new Regex(options.Pattern) : null);
                    b.DownloadProgressChangedEventHandler = DownloadProgressed;
                    return b;
                default:
                case DownloadType.PlayStarbound:
                    if (string.IsNullOrWhiteSpace(options.Cookie))
                    {
                        throw new ArgumentException("Flag '--cookie' not defined. Use --help for more information.");
                    }

                    return DownloaderBuilder.BuildPSBDownloader(new Uri(options.Input), options.Cookie);
            }
        }

        static int counter = 0;
        static void DownloadProgressed(object sender, DownloadProgressChangedEventArgs args)
        {
            counter++;

            if (counter % 1024 == 0)
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.Write(args.BytesReceived / 1024 + " KB");
            }
        }

        /// <summary>
        /// Downloads a mod using the configured downloader.
        /// </summary>
        /// <param name="downloader">Configured downloader.</param>
        /// <param name="uri">Download location.</param>
        static DownloadResult Download(IModDownloader downloader, Uri uri)
        {
            Console.CursorVisible = false;
            var task = downloader.Download();
            task.Wait();
            Console.CursorVisible = true;
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
