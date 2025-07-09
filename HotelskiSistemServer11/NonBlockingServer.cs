using Gost1;
using HotelskiSistemServer11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class NonBlockingServer
{
    private Socket udpSocket;
    private Socket tcpListenSocket;
    private List<Socket> tcpClients = new List<Socket>();
    private List<Apartman11> apartmani = new List<Apartman11>();
    private int udpPort, tcpPort;

    private DateTime? vremeRezervacije = null;

    private IPEndPoint lastGuestEndPoint;

    // Mapiramo IPEndPoint gosta na apartman (za slanje statusa)
    //private Dictionary<IPEndPoint, Apartman11> gostApartmanMap = new Dictionary<IPEndPoint, Apartman11>();

    public NonBlockingServer(int udpPort, int tcpPort)
    {
        this.udpPort = udpPort;
        this.tcpPort = tcpPort;

        //brAp,sprat,klasa,maxGostiju
        apartmani.Add(new Apartman11(101, 1, 3, 4));
        apartmani.Add(new Apartman11(102, 1, 2, 2));
        apartmani.Add(new Apartman11(201, 2, 1, 5));

        // TEST: Postavi stanje i alarm da vidiš da li server šalje zadatke
        apartmani[0].Stanje = StanjeApartmana.PotrebnoCiscenje;  // Apartman 101 čeka čišćenje
        apartmani[1].Alarm = StanjeAlarma.Aktivirano;            // Apartman 102 ima alarm aktiviran
        apartmani[2].StanjeMinibara["Voda"] = 3;                 // Apartman 201 ima minibar sa niskim zalihama
    
}

    public void Start()
    {
        udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        udpSocket.Bind(new IPEndPoint(IPAddress.Any, udpPort));
        udpSocket.Blocking = false;

        tcpListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        tcpListenSocket.Bind(new IPEndPoint(IPAddress.Any, tcpPort));
        tcpListenSocket.Listen(10);
        tcpListenSocket.Blocking = false;

        Console.WriteLine($"[UDP] Listening on port {udpPort}");
        Console.WriteLine($"[TCP] Listening on port {tcpPort}");

        byte[] udpBuffer = new byte[4096];
        byte[] tcpBuffer = new byte[4096];

        ////////////////
        //Apartman11 apartman = apartmani[0];

        while (true)
        {
            ///////////////////////////////
            /*
            if (vremeRezervacije.HasValue)
            {
                TimeSpan elapsed = DateTime.Now - vremeRezervacije.Value;
                if (elapsed.TotalSeconds >= 5 && apartman.Stanje == StanjeApartmana.Zauzet)
                {
                    //Apartman11 apartman = apartmani[0];


                    apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                    Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} status promenjen u PotrebnoCiscenje.");
                    vremeRezervacije = null; // resetujemo vreme da se ovo ne ponavlja

                }
            }*/
            // UDP - primanje rezervacija
            if (udpSocket.Available > 0)
            {
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                int received = udpSocket.ReceiveFrom(udpBuffer, ref remoteEP);
                //var gostEndPoint = (IPEndPoint)remoteEP;
                lastGuestEndPoint = (IPEndPoint)remoteEP;

                byte[] receivedData = new byte[received];
                Array.Copy(udpBuffer, receivedData, received);

                string poruka = Encoding.UTF8.GetString(receivedData);
                string[] delovi = poruka.Split(';');
                if (delovi.Length >= 3 &&
                    int.TryParse(delovi[0].Split('=')[1], out int brojApartmana) &&
                    int.TryParse(delovi[1].Split('=')[1], out int brojGostiju) &&
                    int.TryParse(delovi[2].Split('=')[1], out int brojNoci))
                {
                    Apartman11 apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojApartmana);
                if (apartman != null)
                {
                    apartman.TrenutniBrojGostiju = brojGostiju;
                    apartman.PreostaleNoci = brojNoci;
                    apartman.Stanje = StanjeApartmana.Zauzet;
                    Console.WriteLine($"[UDP] Gost prijavljen: Apartman={brojApartmana}, Gostiju={brojGostiju}, Noci={brojNoci}");

                    string potvrda = $"POTVRDA=OK;APARTMAN={brojApartmana}";
                    byte[] potvrdaBytes = Encoding.UTF8.GetBytes(potvrda);
                    udpSocket.SendTo(potvrdaBytes, remoteEP);
                }
                else
                {
                    string error = "GRESKA=Apartman ne postoji";
                    byte[] errorBytes = Encoding.UTF8.GetBytes(error);
                    udpSocket.SendTo(errorBytes, remoteEP);
                }

                ///////////////////////////////////////////////
                /*
                Gost11 gost = Gost11.Deserijalizuj(receivedData);
                Console.WriteLine($"[UDP] Rezervacija od gosta: {gost}");

                //Apartman11 apartman = apartmani[0];
                apartman.TrenutniBrojGostiju++;
                apartman.Stanje = StanjeApartmana.Zauzet;
                vremeRezervacije = DateTime.Now;
                
                // Pamti koji gost je dobio koji apartman
                //gostApartmanMap[gostEndPoint] = apartman;

                string potvrda = $"POTVRDA=OK;APARTMAN={apartman.BrojApartmana}";
                byte[] potvrdaBytes = Encoding.UTF8.GetBytes(potvrda);
                udpSocket.SendTo(potvrdaBytes, remoteEP);
                Console.WriteLine($"[UDP] Poslata potvrda: {potvrda}");
                */


            }

            // TCP - prihvatanje novih konekcija osoblja
            if (tcpListenSocket.Poll(0, SelectMode.SelectRead))
            {
                Socket client = tcpListenSocket.Accept();
                client.Blocking = false;
                tcpClients.Add(client);
                Console.WriteLine($"[TCP] New client connected: {client.RemoteEndPoint}");

                bool zadatakPoslat = false;
                foreach (var apartman in apartmani)
                {
                    if (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje)
                    {
                        string zadatak = $"ZADATAK=Ciscenje;APARTMAN={apartman.BrojApartmana}";
                        byte[] data = Encoding.UTF8.GetBytes(zadatak);
                        client.Send(data);
                        Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");
                        zadatakPoslat = true;
                        break; // pošalji samo jedan zadatak odjednom
                    }
                }

                if (!zadatakPoslat)
                {
                    byte[] data = Encoding.UTF8.GetBytes("NEMA_ZADATAKA");
                    client.Send(data);
                    Console.WriteLine($"[TCP] Trenutno nema zadataka za osoblje.");
                }

                ////////////////////////////////////////////
                /*
                // Salji zadatak samo ako je apartman zauzet (dakle gost je rezervisao)
                //ovde sam izmenila samo ako je potrebno ciscenje
                if (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje)
                {
                    string zadatak = $"ZADATAK=Ciscenje;APARTMAN={apartman.BrojApartmana}";
                    byte[] data = Encoding.UTF8.GetBytes(zadatak);
                    client.Send(data);
                    Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak} -> {client.RemoteEndPoint}");
                }
                else
                {
                    string nema = "NEMA_ZADATAKA";
                    byte[] data = Encoding.UTF8.GetBytes(nema);
                    client.Send(data);
                    Console.WriteLine($"[TCP] Trenutno nema zadataka za osoblje.");
                }
                */
            }
            // TCP - obrada odgovora osoblja
            for (int i = tcpClients.Count - 1; i >= 0; i--)
            {
                Socket client = tcpClients[i];
                if (client.Available > 0)
                {
                    int received2 = client.Receive(tcpBuffer);
                    string response = Encoding.UTF8.GetString(tcpBuffer, 0, received2);
                    Console.WriteLine($"[TCP] Received from {client.RemoteEndPoint}: {response}");

                        if (response.Contains("Zadatak primljen i izvrsen"))
                        {
                            foreach (var apartman in apartmani.Where(a => a.Stanje == StanjeApartmana.PotrebnoCiscenje))
                            {
                                apartman.Stanje = StanjeApartmana.Prazan;
                                apartman.TrenutniBrojGostiju = 0;
                                apartman.PreostaleNoci = 0;
                                Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada je Prazan.");
                            }
                        }
                        ///////////////////////////////
                        /*
                        apartman.Stanje = StanjeApartmana.Prazan;
                        apartman.TrenutniBrojGostiju = 0;
                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada je Prazan.");

                        if (lastGuestEndPoint != null)
                        {
                            string status = $"STATUS={apartman.BrojApartmana};STANJE={apartman.Stanje}";
                            byte[] statusBytes = Encoding.UTF8.GetBytes(status);
                            udpSocket.SendTo(statusBytes, lastGuestEndPoint);
                            Console.WriteLine($"[UDP] Poslat status gostu: {status}");
                        }
                        */
                    }
               
                    client.Shutdown(SocketShutdown.Both);
                    client.Close();
                    tcpClients.RemoveAt(i);
                }
            }

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.S)
                {
                        Console.WriteLine("\n----- Trenutno stanje apartmana -----");
                        foreach (var apartman in apartmani)
                    {
                        Console.WriteLine($"[STATUS] Apartman {apartman.BrojApartmana} | Sprat: {apartman.Sprat} | Klasa: {apartman.Klasa} | MaxGostiju: {apartman.MaksimalanBrojGostiju} | TrenutnoGostiju: {apartman.TrenutniBrojGostiju} | PreostaleNoci: {apartman.PreostaleNoci} | Stanje: {apartman.Stanje}");
                        Console.WriteLine($"         Minibar: {string.Join(", ", apartman.StanjeMinibara.Select(x => $"{x.Key}-{x.Value}"))}");
                    }
                        Console.WriteLine("--------------------------------------\n");
                    }
                }
        }
    }
}
