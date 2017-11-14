using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;


//这里演示了main函数的await缺省会随意使用一个线程
//https://blogs.msdn.microsoft.com/pfxteam/2012/01/20/await-synchronizationcontext-and-console-apps/
class Program
{
    static void Main()
    {
        //Main1();
        Console.WriteLine("\r\n\r\nFixed:");
        Main2();
        Console.ReadKey();
    }

    static void Main2()
    {
        Console.WriteLine("Main2 @"+Thread.CurrentThread.ManagedThreadId);
        AsyncPump.Run(async delegate
        {
            await DemoAsync();
        });
    }


    static void Main1()
    {
        DemoAsync().Wait();
    }

    static async Task DemoAsync()
    {
        var d = new Dictionary<int, int>();
        for (int i = 0; i < 100; i++)
        {
            int id = Thread.CurrentThread.ManagedThreadId;
            int count;
            Console.WriteLine("Processing #" + i + " @" +id);
            d[id] = d.TryGetValue(id, out count) ? count + 1 : 1;
            await Task.Yield();
        }

        foreach (var pair in d)
            Console.WriteLine(pair);
    }
}

public static class AsyncPump
{
    /// <summary>Runs the specified asynchronous function.</summary>
    /// <param name="func">The asynchronous function to execute.</param>
    public static void Run(Func<Task> func)
    {
        if (func == null) throw new ArgumentNullException("func");

        var prevCtx = SynchronizationContext.Current;
        try
        {
            // Establish the new context
            var syncCtx = new SingleThreadSynchronizationContext();
            SynchronizationContext.SetSynchronizationContext(syncCtx);

            // Invoke the function and alert the context to when it completes
            var t = func(); //开始启动func，func在第一个await的地方返回了
            if (t == null) throw new InvalidOperationException("No task provided.");

            t.ContinueWith(delegate { syncCtx.Complete(); }, TaskScheduler.Default);
            //制定t在完成做什么

            // Pump continuations and propagate any exceptions
            syncCtx.RunOnCurrentThread(); 
            t.GetAwaiter().GetResult();
        }
        finally { SynchronizationContext.SetSynchronizationContext(prevCtx); }
    }

    /// <summary>Provides a SynchronizationContext that's single-threaded.</summary>
    private sealed class SingleThreadSynchronizationContext : SynchronizationContext
    {
        /// <summary>The queue of work items.</summary>
        private readonly BlockingCollection<KeyValuePair<SendOrPostCallback, object>> m_queue =
            new BlockingCollection<KeyValuePair<SendOrPostCallback, object>>();
        /// <summary>The processing thread.</summary>
        private readonly Thread m_thread = Thread.CurrentThread;

        /// <summary>Dispatches an asynchronous message to the synchronization context.</summary>
        /// <param name="d">The System.Threading.SendOrPostCallback delegate to call.</param>
        /// <param name="state">The object passed to the delegate.</param>
        public override void Post(SendOrPostCallback d, object state)
        {
            Console.WriteLine("Post @" + Thread.CurrentThread.ManagedThreadId);
            if (d == null) throw new ArgumentNullException("d");
            m_queue.Add(new KeyValuePair<SendOrPostCallback, object>(d, state));
        }

        /// <summary>Not supported.</summary>
        public override void Send(SendOrPostCallback d, object state)
        {
            throw new NotSupportedException("Synchronously sending is not supported.");
        }

        /// <summary>Runs an loop to process all queued work items.</summary>
        public void RunOnCurrentThread()
        {
            Console.WriteLine("RunOnCurrentThread @" + Thread.CurrentThread.ManagedThreadId);
            foreach (var workItem in m_queue.GetConsumingEnumerable())
            {
                Console.WriteLine("work @"+ Thread.CurrentThread.ManagedThreadId);
                workItem.Key(workItem.Value);
            }
        }

        /// <summary>Notifies the context that no more work will arrive.</summary>
        public void Complete()
        {
            Console.WriteLine("Complete @" + Thread.CurrentThread.ManagedThreadId);
            m_queue.CompleteAdding();
        }
    }
}
