using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Gost1
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Kreiraj gosta
            var gost = new Gost11("Marko", "Markovic", Pol.Muski, new DateTime(1990, 5, 21), "123456789");

            // Serijalizuj gosta
            byte[] gostBytes = gost.Serijalizuj();

            using (var udpClient = new UdpClient())
            {
                //slanje rez serverurr
                await udpClient.SendAsync(gostBytes, gostBytes.Length, "127.0.0.1", 12345);
                Console.WriteLine("Rezervacija poslana putem UDP.");

                //oslruskuje potvrdu rez
                var result1 = await udpClient.ReceiveAsync();
                string potvrda = Encoding.UTF8.GetString(result1.Buffer);
                Console.WriteLine("Potvrda od servera: " + potvrda);

                /*        // Slanje zahteva za čišćenje
                        byte[] zahtevZaCiscenje = Encoding.UTF8.GetBytes("ZAHTEV=CISCENJE;APARTMAN=101");
                        await udpClient.SendAsync(zahtevZaCiscenje, zahtevZaCiscenje.Length, "127.0.0.1", 12345);
                        Console.WriteLine("Zahtev za čišćenje poslat serveru.");
                */
                while (true)
                {
                    var result = await udpClient.ReceiveAsync();
                    string poruka = Encoding.UTF8.GetString(result.Buffer);

                    if (poruka.Contains("ZADATAK=IZVRSEN"))
                    {
                        Console.WriteLine("Zadatak čišćenja je uspešno izvršen.");
                    }
                    else if (poruka.Contains("STANJE=PRAZAN"))
                    {
                        Console.WriteLine("Apartman je sada prazan i spreman za novu rezervaciju.");
                    }
                    else
                    {
                        Console.WriteLine("Poruka od servera: " + poruka);
                    }
                }/*
                var result2 = await udpClient.ReceiveAsync();
                string poruka2 = Encoding.UTF8.GetString(result2.Buffer);
                Console.WriteLine("Odgovor servera: " + poruka2);
        */
                /*
                if (potvrda.StartsWith("POTVRDA"))
                    Console.WriteLine("Potvrda od servera: " + potvrda);
                else if (potvrda.StartsWith("STATUS"))
                    Console.WriteLine("Status od servera: " + potvrda);
                */
            }

            // Gost šalje rezervaciju serveru putem UDP
            //await SendUdpRezervaciju("127.0.0.1", 12345, gostBytes);

            // Pokreni TCP client za osoblje
            //await TcpOsobljeClient("127.0.0.1", 12346);

            // Sačekaj server da radi (za demo)
            Console.WriteLine("Pritisni bilo koji taster za kraj...");
            Console.ReadKey();
        }
        /*
        static async Task SendUdpRezervaciju(string serverIp, int serverPort, byte[] gostPodaci)
        {
            var udpClient = new UdpClient();
            await udpClient.SendAsync(gostPodaci, gostPodaci.Length, serverIp, serverPort);
            Console.WriteLine("Rezervacija poslana putem UDP.");

            var result = await udpClient.ReceiveAsync();
            string potvrda = Encoding.UTF8.GetString(result.Buffer);
            Console.WriteLine("Potvrda od servera: " + potvrda);
        }*/
        /*
        static async Task TcpOsobljeClient(string serverIp, int serverPort)
        {
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(serverIp, serverPort);
            Console.WriteLine("Osoblje povezano na TCP server.");

            var networkStream = tcpClient.GetStream();

            // Čitaj zadatke od servera
            byte[] buffer = new byte[1024];
            int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
            string zadatak = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Primljen zadatak: " + zadatak);

            // Pošalji potvrdu nazad
            string potvrda = "Zadatak primljen i izvršen";
            byte[] potvrdaBytes = Encoding.UTF8.GetBytes(potvrda);
            await networkStream.WriteAsync(potvrdaBytes, 0, potvrdaBytes.Length);
            Console.WriteLine("Potvrda poslata serveru.");
        }
        */
        /*
        static async Task RunUdpServer()
        {
            var udpServer = new UdpClient(12345);
            Console.WriteLine("UDP server startovan na portu 12345.");

            while (true)
            {
                var result = await udpServer.ReceiveAsync();
                byte[] data = result.Buffer;
                var gost = Gost11.Deserijalizuj(data);
                Console.WriteLine("Primljen gost preko UDP: " + gost);

                // Posalji potvrdu
                byte[] odgovor = Encoding.UTF8.GetBytes("POTVRDA=OK");
                await udpServer.SendAsync(odgovor, odgovor.Length, result.RemoteEndPoint);
                Console.WriteLine("Poslata potvrda gostu.");
            }
        }
        */
    }
}
