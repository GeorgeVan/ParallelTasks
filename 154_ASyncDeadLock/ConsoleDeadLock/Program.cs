using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleDeadLock
{
    class Program
    {
        static void Main(string[] args)
        {
            TestDeadLock();
            Console.ReadKey();
        }

        private async static Task DelayAsync()
        {
            Console.WriteLine("     Inner { @ " + Thread.CurrentThread.ManagedThreadId);
            await Task.Delay(1000);
            Console.WriteLine("     Inner } @ " + Thread.CurrentThread.ManagedThreadId);
            //这个会在另外一个线程里面执行
        }
        // This method causes a deadlock when called in a GUI or ASP.NET context.
        public static void TestDeadLock()
        {
            // Start the delay.
            Console.WriteLine("Outer 1 @ " + Thread.CurrentThread.ManagedThreadId);
            var delayTask = DelayAsync();
            Console.WriteLine("Outer 2 @ " + Thread.CurrentThread.ManagedThreadId);
            // Wait for the delay to complete.
            delayTask.Wait();
            Console.WriteLine("Outer 3 @ " + Thread.CurrentThread.ManagedThreadId);
        }
    }


}


