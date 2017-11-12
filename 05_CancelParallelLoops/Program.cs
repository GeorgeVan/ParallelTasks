//https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-cancel-a-parallel-for-or-foreach-loop

namespace CancelParallelLoops
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {
        static void Main()
        {
            int[] nums = Enumerable.Range(0, 10000000).ToArray();
            CancellationTokenSource cts = new CancellationTokenSource();

            // Use ParallelOptions instance to store the CancellationToken
            ParallelOptions po = new ParallelOptions
            {
                CancellationToken = cts.Token,
                MaxDegreeOfParallelism = System.Environment.ProcessorCount
            };

            Console.WriteLine("Press any key to start. Press 'c' to cancel.");
            Console.ReadKey();

            // Run a task so that we can cancel from another thread.
            Task.Factory.StartNew(() =>
            {
                if (Console.ReadKey().KeyChar == 'c')
                    cts.Cancel();
            });

            /* George: 这个并不行，后来发现Invoke会Block。
             * The most important difference between these two is that Parallel.
             * Invoke will wait for all the actions to complete before continuing with the code, 
             * whereas StartNew will move on to the next line of code, allowing the tasks to complete in their own good time.
            Parallel.Invoke(() =>
            {
                Console.WriteLine("XXXXXXXXXXXXXXXXXXXXX");
                if (Console.ReadKey().KeyChar == 'c')
                    cts.Cancel();
                Console.WriteLine("YYYYYYYYYYYYYYYYYYYY");
            });
            */

            int cancelTouched = 0;
            try
            {
                Parallel.ForEach(nums, po, (num) =>
                {
                    double d = Math.Sqrt(num);
                    Console.WriteLine("{0} on {1}", d, Thread.CurrentThread.ManagedThreadId);
                    //po.CancellationToken.ThrowIfCancellationRequested();
                    //原本例子上有这句话。发现没有啥意义。因为有没有，cancelTouched都会大于1.
                    if (po.CancellationToken.IsCancellationRequested)
                    {
                        Interlocked.Increment(ref cancelTouched);
                        po.CancellationToken.ThrowIfCancellationRequested();
                    }
                });
            }
            catch (OperationCanceledException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("监测到几次："+cancelTouched);
            }
            finally
            {
                cts.Dispose();
            }

            Console.WriteLine("press any key to exit");
            Console.ReadKey();
        }
    }
}



