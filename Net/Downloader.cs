using System.IO;
using System.Net;
using System.Threading.Tasks;
using TEKLauncher.Data;
using static System.Math;
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
        private readonly byte[] Buffer = new byte[2048];
        private readonly Progress Progress;
        internal delegate void DownloadBeganEventHandler();
        private void DownloadFile(string FilePath, string URL)
        {
            HttpWebRequest Request = CreateHttp(URL);
            Request.Timeout = 7000;
            using (HttpWebResponse Response = (HttpWebResponse)Request.GetResponse())
            using (Stream ResponseStream = Response.GetResponseStream())
            using (FileStream Writer = File.Create(FilePath))
            {
                if (!(Progress is null))
                    Progress.Total = Response.ContentLength;
                int BytesRead = ResponseStream.Read(Buffer, 0, 2048);
                if (!(DownloadBegan is null))
                    Current.Dispatcher.Invoke(DownloadBegan);
                while (BytesRead != 0)
                {
                    Writer.Write(Buffer, 0, BytesRead);
                    Progress?.Increase(BytesRead);
                    BytesRead = ResponseStream.Read(Buffer, 0, 2048);
                }
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
        private byte[] DownloadData(string URL)
        {
            HttpWebRequest Request = CreateHttp(URL);
            Request.Timeout = 7000;
            using (HttpWebResponse Response = (HttpWebResponse)Request.GetResponse())
            using (Stream ResponseStream = Response.GetResponseStream())
            {
                int ContentLength = (int)Response.ContentLength;
                if (ContentLength == -1)
                    using (MemoryStream Writer = new MemoryStream())
                    {
                        int BytesRead = ResponseStream.Read(Buffer, 0, 2048);
                        while (BytesRead != 0)
                        {
                            Writer.Write(Buffer, 0, BytesRead);
                            BytesRead = ResponseStream.Read(Buffer, 0, 2048);
                        }
                        return Writer.ToArray();
                    }
                else
                {
                    byte[] Data = new byte[ContentLength];
                    int Offset = ResponseStream.Read(Data, 0, Min(ContentLength, 2048));
                    while (Offset != ContentLength)
                        Offset += ResponseStream.Read(Data, Offset, Min(ContentLength - Offset, 2048));
                    return Data;
                }
            }
        }
        private string DownloadString(string URL) => UTF8.GetString(DownloadData(URL));
        private string TryDownloadString(object URLs)
        {
            foreach (string URL in (string[])URLs)
            {
                try { return DownloadString(URL); }
                catch { continue; }
            }
            return null;
        }
        internal bool TryDownloadFile(string FilePath, params string[] URLs) => TryDownloadFile(new object[] { FilePath, URLs });
        internal byte[] TryDownloadData(params string[] URLs)
        {
            foreach (string URL in URLs)
            {
                try
                { return DownloadData(URL); }
                catch { continue; }
            }
            return null;
        }
        internal string TryDownloadString(params string[] URLs) => TryDownloadString((object)URLs);
        internal Task<bool> TryDownloadFileAsync(string FilePath, params string[] URLs) => Factory.StartNew(TryDownloadFile, new object[] { FilePath, URLs });
        internal Task<string> TryDownloadStringAsync(params string[] URLs) => Factory.StartNew(TryDownloadString, URLs);
    }
}