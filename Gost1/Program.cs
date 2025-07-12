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
            using (var udpClient = new UdpClient())
            {

                // 1. Zatraži listu slobodnih apartmana
                string zahtevLista = "ZAHTEV=LISTA";
                byte[] zahtevListaBytes = Encoding.UTF8.GetBytes(zahtevLista);
                await udpClient.SendAsync(zahtevListaBytes, zahtevListaBytes.Length, "127.0.0.1", 12345);

                var odgovorLista = await udpClient.ReceiveAsync();
                string listaApartmana = Encoding.UTF8.GetString(odgovorLista.Buffer);

                Console.WriteLine("[KLIJENT] Lista slobodnih apartmana:\n" + listaApartmana);

                // 2. Unos izbora apartmana i podataka za rezervaciju
                Console.Write("Unesite broj apartmana za rezervaciju: ");
                int brojApartmana = int.Parse(Console.ReadLine());

                Console.Write("Unesite broj gostiju: ");
                int brojGostiju = int.Parse(Console.ReadLine());

                Console.Write("Unesite broj noći: ");
                int brojNoci = int.Parse(Console.ReadLine());

                // 3. Šalji rezervaciju
                string rezervacija = $"REZERVACIJA;APARTMAN={brojApartmana};GOSTIJU={brojGostiju};NOCI={brojNoci}";
                byte[] rezervacijaBytes = Encoding.UTF8.GetBytes(rezervacija);
                await udpClient.SendAsync(rezervacijaBytes, rezervacijaBytes.Length, "127.0.0.1", 12345);

                var odgovorRezervacija = await udpClient.ReceiveAsync();
                string potvrda = Encoding.UTF8.GetString(odgovorRezervacija.Buffer);
                Console.WriteLine("[KLIJENT] Odgovor servera: " + potvrda);

                if (!potvrda.StartsWith("POTVRDA=OK"))
                {
                    Console.WriteLine("Rezervacija nije uspela. Probajte ponovo ili izaberite drugi apartman.");
                    return;
                }

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

                    // Šalji svaki zahtev posebno
                    foreach (var zahtev1 in zahteviZaSlanje)
                    {
                        byte[] zahtevBytes1 = Encoding.UTF8.GetBytes(zahtev1);
                        await udpClient.SendAsync(zahtevBytes1, zahtevBytes1.Length, "127.0.0.1", 12345);
                        var odgovor = await udpClient.ReceiveAsync();
                        Console.WriteLine("[KLIJENT] Odgovor servera: " + Encoding.UTF8.GetString(odgovor.Buffer));
                    }
                }

                Console.WriteLine("Pritisni bilo koji taster za kraj...");
                Console.ReadKey();
            }
        }

        /*radi do izbacivanja liste !
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
                string poruka = $"ZAHTEV;APARTMAN={brojApartmana};GOSTIJU={brojGostiju};NOCI={brojNoci}";
                byte[] porukaBytes = Encoding.UTF8.GetBytes(poruka);
                await udpClient.SendAsync(porukaBytes, porukaBytes.Length, "127.0.0.1", 12345);
                Console.WriteLine("[KLIJENT] Poslata rezervacija serveru.");
                var result = await udpClient.ReceiveAsync();
                string potvrda = Encoding.UTF8.GetString(result.Buffer);
                Console.WriteLine("[KLIJENT] Odgovor servera: " + potvrda);

                if (potvrda.StartsWith("POTVRDA=OK"))
                {
                    Console.WriteLine($"\nGost boravi {brojNoci} sekundi (1 sekunda = 1 noć). Možete slati zahteve osoblju dok traje boravak.");
                }
                else
                {
                    Console.WriteLine("\nRezervacija nije uspela. Pokušajte ponovo ili izaberite drugi apartman.");
                }

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

                    foreach (var zahtev1 in zahteviZaSlanje)
                    {
                        byte[] zahtevBytes1 = Encoding.UTF8.GetBytes(poruka);
                        await udpClient.SendAsync(zahtevBytes1, zahtevBytes1.Length, "127.0.0.1", 12345);
                        var odgovor = await udpClient.ReceiveAsync();
                        Console.WriteLine("[KLIJENT] Odgovor servera: " + Encoding.UTF8.GetString(odgovor.Buffer));
                    }
                }

            }
            // Sačekaj server da radi (za demo)
            Console.WriteLine("Pritisni bilo koji taster za kraj...");
            Console.ReadKey();
        }*/

    }
}
