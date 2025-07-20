using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Osoblje1
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.WriteLine("Izaberite funkciju osoblja:");
            Console.WriteLine("1 - Ciscenje");
            Console.WriteLine("2 - Minibar");
            Console.WriteLine("3 - Alarm");
            Console.Write("Unesite opciju: ");
            string opcija = Console.ReadLine();

            string funkcija;
            switch (opcija)
            {
                case "1":
                    funkcija = "Ciscenje";
                    break;
                case "2":
                    funkcija = "UpravljanjeMinibarem";
                    break;
                case "3":
                    funkcija = "SanacijaAlarma";
                    break;
                default:
                    funkcija = "Ciscenje";
                    break;
            }


            await TcpOsobljeClient("127.0.0.1", 12346, funkcija);
            Console.WriteLine("Pritisni bilo koji taster za kraj...");
            Console.ReadKey();
        }

        static async Task TcpOsobljeClient(string serverIp, int serverPort, string funkcija)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync(serverIp, serverPort);
                Console.WriteLine($"[OSOBLJE] Povezano na server putem TCP kao {funkcija}.");

                using (NetworkStream networkStream = tcpClient.GetStream())
                {
                    // 1️⃣ Pošalji funkciju serveru odmah po konekciji
                    string porukaFunkcija = $"FUNKCIJA={funkcija}";
                    byte[] funkcijaBytes = Encoding.UTF8.GetBytes(porukaFunkcija);
                    await networkStream.WriteAsync(funkcijaBytes, 0, funkcijaBytes.Length);
                    Console.WriteLine($"[OSOBLJE] Poslata funkcija serveru: {porukaFunkcija}");

                    byte[] buffer = new byte[1024];

                    while (true)
                    {
                        int bytesRead = 0;

                        try
                        {
                            bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                            if (bytesRead == 0)
                            {
                                Console.WriteLine("[OSOBLJE] Server je zatvorio konekciju.");
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[OSOBLJE] Greska: {ex.Message}");
                            return;
                        }

                        string zadatak = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"\n[OSOBLJE] Primljen zadatak: {zadatak}");

                        if (zadatak == "NEMA_ZADATAKA")
                        {
                            Console.WriteLine("[OSOBLJE] Trenutno nema zadataka. Čekam sledeći...");
                            await Task.Delay(2000);
                            continue;
                        }

                        // Ako želiš možeš ovde dodatno proveriti da li je zadatak iz tvoje funkcije
                        // ali server već filtrira šta šalje

                        // Izvuci broj apartmana
                        string brojApartmana = "0";
                        string vrstaZadatka = "";
                        var delovi = zadatak.Split(';');
                        foreach (var deo in delovi)
                        {
                            if (deo.StartsWith("APARTMAN="))
                                brojApartmana = deo.Split('=')[1];
                            else if (deo.StartsWith("ZADATAK="))
                                vrstaZadatka = deo.Split('=')[1];
                        }

                        Console.WriteLine($"[OSOBLJE] Obrada zadatka: {vrstaZadatka} za apartman {brojApartmana}");

                        // Potvrda serveru
                        string potvrda = $"Zadatak primljen i izvrsen;VRSTA={vrstaZadatka};APARTMAN={brojApartmana}";
                        byte[] potvrdaBytes = Encoding.UTF8.GetBytes(potvrda);
                        await networkStream.WriteAsync(potvrdaBytes, 0, potvrdaBytes.Length);
                        Console.WriteLine($"[OSOBLJE] Poslata potvrda serveru: {potvrda}");

                        await Task.Delay(1000);
                    }
                }
            }
        }
    }
}
