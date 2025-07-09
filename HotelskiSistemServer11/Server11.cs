using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;




//ovo je za KT1-vise ne koristimo





namespace HotelskiSistemServer11
{
    public class Server
    {
        private int udpPort;
        private int tcpPort;

        public Server(int udpPort, int tcpPort)
        {
            this.udpPort = udpPort;
            this.tcpPort = tcpPort;
        }

        public async Task Pokreni()
        {
            // Pokreni oba servera paralelno
            var udpTask = PokreniUDPServer();
            var tcpTask = PokreniTCPServer();

            await Task.WhenAll(udpTask, tcpTask);
        }

        private async Task PokreniUDPServer()
        {
            using (var udp = new UdpClient(udpPort))
            {
                Console.WriteLine($"UDP server startovan na portu {udpPort}");

                while (true)
                {
                    var result = await udp.ReceiveAsync();
                    string poruka = Encoding.UTF8.GetString(result.Buffer);
                    Console.WriteLine($"Potvrda rezervacije: {poruka}");

                    /*
                    result = await udp.ReceiveAsync();
                    string status = Encoding.UTF8.GetString(result.Buffer);
                    Console.WriteLine("Status sobe: " + status);
                    */

                    // Vrati potvrdu
                    byte[] odgovor = Encoding.UTF8.GetBytes("POTVRDA=OK");
                    await udp.SendAsync(odgovor, odgovor.Length, result.RemoteEndPoint);
                    Console.WriteLine("Potvrda poslata gostu.");
                }
            }
        }

        private async Task PokreniTCPServer()
        {
            var listener = new TcpListener(IPAddress.Any, tcpPort);
            listener.Start();
            Console.WriteLine($"TCP server startovan na portu {tcpPort}");

            while (true)
            {
                TcpClient client = await listener.AcceptTcpClientAsync();
                Console.WriteLine("Primljena TCP konekcija.");

                _ = ObradiKlijenta(client); // Fire-and-forget obrada klijenta
            }
        }

        private async Task ObradiKlijenta(TcpClient client)
        {
            try
            {
                using (client)
                {
                    var stream = client.GetStream();

                    // Posalji zadatak osoblju
                    string zadatak = "Zadatak: Ocistiti apartman 101";
                    byte[] zadatakBytes = Encoding.UTF8.GetBytes(zadatak);
                    await stream.WriteAsync(zadatakBytes, 0, zadatakBytes.Length);
                    Console.WriteLine("Poruka poslata ka osoblju.");

                    // Čekaj potvrdu od osoblja
                    byte[] buffer = new byte[512];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string potvrda = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"Potvrda od osoblja: {potvrda}");
                    }
                    else
                    {
                        Console.WriteLine("Nije primljena potvrda od osoblja.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška u komunikaciji sa klijentom: {ex.Message}");
            }
        }
    }
}
