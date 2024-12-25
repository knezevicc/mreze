using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelskiSistem
{
    public class Program
    {
        static void Main(string[] args)
        {
            int udpPort = 8080;
            int tcpPort = 9090;

            Server server = new Server(udpPort, tcpPort);
            
            System.Threading.Tasks.Task.Run(() => server.ListenForUdpMessages());
            System.Threading.Tasks.Task.Run(() => server.ListenForTcpClients());

            Console.WriteLine("Server is running...");
            Console.WriteLine("Press Enter to exit...");
            Console.ReadLine();
        }
    }
}
