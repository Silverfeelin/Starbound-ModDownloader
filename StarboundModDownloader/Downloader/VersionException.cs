using System;

namespace StarboundModDownloader.Downloader
{
    public class VersionException : Exception
    {
        public VersionException() : base() { }
        public VersionException(string message) : base(message) { }
    }
}
