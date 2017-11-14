using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class Example
{
    public static void Main()
    {
        /*
        Main1();
        Main2();
        Main3();
        Main4();
        Main5();

        Console.WriteLine("\r\n\r\n______演示Continution的Cancel__________________");
        Main6();
        
        Console.WriteLine("\r\n\r\n______演示有跟随子任务的等待__________________");
        Main7(TaskCreationOptions.AttachedToParent);

        Console.WriteLine("\r\n\r\n______演示有自由子任务的等待__________________");
        Main7(TaskCreationOptions.DenyChildAttach);

        Console.WriteLine("\r\n\r\n______演示AsyncState__________________");
        Main8();
*/
        

        Console.WriteLine("\r\n\r\n______演示后续任务检查先前任务的Exception__________________");
        Main9();
        Console.ReadKey();
    }

    public static void Main1()
    {
        var getData = Task.Factory.StartNew(() => {
            Random rnd = new Random();
            int[] values = new int[100];
            for (int ctr = 0; ctr <= values.GetUpperBound(0); ctr++)
                values[ctr] = rnd.Next();

            return values;
        });
        var processData = getData.ContinueWith((x) => {
            int n = x.Result.Length;
            long sum = 0;
            double mean;

            for (int ctr = 0; ctr <= x.Result.GetUpperBound(0); ctr++)
                sum += x.Result[ctr];

            mean = sum / (double)n;
            return Tuple.Create(n, sum, mean);
        });
        var displayData = processData.ContinueWith((x) => {
            return String.Format("N={0:N0}, Total = {1:N0}, Mean = {2:N2}",
                                 x.Result.Item1, x.Result.Item2,
                                 x.Result.Item3);
        });
        Console.WriteLine(displayData.Result);
    }
    public static void Main2()
    {
        var displayData = Task.Factory.StartNew(() => {
            Random rnd = new Random();
            int[] values = new int[100];
            for (int ctr = 0; ctr <= values.GetUpperBound(0); ctr++)
                values[ctr] = rnd.Next();

            return values;
        }).
                          ContinueWith((x) => {
                              int n = x.Result.Length;
                              long sum = 0;
                              double mean;

                              for (int ctr = 0; ctr <= x.Result.GetUpperBound(0); ctr++)
                                  sum += x.Result[ctr];

                              mean = sum / (double)n;
                              return Tuple.Create(n, sum, mean);
                          }).
                          ContinueWith((x) => {
                              return String.Format("N={0:N0}, Total = {1:N0}, Mean = {2:N2}",
                                                 x.Result.Item1, x.Result.Item2,
                                                 x.Result.Item3);
                          });
        Console.WriteLine(displayData.Result);

    }

    //演示没有父子关系的Task
    public static void Main3()
    {
        var outer = Task.Factory.StartNew(() =>
        {
            Console.WriteLine("Outer task beginning.");

            var child = Task.Factory.StartNew(() =>
            {
                Thread.SpinWait(5000000);
                Console.WriteLine("Detached task completed.");
            });
        });

        outer.Wait();
        Console.WriteLine("Outer task completed.");
        // The example displays the following output:
        //    Outer task beginning.
        //    Outer task completed.
        //    Detached task completed.
    }

    //演示有父子关系的结束
    public static void Main4()
    {
        var parent = Task.Factory.StartNew(() => {
            Console.WriteLine("Parent task beginning.");
            for (int ctr = 0; ctr < 10; ctr++)
            {
                int taskNo = ctr;
                Task.Factory.StartNew((x) => {
                    Thread.SpinWait(5000000);
                    Console.WriteLine("Attached child #{0} completed.",
                                      x);
                },
                                      taskNo, TaskCreationOptions.AttachedToParent);
            }
        });

        parent.Wait();
        Console.WriteLine("Parent task completed.");
    }


    //演示WhenAll不Block，在使用Result的时候才会Block
    //https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/chaining-tasks-by-using-continuation-tasks
    public static void Main5()
    {
        ConcurrentQueue<String> logs = new ConcurrentQueue<String>();
        List<Task<int>> tasks = new List<Task<int>>();
        for (int ctr = 1; ctr <= 10; ctr++)
        {
            int baseValue = ctr;
            tasks.Add(Task.Factory.StartNew((b) => {
                Thread.SpinWait(5000000);
                int i = (int)b;
                logs.Enqueue ("Task " + i);
                return i * i;
            }, baseValue));
            //这个是原生的例子；尽管也可以直接使用Main00或者Main0的样子。但是这里把两种给结合起来不伦不类
        }
        var continuation = Task.WhenAll(tasks);
        logs.Enqueue ("After WhenAll()" );

        long sum = 0;
        for (int ctr = 0; ctr <= continuation.Result.Length - 1; ctr++)
        {
            if (ctr == 0) logs.Enqueue("First loop");
            Console.Write("{0} {1} ", continuation.Result[ctr],
                          ctr == continuation.Result.Length - 1 ? "=" : "+");
            sum += continuation.Result[ctr];
        }
        Console.WriteLine(sum);
        Console.WriteLine(String.Join("\r\n", logs));
    }

    //演示用同一个Token来取消两个连续任务
    public static void Main6()
    {
        Random rnd = new Random();
        var cts = new CancellationTokenSource();
        CancellationToken token = cts.Token;
        Timer timer = new Timer(Elapsed, cts, 5000, Timeout.Infinite);
        //Timer timer = new Timer(Elapsed, cts, 2000, Timeout.Infinite); //2000就可以在第一个任务运行的时候取消

        var t = Task.Run(() => {
            List<int> product33 = new List<int>();
            for (int ctr = 1; ctr < Int16.MaxValue; ctr++)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("\n关Cancellation requested in antecedent...\n");
                    token.ThrowIfCancellationRequested();
                }
                if (ctr % 2000 == 0)
                {
                    int delay = rnd.Next(16, 501);
                    Thread.Sleep(delay);
                }

                if (ctr % 33 == 0)
                    product33.Add(ctr);
            }
            return product33.ToArray();
        }, token);

        Task continuation = t.ContinueWith(antecedent => {
            Console.WriteLine("关Multiples of 33:\n");
            var arr = antecedent.Result;
            for (int ctr = 0; ctr < arr.Length; ctr++)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("\n关Cancellation requested in continuation...\n");
                    token.ThrowIfCancellationRequested();
                }

                if (ctr % 100 == 0)
                {
                    int delay = rnd.Next(16, 251);
                    Thread.Sleep(delay);
                }
                Console.Write("{0:N0}{1}", arr[ctr],
                              ctr != arr.Length - 1 ? ", " : "");
                if (Console.CursorLeft >= 74)
                    Console.WriteLine();
            }
            Console.WriteLine();
        }, token);

        Console.WriteLine("关Next to continuation.Wait();");
        try
        {
            t.Wait(); 
            //比例子上要多加这么一句话；
            //否则如果在第一个任务里面抛出异常后，主程序直接执行到最后面，此时第二个任务是Cancel状态而第一个是running
            continuation.Wait();
        }
        catch (AggregateException e)
        {
            foreach (Exception ie in e.InnerExceptions)
                Console.WriteLine("{0}: {1}", ie.GetType().Name,
                                  ie.Message);
        }
        finally
        {
            cts.Dispose();
        }

        Console.WriteLine("\n关Antecedent Status: {0}", t.Status);
        Console.WriteLine("关Continuation Status: {0}", continuation.Status);
    }

    private static void Elapsed(object state)
    {
        CancellationTokenSource cts = state as CancellationTokenSource;
        if (cts == null) return;
        try { 
            cts.Cancel();
            Console.WriteLine("\nCancellation request issued...\n");
        }catch(ObjectDisposedException){
            Console.WriteLine("\nStop Normally, no need to cancel...\n");
        }
    }

    //https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/chaining-tasks-by-using-continuation-tasks
    //等待有子任务的父任务结束
    public static void Main7(TaskCreationOptions opts)
    {
        Console.WriteLine("Creating antecedent task...");
        var t = Task.Factory.StartNew(() => {
            Console.WriteLine("   Running antecedent task {0}...", Task.CurrentId);
            Console.WriteLine("   Launching attached child tasks...");
            for (int ctr = 1; ctr <= 5; ctr++)
            {
                int index = ctr;
                Task.Factory.StartNew((value) => {
                    Console.WriteLine("      Attached child task #{0} running", value);
                    Thread.Sleep(1000);
                }, index, opts);
            }
            Console.WriteLine("   Finished launching attached child tasks...");
        });

        Console.WriteLine("Creating Continue task...");
        var continuation = t.ContinueWith((antecedent) => {
            Console.WriteLine("   Executing continuation of Task {0}",
                              antecedent.Id);
        });

        Console.WriteLine("Waitfor Continue task");
        continuation.Wait();
        Console.WriteLine("Continue task Ended.");
    }

    // Simluates a lengthy operation and returns the time at which
    // the operation completed.
    public static DateTime DoWork()
    {
        // Simulate work by suspending the current thread 
        // for two seconds.
        Thread.Sleep(2000);

        // Return the current time.
        return DateTime.Now;
    }

    //https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/chaining-tasks-by-using-continuation-tasks
    //测试AsyncState
    public static void Main8()
    {
        // Start a root task that performs work.
        Task<DateTime> t = Task<DateTime>.Run(delegate { return DoWork(); });
        //用delegate来声明一个匿名函数的好处就是可以自动转换为各种Function和Action<>。

        // Create a chain of continuation tasks, where each task is 
        // followed by another task that performs work.
        List<Task<DateTime>> continuations = new List<Task<DateTime>>();
        for (int i = 0; i < 5; i++)
        {
            // Provide the current time as the state of the continuation.
            t = t.ContinueWith(delegate { return DoWork(); }, DateTime.Now);
            continuations.Add(t);
        }

        // Wait for the last task in the chain to complete.
        t.Wait();

        // Print the creation time of each continuation (the state object)
        // and the completion time (the result of that task) to the console.
        foreach (var continuation in continuations)
        {
            DateTime start = (DateTime)continuation.AsyncState;
            //这里不能用as是因为DateTime是一个struct
            DateTime end = continuation.Result;

            Console.WriteLine("Task was created at {0} and finished at {1}.",
               start.TimeOfDay, end.TimeOfDay);
        }
    }

    //只在异常的时候执行
    public static void Main9()
    {
        var t = Task.Run(delegate { return File.ReadAllText(@"C:\NonexistentFile.txt"); });

        var c = t.ContinueWith((antecedent) =>
        { // Get the antecedent's exception information.
            foreach (var ex in antecedent.Exception.InnerExceptions)
            {
                if (ex is FileNotFoundException)
                    Console.WriteLine(ex.Message);
            }
        }, TaskContinuationOptions.OnlyOnFaulted);

        c.Wait();

        Console.WriteLine("t.status" + t.Status);
        Console.WriteLine("c.status" + c.Status);
    }


}
