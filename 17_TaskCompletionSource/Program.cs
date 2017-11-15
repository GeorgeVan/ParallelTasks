using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class TCSDemo
{
    // Demonstrated features:
    // 		TaskCompletionSource ctor()
    // 		TaskCompletionSource.SetResult()
    // 		TaskCompletionSource.SetException()
    //		Task.Result
    // Expected results:
    // 		The attempt to get t1.Result blocks for ~1000ms until tcs1 gets signaled. 15 is printed out.
    // 		The attempt to get t2.Result blocks for ~1000ms until tcs2 gets signaled. An exception is printed out.

    static void Main()
    {
        //Main0();
        Console.WriteLine("\r\n\r\n\r\n");

        //Main1和Main3是完全异步程序的一个例子。
        //Main1();
        Console.WriteLine("\r\n\r\n\r\n");
        //Main3();

        Console.WriteLine("\r\n\r\n\r\n");
        Main4();
        Console.ReadKey();
    }

    static void Main0()
    {
        TaskCompletionSource<int> tcs1 = new TaskCompletionSource<int>();
        Task<int> t1 = tcs1.Task;

        // Start a background task that will complete tcs1.Task
        Task.Factory.StartNew(() =>
        {
            Thread.Sleep(1000);
            tcs1.SetResult(15);
        });

        // The attempt to get the result of t1 blocks the current thread until the completion source gets signaled.
        // It should be a wait of ~1000 ms.
        Stopwatch sw = Stopwatch.StartNew();
        int result = t1.Result;
        sw.Stop();

        Console.WriteLine("(ElapsedTime={0}): t1.Result={1} (expected 15) ", sw.ElapsedMilliseconds, result);

        // ------------------------------------------------------------------

        // Alternatively, an exception can be manually set on a TaskCompletionSource.Task
        TaskCompletionSource<int> tcs2 = new TaskCompletionSource<int>();
        Task<int> t2 = tcs2.Task;

        // Start a background Task that will complete tcs2.Task with an exception
        Task.Factory.StartNew(() =>
        {
            Thread.Sleep(1000);
            tcs2.SetException(new InvalidOperationException("SIMULATED EXCEPTION"));
        });

        // The attempt to get the result of t2 blocks the current thread until the completion source gets signaled with either a result or an exception.
        // In either case it should be a wait of ~1000 ms.
        sw = Stopwatch.StartNew();
        try
        {
            result = t2.Result;

            Console.WriteLine("t2.Result succeeded. THIS WAS NOT EXPECTED.");
        }
        catch (AggregateException e)
        {
            Console.Write("(ElapsedTime={0}): ", sw.ElapsedMilliseconds);
            Console.WriteLine("The following exceptions have been thrown by t2.Result: (THIS WAS EXPECTED)");
            for (int j = 0; j < e.InnerExceptions.Count; j++)
            {
                Console.WriteLine("\n-------------------------------------------------\n{0}", e.InnerExceptions[j].ToString());
            }
        }
    }

    static void Main1()
    {
        Main1Async().GetAwaiter().GetResult();
    }

    static async Task Main1Async()
    {
        TaskCompletionSource<int> tcs1 = new TaskCompletionSource<int>();
        Task<int> t1 = tcs1.Task;

        // Start a background task that will complete tcs1.Task
        Action backAction = async delegate { await Task.Delay(1000).ConfigureAwait(false); tcs1.SetResult(15); };
        backAction();

        // The attempt to get the result of t1 blocks the current thread until the completion source gets signaled.
        // It should be a wait of ~1000 ms.
        Stopwatch sw = Stopwatch.StartNew();
        int result = await t1.ConfigureAwait(false);
        sw.Stop();

        Console.WriteLine("(ElapsedTime={0}): t1.Result={1} (expected 15) ", sw.ElapsedMilliseconds, result);

        // ------------------------------------------------------------------

        // Alternatively, an exception can be manually set on a TaskCompletionSource.Task
        TaskCompletionSource<int> tcs2 = new TaskCompletionSource<int>();
        Task<int> t2 = tcs2.Task;

        // Start a background Task that will complete tcs2.Task with an exception
        backAction = async delegate 
        {
            await Task.Delay(1000).ConfigureAwait(false);
            tcs2.SetException(new InvalidOperationException("SIMULATED EXCEPTION"));
        };
        backAction();

        // The attempt to get the result of t2 blocks the current thread until the completion source gets signaled with either a result or an exception.
        // In either case it should be a wait of ~1000 ms.
        sw = Stopwatch.StartNew();
        try
        {
            result = await t2.ConfigureAwait(false);
            Console.WriteLine("t2.Result succeeded. THIS WAS NOT EXPECTED.");
        }
        catch (InvalidOperationException e)
        {
            Console.Write("(ElapsedTime={0}): ", sw.ElapsedMilliseconds);
            Console.WriteLine("The following exceptions have been thrown by t2.Result: (THIS WAS EXPECTED)");
            Console.WriteLine("\n-------------------------------------------------\n{0}", e);
        }
    }

    static void Main3()
    {
        Main3Async().GetAwaiter().GetResult();
        Main3AsynEx().GetAwaiter().GetResult();
    }

    static async Task Main3Async()
    {
        Func<Task> job = async delegate
        {
            Console.WriteLine("IN @ " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(1000).ConfigureAwait(false);
            Console.WriteLine("OUT @ " + Thread.CurrentThread.ManagedThreadId);
        };

        List<Task> tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(job());
        }
        await Task.WhenAll(tasks);

    }
    static async Task Main3AsynEx() { 
        Func<int,Task<int>> job1 = async (i)=>
        {
            Console.WriteLine("IN @ " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(3000).ConfigureAwait(false);
            Console.WriteLine("OUT @ " + Thread.CurrentThread.ManagedThreadId);
            return i*i;
        };

        List<Task<int>> tasks1 = new List<Task<int>>();
        for (int i = 0; i < 10; i++)
        {
            tasks1.Add(job1(i));
        }
        Console.WriteLine("Start await task.whenall");
        await Task.WhenAll(tasks1);
        Console.WriteLine("awaited");

        foreach (Task<int> t in tasks1)
        {
            Console.WriteLine(t.Result);
        }
    }

    static void Main4()
    {
        Main4Async().GetAwaiter().GetResult();
    }

    //测试多个任务等待一个任务
    async static Task Main4Async()
    {
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        Func<Task> job = async delegate
        {
            Console.WriteLine(stopwatch.Elapsed.Seconds + ": IN @ " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(2000).ConfigureAwait(false);
            Console.WriteLine(stopwatch.Elapsed.Seconds + ": OUT @ " + Thread.CurrentThread.ManagedThreadId);
        };


        Task sharedTask = job();

        Func<int,Task> job1 = async (i)=>
        {
            Console.WriteLine(stopwatch.Elapsed.Seconds + ": await sharedTask # " + i +" @ "+Thread.CurrentThread.ManagedThreadId );
            await sharedTask.ConfigureAwait(false);
            Console.WriteLine(stopwatch.Elapsed.Seconds + ": Waited # " + i + " @ " + Thread.CurrentThread.ManagedThreadId);
        };

        await Task.WhenAll(Enumerable.Range(1,10).Select(job1));
    }
}