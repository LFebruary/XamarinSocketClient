using MvvmHelpers;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace XamarinClient
{
    internal class SocketListenerViewModel : BaseViewModel
    {
        internal Action OnSocketValueChanged;

        private string _socketValue;
        public string SocketValue
        {
            get => _socketValue;
            set => SetProperty(ref _socketValue, value, onChanged: OnSocketValueChanged);
        }

        private string _socketAddress;
        public string SocketAddress
        {
            get => _socketAddress;
            set => SetProperty(ref _socketAddress, value);
        }

        private int _socketPort;
        public int SocketPort
        {
            get => _socketPort;
            set => SetProperty(ref _socketPort, value);
        }

        private bool _receiverFlag = true;

        public SocketListenerViewModel()
        {
            Task.Run(async () =>
            {
                await Task.Delay(500);
                TryToStartClient();
            });
        }

        private void TryToStartClient()
        {
            IPAddress ipAddress = IPAddress.Parse(SocketAddress);
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, SocketPort);
            do
            {
                byte[] bytes = new byte[1024];

                try
                {
                    Socket receiver = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    try
                    {
                        receiver.Connect(remoteEP);
                        int bytesRec = receiver.Receive(bytes);
                        SocketValue = Encoding.ASCII.GetString(bytes, 0, bytesRec);

                    }
                    catch (ArgumentNullException ane)
                    {
                        CustomDebug.WriteLine($"ArgumentNullException : {ane}");
                    }
                    catch (SocketException se)
                    {
                        if (se.ErrorCode == 10054 || se.ErrorCode == 10061)
                        {
                            CustomDebug.WriteLine("Server is currently offline.");
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

        private bool ReceiverFlag
        {
            get => _receiverFlag;
            set => SetProperty(ref _receiverFlag, value);
        }

        private int _refreshRate = 500;
        public int RefreshRate
        {
            get => _refreshRate;
            set => SetProperty(ref _refreshRate, value, onChanged: () =>
            {

            });
        }
    }
}
