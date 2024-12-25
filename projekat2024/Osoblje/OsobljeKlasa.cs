using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Osoblje
{
    public class OsobljeKlasa
    {
        private readonly string serverAddress = "127.0.0.1"; 
        private readonly int port = 9090; 

        public void SendRequest()
        {
            Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                // Povezivanje na server
                tcpSocket.Connect(new IPEndPoint(IPAddress.Parse(serverAddress), port));

                string requestData = "Staff request: Clean room 101\n";
                byte[] requestBytes = Encoding.UTF8.GetBytes(requestData);

                // Slanje zahteva serveru
                tcpSocket.Send(requestBytes);
                Console.WriteLine($"Request sent: {requestData}");

                // Čitanje odgovora sa servera
                byte[] responseBuffer = new byte[256];
                int bytesRead = tcpSocket.Receive(responseBuffer);
                string response = Encoding.UTF8.GetString(responseBuffer, 0, bytesRead);

                Console.WriteLine($"Server response: {response}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                tcpSocket.Close(); // Zatvaranje utičnice
            }
        }
        /*
        using (TcpClient client = new TcpClient(serverAddress, port))
        {
            using (NetworkStream stream = client.GetStream())
            {
                byte[] requestData = Encoding.UTF8.GetBytes("Staff request: Clean room 101\n");
                stream.Write(requestData, 0, requestData.Length);

                byte[] responseData = new byte[256];
                int bytesRead = stream.Read(responseData, 0, responseData.Length);
                Console.WriteLine($"Server response: {Encoding.UTF8.GetString(responseData, 0, bytesRead)}");
            }
        }
       */
    }
}
