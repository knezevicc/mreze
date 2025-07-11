using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HotelskiSistemServer11
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("[DEBUG] Main metoda startovana");

            int udpPort = 12345;
            int tcpPort = 12346;

            // Kreiraj i pokreni server
            var server = new NonBlockingServer(udpPort, tcpPort);

            Console.WriteLine("Pokrećem server...");
            /*await*/ server.Start();

            // Server radi dok ne pritisneš taster
            Console.WriteLine("Server radi. Pritisni taster za zaustavljanje...");
            Console.ReadKey();
        }
    }
}
