using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Data;

namespace Gost1
{
    public class Program
    {
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

        public static async Task ObradiPoruku(string poruka, UdpClient udpClient)
        {
            int GetBrojApartmana(string p)
            {
                string prefix = "APARTMAN=";
                var deo = p.Split(';').FirstOrDefault(d => d.StartsWith(prefix));
                if (deo != null)
                {
                    if (int.TryParse(deo.Substring(prefix.Length), out int br))
                        return br;
                }
                return -1;
            }

            int brojApartmana = GetBrojApartmana(poruka);
            if (poruka.StartsWith("UKUPNA_CENA"))
            {
                
                //ode ni ne udje
                string[] delovi = poruka.Split(';');
                //int brojApartmana = -1;
                double iznos = 0;
                string troskovi = "";

                foreach (var deo in delovi)
                {
                    if (deo.StartsWith("APARTMAN="))
                    {
                        int.TryParse(deo.Split('=')[1], out brojApartmana);
                    }
                    else if (deo.StartsWith("IZNOS="))
                    {
                        double.TryParse(deo.Split('=')[1], out iznos);
                    }
                    else if (deo.StartsWith("TROSKOVI="))
                    {
                        troskovi = deo.Substring("TROSKOVI=".Length);
                    }
                }

                Console.WriteLine($"\n [SERVER] Boravak u apartmanu {brojApartmana} je završen.");
                Console.WriteLine($" Ukupna cena za uplatu: {iznos} EUR.\n");

                if (!string.IsNullOrEmpty(troskovi))
                {
                    Console.WriteLine("Detaljni troškovi:");
                    // Razdvoji i ispiši lepo svaki trošak
                    foreach (var t in troskovi.Split(','))
                    {
                        Console.WriteLine("- " + t);
                    }
                    Console.WriteLine();
                }

            }
            else if (poruka.StartsWith("BORAVAK_ZAVRSEN"))
            {
                Console.WriteLine("\n[SERVER] Boravak je završen! Molimo vas da napustite apartman.\n");

                string[] delovi = poruka.Split(';');
                string ukupno = delovi.FirstOrDefault(x => x.StartsWith("UKUPNO="))?.Split('=')[1] ?? "Nepoznato";
                string troskovi = delovi.FirstOrDefault(x => x.StartsWith("TROSKOVI="))?.Substring("TROSKOVI=".Length) ?? "Nema troškova";


                Console.WriteLine($"Ukupno za uplatu: {ukupno} EUR.");
                Console.WriteLine("Detaljni troškovi:");
                foreach (var t in troskovi.Split(','))
                {
                    Console.WriteLine("- " + t.Trim());
                }
                Console.WriteLine();


                Console.Write("Unesite broj kreditne kartice za plaćanje ili 'X' za odustajanje: ");
                string brojKartice = Console.ReadLine();
                /*if (!string.Equals(brojKartice, "X", StringComparison.OrdinalIgnoreCase))
                {
                    if (brojApartmana == -1)
                    {
                        string prefix = "APARTMAN=";
                        var deo = delovi.FirstOrDefault(d => d.StartsWith(prefix));
                        if (deo != null) int.TryParse(deo.Substring(prefix.Length), out brojApartmana);
                    }

                    string porukaKartica = $"POTVRDA_PLACANJA;APARTMAN={brojApartmana};KARTICA={brojKartice}";
                    byte[] porukaKarticaBytes = Encoding.UTF8.GetBytes(porukaKartica);

                    await udpClient.SendAsync(porukaKarticaBytes, porukaKarticaBytes.Length, "127.0.0.1", 12345);

                    udpClient.Client.ReceiveTimeout = 5000; // 5 sekundi

                    try
                    {
                        var odgovor = await udpClient.ReceiveAsync();
                        string odgovorTekst = Encoding.UTF8.GetString(odgovor.Buffer);
                        Console.WriteLine($"[KLIJENT] Odgovor servera: {odgovorTekst}");
                    }
                    catch (SocketException)
                    {
                        Console.WriteLine("[KLIJENT] Nije primljen odgovor od servera (istekao timeout).");
                    }
                }
                else
                {
                    Console.WriteLine("[KLIJENT] Plaćanje nije izvršeno. Možete pokušati ponovo kasnije.");
                }*/
                await PosaljiIPrihvatiOdgovor(udpClient, $"POTVRDA_PLACANJA;APARTMAN={brojApartmana};KARTICA={brojKartice}");

            }
            else
            {
                Console.WriteLine("[SERVER] " + poruka);
            }
        }

        public static async Task Main(string[] args)
        {
            using (var udpClient = new UdpClient())
            {
                int brojNoci;

                string zahtevLista = "ZAHTEV=LISTA";
                byte[] zahtevListaBytes = Encoding.UTF8.GetBytes(zahtevLista);
                await udpClient.SendAsync(zahtevListaBytes, zahtevListaBytes.Length, "127.0.0.1", 12345);

                var odgovorLista = await udpClient.ReceiveAsync();
                string listaApartmana = Encoding.UTF8.GetString(odgovorLista.Buffer);
                Console.WriteLine("[KLIJENT] Lista slobodnih apartmana:\n" + listaApartmana);

                Console.Write("Unesite broj apartmana za rezervaciju: ");
                int brojApartmana = int.Parse(Console.ReadLine());
                Console.Write("Unesite broj gostiju: ");
                int brojGostiju = int.Parse(Console.ReadLine());

                string rezervacija = $"REZERVACIJA;APARTMAN={brojApartmana};GOSTIJU={brojGostiju}";
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

                Console.WriteLine("Rezervacija je uspešno evidentirana.");

                Console.Write("Unesite broj noći: ");
                brojNoci = int.Parse(Console.ReadLine());

                Console.WriteLine($"Unesite podatke za {brojGostiju} gosta/gostiju:");
                List<string> gostiPodaci = new List<string>();

                for (int i = 0; i < brojGostiju; i++)
                {
                    Console.WriteLine($"\nGost {i + 1}:");
                    Console.Write("Ime: ");
                    string ime = Console.ReadLine();
                    Console.Write("Prezime: ");
                    string prezime = Console.ReadLine();
                    Console.Write("Pol: ");
                    string pol = Console.ReadLine();
                    Console.Write("Datum rođenja (dd.MM.yyyy): ");
                    string datumRodjenja = Console.ReadLine();
                    Console.Write("Broj pasoša: ");
                    string brojPasosa = Console.ReadLine();

                    gostiPodaci.Add($"{ime},{prezime},{pol},{datumRodjenja},{brojPasosa}");
                }

                string podaciZaSlanje = $"GOSTI;APARTMAN={brojApartmana};NOCI={brojNoci};";
                for (int i = 0; i < gostiPodaci.Count; i++)
                {
                    podaciZaSlanje += $"GOST{i + 1}={gostiPodaci[i]};";
                }

                byte[] gostiBytes = Encoding.UTF8.GetBytes(podaciZaSlanje);
                await udpClient.SendAsync(gostiBytes, gostiBytes.Length, "127.0.0.1", 12345);

                var odgovorGosti = await udpClient.ReceiveAsync();
                string potvrdaGosti = Encoding.UTF8.GetString(odgovorGosti.Buffer);
                Console.WriteLine("[KLIJENT] Odgovor servera: " + potvrdaGosti);

                Console.WriteLine($"\nGost boravi {brojNoci} sekundi (1 sekunda = 1 noć). Možete slati zahteve osoblju dok traje boravak.");


                DateTime krajBoravka = DateTime.Now.AddSeconds(brojNoci);
                bool boravakZavrsen = false;
                //udpClient.Client.ReceiveTimeout = 100; // doda timeout da ne blokira stalno


                //var listenTask = Task.Run(async () =>
                //{
                //while (DateTime.Now < krajBoravka)
                    //{
                    //while (udpClient.Available > 0)
                    //{
                        // Provera da li je stigla poruka da je boravak završen

                       /* while (!boravakZavrsen)
                       // {
                            try
                            {
                                var odgovor = await udpClient.ReceiveAsync();
                                string poruka = Encoding.UTF8.GetString(odgovor.Buffer);

                                if (poruka.StartsWith("BORAVAK_ZAVRSEN"))
                                {
                                    Console.WriteLine("\n[SERVER1] Boravak je završen! Molimo vas da napustite apartman.");
                                    //break; // izlaz iz petlje jer je boravak gotov
                                    boravakZavrsen = true;
                                }
                                else
                                {
                                    Console.WriteLine("[SERVER] " + poruka);
                                }
                            }
                            catch (Exception)
                            {
                                // ignoriši timeout ako ga koristiš
                            }
                        }*/
                    
            
                //while(DateTime.Now < krajBoravka && !boravakZavrsen) { 
                //while(!boravakZavrsen && DateTime.Now < krajBoravka) {
                while (true) {

                    if (boravakZavrsen)
                        Console.WriteLine("\nBoravak je završen. Meni ostaje aktivan, ali ne možete više slati zahteve serveru.");

                    Console.WriteLine("\nIzaberite opciju:");
                    Console.WriteLine("1 - Aktivacija alarma");
                    Console.WriteLine("2 - Upravljanje minibarom");
                    Console.WriteLine("3 - Zavrsno ciscenje");
                    Console.WriteLine("4 - Trazeno čišćenje apartmana");
                    Console.WriteLine("X - Izlaz iz menija");

                    Console.Write("Unos: ");
                    string opcija = Console.ReadLine();

                    if (opcija.ToUpper() == "X")
                    {
                        string zahtevZaCenu = $"UKUPNA_CENA;APARTMAN={brojApartmana}";
                        byte[] zahtevBytes = Encoding.UTF8.GetBytes(zahtevZaCenu);
                        await udpClient.SendAsync(zahtevBytes, zahtevBytes.Length, "127.0.0.1", 12345);

                        var odgovorCena = await udpClient.ReceiveAsync();
                        string poruka = Encoding.UTF8.GetString(odgovorCena.Buffer);

                        await ObradiPoruku(poruka,udpClient);

                        //if (poruka.StartsWith("UKUPNA_CENA"))
                        if (poruka.Contains("UKUPNA_CENA"))

                        {
                            string[] deloviCene = poruka.Split(';');
                            string iznos = deloviCene.FirstOrDefault(x => x.StartsWith("IZNOS="))?.Split('=')[1] ?? "Nepoznato";
                            Console.WriteLine($"\nUkupna cena boravka: {iznos} EUR\n");
                        }
                        else
                        {
                            Console.WriteLine("[KLIJENT] Odgovor servera: " + poruka);
                        }
                        Console.WriteLine("Pritisnite bilo koji taster za izlaz...");
                        Console.ReadKey();
                        break;
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
                        zahteviZaSlanje.Add($"AKCIJA=CISCENJE2;APARTMAN={brojApartmana}");
                        /*while (true)
                        {
                            var odgovor = await udpClient.ReceiveAsync();
                            string poruka = Encoding.UTF8.GetString(odgovor.Buffer);

                            if (poruka.StartsWith("BORAVAK_ZAVRSEN"))
                            {
                                Console.WriteLine("\n[SERVER] Boravak je završen! Molimo vas da napustite apartman.");
                                break;
                            }
                            else
                            {
                                Console.WriteLine("[SERVER] " + poruka);
                            }
                        }
                        break; */
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

                        await ObradiPoruku(poruka,udpClient);
                        //ObradiPoruku(poruka);

                        //proba
                        if (poruka.StartsWith("BORAVAK_ZAVRSEN"))
                        {
                            boravakZavrsen = true;  // ovo je ključno
                            Console.WriteLine("Boravak je završen.");
                            break; // prekidamo slanje daljih zahteva jer je boravak završen
                        }
                    }
                    //Console.WriteLine(boravakZavrsen);

                    //Console.WriteLine("");
                }
                //Console.WriteLine("\nBoravak je završen. Pritisni bilo koji taster za kraj...");
                //Console.ReadKey();
            }
            

        }
    }
}
