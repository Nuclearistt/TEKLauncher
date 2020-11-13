using System;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static System.Math;
using static System.Net.WebRequest;
using static System.Text.Encoding;
using static System.Threading.Tasks.Task;
using static System.Windows.Media.Brushes;
using static TEKLauncher.Data.Links;
using static TEKLauncher.Utils.UtilFunctions;

namespace TEKLauncher.Windows
{
    public partial class CrashWindow : Window
    {
        internal CrashWindow(Exception Error)
        {
            InitializeComponent();
            this.Error = Error;
            Type.Text = Error.GetType().ToString();
            if (Error is FileNotFoundException FNFE && FNFE.Message.Contains("System.") || Error is MissingMethodException)
            {
                IsFrameworkCrash = true;
                Message.Text = "This error occured because you don't have .NET Framework 4.8 installed. Get it ";
                Hyperlink Link = new Hyperlink { Foreground = (SolidColorBrush)FindResource("CyanBrush"), NavigateUri = new Uri(DotNETFramework) };
                Link.Inlines.Add("here");
                Link.Click += FollowLink;
                Message.Inlines.Add(Link);
                Message.Inlines.Add(", install and try again");
            }
            else
                Message.Text = Error.Message;
            StackTrace.Text = Error.StackTrace;
        }
        private readonly bool IsFrameworkCrash = false;
        private readonly Exception Error;
        private void Close(object Sender, RoutedEventArgs Args) => Close();
        private void Drag(object Sender, MouseButtonEventArgs Args) => DragMove();
        private void FollowLink(object Sender, RoutedEventArgs Args) => Execute(((Hyperlink)Sender).NavigateUri.ToString());
        private async void LoadedHandler(object Sender, RoutedEventArgs Args)
        {
            if (IsFrameworkCrash)
                Status.Text = string.Empty;
            else if (!(Error is COMException || Error is NotImplementedException) && await Run(UploadCrash))
            {
                Status.Foreground = DarkGreen;
                Status.Text = "Crash data was automatically sent to developer";
            }
            else
            {
                Status.Foreground = (SolidColorBrush)FindResource("BrightGrayBrush");
                Status.Text = "Please post screenshot of this window in ";
                Hyperlink Link = new Hyperlink { Foreground = (SolidColorBrush)FindResource("CyanBrush"), NavigateUri = new Uri(SupportChannel) };
                Link.Inlines.Add("our Discord");
                Link.Click += FollowLink;
                Status.Inlines.Add(Link);
            }
        }
        private bool UploadCrash()
        {
            HttpWebRequest Request = CreateHttp(CrashReporterWebhook);
            Request.ContentType = "application/json";
            Request.Method = "POST";
            Request.Timeout = 4000;
            try
            {
                byte[] Content = UTF8.GetBytes($@"{{""content"":""**Version:** {App.Version}\n**Type:** {Error.GetType()}\n**Message:**\n{Error.Message}\n**Stack trace:**\n{Error.StackTrace.Replace(@"\", @"\\").Replace("\n", @"\n").Replace("\r", @"\r").Replace("\"", @"\""")}""}}");
                using (Stream RequestStream = Request.GetRequestStream())
                    RequestStream.Write(Content, 0, Min(2000, Content.Length));
                Request.GetResponse().Dispose();
                return true;
            }
            catch { return false; }
        }
    }
}