using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

//https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-handle-exceptions-in-parallel-loops

class ExceptionDemo2
{
    public static void Main()
    {
        //Main1();
        //Main2();
        //Main22();
        //Main10();
        //Main101();
        //AsyncVoidExceptions_CannotBeCaughtByCatch();
        //Main3();
        Main4();
        //Main4Sync().Wait();
        Console.ReadKey();
    }

    static void Main1()
    {
        // Create some random data to process in parallel.
        // There is a good probability this data will cause some exceptions to be thrown.
        byte[] data = new byte[5000];
        Random r = new Random();
        r.NextBytes(data);

        try
        {
            ProcessDataInParallel(data);
        }
        catch (AggregateException ae)
        {
            // This is where you can choose which exceptions to handle.
            foreach (var ex in ae.InnerExceptions)
            {
                if (ex is ArgumentException)
                    Console.WriteLine(ex.Message);
                else
                    throw ex;
            }
        }

        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }

    private static void ProcessDataInParallel(byte[] data)
    {
        // Use ConcurrentQueue to enable safe enqueueing from multiple threads.
        var exceptions = new ConcurrentQueue<Exception>();

        // Execute the complete loop and capture all exceptions.
        Parallel.ForEach(data, d =>
        {
            try
            {
                // Cause a few exceptions, but not too many.
                if (d < 0x3)
                    throw new ArgumentException(String.Format("value is {0:x}. Elements must be greater than 0x3.", d));
                else
                    Console.Write(d + " ");
            }
            // Store the exception and continue with the loop.                    
            catch (Exception e) { exceptions.Enqueue(e); }
        });

        // Throw the exceptions here after the loop completes.
        if (exceptions.Count > 0) throw new AggregateException(exceptions);
    }

    public class CustomException : Exception
    {
        public CustomException(String message) : base(message)
        { }
    }

    public static void Main2()
    {
        var task1 = Task.Run(() => { throw new CustomException("This exception is expected!"); });

        try
        {
            task1.Wait();
        }
        catch (AggregateException ae)
        {
            foreach (var e in ae.InnerExceptions)
            {
                // Handle the custom exception.
                if (e is CustomException)
                {
                    Console.WriteLine(e.Message);
                }
                // Rethrow any other exception.
                else
                {
                    throw;
                }
            }
        }
    }
    public static void Main22()
    {
        main22Async().Wait();
    }

    public static async Task main22Async()
    {
        var task1 = Task.Run(() => { throw new CustomException("This exception is expected!"); });
        try
        {
            await task1;
        }
        catch (CustomException e)
        {
            Console.WriteLine("OK:" + e.Message);
        }
    }

    //https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/exception-handling-task-parallel-library
    //演示AggregateException.Handle method 
    public static void Main10()
    {
        Console.WriteLine("测试第一种情况");
        // This should throw an UnauthorizedAccessException.
        try
        {
            var files = GetAllFiles(@"C:\");
            if (files != null)
                foreach (var file in files)
                    Console.WriteLine(file);
        }
        catch (AggregateException ae)
        {
            Console.WriteLine("还有其他异常：");
            foreach (var ex in ae.InnerExceptions)
                Console.WriteLine("{0}: {1}", ex.GetType().Name, ex.Message);
            //这个例子不会允许到这里。
        }
        Console.WriteLine();

        Console.WriteLine("测试第二种情况");
        // This should throw an ArgumentException.
        try
        {
            foreach (var s in GetAllFiles(""))
                Console.WriteLine(s);
        }
        catch (AggregateException ae)
        {
            Console.WriteLine("还有其他异常：");
            foreach (var ex in ae.InnerExceptions)
                Console.WriteLine("{0}: {1}", ex.GetType().Name, ex.Message);
        }
    }

    static string[] GetAllFiles(string path)
    {
        var task1 = Task.Run(() => Directory.GetFiles(path, "*.txt",
                                                      SearchOption.AllDirectories));

        try
        {
            return task1.Result;
        }
        catch (AggregateException ae)
        {
            ae.Handle(x => { // Handle an UnauthorizedAccessException
                if (x is UnauthorizedAccessException)
                {
                    Console.WriteLine("You do not have permission to access all folders in this path.");
                    Console.WriteLine("See your network administrator or try another path.");
                }
                return x is UnauthorizedAccessException;
                //在这里处理了此种类型的异常后，就把这个异常删掉了。
            });
            return Array.Empty<String>();
        }
    }

    public static void Main101()
    {
        Main101Async().Wait();
    }

    public async static Task Main101Async()
    {
        Console.WriteLine("测试第一种情况");
        try
        {
            // This should throw an UnauthorizedAccessException.
            var files = await GetAllFilesAsync(@"C:\");
            if (files != null)
                foreach (var file in files)
                    Console.WriteLine(file);
        }
        catch (Exception ex)
        {
            Console.WriteLine("还有其他异常：");
            Console.WriteLine("{0}: {1}", ex.GetType().Name, ex.Message);
            //这个例子不会允许到这里。
        }
        Console.WriteLine();

        Console.WriteLine("测试第二种情况");
        // This should throw an ArgumentException.
        try
        {
            foreach (var s in await GetAllFilesAsync(""))
                Console.WriteLine(s);
        }
        catch (Exception ex)
        {
            Console.WriteLine("还有其他异常：");
            Console.WriteLine("{0}: {1}", ex.GetType().Name, ex.Message);
        }
    }

    static async Task<string[]> GetAllFilesAsync(string path)
    {
        var task1 = Task.Run(() => Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories));
        //var task1 = Task.FromResult(Directory.GetFiles(path, "*.txt", SearchOption.AllDirectories));
        try
        {
            return await task1.ConfigureAwait(false);
        }
        catch (UnauthorizedAccessException)
        {
            Console.WriteLine("You do not have permission to access all folders in this path.");
            Console.WriteLine("See your network administrator or try another path.");
            return Array.Empty<String>();
        }
    }

    //https://msdn.microsoft.com/en-us/magazine/jj991977.aspx
    private async static void ThrowExceptionAsync()
    {
        Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
        throw new InvalidOperationException();
    }
    public static void AsyncVoidExceptions_CannotBeCaughtByCatch()
    {
        try
        {
            Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
            ThrowExceptionAsync();
        }
        catch (Exception)
        {
            Console.WriteLine("Caught exception");
            // The exception is never caught here!
            //  这里竟然不能捕获！！！
            throw;
        }
    }

    public static void Main3()
    {
        Main3Async().Wait();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }


    //测试await只抛出第一个异常，Task里面还保存了所有异常（包括第一个）
    public async static Task Main3Async()
    {
        /*
        var task = Task.WhenAll(from c in "abcdef"
                                select Task.Run(delegate
                                {
                                    Console.WriteLine("Throwing # " + c);
                                    throw new Exception("George Test " + c);
                                }));
        */

        var task = Task.WhenAll("abcdef".Select(c=> Task.Run(delegate
                                {
                                    Console.WriteLine("Throwing #" + c);
                                    throw new Exception("George Test" + c);
                                })));


        try
        {
            await task;
        }
        catch(Exception ex)
        {
            Console.WriteLine("Catched {0}: {1}", ex.GetType().Name, ex.Message);
        }

       // Console.WriteLine("\r\nTask.Exception contains:\r\n"+
         //   string.Join(", \r\n", task.Exception.Flatten().InnerExceptions.Select(e => e.Message)));
    }

    //测试unobservedexception
    public static void Main4()
    {
        Main4Sync().Wait();
        Thread.Sleep(2000);//让t2结束。
        Console.WriteLine("StartGC");
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
    public async static Task Main4Sync()
    {
        Task t1= Task.Run(delegate { throw new Exception("aaaaaaaaaaa"); });
        Task t2= Task.Run(async delegate 
        {
            Console.WriteLine("Task2 Stated.");
            await Task.Delay(1000);
            Console.WriteLine("Task2 ended.");
            throw new Exception("bbbbb");
        });
        try
        {
            await t1;
            await t2;
        }
        catch
        {
            Console.WriteLine("catched Ex");
        }
        Console.WriteLine("Main4Sync ended");
    }
}

