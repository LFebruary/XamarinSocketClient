using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace XamarinClient
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();

            BindingContext = new MainViewModel(this);
        }
    }

    internal class MainViewModel : MvvmHelpers.BaseViewModel
    {
        #region Fields
        protected   CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        public      bool                    deferActions = false;
        #endregion

        private MainPage mainPage;

        private bool _receiverFlag = true;
        public bool ReceiverFlag
        {
            get => _receiverFlag;
            set => SetProperty(ref _receiverFlag, value);
        }

        public MainViewModel(MainPage mainPage)
        {
            this.mainPage = mainPage;

            Task.Run(async () =>
            {
                await Task.Delay(500);
                TryToStartClient();
            });
        }

        private void TryToStartClient()
        {
            IPAddress   ipAddress   = IPAddress.Parse("192.168.1.37");
            FarDebug.WriteLine("IP address created");
            IPEndPoint  remoteEP    = new IPEndPoint(ipAddress, 5050);
            FarDebug.WriteLine("remoteEndPoint created");
            do
            {
                byte[] bytes = new byte[1024];

                try
                {
                    Socket receiver = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    FarDebug.WriteLine("Receiver socket created");
                    try
                    {
                        FarDebug.WriteLine("Before receiver socket connect");
                        receiver.Connect(remoteEP);
                        FarDebug.WriteLine("After receiver socket connect");

                        FarDebug.WriteLine($"Socket connected to {receiver.RemoteEndPoint}");

                        FarDebug.WriteLine("Before receiver.Receive(bytes)");
                        int bytesRec = receiver.Receive(bytes);
                        FarDebug.WriteLine("After receiver.Receive(bytes)");

                        FarDebug.WriteLine("Before Encoding.ASCII.GetString(bytes, 0, bytesRec)");
                        var socketStringValue = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        FarDebug.WriteLine("After Encoding.ASCII.GetString(bytes, 0, bytesRec)");
                        FarDebug.WriteLine($"string value: {socketStringValue}");
                        SocketValue = socketStringValue;

                    }
                    catch (ArgumentNullException ane)
                    {
                        FarDebug.WriteLine($"ArgumentNullException : {ane}");
                    }
                    catch (SocketException se)
                    {
                        if (se.ErrorCode == 10054 || se.ErrorCode == 10061)
                        {
                            FarDebug.WriteLine("Server is currently offline.\nPress any key to retry");
                        }
                        else
                        {
                            FarDebug.WriteLine($"SocketException : {se}");
                        }
                    }
                    catch (Exception e)
                    {
                        FarDebug.WriteLine($"Unexpected exception : {e}");
                    }
                }
                catch (Exception e)
                {
                    FarDebug.WriteLine(e.ToString());
                }
            }
            while (ReceiverFlag);
        }

        private string _socketValue;
        public string SocketValue 
        { 
            get => _socketValue; 
            set => SetProperty(ref _socketValue, value);
        }
    }

    /// <summary>
    /// Manages queue of tasks and auto run them one after another in order of being added to queue
    /// </summary>
    public class TaskQueue
    {
        #region Constructor
        public TaskQueue(CancellationTokenSource cancellationTokenSource)
        {
            CancellationTokenSource = cancellationTokenSource;
        }
        #endregion

        #region Fields
        private readonly CancellationTokenSource CancellationTokenSource;

        // Used to make sure items are enqueued in order
        private readonly object EnqueueKey = new object();

        // Used to make sure only one task at a time may run
        private readonly object RunKey = new object();

        private readonly Queue<TaskItem> TaskItems = new Queue<TaskItem>();
        #endregion

        #region TaskItem
        private class TaskItem
        {
            #region Constructor
            internal TaskItem(TaskTypeEnum taskType, object item)
            {
                TaskType = taskType;
                Item = item;
            }
            #endregion

            #region Fields
            internal TaskTypeEnum TaskType;
            internal object Item;
            #endregion

            internal enum TaskTypeEnum { Task, Delay, Action }
        }
        #endregion

        #region Methods
        public void Enqueue(Action action)
        {
            lock (EnqueueKey)
            {
                Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Enqueue::Task::Start");
                TaskItems.Enqueue(new TaskItem(TaskItem.TaskTypeEnum.Action, action));
                Task.Run(StartItem);
                Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Enqueue::Task::End");
            }
        }

        public void Enqueue(Func<Task> func)
        {
            lock (EnqueueKey)
            {
                Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Enqueue::Task::Start");
                TaskItems.Enqueue(new TaskItem(TaskItem.TaskTypeEnum.Task, func));
                Task.Run(StartItem);
                Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Enqueue::Task::End");
            }
        }

        public void Enqueue(int millisecondsDelay)
        {
            lock (EnqueueKey)
            {
                Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Enqueue::Task::Start");
                TaskItems.Enqueue(new TaskItem(TaskItem.TaskTypeEnum.Delay, millisecondsDelay));
                Task.Run(StartItem);
                Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Enqueue::Task::End");
            }
        }

        private void StartItem()
        {
            {
                lock (RunKey)
                {
                    var taskItem = TaskItems.Dequeue();
                    if (!CancellationTokenSource.IsCancellationRequested)
                    {
                        Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Task::Dequeue::Started");

                        Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Task::Dequeue::Ended");
                        Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Task::Started");
                        switch (taskItem.TaskType)
                        {
                            case TaskItem.TaskTypeEnum.Task:
                                Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Task::IsTask");
                                ((Func<Task>)taskItem.Item).Invoke().Wait();
                                break;
                            case TaskItem.TaskTypeEnum.Delay:
                                Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Task::IsDelay");
                                Task.Delay((int)taskItem.Item).Wait();
                                break;
                            case TaskItem.TaskTypeEnum.Action:
                                Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Task::IsAction");
                                Task.Run((Action)taskItem.Item).Wait();
                                break;
                        }
                        Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Task::Ended");
                    }
                    else
                    {
                        Console.WriteLine($"{DateTime.Now} {Thread.CurrentThread.ManagedThreadId} {this}::Task::Cancelled");
                    }
                }
            }
        }
        #endregion
    }

    /// <summary>
    /// Provides additional methods to extend the debugging capabilities of <see cref="Debug"/>.
    /// </summary>
    /// <remarks>
    /// These methods are provided in a separate class since as of writing C# does not support static extensions.
    /// </remarks>
    public static class FarDebug
    {
        #region Package File Path Separator

        private static string packageSeparator { get; set; } = @"\XamarinClient\";

        #endregion

        #region WriteLine Extensions

        /// <summary>
        /// Writes a blank line to the trace listeners in the <see cref="System.Diagnostics.Debug.Listeners"/> collection.
        /// </summary>
        [Conditional("DEBUG")]
        public static void WriteLine()
        {
            Debug.WriteLine(string.Empty);
        }

        /// <summary>
        /// Writes a message preceded by callsite information to the trace listeners in the <see cref="System.Diagnostics.Debug.Listeners"/> collection.
        /// </summary>
        /// <param name="message">The message to write.</param>
        /// <param name="addBlankLine">Whether to write a blank line after the written message.</param>
        /// <param name="filePath">The file from which the method is called.</param>
        /// <param name="caller"> The method in which the method is called.</param>
        /// <param name="line">The line at which the method is called.</param>
        [Conditional("DEBUG")]
        public static void WriteLine(string message, bool addBlankLine = false, [CallerFilePath] string filePath = "", [CallerMemberName] string caller = "", [CallerLineNumber] int line = -1)
        {
            string fileString = filePath.Split(new string[] { packageSeparator }, StringSplitOptions.None).Last();

            Debug.WriteLine($"{fileString}: {caller}: line {line}: {message}");

            if (addBlankLine)
            {
                WriteLine();
            }
        }

        /// <summary>
        /// Writes the value of the object's <see cref="object.ToString()"/> method preceded by callsite information to the trace listeners in the <see cref="System.Diagnostics.Debug.Listeners"/> collection.
        /// </summary>
        /// <param name="value">The object to write the string representation of.</param>
        /// <param name="addBlankLine">Whether to write a blank line after the written message.</param>
        /// <param name="filePath">The file from which the method is called.</param>
        /// <param name="caller"> The method in which the method is called.</param>
        /// <param name="line">The line at which the method is called.</param>
        [Conditional("DEBUG")]
        public static void WriteLine(object value, bool addBlankLine = false, [CallerFilePath] string filePath = "", [CallerMemberName] string caller = "", [CallerLineNumber] int line = -1)
        {
            WriteLine(value?.ToString() ?? "null", addBlankLine, filePath, caller, line);

            if (addBlankLine)
            {
                WriteLine();
            }
        }

        ///// <summary>
        ///// Writes the string representation of the provided stream preceded by callsite information to the trace listeners in the <see cref="System.Diagnostics.Debug.Listeners"/> collection using a StreamReader.
        ///// </summary>
        ///// <remarks>
        /////     <para>
        /////         This method keeps track of the current value of the input stream's Position property and resets it after the stream has been read.
        /////     </para>
        /////     <para>
        /////         Internally, this method copies the contents of the provided stream to a new MemoryStream, resets the original stream's Position property, and then creates and uses a StreamReader with the copied stream.
        /////     </para>
        ///// </remarks>
        ///// <param name="stream">The stream to write the string representation of.</param>
        ///// <param name="addBlankLine">Whether to write a blank line after the written message.</param>
        ///// <param name="filePath">The file from which the method is called.</param>
        ///// <param name="caller"> The method in which the method is called.</param>
        ///// <param name="line">The line at which the method is called.</param>
        //public static async Task WriteStream(Stream stream, bool addBlankLine = false, [CallerFilePath] string filePath = "", [CallerMemberName] string caller = "", [CallerLineNumber] int line = -1)
        //{
        //    using MemoryStream streamCopy = new MemoryStream();
        //    long originalPosition = stream.Position;
        //    await stream.CopyToAsync(streamCopy);

        //    stream.Position = originalPosition;
        //    streamCopy.Position = 0;

        //    using StreamReader reader = new StreamReader(streamCopy);
        //    WriteLine(await reader.ReadToEndAsync(), addBlankLine, filePath, caller, line);
        //}

        #endregion
    }
}
