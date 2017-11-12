using System;
using System.Threading;
using System.Threading.Tasks;

//https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/task-based-asynchronous-programming
class CustomData
{
    public long CreationTime;
    public int Name;
    public int ThreadNum;
}

public class Example
{
    public static void Main()
    {
        TestNullable();

        Console.WriteLine("_______________简单的实现方式1:George___________");
        Main00();
        Console.WriteLine("_______________简单的实现方式:George___________");
        Main0();
        Console.WriteLine("\r\n\r\n_______________旧的错误实现方式更正:George___________");
        Main1(true);
        Console.WriteLine("\r\n\r\n_______________旧的错误实现方式___________");
        Main1(false);
        Console.WriteLine("\r\n\r\n_______________旧的正确实现方式___________");
        Main2();
        Console.WriteLine("\r\n\r\n_______________旧的正确实现方式Ex___________");
        Main3();

        Console.ReadKey();
    }

    private static void FuncA(int a)
    {
        Console.WriteLine(a);
    }

    private static void TestNullable()
    {
        int? abc = 1;
        //FuncA(abc); //这个不行
        FuncA((int)abc); //这里必须强制类型转换

        int bcd = abc.Value;
        //int bcd = abc; //这个也不行；
        Console.WriteLine(abc);
    }

    //George：这个是我的实现，非常简洁。
    private static void Main00()

    {
        Task[] taskArray = new Task[10];
        for (int i = 0; i < taskArray.Length; i++)
        {
            //虽然i不能在task里面使用，但是这个taskName可以。
            int taskName = i;
            taskArray[i] = Task.Factory.StartNew(() => {
                Console.WriteLine("Task #{0} created at {1} on thread #{2}.",
                                  taskName, DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            });
        }
        Task.WaitAll(taskArray);
    }

    //George：这个是我的实现，非常简洁。
    private static void Main0()

    {
        Task[] taskArray = new Task[10];
        for (int i = 0; i < taskArray.Length; i++)
        {
            taskArray[i] = Task.Factory.StartNew((obj) => {
                Console.WriteLine("Task #{0} created at {1} on thread #{2}.",
                                  obj as int?, DateTime.Now.Ticks, Thread.CurrentThread.ManagedThreadId);
            },
                                                 i);
        }
        Task.WaitAll(taskArray);
    }

    private static void Main1(bool bGeorge)
    { 
        // Create the task object by using an Action(Of Object) to pass in the loop
        // counter. This produces an unexpected result.
        Task[] taskArray = new Task[10];
        for (int i = 0; i < taskArray.Length; i++)
        {
            taskArray[i] = Task.Factory.StartNew((Object obj) => {
                var data = new CustomData() {
                    Name = bGeorge? (int)obj : i,  
                    //George：原始的错误只是因为这里不应该用i。后面的那个i会最为obj传递过来给我们用。为何不用？
                    //在这段程序运行的时候，for循环大概率早就结束了，故此i可能是比当时那个i大的任何一个数字；
                    CreationTime = DateTime.Now.Ticks,
                    ThreadNum = Thread.CurrentThread.ManagedThreadId };
                
                Console.WriteLine("Task #{0} created at {1} on thread #{2}.",
                                  data.Name, data.CreationTime, data.ThreadNum);
            },
                                                 i);
            Console.Write(i);
        }
        Console.WriteLine("For Ened.");
        Task.WaitAll(taskArray);
    }

    //这个虽然对，但是没有必要。
    public static void Main2()
    {
        // Create the task object by using an Action(Of Object) to pass in custom data
        // to the Task constructor. This is useful when you need to capture outer variables
        // from within a loop. 
        Task[] taskArray = new Task[10];
        for (int i = 0; i < taskArray.Length; i++)
        {
            taskArray[i] = Task.Factory.StartNew((obj) => {
                CustomData data = obj as CustomData;
                if (data == null)
                    return;

                data.ThreadNum = Thread.CurrentThread.ManagedThreadId;
                Console.WriteLine("Task #{0} created at {1} on thread #{2}.",
                                 data.Name, data.CreationTime, data.ThreadNum);
            },
                                                  new CustomData() { Name = i, CreationTime = DateTime.Now.Ticks });
        }
        Task.WaitAll(taskArray);
    }


    public static void Main3()
    {
        Task[] taskArray = new Task[10];
        for (int i = 0; i < taskArray.Length; i++)
        {
            taskArray[i] = Task.Factory.StartNew((obj) => {
                CustomData data = obj as CustomData;
                data.ThreadNum = Thread.CurrentThread.ManagedThreadId;
            },
                                                  new CustomData() { Name = i, CreationTime = DateTime.Now.Ticks });
        }
        Task.WaitAll(taskArray);

        foreach (var task in taskArray)
        {
            var data = task.AsyncState as CustomData;
            Console.WriteLine("Task #{0} created at {1}, ran on thread #{2}.",
                                data.Name, data.CreationTime, data.ThreadNum);
        }
    }
    // https://social.msdn.microsoft.com/Forums/en-US/1988294c-de41-476a-a104-aa550b7409f5/tpl-api-task-create-methods-signature-should-be-different?forum=parallelextensions
    //https://stackoverflow.com/questions/1840078/why-is-the-taskfactory-startnew-method-not-generic
    //这里比较奇怪的是 public Task StartNew(Action<object> action,object state) 没有用到模板，因此大家都得去做类型转换。
    //上面的两个连接说明了原因
}
