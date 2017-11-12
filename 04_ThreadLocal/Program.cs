using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


class Test
{
    static void Main()
    {
        int[] nums = Enumerable.Range(0, 1000000).ToArray();
        long total = 0;

        // Use type parameter to make subtotal a long, not an int
        Parallel.For(0, nums.Length, () => 0, (j, loop, subtotal) =>
        {
            //()=>0; 0是subtotal的初始值
            subtotal += nums[j];
            return subtotal;
        },
            (x) => Interlocked.Add(ref total, x)
        );

        Console.WriteLine("The total is {0:N0}", total);
        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }
}