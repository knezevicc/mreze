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
        {/*
            // Primer kreiranja apartmana
            var apartman = new Apartman11(101, 1, 3, 4);
            apartman.ListaGostiju.Add("Marko Markovic");
            Console.WriteLine(apartman);

            var server = new NonBlockingServer(12345, 12346);
            server.Start();
        */
            int udpPort = 12345;
            int tcpPort = 12346;

            // Kreiraj i pokreni server
            /*await*/var server = new NonBlockingServer(udpPort, tcpPort);

            Console.WriteLine("Pokrećem server...");
            server.Start();

            // Server radi dok ne pritisneš taster
            Console.WriteLine("Server radi. Pritisni taster za zaustavljanje...");
            Console.ReadKey();
        }
    }
}
