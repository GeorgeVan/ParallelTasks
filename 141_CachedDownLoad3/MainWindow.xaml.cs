using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace _141_CachedDownLoad3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            _mainWindow = this;
        }

        private async void buttonStart_Click(object sender, RoutedEventArgs e)
        {
            await Program.DownLoad(CachedDownloads6.GetContentsAsync);
        }
        private static MainWindow _mainWindow;

        public static void WriteLine(string s)
        {
            _mainWindow.listBoxMsg.Items.Add(s);
        }
    }

    public class Program { 
        static async Task DownLoadInner(Func<string,Task<string>> GetContentsAsync)
        {
            string[] urls = new string[]
            {
             "http://msdn.microsoft.com",
             "http://www.contoso.com",
             "http://www.microsoft.com"
            };

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var results = await Task.WhenAll(urls.Select(url => GetContentsAsync(url)));
            stopwatch.Stop();

            MainWindow.WriteLine(String.Format("Retrieved {0} characters. Elapsed time was {1} ms.",
                results.Sum(result => result.Length),
                stopwatch.ElapsedMilliseconds));
        }

        public static async Task DownLoad(Func<string, Task<string>> GetContentsAsync)
        {
            MainWindow.WriteLine("Main @" + Thread.CurrentThread.ManagedThreadId);
            await DownLoadInner(GetContentsAsync);
            await DownLoadInner(GetContentsAsync);
        }
    }
    class CachedDownloads6
    {
        private static ConcurrentDictionary<string, Task<string>> s_urlToContents = new ConcurrentDictionary<string, Task<string>>();
        public static Task<string> GetContentsAsync(string url)
        {
            Task<string> contents;
            if (!s_urlToContents.TryGetValue(url, out contents))
            {
                MainWindow.WriteLine("Before call @" + Thread.CurrentThread.ManagedThreadId);
                Func<string, Task<string>> GetContentsAsyncInner = async (u) => {
                    MainWindow.WriteLine("async {@" + Thread.CurrentThread.ManagedThreadId);
                    var content = await new WebClient().DownloadStringTaskAsync(url);
                    MainWindow.WriteLine("async }@" + Thread.CurrentThread.ManagedThreadId);
                    s_urlToContents.TryAdd(url, Task.FromResult(content));
                    return content;
                };
                //这个版本试验直接在里面定义async lambda
                contents = GetContentsAsyncInner(url);
            }
            return contents;
        }
    }
}
