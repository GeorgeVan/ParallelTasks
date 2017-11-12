using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

public class Example
{
    public static void Main()
    {
        long totalSize = 0;

        String[] args = Environment.GetCommandLineArgs();

        if (args.Length == 1)
        {
            args = new string[] { args[0], @"c:\windows\" };
        }
        if (!Directory.Exists(args[1]))
        {
            Console.WriteLine("The directory does not exist.");
            return;
        }

        String[] files = Directory.GetFiles(args[1]);
        Parallel.For(0, files.Length,
                     index => Interlocked.Add(ref totalSize, new FileInfo(files[index]).Length ));

        Console.WriteLine("Directory '{0}':", args[1]);
        Console.WriteLine("{0:N0} files, {1:N0} bytes", files.Length, totalSize);

        GTools.Mini.ConsoleHitAndExit();
    }
}
// The example displaysoutput like the following:
//       Directory 'c:\windows\':
//       32 files, 6,587,222 bytes