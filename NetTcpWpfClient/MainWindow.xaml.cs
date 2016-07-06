using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
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

namespace NetTcpWpfClient
{
    public class ClientModel
    {
        public Socket Socket { get; set; }
        public byte[] Buffer = new byte[BUFFER_SIZE];
        public static int BUFFER_SIZE = 1024;
    }
    
    public partial class MainWindow : Window
    {
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
        private IPEndPoint _serverEndpoint;

        public ClientModel Client
        {
            get
            {
                if (_client == null)
                {
                    _client = new ClientModel
                    {
                        Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                    };
                }

                return _client;
            }
        }
        private ClientModel _client;

        public MainWindow()
        {
            InitializeComponent();

            if (ConnectWithServer())
            {
                Client.Socket.BeginReceive(Client.Buffer, 0, ClientModel.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(RecieveCallback), Client);
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
            catch (SocketException)
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
            ClientModel client = result.AsyncState as ClientModel;

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

            Dispatcher.Invoke(() => chatMessage.Text += message + Environment.NewLine);

            client.Socket.BeginReceive(client.Buffer, 0, ClientModel.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(RecieveCallback), client);
        }

        private void sentMessage_Click(object sender, RoutedEventArgs e)
        {
            SendDataToServer();
        }

        private void chat_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendDataToServer();
            }
        }

        private void SendDataToServer()
        {
            byte[] message = Encoding.ASCII.GetBytes(chat.Text);

            try
            {
                Client.Socket.Send(message);
            }
            catch (SocketException)
            {
               MessageBoxResult result = MessageBox.Show("The server is unreachable. Do you want to reconnect?", "Server error", MessageBoxButton.YesNo);

                if(result == MessageBoxResult.Yes)
                {
                    if (ConnectWithServer())
                    {
                        Client.Socket.BeginReceive(Client.Buffer, 0, ClientModel.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(RecieveCallback), Client);
                    }
                }
            }            
            catch (Exception ex)
            {
                MessageBox.Show("Ann error occured while sending data");
            }

            chat.Text = string.Empty;
        }
    }
}
