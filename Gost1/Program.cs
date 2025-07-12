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

            //prijava preko UDP
            using (var udpClient = new UdpClient())
            {
                string poruka = $"APARTMAN={brojApartmana};GOSTIJU={brojGostiju};NOCI={brojNoci}";
                byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);

                await udpClient.SendAsync(porukaBytes, porukaBytes.Length, "127.0.0.1", 12345);
                Console.WriteLine("[KLIJENT] Poslata rezervacija serveru.");

                var result = await udpClient.ReceiveAsync();
                string potvrda = Encoding.UTF8.GetString(result.Buffer);
                Console.WriteLine("[KLIJENT] Odgovor servera: " + potvrda);
            

            Console.WriteLine($"\nGost boravi {brojNoci} sekundi (1 sekunda = 1 noć). Možete slati zahteve osoblju dok traje boravak.");

            DateTime krajBoravka = DateTime.Now.AddSeconds(brojNoci);

            while (DateTime.Now < krajBoravka)
            {
                Console.WriteLine("\nIzaberite opciju:");
                Console.WriteLine("1 - Aktivacija alarma");
                Console.WriteLine("2 - Upravljanje minibarom");
                Console.WriteLine("3 - Zahtev za čišćenje apartmana");
                Console.WriteLine("X - Izlaz iz menija");

                Console.Write("Unos: ");
                string opcija = Console.ReadLine();

                if (opcija.ToUpper() == "X")
                    break;

                //pokusaj slanja preko udp serveru
                List<string> zahteviZaSlanje = new List<string>();

                if (opcija == "1")
                {
                    zahteviZaSlanje.Add($"AKCIJA=ALARM;APARTMAN={brojApartmana}");
                }
                else if (opcija == "2")
                {
                    Console.WriteLine("Unesite naziv artikla iz minibara (Pivo, Voda, Cokoladica), ili 'X' za kraj:");

                    while (true)
                    {
                        Console.Write("Artikal: ");
                        string artikal = Console.ReadLine();
                        if (string.Equals(artikal, "X", StringComparison.OrdinalIgnoreCase))
                            break;

                        if (artikal == "Pivo" || artikal == "Voda" || artikal == "Cokoladica")
                            zahteviZaSlanje.Add($"AKCIJA=MINIBAR;APARTMAN={brojApartmana};ARTIKAL={artikal}");
                        else
                            Console.WriteLine("Nepoznat artikal, pokušajte ponovo.");
                    }

                    if (zahteviZaSlanje.Count == 0)
                    {
                        Console.WriteLine("Niste izabrali nijedan artikal iz minibara.");
                        continue;
                    }
                }
                else if (opcija == "3")
                {
                    zahteviZaSlanje.Add($"AKCIJA=CISCENJE;APARTMAN={brojApartmana}");
                }
                else
                {
                    Console.WriteLine("Nepoznata opcija.");
                    continue;
                }

                foreach (var zahtev in zahteviZaSlanje)
                {
                    byte[] zahtevBytes = Encoding.UTF8.GetBytes(zahtev);
                    await udpClient.SendAsync(zahtevBytes, zahtevBytes.Length, "127.0.0.1", 12345);
                    var odgovor = await udpClient.ReceiveAsync();
                    Console.WriteLine("[KLIJENT] Odgovor servera: " + Encoding.UTF8.GetString(odgovor.Buffer));
                }
            }

            /*
            string zahtev;
            if (opcija == "1")
                zahtev = $"ZAHTEV=AktivirajAlarm;APARTMAN={brojApartmana}";
            else if (opcija == "2") { 

                // Minibar - korisnik bira artikal
                Console.WriteLine("Unesite naziv artikla iz minibara (Pivo, Voda, Cokoladica), ili 'X' za kraj:");
                List<string> izabraniArtikli = new List<string>();

                while (true)
                {
                    Console.Write("Artikal: ");
                    string artikal = Console.ReadLine();

                    if (string.Equals(artikal, "X", StringComparison.OrdinalIgnoreCase))
                        break;

                    if (artikal == "Pivo" || artikal == "Voda" || artikal == "Cokoladica")
                    {
                        izabraniArtikli.Add(artikal);
                    }
                    else
                    {
                        Console.WriteLine("Nepoznat artikal, pokušajte ponovo.");
                    }
                }

                if (izabraniArtikli.Count == 0)
                {
                    Console.WriteLine("Niste izabrali nijedan artikal iz minibara.");
                    continue; // preskoci slanje zahteva ako nije izabrano ništa
                }

                // Saljemo jedan zahtev za svaki artikal posebno
                foreach (var artikal in izabraniArtikli)
                {
                    zahtev = $"AKCIJA=MINIBAR;APARTMAN={brojApartmana};ARTIKAL={artikal}";
                    await PosaljiTcpZahtev(zahtev);
                }
                continue; // vec poslali zahtev, idemo na sledecu iteraciju pž
            }
                /*zahtev = $"ZAHTEV=MinibarVoda;APARTMAN={brojApartmana}";//
            else if (opcija == "3")
                zahtev = $"ZAHTEV=Ciscenje;APARTMAN={brojApartmana}";
            else
                zahtev = "NEPOZNAT_ZAHTEV";

            await PosaljiTcpZahtev(zahtev){ }
            */

        }

            // Sačekaj server da radi (za demo)
            Console.WriteLine("Pritisni bilo koji taster za kraj...");
            Console.ReadKey();
        }
        private static async Task PosaljiTcpZahtev(string zahtev)
        {
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
    }
}
