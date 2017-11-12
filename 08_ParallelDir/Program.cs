using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Threading;
using System.Threading.Tasks;

//https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/how-to-iterate-file-directories-with-the-parallel-class
class Program
{


    static void Main()
    {
        try
        {
            TraverseTreeParallelForEach(@"C:\Software", (f) =>
            {
                // Exceptions are no-ops.
                try
                {
                    // Do nothing with the data except read it.
                    byte[] data = File.ReadAllBytes(f);
                }
                catch (FileNotFoundException) { }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
                catch (SecurityException) { }
                // Display the filename.
                Console.WriteLine(f);
            });
        }
        catch (ArgumentException)
        {
            Console.WriteLine(@"The directory 'C:\Program Files' does not exist.");
        }

        // Keep the console window open.
        Console.ReadKey();
    }

    public static void TraverseTreeParallelForEach(string root, Action<string> action)
    {
        if (!Directory.Exists(root))
        {
            throw new ArgumentException();
        }

        //Count of files traversed and timer for diagnostic output
        int fileCount = 0;
        var sw = Stopwatch.StartNew();

        // Determine whether to parallelize file processing on each folder based on processor count.
        int procCount = System.Environment.ProcessorCount;

        // Data structure to hold names of subfolders to be examined for files.
        Stack<string> dirs = new Stack<string>();
        dirs.Push(root);

        while (dirs.Count > 0)
        {
            string currentDir = dirs.Pop();
            string[] subDirs = { };
            string[] files = { };

            try
            {
                subDirs = Directory.GetDirectories(currentDir);
                files = Directory.GetFiles(currentDir);
            }
            // Thrown if we do not have discovery permission on the directory.
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }
            // Thrown if another process has deleted the directory after we retrieved its name.
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.Message);
                continue;
            }

            // Execute in parallel if there are enough files in the directory.
            // Otherwise, execute sequentially.Files are opened and processed
            // synchronously but this could be modified to perform async I/O.
            try
            {
                if (files.Length < procCount)
                {
                    foreach (var file in files)
                    {
                        action(file);
                        fileCount++;
                    }
                }
                else
                {
                    Parallel.ForEach(files, () => 0, (file, loopState, localCount) =>
                    {
                        action(file);
                        return ++localCount;
                    },
                                     (c) =>
                                     {
                                         Interlocked.Add(ref fileCount, c);
                                     });
                }
            }
            catch (AggregateException ae)
            {
                ae.Handle((ex) => {
                    if (ex is UnauthorizedAccessException)
                    {
                        // Here we just output a message and go on.
                        Console.WriteLine(ex.Message);
                        return true;
                    }
                    // Handle other exceptions here if necessary...

                    return false;
                });
            }

            // Push the subdirectories onto the stack for traversal.
            // This could also be done before handing the files.
            //foreach (string str in subDirs)
            //    dirs.Push(str);
            dirs.PushRange(subDirs);
        }

        // For diagnostic purposes.
        Console.WriteLine("Processed {0} files in {1} milleseconds", fileCount, sw.ElapsedMilliseconds);
    }

}

public static class Extensions
{
    public static void PushRange<T>(this Stack<T> source, IEnumerable<T> collection)
    {
        foreach (var item in collection)
            source.Push(item);
    }
}