using System.Net.Sockets;

namespace Models
{
    public class SocketModel
    {
        public static int BUFFER_SIZE = 1024;
        public byte[] Buffer = new byte[BUFFER_SIZE];
        public Socket Socket { get; set; }
    }
}