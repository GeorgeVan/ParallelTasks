using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

public delegate Task<string> GetContentsAsync(string address);

class Program
{
    static void Main()
    {
        Console.WriteLine("原始实现:");
        DownLoad(CachedDownloads.GetContentsAsync).Wait();
        Console.WriteLine("\r\n\r\n实现2:");
        DownLoad(CachedDownloads2.GetContentsAsync).Wait();
        Console.WriteLine("\r\n\r\n实现3:");
        DownLoad(CachedDownloads3.GetContentsAsync).Wait();

        Console.ReadKey();
    }

    static async Task DownLoad(GetContentsAsync GetContentsAsync)
    {
        // The URLs to download.
        string[] urls = new string[]
        {
         "http://msdn.microsoft.com",
         "http://www.contoso.com",
         "http://www.microsoft.com"
        };

        // Used to time download operations.
        Stopwatch stopwatch = new Stopwatch();

        // Compute the time required to download the URLs.
        stopwatch.Start();
        var downloads = from url in urls
                        select GetContentsAsync(url);
        await Task.WhenAll(downloads).ContinueWith(results =>
        {
            stopwatch.Stop();

            // Print the number of characters download and the elapsed time.
            Console.WriteLine("Retrieved {0} characters. Elapsed time was {1} ms.",
               results.Result.Sum(result => result.Length),
               stopwatch.ElapsedMilliseconds);
        }).ConfigureAwait(false);

        // Perform the same operation a second time. The time required
        // should be shorter because the results are held in the cache.
        stopwatch.Restart();
        downloads = from url in urls
                    select GetContentsAsync(url);
        await Task.WhenAll(downloads).ContinueWith(results =>
        {
            stopwatch.Stop();

            // Print the number of characters download and the elapsed time.
            Console.WriteLine("Retrieved {0} characters. Elapsed time was {1} ms.",
               results.Result.Sum(result => result.Length),
               stopwatch.ElapsedMilliseconds);
        }).ConfigureAwait(false);
    }
}
//https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-create-pre-computed-tasks
// Demonstrates how to use Task<TResult>.FromResult to create a task 
// that holds a pre-computed result.
class CachedDownloads
{
    // Holds the results of download operations.
    static ConcurrentDictionary<string, string> cachedDownloads =
       new ConcurrentDictionary<string, string>();

    // Asynchronously downloads the requested resource as a string.
    public static Task<string> GetContentsAsync(string address)
    {
        // First try to retrieve the content from cache.
        string content;
        if (cachedDownloads.TryGetValue(address, out content))
        {
            return Task.FromResult<string>(content);
        }

        // If the result was not in the cache, download the 
        // string and add it to the cache.
        return Task.Run(async () =>
        {
            content = await new WebClient().DownloadStringTaskAsync(address).ConfigureAwait(false);
            cachedDownloads.TryAdd(address, content);
            return content;
        });
    }



}

/* Sample output:
Retrieved 27798 characters. Elapsed time was 1045 ms.
Retrieved 27798 characters. Elapsed time was 0 ms.
*/


//https://msdn.microsoft.com/en-us/magazine/hh456402.aspx
class CachedDownloads2
{
    private static ConcurrentDictionary<string, string> s_urlToContents = new ConcurrentDictionary<string, string>();
    public static async Task<string> GetContentsAsync(string url)
    {
        string contents;
        if (!s_urlToContents.TryGetValue(url, out contents))
        {
            var response = await new HttpClient().GetAsync(url).ConfigureAwait(false);
            contents = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false);
            s_urlToContents.TryAdd(url, contents);
        }
        return contents;
    }
}


//https://msdn.microsoft.com/en-us/magazine/hh456402.aspx
class CachedDownloads3
{
    private static ConcurrentDictionary<string, Task<string>> s_urlToContents = new ConcurrentDictionary<string, Task<string>>();
    public static Task<string> GetContentsAsync(string url)
    {
        Task<string> contents;
        if (!s_urlToContents.TryGetValue(url, out contents))
        {
            contents = GetContentsInternalAsync(url);
            contents.ContinueWith(delegate
            {
                s_urlToContents.TryAdd(url, contents);
            }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnRanToCompletion |
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        }
        return contents;
    }
    private static async Task<string> GetContentsInternalAsync(string url)
    {
        var response = await new HttpClient().GetAsync(url).ConfigureAwait(false);
        return await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false);
    }
}
