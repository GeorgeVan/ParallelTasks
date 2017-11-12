using System;
using System.Threading.Tasks;
public class Example
{
    public static void Main()
    {
        Task<Double>[] taskArray = { Task.Factory.StartNew(() => DoComputation(1.0)),
                                     Task.Factory.StartNew(() => DoComputation(100.0)),
                                     Task.Run(() => DoComputation(1000.0)) };

        var results = new Double[taskArray.Length];
        Double sum = 0;

        for (int i = 0; i < taskArray.Length; i++)
        {
            results[i] = taskArray[i].Result;
            /*
             *  If the Result property is accessed before the computation finishes, 
             *  the property blocks the calling thread until the value is available.
             */
            Console.Write("{0:N1} {1}", results[i],
                              i == taskArray.Length - 1 ? "= " : "+ ");
            sum += results[i];
        }
        Console.WriteLine("{0:N1}", sum);

        Console.ReadKey();
    }



    private static Double DoComputation(Double start)
    {
        Double sum = 0;
        for (var value = start; value <= start + 10; value += .1)
            sum += value;

        return sum;
    }
}
// The example displays the following output:
//        606.0 + 10,605.0 + 100,495.0 = 111,706.0
                                                  