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
            await TcpOsobljeClient("127.0.0.1", 12346);
            Console.WriteLine("Pritisni bilo koji taster za kraj...");
            Console.ReadKey();
        }



        static async Task TcpOsobljeClient(string serverIp, int serverPort)
        {
            using (TcpClient tcpClient = new TcpClient())
            {
                await tcpClient.ConnectAsync(serverIp, serverPort);
                Console.WriteLine("[OSOBLJE] Povezano na server putem TCP.");

                using (NetworkStream networkStream = tcpClient.GetStream())
                {
                    byte[] buffer = new byte[1024];

                    while (true)
                    {
                        int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0)
                        {
                            Console.WriteLine("[OSOBLJE] Server je zatvorio konekciju.");
                            break;
                        }

                        string zadatak = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"\n[OSOBLJE] Primljen zadatak: {zadatak}");

                        if (zadatak == "NEMA_ZADATAKA")
                        {
                            Console.WriteLine("[OSOBLJE] Trenutno nema zadataka. Čekam sledeći...");
                            await Task.Delay(2000);
                            continue;
                        }

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

                        // Meni redom: 1 Alarm, 2 Minibar, 3 Ciscenje
                        Console.WriteLine("\nIzaberite zadatak koji želite da izvršite:");
                        Console.WriteLine("1 - Sanacija alarma");
                        Console.WriteLine("2 - Upravljanje minibarem");
                        Console.WriteLine("3 - Čišćenje apartmana");
                        Console.WriteLine("X - Izlaz");

                        Console.Write("Unesite opciju: ");
                        string opcija = Console.ReadLine();

                        if (opcija.ToUpper() == "X")
                        {
                            Console.WriteLine("[OSOBLJE] Završavam rad.");
                            break;
                        }

                        string vrstaZadatka1 = zadatak.Split(';')[0].Split('=')[1];  // izvuci vrstu zadatka iz primljenog stringa "ZADATAK=..."
                        string potvrda = $"Zadatak primljen i izvrsen;VRSTA={vrstaZadatka1};APARTMAN={brojApartmana}";

                        //string potvrda = $"Zadatak primljen i izvrsen;APARTMAN={brojApartmana};VRSTA={vrstaZadatka}";
                        byte[] potvrdaBytes = Encoding.UTF8.GetBytes(potvrda);
                        await networkStream.WriteAsync(potvrdaBytes, 0, potvrdaBytes.Length);
                        Console.WriteLine($"[OSOBLJE] Poslata potvrda serveru: {potvrda}");

                        await Task.Delay(1000);
                    }

                    Console.WriteLine("[OSOBLJE] Konekcija zatvorena.");
                }
            }


        }
            /*{
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(serverIp, serverPort);
            Console.WriteLine("Povezano na server putem TCP.");

            var networkStream = tcpClient.GetStream();

            // Čitaj zadatke od servera
            byte[] buffer = new byte[1024];
            int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
            string zadatak = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Primljen zadatak: " + zadatak);

            if (zadatak.StartsWith("ZADATAK"))
            {
                Console.WriteLine("Izaberite zadatak koji želite da izvršite:");
                Console.WriteLine("1 - Čišćenje apartmana");
                Console.WriteLine("2 - Sanacija alarma");
                Console.WriteLine("3 - Upravljanje minibarem");

                Console.Write("Unesite opciju: ");
                string opcija = Console.ReadLine();

                // Izvuci broj apartmana iz primljenog zadatka (npr: "ZADATAK=Ciscenje;APARTMAN=101")
                string brojApartmana = "0";
                var delovi = zadatak.Split(';');
                foreach (var deo in delovi)
                {
                    if (deo.StartsWith("APARTMAN="))
                    {
                        brojApartmana = deo.Split('=')[1];
                        break;
                    }
                }

                string potvrda = $"Zadatak primljen i izvrsen;APARTMAN={brojApartmana}";

                /*
                string potvrda = "";
                switch (opcija)
                {
                    case "1":
                        potvrda = "Zadatak primljen i izvrsen: Ciscenje apartmana";
                        break;
                    case "2":
                        potvrda = "Zadatak primljen i izvrsen: Sanacija alarma";
                        break;
                    case "3":
                        potvrda = "Zadatak primljen i izvrsen: Upravljanje minibarem";
                        break;
                    default:
                        potvrda = "Nepoznata opcija, ali zadatak izvrsen";
                        break;
                }
                //
                byte[] potvrdaBytes = Encoding.UTF8.GetBytes(potvrda);
                await networkStream.WriteAsync(potvrdaBytes, 0, potvrdaBytes.Length);
                Console.WriteLine($"[OSOBLJE] Poslata potvrda serveru: {potvrda}");
            }
            else
            {
                Console.WriteLine("[OSOBLJE] Trenutno nema zadataka.");
            }

            await Task.Delay(1000);
            tcpClient.Close();
            Console.WriteLine("[OSOBLJE] Konekcija zatvorena.");
        }*/
    }
}
