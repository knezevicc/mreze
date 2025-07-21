
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Data;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.CompilerServices;

namespace Gost1
{
    public class Program
    {
        private static int brojApartmana = -1;
        public static async Task RezervisiApartman(UdpClient udpClient)
        {
            int brojNoci;

            string zahtevKlase = "ZAHTEV=KLASE";
            byte[] zahtevKlaseBytes = Encoding.UTF8.GetBytes(zahtevKlase);
            await udpClient.SendAsync(zahtevKlaseBytes, zahtevKlaseBytes.Length, "127.0.0.1", 12345);

            var odgovorKlase = await udpClient.ReceiveAsync();
            string klaseString = Encoding.UTF8.GetString(odgovorKlase.Buffer);
            Console.WriteLine("[KLIJENT] Slobodne klase apartmana: " + klaseString);

            //Console.Write("Unesite broj apartmana za rezervaciju: ");
            //int brojApartmana = int.Parse(Console.ReadLine());
            // 2. Klijent bira klasu
            Console.Write("Izaberite klasu apartmana: ");
            string izabranaKlasa = Console.ReadLine();

            Console.Write("Unesite broj gostiju: ");
            int brojGostiju = int.Parse(Console.ReadLine());

            //string rezervacija = $"REZERVACIJA;APARTMAN={brojApartmana};GOSTIJU={brojGostiju}";
            string rezervacija = $"REZERVACIJA;KLASA={izabranaKlasa};GOSTIJU={brojGostiju}";
            byte[] rezervacijaBytes = Encoding.UTF8.GetBytes(rezervacija);
            await udpClient.SendAsync(rezervacijaBytes, rezervacijaBytes.Length, "127.0.0.1", 12345);

            var odgovorRezervacija = await udpClient.ReceiveAsync();
            string potvrda = Encoding.UTF8.GetString(odgovorRezervacija.Buffer).Trim();
            Console.WriteLine("[KLIJENT] Odgovor servera: " + potvrda);

            if (!potvrda.StartsWith("POTVRDA=OK"))
            {
                Console.WriteLine("Rezervacija nije uspela. Probajte ponovo ili izaberite drugi apartman.");
                return;
            }

            //Console.WriteLine("Rezervacija je uspešno evidentirana.");
            brojApartmana = potvrda.Split(';')
                .FirstOrDefault(x => x.StartsWith("APARTMAN="))?
                .Split('=')[1] is string brojStr && int.TryParse(brojStr, out int br) ? br : -1;
            Console.WriteLine("Rezervacija je uspešno evidentirana.");

            Console.Write("Unesite broj noći: ");
            brojNoci = int.Parse(Console.ReadLine());

            Console.WriteLine($"Unesite podatke za {brojGostiju} gosta/gostiju:");
            //List<string> gostiPodaci = new List<string>();
            List<Gost11> gosti = new List<Gost11>();

            for (int i = 0; i < brojGostiju; i++)
            {
                Console.WriteLine($"\nGost {i + 1}:");
                Console.Write("Ime: ");
                string ime = Console.ReadLine();
                Console.Write("Prezime: ");
                string prezime = Console.ReadLine();
                Console.Write("Pol: ");
                string pol = Console.ReadLine();

                // Console.Write("Datum rođenja (dd.MM.yyyy): ");
                // string datumRodjenja = Console.ReadLine();
                DateTime datumRodjenja;
                while (true)
                {
                    Console.Write("Datum rođenja (dd.MM.yyyy): ");
                    string unos = Console.ReadLine();

                    if (DateTime.TryParseExact(unos, "dd.MM.yyyy", null, System.Globalization.DateTimeStyles.None, out datumRodjenja))
                        break;

                    Console.WriteLine("Neispravan format. Pokušaj ponovo (primer: 23.07.2001)");
                }


                Console.Write("Broj pasoša: ");
                string brojPasosa = Console.ReadLine();

                //gostiPodaci.Add($"{ime},{prezime},{pol},{datumRodjenja},{brojPasosa}");

                Gost11 gost = new Gost11(ime, prezime, pol, datumRodjenja, brojPasosa);
                gosti.Add(gost);

            }
          

            // Kreiraj objekat koji sadrži i meta podatke (apartman i noći)
            // Možeš napraviti i novu klasu ako želiš, ali za sada ćemo prvo slati ove meta-podatke tekstualno.
            string header = $"GOSTI_BINARNO;APARTMAN={brojApartmana};NOCI={brojNoci};GOSTI=";
            byte[] headerBytes = Encoding.UTF8.GetBytes(header);

            // Serijalizacija liste gostiju
            byte[] combined;
            using (MemoryStream combinedStream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                byte[] separator = Encoding.UTF8.GetBytes(",,,");

                for (int i = 0; i < gosti.Count; i++)
                {
                    using (MemoryStream tempStream = new MemoryStream())
                    {
                        formatter.Serialize(tempStream, gosti[i]);
                        byte[] serializedGost = tempStream.ToArray();
                        combinedStream.Write(serializedGost, 0, serializedGost.Length);

                        // Dodavanje separatoar osim posle poslednjeg
                        if (i < gosti.Count - 1)
                            combinedStream.Write(separator, 0, separator.Length);
                    }
                }

                combined = combinedStream.ToArray();
            }

            // Spajanje
            byte[] finalPacket = new byte[headerBytes.Length + combined.Length];
            Buffer.BlockCopy(headerBytes, 0, finalPacket, 0, headerBytes.Length);
            Buffer.BlockCopy(combined, 0, finalPacket, headerBytes.Length, combined.Length);

            // Pošalji sve
            await udpClient.SendAsync(finalPacket, finalPacket.Length, "127.0.0.1", 12345);

            // Primanje odgovora
            var odgovorGosti = await udpClient.ReceiveAsync();
            string potvrdaGosti = Encoding.UTF8.GetString(odgovorGosti.Buffer);
            Console.WriteLine("[KLIJENT] Odgovor servera: " + potvrdaGosti);

            Console.WriteLine($"\nGost boravi {brojNoci} sekundi (1 sekunda = 1 noć). Možete slati zahteve osoblju dok traje boravak.");

            return;

        }

        public static async Task PosaljiIPrihvatiOdgovor(UdpClient udpClient, string porukaZaSlanje)
        {
            byte[] porukaBytes = Encoding.UTF8.GetBytes(porukaZaSlanje);
            await udpClient.SendAsync(porukaBytes, porukaBytes.Length, "127.0.0.1", 12345);

            try
            {
                udpClient.Client.ReceiveTimeout = 3000; // 3 sekunde
                var odgovor = await udpClient.ReceiveAsync();
                string odgovorTekst = Encoding.UTF8.GetString(odgovor.Buffer);
                Console.WriteLine($"[KLIJENT] Odgovor servera: {odgovorTekst}");
            }
            catch (SocketException)
            {
                Console.WriteLine("[KLIJENT] Server nije poslao odgovor na vreme, pokušajte ponovo.");
            }
        }



        public static async Task Main(string[] args)
        {
            using (var udpClient = new UdpClient())
            {
                await RezervisiApartman(udpClient);

                //DateTime krajBoravka = DateTime.Now.AddSeconds(brojNoci); stari sistem
                bool boravakZavrsen = false;
                var cancellationTokenSource = new CancellationTokenSource();
                var token = cancellationTokenSource.Token;

                // Uvek proverava kraj boravka
                var listenerTask = Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        try
                        {
                            if (udpClient.Available > 0)
                            {
                                var response = await udpClient.ReceiveAsync();
                                string message = Encoding.UTF8.GetString(response.Buffer);

                                if (message.StartsWith("BORAVAK_ZAVRSEN"))
                                {

                                    string[] delovi = message.Split(';');
                                    int brojApartmanaZaPlacanje = -1;
                                    double ukupno = 0;
                                    string troskovi = "";

                                    foreach (var deo in delovi)
                                    {
                                        if (deo.StartsWith("APARTMAN="))
                                            int.TryParse(deo.Substring("APARTMAN=".Length), out brojApartmanaZaPlacanje);
                                        else if (deo.StartsWith("UKUPNO="))
                                            double.TryParse(deo.Substring("UKUPNO=".Length), out ukupno);
                                        else if (deo.StartsWith("TROSKOVI="))
                                            troskovi = deo.Substring("TROSKOVI=".Length);
                                    }

                                    Console.WriteLine($"Podaci za plaćanje:");
                                    Console.WriteLine($"- Apartman: {brojApartmanaZaPlacanje}");
                                    Console.WriteLine($"- Ukupan iznos: {ukupno} EUR");
                                    Console.WriteLine($"- Troškovi: {troskovi}");

                                    Console.WriteLine($"Uskoro stiže račun... pritisnite bilo koje dugme");
                                    // Pamti šta mu server kaže 
                                    if (brojApartmana != brojApartmanaZaPlacanje)
                                    {
                                        Console.WriteLine("Došlo je do zabune oko rezervisanog apartmana javite se na recepciju ili pozovite 0601234567");
                                        Console.ReadKey();
                                        // Zatvaranje slušanja i završetak
                                        cancellationTokenSource.Cancel();
                                        Environment.Exit(0);
                                    }

                                    boravakZavrsen = true;
                                }
                            }
                            else
                            {
                                await Task.Delay(200);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[KLIJENT] Greška pri primanju poruke: {ex.Message}");
                            break;
                        }
                    }
                }, token);



                while (true)
                {

                    if (boravakZavrsen)
                    {

                        Console.WriteLine("\nBoravak je završen. Ne možete više slati zahteve serveru.");

                        Console.Write("Unesite broj kartice za plaćanje: ");
                        string brojKartice = Console.ReadLine();

                        if (brojKartice.ToUpper() == "X")
                        {
                            Console.WriteLine("Bežanija...");
                            Console.ReadKey();
                            cancellationTokenSource.Cancel();
                            await listenerTask;
                            Environment.Exit(0);
                        }

                        string potvrdaPlaćeanja = $"POTVRDA_PLACANJA;APARTMAN={brojApartmana};KARTICA={brojKartice}";
                        byte[] potvrdaBytes = Encoding.UTF8.GetBytes(potvrdaPlaćeanja);

                        await udpClient.SendAsync(potvrdaBytes, potvrdaBytes.Length, "127.0.0.1", 12345);
                        Console.WriteLine("[KLIJENT] Poslata potvrda o plaćanju.");

                        var odgovor = await udpClient.ReceiveAsync();
                        string poruka = Encoding.UTF8.GetString(odgovor.Buffer);

                        if (poruka.StartsWith("PLACANJE_PRIHVACENO"))
                        {
                            Console.WriteLine("\n[SERVER] Plaćanje je uspešno prihvaćeno.");
                            Console.WriteLine("[KLIJENT] Želite li:");
                            Console.WriteLine("1. Pokrenuti novu rezervaciju");
                            Console.WriteLine("2. Izlogovati se (zatvori aplikaciju)");

                            string izbor = Console.ReadLine();

                            if (izbor == "1")
                            {
                                brojApartmana = -1;
                                boravakZavrsen = false;

                                await RezervisiApartman(udpClient);
                                continue; // ponovo pokreni meni
                            }
                            else if (izbor == "2")
                            {
                                Console.WriteLine("Hvala što ste koristili aplikaciju. Doviđenja!");
                                cancellationTokenSource.Cancel();
                                await listenerTask;
                                Environment.Exit(0); // zatvori aplikaciju
                            }
                            else
                            {
                                Console.WriteLine("Nepoznat unos. Aplikacija će se sada zatvoriti.");
                                cancellationTokenSource.Cancel();
                                await listenerTask;
                                Environment.Exit(0);
                            }
                        }
                        else if (poruka.StartsWith("GRESKA"))
                        {
                            Console.WriteLine("\n[SERVER] Došlo je do greške u plaćanju.");
                            Console.ReadKey();
                            // Zatvaranje slušanja i završetak
                            cancellationTokenSource.Cancel();
                            await listenerTask;
                            Environment.Exit(0);
                        }

                    }


                    Console.WriteLine("\nIzaberite opciju:");
                    Console.WriteLine("1 - Aktivacija alarma");
                    Console.WriteLine("2 - Upravljanje minibarom");
                    Console.WriteLine("3 - Zavrsno ciscenje");
                    Console.WriteLine("4 - Trazeno čišćenje apartmana");
                    Console.WriteLine("X - Izlaz iz menija");

                    Console.Write("Unos: ");
                    string opcija = Console.ReadLine();
                    if (boravakZavrsen)
                        continue;

                    if (opcija.ToUpper() == "X")
                    {
                        Console.WriteLine("[KLIJENT] rezervacija je u toku želite li:");
                        Console.WriteLine("1. Sačekati račun");
                        Console.WriteLine("2. Vratiti se u meni");
                        Console.WriteLine("3. Pobeći");

                        string opcijaIzlaza = Console.ReadLine();
                        if (boravakZavrsen && opcijaIzlaza.Trim() != "3")
                            continue;

                        if (opcijaIzlaza.Trim() == "3")
                        {
                            Console.WriteLine("Bežanija...");
                            Console.ReadKey();

                            cancellationTokenSource.Cancel();
                            await listenerTask;
                            Environment.Exit(0);
                        }
                        else if (opcijaIzlaza.Trim() == "2")
                        {
                            continue;
                        }
                        else if (opcijaIzlaza.Trim() == "1")
                        {
                            Console.WriteLine("Hvala na boravku, kada istekne rezervacija dolazi račun...");
                            while (true)
                            {
                                if (boravakZavrsen)
                                    continue;
                            }
                        }

                    }

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
                            if (boravakZavrsen)
                                break;

                            if (string.Equals(artikal, "X", StringComparison.OrdinalIgnoreCase))
                                break;

                            if (artikal == "Pivo" || artikal == "Voda" || artikal == "Cokoladica")
                                zahteviZaSlanje.Add($"AKCIJA=MINIBAR;APARTMAN={brojApartmana};ARTIKAL={artikal}");
                            else
                                Console.WriteLine("Nepoznat artikal, pokušajte ponovo.");
                        }
                        if (boravakZavrsen)
                            continue;

                        if (zahteviZaSlanje.Count == 0)
                        {
                            Console.WriteLine("Niste izabrali nijedan artikal iz minibara.");
                            continue;
                        }
                    }
                    else if (opcija == "3")
                    {
                        zahteviZaSlanje.Add($"AKCIJA=CISCENJE2;APARTMAN={brojApartmana}");

                    }
                    else if (opcija == "4")
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
                        string poruka = Encoding.UTF8.GetString(odgovor.Buffer);
                        //Console.WriteLine("[KLIJENT] Odgovor servera: " + poruka);

                        if (poruka.StartsWith("UKUPNA_CENA"))
                        {
                            string[] deloviCene = poruka.Split(';');
                            string iznos = deloviCene.FirstOrDefault(x => x.StartsWith("IZNOS="))?.Split('=')[1] ?? "Nepoznato";
                            Console.WriteLine($"\nUkupna cena boravka: {iznos} EUR\n");
                        }
                        else
                        {
                            Console.WriteLine("[KLIJENT] Odgovor servera: " + poruka);
                        }

                    }

                }

            }


        }
    }
}

