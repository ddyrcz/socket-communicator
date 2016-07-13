using System.Net.Sockets;

namespace Client.Models
{
    internal class ClientModel
    {
        public static int BUFFER_SIZE = 1024;
        public byte[] Buffer = new byte[BUFFER_SIZE];
        public Socket Socket { get; set; }
    }
}