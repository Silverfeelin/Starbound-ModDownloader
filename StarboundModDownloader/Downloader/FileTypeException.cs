using System;

namespace StarboundModDownloader.Downloader
{
    public class FileTypeException : Exception
    {
        public FileTypeException() : base() { }
        public FileTypeException(string message) : base(message) { }
    }
}
