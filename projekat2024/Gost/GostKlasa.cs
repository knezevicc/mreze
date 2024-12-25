using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Gost
{
    public class GostKlasa
    {
        private readonly string serverAddress = "127.0.0.1"; 
        private readonly int port = 9090; 

        public void SendReservation()
        {
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(serverAddress), port);

                string reservationData = "Guest reservation: Room 101, 2 guests, 3 nights";
                byte[] data = Encoding.UTF8.GetBytes(reservationData);

                udpSocket.SendTo(data, serverEndPoint);
                Console.WriteLine($"Reservation sent: {reservationData}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                udpSocket.Close(); 
            }
            /*
            using (UdpClient udpClient = new UdpClient())
            {
                udpClient.Connect(serverAddress, port);

                string reservationData = "Guest reservation: Room 101, 2 guests, 3 nights";
                byte[] data = Encoding.UTF8.GetBytes(reservationData);

                udpClient.Send(data, data.Length);

                Console.WriteLine($"Reservation sent: {reservationData}");
            }
            */
        }
    }
}
