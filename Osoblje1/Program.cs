using System;
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
            var tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(serverIp, serverPort);
            Console.WriteLine("Povezano na server putem TCP.");

            var networkStream = tcpClient.GetStream();

            // Čitaj zadatke od servera
            byte[] buffer = new byte[1024];
            int bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length);
            string zadatak = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            Console.WriteLine("Primljen zadatak: " + zadatak);

            // Simuliraj izvršenje zadatka
            Console.WriteLine("Izvršavam zadatak...");
            await Task.Delay(2000);

            // Pošalji potvrdu nazad
            string potvrda = "Zadatak primljen i izvršen";
            byte[] potvrdaBytes = Encoding.UTF8.GetBytes(potvrda);
            await networkStream.WriteAsync(potvrdaBytes, 0, potvrdaBytes.Length);
            Console.WriteLine("Potvrda poslata serveru.");

            tcpClient.Close();

        }
    }
}
