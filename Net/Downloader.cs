using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TEKLauncher.Data;
using static System.Net.WebRequest;
using static System.Text.Encoding;
using static System.Threading.Tasks.Task;
using static System.Windows.Application;

namespace TEKLauncher.Net
{
    internal class Downloader
    {
        internal Downloader(Progress Progress = null) => this.Progress = Progress;
        internal DownloadBeganEventHandler DownloadBegan;
        private readonly byte[] Buffer = new byte[8192];
        private readonly Progress Progress;
        internal delegate void DownloadBeganEventHandler();
        private void DownloadFile(string FilePath, string URL)
        {
            HttpWebRequest Request = CreateHttp(URL);
            Request.ReadWriteTimeout = Request.Timeout = 14000;
            using (HttpWebResponse Response = (HttpWebResponse)Request.GetResponse())
            using (Stream ResponseStream = Response.GetResponseStream())
            using (FileStream Writer = File.Create(FilePath))
            {
                long ContentLength = Response.ContentLength;
                if (!(Progress is null))
                    Progress.Total = ContentLength;
                int BytesRead = ResponseStream.Read(Buffer, 0, 8192);
                if (!(DownloadBegan is null))
                    Current.Dispatcher.Invoke(DownloadBegan);
                while (BytesRead != 0)
                {
                    Writer.Write(Buffer, 0, BytesRead);
                    Progress?.Increase(BytesRead);
                    BytesRead = ResponseStream.Read(Buffer, 0, 8192);
                }
                if (ContentLength > 0L && Writer.Length != ContentLength)
                    throw new WebException("Incomplete download");
            }
        }
        private bool TryDownloadFile(object Args)
        {
            object[] ArgsArray = (object[])Args;
            string FilePath = (string)ArgsArray[0];
            foreach (string URL in (string[])ArgsArray[1])
                try
                {
                    DownloadFile(FilePath, URL);
                    return true;
                }
                catch { }
            return false;
        }
        private static string DownloadString(string URL) => UTF8.GetString(DownloadData(URL));
        internal bool TryDownloadFile(string FilePath, params string[] URLs) => TryDownloadFile(new object[] { FilePath, URLs });
        internal Task<bool> TryDownloadFileAsync(string FilePath, params string[] URLs) => Factory.StartNew(TryDownloadFile, new object[] { FilePath, URLs });
        private static byte[] DownloadData(string URL)
        {
            HttpWebRequest Request = CreateHttp(URL);
            Request.ReadWriteTimeout = Request.Timeout = 14000;
            using (HttpWebResponse Response = (HttpWebResponse)Request.GetResponse())
            using (Stream ResponseStream = Response.GetResponseStream())
            {
                int ContentLength = (int)Response.ContentLength;
                if (ContentLength == -1)
                    using (MemoryStream Writer = new MemoryStream())
                    {
                        byte[] Buffer = new byte[8192];
                        int BytesRead;
                        while ((BytesRead = ResponseStream.Read(Buffer, 0, 8192)) != 0)
                            Writer.Write(Buffer, 0, BytesRead);
                        return Writer.ToArray();
                    }
                else
                {
                    byte[] Data = new byte[ContentLength];
                    int Offset = 0;
                    while (Offset < ContentLength)
                    {
                        int BytesToRead = ContentLength - Offset;
                        if (BytesToRead > 8192)
                            BytesToRead = 8192;
                        int BytesRead = ResponseStream.Read(Data, Offset, BytesToRead);
                        if (BytesRead == 0)
                            throw new WebException("Incomplete download");
                        Offset += BytesRead;
                    }
                    return Data;
                }
            }
        }
        private static string TryDownloadString(object URLs)
        {
            foreach (string URL in (string[])URLs)
            {
                try { return DownloadString(URL); }
                catch { continue; }
            }
            return null;
        }
        internal static byte[] TryDownloadData(params string[] URLs)
        {
            foreach (string URL in URLs)
            {
                try { return DownloadData(URL); }
                catch { continue; }
            }
            return null;
        }
        internal static string TryDownloadString(params string[] URLs) => TryDownloadString((object)URLs);
        internal static async Task<byte[]> DownloadSteamChunk(string BaseURL, string GID, int Size, CancellationToken Token)
        {
            HttpWebRequest Request = CreateHttp($"{BaseURL}chunk/{GID}");
            Request.ReadWriteTimeout = Request.Timeout = 14000;
            using (HttpWebResponse Response = (HttpWebResponse)await Request.GetResponseAsync())
            using (Stream ResponseStream = Response.GetResponseStream())
            {
                byte[] Data = new byte[Size];
                int Offset = 0;
                while (Offset < Size)
                {
                    int BytesToRead = Size - Offset;
                    if (BytesToRead > 8192)
                        BytesToRead = 8192;
                    int BytesRead = await ResponseStream.ReadAsync(Data, Offset, BytesToRead, Token);
                    if (BytesRead == 0)
                        throw new WebException("Incomplete download");
                    Offset += BytesRead;
                }
                return Data;
            }
        }
        internal static Task<string> TryDownloadStringAsync(params string[] URLs) => Factory.StartNew(TryDownloadString, URLs);
    }
}