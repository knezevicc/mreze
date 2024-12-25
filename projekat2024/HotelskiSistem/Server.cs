using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace HotelskiSistem{    
    class Server
    {
        private int udpPort;
        private int tcpPort;

        public Server(int udpPort, int tcpPort)
        {
            this.udpPort = udpPort;
            this.tcpPort = tcpPort;
        }

        public void ListenForUdpMessages()
        {
            Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            IPEndPoint udpEndPoint = new IPEndPoint(IPAddress.Any, udpPort);

            udpSocket.Bind(udpEndPoint);
            Console.WriteLine($"UDP server listening on port {udpPort}...");

            byte[] buffer = new byte[1024];
            while (true)
            {
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                int receivedBytes = udpSocket.ReceiveFrom(buffer, ref remoteEndPoint);
                string message = Encoding.ASCII.GetString(buffer, 0, receivedBytes);

                Console.WriteLine($"UDP Message received: {message}");
            }
            /*
            using (UdpClient udpServer = new UdpClient(udpPort))
            {
                Console.WriteLine($"UDP server listening on port {udpPort}...");
                IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, udpPort);

                while (true)
                {
                    byte[] receivedData = udpServer.Receive(ref remoteEndPoint);
                    string receivedMessage = Encoding.ASCII.GetString(receivedData);
                    Console.WriteLine($"UDP Message received: {receivedMessage}");
                }
            }
            */
        }
        public void ListenForTcpClients()
        {
            Socket tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint tcpEndPoint = new IPEndPoint(IPAddress.Any, tcpPort);

            tcpSocket.Bind(tcpEndPoint);
            tcpSocket.Listen(10); 
            Console.WriteLine($"TCP server listening on port {tcpPort}...");

            while (true)
            {
                Socket clientSocket = tcpSocket.Accept();

                byte[] buffer = new byte[1024];
                int receivedBytes = clientSocket.Receive(buffer);
                string message = Encoding.ASCII.GetString(buffer, 0, receivedBytes);

                Console.WriteLine($"TCP Message received: {message}");

                string responseMessage = "Received your request";
                byte[] response = Encoding.ASCII.GetBytes(responseMessage);
                clientSocket.Send(response);

                clientSocket.Close(); 
            }
            /*
            TcpListener tcpListener = new TcpListener(IPAddress.Any, tcpPort);
            tcpListener.Start();
            Console.WriteLine($"TCP server listening on port {tcpPort}...");

            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                NetworkStream stream = tcpClient.GetStream();

                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                Console.WriteLine($"TCP Message received: {message}");

                byte[] response = Encoding.ASCII.GetBytes("Received your request");
                stream.Write(response, 0, response.Length);

                tcpClient.Close();
            }
            */
        }
    }
}