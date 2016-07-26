using Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Server.ViewModels
{
    internal class ServerViewModel : BaseViewModel
    {
        private List<SocketModel> _clients = new List<SocketModel>();

        private string _clientsString;

        private string _logs;

        private IPEndPoint _serverEndpoint;

        public ServerViewModel()
        {
            Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            listener.Bind(EndPoint);
            listener.Listen(100);

            Logs += string.Format("[{3}] Listining on {0}:{1}{2}", EndPoint.Address.ToString(), EndPoint.Port.ToString(), Environment.NewLine, DateTime.Now);

            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
        }

        public string ClientsString
        {
            get { return _clientsString; }
            set
            {
                _clientsString = value;
                NotifyOfPropertyChange();
            }
        }

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

        public string Logs
        {
            get { return _logs; }
            set
            {
                _logs = value;
                NotifyOfPropertyChange();
            }
        }

        public void AcceptCallback(IAsyncResult result)
        {
            var server = result.AsyncState as Socket;

            // start listening for another connection
            server.BeginAccept(new AsyncCallback(AcceptCallback), server);

            Socket clientSocket = server.EndAccept(result);

            Logs += string.Format("[{2}] Connection from {0} accepted{1}", clientSocket.RemoteEndPoint.ToString(), Environment.NewLine, DateTime.Now);

            ClientsString += string.Format("{0}{1}", clientSocket.RemoteEndPoint.ToString(), Environment.NewLine, DateTime.Now);

            var client = new SocketModel()
            {
                Socket = clientSocket,
            };

            _clients.Add(client);

            // start listining for a data from the client
            client.Socket.BeginReceive(client.Buffer, 0, SocketModel.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(ReadCallback), client);
        }

        public void ReadCallback(IAsyncResult result)
        {            
            SocketModel client = result.AsyncState as SocketModel;
            
            int bytesRead = 0;

            try
            {
                bytesRead = client.Socket.EndReceive(result);
            }
            catch (SocketException ex)
            {
                Logs += string.Format("[{2}] Connection with {0} has been broken{1}", client.Socket.RemoteEndPoint.ToString(), Environment.NewLine, DateTime.Now);

                ClientsString = ClientsString.Replace(string.Format("{0}{1}", client.Socket.RemoteEndPoint.ToString(), Environment.NewLine), string.Empty);

                _clients.Remove(client);

                return;
            }

            if (bytesRead > 0)
            {
                string message = Encoding.ASCII.GetString(client.Buffer, 0, bytesRead);

                Logs += string.Format("[{3}] '{0}' received from {1}{2}", message, client.Socket.RemoteEndPoint.ToString(), Environment.NewLine, DateTime.Now);
                
                SendToAllClients(string.Format("[{0}] {1}", client.Socket.RemoteEndPoint.ToString(), message));

                // start listining for another data from the client
                client.Socket.BeginReceive(client.Buffer, 0, SocketModel.BUFFER_SIZE, SocketFlags.None, new AsyncCallback(ReadCallback), client);
            }
        }

        private void SendToAllClients(string data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] message = Encoding.ASCII.GetBytes(data);

            foreach (SocketModel client in _clients)
            {
                client.Socket.Send(message);
            }
        }
    }
}