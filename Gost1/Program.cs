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

                await udpClient.SendAsync(porukaBytes, porukaBytes.Length, "127.0.0.1", 12345);
                Console.WriteLine("[KLIJENT] Poslata rezervacija serveru.");

                var result = await udpClient.ReceiveAsync();
                string potvrda = Encoding.UTF8.GetString(result.Buffer);
                Console.WriteLine("[KLIJENT] Odgovor servera: " + potvrda);
            }

            Console.WriteLine($"\nGost boravi {brojNoci} sekundi (1 sekunda = 1 noć). Možete slati zahteve osoblju dok traje boravak.");

            DateTime krajBoravka = DateTime.Now.AddSeconds(brojNoci);

            while (DateTime.Now < krajBoravka)
            {
                Console.WriteLine("\nIzaberite opciju:");
                Console.WriteLine("1 - Aktivacija alarma");
                Console.WriteLine("2 - Upravljanje minibarom (uzima se 1 voda)");
                Console.WriteLine("3 - Zahtev za čišćenje apartmana");
                Console.WriteLine("X - Izlaz iz menija dok traje boravak");

                Console.Write("Unos: ");
                string opcija = Console.ReadLine();

                if (opcija.ToUpper() == "X")
                    break;

                string zahtev;
                if (opcija == "1")
                    zahtev = $"ZAHTEV=AktivirajAlarm;APARTMAN={brojApartmana}";
                else if (opcija == "2")
                    zahtev = $"ZAHTEV=MinibarVoda;APARTMAN={brojApartmana}";
                else if (opcija == "3")
                    zahtev = $"ZAHTEV=Ciscenje;APARTMAN={brojApartmana}";
                else
                    zahtev = "NEPOZNAT_ZAHTEV";

                using (TcpClient tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync("127.0.0.1", 12346);
                    NetworkStream stream = tcpClient.GetStream();

                    byte[] zahtevBytes = Encoding.UTF8.GetBytes(zahtev);
                    await stream.WriteAsync(zahtevBytes, 0, zahtevBytes.Length);
                    Console.WriteLine($"[KLIJENT] Poslat zahtev: {zahtev}");

                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string odgovor = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"[KLIJENT] Odgovor servera: {odgovor}");
                }
            }
            /*
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

                Console.WriteLine($"Tokom boravka ({brojNoci} sekundi) možete slati zahteve:");
                Console.WriteLine("1 - Aktiviraj alarm");
                Console.WriteLine("2 - Uzmi iz minibara");
                Console.WriteLine("3 - Zatraži čišćenje sobe");

                DateTime krajBoravka = DateTime.Now.AddSeconds(brojNoci);

                while (DateTime.Now < krajBoravka)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true).Key;
                        string zahtev = "";

                        if (key == ConsoleKey.D1)
                        {
                            zahtev = $"AKCIJA=ALARM;APARTMAN={brojApartmana}";
                        }
                        else if (key == ConsoleKey.D2)
                        {
                            zahtev = $"AKCIJA=MINIBAR;APARTMAN={brojApartmana}";
                        }
                        else if (key == ConsoleKey.D3)
                        {
                            zahtev = $"AKCIJA=CISCENJE;APARTMAN={brojApartmana}";
                        }

                        if (!string.IsNullOrEmpty(zahtev))
                        {
                            byte[] zahtevBytes = Encoding.UTF8.GetBytes(zahtev);
                            await udpClient.SendAsync(zahtevBytes, zahtevBytes.Length, "127.0.0.1", 12345);
                            Console.WriteLine($"[KLIJENT] Poslat zahtev: {zahtev}");

                            var response = await udpClient.ReceiveAsync();
                            string odgovor = Encoding.UTF8.GetString(response.Buffer);
                            Console.WriteLine($"[KLIJENT] Odgovor servera: {odgovor}");
                        }
                    }

                    await Task.Delay(100); // smanjuje CPU opterećenje
                }
            }*/

            // Sačekaj server da radi (za demo)
            Console.WriteLine("Pritisni bilo koji taster za kraj...");
            Console.ReadKey();
        }
        
    }
}
