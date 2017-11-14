using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// Add a using directive and a reference for System.Net.Http;
using System.Net.Http;

namespace AsyncTracer
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Debug.WriteLine = x => resultsTextBox.Text += x +"\r\n";
        }

        private async void startButton_Click(object sender, RoutedEventArgs e)
        {
            // The display lines in the example lead you through the control shifts.
            resultsTextBox.Text += "ONE:   Entering startButton_Click.\r\n" +
                "           Calling AccessTheWebAsync.\r\n";

            Task<int> getLengthTask = AccessTheWebAsync();

            resultsTextBox.Text += "\r\nFOUR:  Back in startButton_Click.\r\n" +
                "           Task getLengthTask is started.\r\n" +
                "           About to await getLengthTask -- no caller to return to.\r\n";

            int contentLength = await getLengthTask;

            resultsTextBox.Text += "\r\nSIX:   Back in startButton_Click.\r\n" +
                "           Task getLengthTask is finished.\r\n" +
                "           Result from AccessTheWebAsync is stored in contentLength.\r\n" +
                "           About to display contentLength and exit.\r\n";

            resultsTextBox.Text +=
                String.Format("\r\nLength of the downloaded string: {0}.\r\n", contentLength);
        }


        async Task<int> AccessTheWebAsync()
        {
            resultsTextBox.Text += "\r\nTWO:   Entering AccessTheWebAsync.";

            // Declare an HttpClient object and increase the buffer size. The default
            // buffer size is 65,536.
            HttpClient client =
                new HttpClient() { MaxResponseContentBufferSize = 1000000 };

            resultsTextBox.Text += "\r\n           Calling HttpClient.GetStringAsync.\r\n";

            // GetStringAsync returns a Task<string>. 
            Task<string> getStringTask = client.GetStringAsync("http://msdn.microsoft.com");

            resultsTextBox.Text += "\r\nTHREE: Back in AccessTheWebAsync.\r\n" +
                "           Task getStringTask is started.";

            // AccessTheWebAsync can continue to work until getStringTask is awaited.

            resultsTextBox.Text +=
                "\r\n           About to await getStringTask and return a Task<int> to startButton_Click.\r\n";

            // Retrieve the website contents when task is complete.
            string urlContents = await getStringTask;

            resultsTextBox.Text += "\r\nFIVE:  Back in AccessTheWebAsync." +
                "\r\n           Task getStringTask is complete." +
                "\r\n           Processing the return statement." +
                "\r\n           Exiting from AccessTheWebAsync.\r\n";

            return urlContents.Length;
        }

        class Debug
        {
            public delegate void WriteLineFun(String s);
            public static WriteLineFun WriteLine;
        }

        private  void exceptionButton_Click(object sender, RoutedEventArgs e)
        {
             DoSomethingAsync();
        }

        //https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/try-catch
        public async Task DoSomethingAsync()
        {
            Task<string> theTask = DelayAsync();

            try
            {
                string result = await theTask;
                Debug.WriteLine("Result: " + result);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception Message: " + ex.Message);
            }

            Debug.WriteLine("Task IsCanceled: " + theTask.IsCanceled);
            Debug.WriteLine("Task IsFaulted:  " + theTask.IsFaulted);

            if (theTask.Exception != null)
            {
                Debug.WriteLine("Task Exception Message: "
                    + theTask.Exception.Message);
                Debug.WriteLine("Task Inner Exception Message: "
                    + theTask.Exception.InnerException.Message);
            }
            Debug.WriteLine("______________________");
        }

        Random random = new Random(DateTime.Now.Second);

        private async Task<string> DelayAsync()
        {
            await Task.Delay(100);

            switch(random.Next(3)){
                case 1:
                    throw new OperationCanceledException("G取消canceled");
                case 2:
                    throw new Exception("G异常Something happened.");
                default:
                    return "G结束Done";
            }
        }

        public async Task DoMultipleAsync()
        {
            Task theTask1 = ExcAsync(info: "First Task");
            Task theTask2 = ExcAsync(info: "Second Task");
            Task theTask3 = ExcAsync(info: "Third Task");

            Task allTasks = Task.WhenAll(theTask1, theTask2, theTask3);

            try
            {
                await allTasks;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception: " + ex.Message);
                Debug.WriteLine("Task IsFaulted: " + allTasks.IsFaulted);
                foreach (var inEx in allTasks.Exception.InnerExceptions)
                {
                    Debug.WriteLine("Task Inner Exception: " + inEx.Message);
                }
            }
        }

        private async Task ExcAsync(string info)
        {
            await Task.Delay(100);

            throw new Exception("Error-" + info);
        }

        private async void multiException_Click(object sender, RoutedEventArgs e)
        {
            await DoMultipleAsync();
        }
    }


}

// Sample Output:

// ONE:   Entering startButton_Click.
//           Calling AccessTheWebAsync.

// TWO:   Entering AccessTheWebAsync.
//           Calling HttpClient.GetStringAsync.

// THREE: Back in AccessTheWebAsync.
//           Task getStringTask is started.
//           About to await getStringTask and return a Task<int> to startButton_Click.

// FOUR:  Back in startButton_Click.
//           Task getLengthTask is started.
//           About to await getLengthTask -- no caller to return to.

// FIVE:  Back in AccessTheWebAsync.
//           Task getStringTask is complete.
//           Processing the return statement.
//           Exiting from AccessTheWebAsync.

// SIX:   Back in startButton_Click.
//           Task getLengthTask is finished.
//           Result from AccessTheWebAsync is stored in contentLength.
//           About to display contentLength and exit.

// Length of the downloaded string: 33946.
