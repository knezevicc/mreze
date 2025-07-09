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
            ////////////////////////////////////
            ///
            /*
            // Kreiraj gosta
            var gost = new Gost11("Marko", "Markovic", Pol.Muski, new DateTime(1990, 5, 21), "123456789");

            // Serijalizuj gosta
            byte[] gostBytes = gost.Serijalizuj();
            */

            Console.Write("Unesite broj apartmana: ");
            int brojApartmana = int.Parse(Console.ReadLine());

            Console.Write("Unesite broj gostiju: ");
            int brojGostiju = int.Parse(Console.ReadLine());

            Console.Write("Unesite broj noći: ");
            int brojNoci = int.Parse(Console.ReadLine());

            



            using (var udpClient = new UdpClient())
            {
                string poruka = $"APARTMAN={brojApartmana};GOSTIJU={brojGostiju};NOCI={brojNoci}";
                byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);

                //slanje rez serverurr
                await udpClient.SendAsync(porukaBytes, porukaBytes.Length, "127.0.0.1", 12345);
                Console.WriteLine("[KLIJENT] Poslata rezervacija serveru.");

                //osluskuje potvrdu rez
                var result = await udpClient.ReceiveAsync();
                string potvrda = Encoding.UTF8.GetString(result.Buffer);
                Console.WriteLine("[KLIJENT] Odgovor servera: " + potvrda);

                
            }

            // Sačekaj server da radi (za demo)
            Console.WriteLine("Pritisni bilo koji taster za kraj...");
            Console.ReadKey();
        }
        
    }
}
