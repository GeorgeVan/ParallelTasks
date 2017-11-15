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

        Console.WriteLine("\r\n\r\n实现4:");
        DownLoad(CachedDownloads4.GetContentsAsync).Wait();

        Console.WriteLine("\r\n\r\n实现5:");
        DownLoad(CachedDownloads5.GetContentsAsync).Wait();

        Console.WriteLine("\r\n\r\n实现6:");
        Console.WriteLine("Main @" + Thread.CurrentThread.ManagedThreadId);
        DownLoad(CachedDownloads6.GetContentsAsync).Wait();
        Console.ReadKey();
    }

    static async Task DownLoadInner(GetContentsAsync GetContentsAsync)
    {
        string[] urls = new string[]
        {
         "http://msdn.microsoft.com",
         "http://www.contoso.com",
         "http://www.microsoft.com"
        };

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        var results = await Task.WhenAll(urls.Select(url => GetContentsAsync(url))).ConfigureAwait(false);
        stopwatch.Stop();

        Console.WriteLine("Retrieved {0} characters. Elapsed time was {1} ms.",
            results.Sum(result => result.Length),
            stopwatch.ElapsedMilliseconds);
    }

    static async Task DownLoad(GetContentsAsync GetContentsAsync)
    {
        await DownLoadInner(GetContentsAsync).ConfigureAwait(false);
        await DownLoadInner(GetContentsAsync).ConfigureAwait(false);

        //await Task.WhenAll(new Task[]{ DownLoadInner(GetContentsAsync), DownLoadInner(GetContentsAsync)});
        //如果用了WhenAll，则两个会同时执行，而无法利用Cahche的结果。
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
    public async static Task<string> GetContentsAsync(string address)
    {
        // First try to retrieve the content from cache.
        string content;
        if (!cachedDownloads.TryGetValue(address, out content))
        {
            // If the result was not in the cache, download the 
            // string and add it to the cache.
            content = await new WebClient().DownloadStringTaskAsync(address).ConfigureAwait(false);
            cachedDownloads.TryAdd(address, content);
        }

        return content;
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
                //这里用ContinueWith的目的在于只有当执行成功后，才可以把这个任务放到字典里面
            }, CancellationToken.None,
            TaskContinuationOptions.OnlyOnRanToCompletion |
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
        }
        return contents;
    }
    //比较上下两个函数可知普通的Task<string>函数必须明确返回一个Task<string>实例；
    //而async函数的返回值缺省string；系统会自动包装一个Task<string>
    //await会将后面函数的返回值Task<string>解开为string

    private static async Task<string> GetContentsInternalAsync(string url)
    {
        var response = await new HttpClient().GetAsync(url).ConfigureAwait(false);
        string content = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false);
        return content;
    }
}
class CachedDownloads4
{
    private static ConcurrentDictionary<string, Task<string>> s_urlToContents = new ConcurrentDictionary<string, Task<string>>();
    public static Task<string> GetContentsAsync(string url)
    {
        Task<string> contents;
        if (!s_urlToContents.TryGetValue(url, out contents) || contents.IsFaulted )
        {
            Task<string>  contents1 = GetContentsInternalAsync(url);
            if(contents==null)
                s_urlToContents.TryAdd(url, contents1);
            else
                //这个版本直接加上去，如果错了，重新再来新的
                s_urlToContents.TryUpdate(url, contents,contents1);
            contents = contents1;
        }
        return contents;
    }

    private static async Task<string> GetContentsInternalAsync(string url)
    {
        var response = await new HttpClient().GetAsync(url).ConfigureAwait(false);
        string content = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false);
        return content;
    }
}

class CachedDownloads5
{
    private static ConcurrentDictionary<string, Task<string>> s_urlToContents = new ConcurrentDictionary<string, Task<string>>();
    public static Task<string> GetContentsAsync(string url)
    {
        Task<string> contents;
        if (!s_urlToContents.TryGetValue(url, out contents))
        {
            contents = GetContentsInternalAsync(url);
        }
        return contents;
    }
    //比较上下两个函数可知普通的Task<string>函数必须明确返回一个Task<string>实例；
    //而async函数的返回值缺省string；系统会自动包装一个Task<string>
    //await会将后面函数的返回值Task<string>解开为string

    private static async Task<string> GetContentsInternalAsync(string url)
    {
        var response = await new HttpClient().GetAsync(url).ConfigureAwait(false);
        string content = await response.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false);
        s_urlToContents.TryAdd(url, Task.FromResult(content));
        return content;
        //这个版本在成功后，创建一个新的DummyTask，除此之外和V3一样的特征。
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
            Console.WriteLine("Before call @" + Thread.CurrentThread.ManagedThreadId);
            GetContentsAsync GetContentsAsyncInner  = async (u) => {
                Console.WriteLine("async {@" + Thread.CurrentThread.ManagedThreadId);
                var content = await new WebClient().DownloadStringTaskAsync(url);
                //var content = await new WebClient().DownloadStringTaskAsync(url).ConfigureAwait(false);
                Console.WriteLine("async }@" + Thread.CurrentThread.ManagedThreadId);
                s_urlToContents.TryAdd(url, Task.FromResult(content));
                return content;
            };
            //这个版本试验直接在里面定义async lambda
            contents = GetContentsAsyncInner(url);
        }
        return contents;
    }
}