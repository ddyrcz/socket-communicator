using Models;
using System;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Client.ViewModels
{
    internal class ClientViewModel : BaseViewModel
    {
        private string _chatMessages;
        private SocketModel _client;

        private string _message;
        private IPEndPoint _serverEndpoint;

        public ClientViewModel()
        {
        }

        public string ChatMessages
        {
            get { return _chatMessages; }
            set
            {
                _chatMessages = value;
                NotifyOfPropertyChange();
            }
        }

        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                NotifyOfPropertyChange();
            }
        }

        public IPEndPoint ServerEndpoint
        {
            get
            {
                if (_serverEndpoint == null)
                {
                    string serverAddress = ConfigurationManager.AppSettings["ServerAddress"].ToString();
                    int port = int.Parse(ConfigurationManager.AppSettings["ServerPort"].ToString());

                    IPAddress ipAddress = IPAddress.Parse(serverAddress);

                    _serverEndpoint = new IPEndPoint(ipAddress, port);
                }

                return _serverEndpoint;
            }
        }

        private SocketModel Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new SocketModel
                    {
                        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                    };
                }

                return _client;
            }
        }

        public void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendDataToServer();
            }
        }

        public void OnSendMessage()
        {
            SendDataToServer();
        }

        public void OnWindowLoaded()
        {
            if (ConnectWithServer())
            {
                Client.Socket.BeginReceive(Client.Buffer, 0, SocketModel.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(RecieveCallback), Client);
            }
        }

        private bool ConnectWithServer()
        {
            try
            {
                Client?.Socket?.Connect(ServerEndpoint);
            }
            catch (ConfigurationErrorsException)
            {
                MessageBox.Show("An error occured while reading configuration file");
                return false;
            }
            catch (SocketException ex)
            {
                MessageBox.Show("An error occured while connecting to the server");
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occured");
                return false;
            }

            return true;
        }

        private void RecieveCallback(IAsyncResult result)
        {
            SocketModel client = result.AsyncState as SocketModel;

            int bytesCount;

            try
            {
                bytesCount = client.Socket.EndReceive(result);
            }
            catch (SocketException)
            {
                client.Socket.Shutdown(SocketShutdown.Both);
                client.Socket.Close();
                client.Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                MessageBox.Show("An error occured while receiving data from the server");
                return;
            }

            string message = Encoding.ASCII.GetString(client.Buffer, 0, bytesCount);

            ChatMessages += message + Environment.NewLine;

            client.Socket.BeginReceive(client.Buffer, 0, SocketModel.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(RecieveCallback), client);
        }

        private void SendDataToServer()
        {
            byte[] message = Encoding.ASCII.GetBytes(Message);

            try
            {
                Client.Socket.Send(message);
            }
            catch (SocketException)
            {
                MessageBoxResult result = MessageBox.Show("The server is unreachable. Do you want to reconnect?", "Server error", MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes)
                {
                    if (ConnectWithServer())
                    {
                        Client.Socket.BeginReceive(Client.Buffer, 0, SocketModel.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(RecieveCallback), Client);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ann error occured while sending data");
            }

            Message = string.Empty;
        }
    }
}