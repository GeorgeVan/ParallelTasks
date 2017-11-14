using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

namespace UIDeadLock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void DebugWriteLine(string s)
        {
            listBoxMsg.Dispatcher.BeginInvoke(new Action(delegate { listBoxMsg.Items.Add(s); }));
        }

        private async Task DelayAsync(bool defaultContent=true)
        {
            DebugWriteLine("     Inner { @ " + Thread.CurrentThread.ManagedThreadId);
            if (!defaultContent)
            {
                await Task.Delay(1000).ConfigureAwait(false);
            }
            else
            {
                await Task.Delay(1000);
            }
            DebugWriteLine("     Inner } @ " + Thread.CurrentThread.ManagedThreadId);
        }
        // This method causes a deadlock when called in a GUI or ASP.NET context.
        public void TestDeadLock()
        {
            // Start the delay.
            DebugWriteLine("Outer 1 @ " + Thread.CurrentThread.ManagedThreadId);
            var delayTask = DelayAsync();
            DebugWriteLine("Outer 2 @ " + Thread.CurrentThread.ManagedThreadId);
            // Wait for the delay to complete.
            delayTask.Wait();
            DebugWriteLine("Outer 3 @ " + Thread.CurrentThread.ManagedThreadId);
        }

        public void TestDeadLockOK1()
        {
            // Start the delay.
            DebugWriteLine("Outer 1 @ " + Thread.CurrentThread.ManagedThreadId);
            var delayTask = DelayAsync(false);
            DebugWriteLine("Outer 2 @ " + Thread.CurrentThread.ManagedThreadId);
            // Wait for the delay to complete.
            delayTask.Wait();
            DebugWriteLine("Outer 3 @ " + Thread.CurrentThread.ManagedThreadId);
        }

        // This method causes a deadlock when called in a GUI or ASP.NET context.
        public async Task TestOK2Async()
        {
            // Start the delay.
            DebugWriteLine("Outer 1 @ " + Thread.CurrentThread.ManagedThreadId);
            var delayTask = DelayAsync();
            DebugWriteLine("Outer 2 @ " + Thread.CurrentThread.ManagedThreadId);
            // Wait for the delay to complete.
            await delayTask;
            DebugWriteLine("Outer 3 @ " + Thread.CurrentThread.ManagedThreadId);
        }

        public async Task TestOK3Async()
        {
            // Start the delay.
            DebugWriteLine("Outer 1 @ " + Thread.CurrentThread.ManagedThreadId);
            var delayTask = DelayAsync(false);
            DebugWriteLine("Outer 2 @ " + Thread.CurrentThread.ManagedThreadId);
            // Wait for the delay to complete.
            await delayTask;
            DebugWriteLine("Outer 3 @ " + Thread.CurrentThread.ManagedThreadId);
        }

        private void buttonDeadLock_Click(object sender, RoutedEventArgs e)
        {
            TestDeadLock();
        }

        private void buttonOK1_Click(object sender, RoutedEventArgs e)
        {
            TestDeadLockOK1();
        }

        private async void buttonOK2_Click(object sender, RoutedEventArgs e)
        {
            await TestOK2Async();
        }

        private async void buttonOK3_Click(object sender, RoutedEventArgs e)
        {
            await TestOK3Async();
        }

        private void buttonClear_Click(object sender, RoutedEventArgs e)
        {
            listBoxMsg.Items.Clear();
        }
    }
}


