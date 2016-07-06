using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
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

namespace NetTcpWpfListener
{
    public class ClientModel
    {
        public Socket Socket { get; set; }
        public byte[] Buffer = new byte[BUFFER_SIZE];
        public static int BUFFER_SIZE = 1024;
    }

    public partial class MainWindow : Window
    {
        private List<ClientModel> _clients = new List<ClientModel>();

        public IPEndPoint EndPoint
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

        public MainWindow()
        {
            InitializeComponent();

            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(EndPoint);
            listener.Listen(100);

            Dispatcher.Invoke(() =>
            {
                logs.Text += string.Format("[{3}] Listining on {0}:{1}{2}", EndPoint.Address.ToString(), EndPoint.Port.ToString(), Environment.NewLine, DateTime.Now);
                logs.ScrollToEnd();
            });

            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
        }


        public void AcceptCallback(IAsyncResult result)
        {
            Socket listener = result.AsyncState as Socket;

            // start listening for another connection
            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

            Socket clientConnection = listener.EndAccept(result);

            Dispatcher.Invoke(() =>
            {
                logs.Text += string.Format("[{2}] Connection from {0} accepted{1}", clientConnection.RemoteEndPoint.ToString(), Environment.NewLine, DateTime.Now);
                logs.ScrollToEnd();

                clients.Text += string.Format("{0}{1}", clientConnection.RemoteEndPoint.ToString(), Environment.NewLine, DateTime.Now);
                clients.ScrollToEnd();
            });

            var client = new ClientModel()
            {
                Socket = clientConnection,
            };

            _clients.Add(client);

            // start listining for a data from the client
            client.Socket.BeginReceive(client.Buffer, 0, ClientModel.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(ReadCallback), client);
        }

        public void ReadCallback(IAsyncResult result)
        {
            string content = string.Empty;

            ClientModel client = result.AsyncState as ClientModel;

            // read data from the sent by the client
            int bytesRead = 0;

            try
            {
                bytesRead = client.Socket.EndReceive(result);
            }
            catch (SocketException ex)
            {
                Dispatcher.Invoke(() =>
                {
                    logs.Text += string.Format("[{2}] {0}{1}", ex.Message, Environment.NewLine, DateTime.Now);
                    logs.Text += string.Format("[{2}] Connection with {0} has been disconnected{1}", client.Socket.RemoteEndPoint.ToString(), Environment.NewLine, DateTime.Now);
                    logs.ScrollToEnd();

                    clients.Text = clients.Text.Replace(string.Format("{0}{1}", client.Socket.RemoteEndPoint.ToString(), Environment.NewLine), string.Empty);
                    clients.ScrollToEnd();
                });

                _clients.Remove(client);

                return;
            }

            if (bytesRead > 0)
            {
                content = Encoding.ASCII.GetString(client.Buffer, 0, bytesRead);

                Dispatcher.Invoke(() =>
                {
                    logs.Text += string.Format("[{3}] '{0}' received from {1}{2}", content, client.Socket.RemoteEndPoint.ToString(), Environment.NewLine, DateTime.Now);
                    logs.ScrollToEnd();
                });

                // Echo the data back to all connected clients.
                SendToAllClients(string.Format("[{0}] {1}", client.Socket.RemoteEndPoint.ToString(), content));

                // start listining for another data from the client
                client.Socket.BeginReceive(client.Buffer, 0, ClientModel.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(ReadCallback), client);
            }
        }

        private void SendToAllClients(string data)
        {
            // Convert the string data to byte data using ASCII encoding.                            
            byte[] message = Encoding.ASCII.GetBytes(data);

            foreach (ClientModel client in _clients)
            {
                client.Socket.Send(message);
            }
        }
    }
}
