using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
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
        protected CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();
        public bool deferActions = false;
        #endregion

        private readonly MainPage mainPage;

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
            IPAddress ipAddress = IPAddress.Parse("192.168.1.37");
            CustomDebug.WriteLine("IP address created");
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 5050);
            CustomDebug.WriteLine("remoteEndPoint created");
            do
            {
                byte[] bytes = new byte[1024];

                try
                {
                    Socket receiver = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    CustomDebug.WriteLine("Receiver socket created");
                    try
                    {
                        CustomDebug.WriteLine("Before receiver socket connect");
                        receiver.Connect(remoteEP);
                        CustomDebug.WriteLine("After receiver socket connect");

                        CustomDebug.WriteLine($"Socket connected to {receiver.RemoteEndPoint}");

                        CustomDebug.WriteLine("Before receiver.Receive(bytes)");
                        int bytesRec = receiver.Receive(bytes);
                        CustomDebug.WriteLine("After receiver.Receive(bytes)");

                        CustomDebug.WriteLine("Before Encoding.ASCII.GetString(bytes, 0, bytesRec)");
                        var socketStringValue = Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        CustomDebug.WriteLine("After Encoding.ASCII.GetString(bytes, 0, bytesRec)");
                        CustomDebug.WriteLine($"string value: {socketStringValue}");
                        SocketValue = socketStringValue;

                    }
                    catch (ArgumentNullException ane)
                    {
                        CustomDebug.WriteLine($"ArgumentNullException : {ane}");
                    }
                    catch (SocketException se)
                    {
                        if (se.ErrorCode == 10054 || se.ErrorCode == 10061)
                        {
                            CustomDebug.WriteLine("Server is currently offline.\nPress any key to retry");
                        }
                        else
                        {
                            CustomDebug.WriteLine($"SocketException : {se}");
                        }
                    }
                    catch (Exception e)
                    {
                        CustomDebug.WriteLine($"Unexpected exception : {e}");
                    }
                }
                catch (Exception e)
                {
                    CustomDebug.WriteLine(e.ToString());
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
}
