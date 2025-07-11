using Gost1;
using HotelskiSistemServer11;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class NonBlockingServer
{
    private Socket udpSocket;
    private Socket tcpListenSocket;
    private List<Socket> tcpClients = new List<Socket>();
    private List<Apartman11> apartmani = new List<Apartman11>();
    private int udpPort, tcpPort;

    private Dictionary<int, DateTime> vremePrijaveGosta = new Dictionary<int, DateTime>();

    private IPEndPoint lastGuestEndPoint;

    public NonBlockingServer(int udpPort, int tcpPort)
    {
        this.udpPort = udpPort;
        this.tcpPort = tcpPort;

        //brAp,sprat,klasa,maxGostiju
        apartmani.Add(new Apartman11(101, 1, 3, 4));
        apartmani.Add(new Apartman11(102, 1, 2, 2));
        apartmani.Add(new Apartman11(201, 2, 1, 5));

        //postavljanje minibara
        apartmani[0].StanjeMinibara = new Dictionary<string, int> { { "Pivo", 5 }, { "Voda", 5 }, { "Cokoladica", 5 } };
        apartmani[1].StanjeMinibara = new Dictionary<string, int> { { "Pivo", 5 }, { "Voda", 5 }, { "Cokoladica", 5 } };
        apartmani[2].StanjeMinibara = new Dictionary<string, int> { { "Pivo", 5 }, { "Voda", 6 }, { "Cokoladica", 5 } };

        #region TEST PODACI
        // TEST: Postavi stanje i alarm da vidiš da li server šalje zadatke
        /*
            apartmani[0].Stanje = StanjeApartmana.PotrebnoCiscenje;  // Apartman 101 čeka čišćenje
            apartmani[1].Alarm = StanjeAlarma.Aktivirano;            // Apartman 102 ima alarm aktiviran
            apartmani[2].StanjeMinibara["Voda"] = 3;                 // Apartman 201 ima minibar sa niskim zalihama

            apartmani[0].StanjeMinibara = new Dictionary<string, int> {
        {"Pivo", 5}, {"Voda", 5}, {"Cokoladica", 3}
    };
            apartmani[1].StanjeMinibara = new Dictionary<string, int> {
        {"Pivo", 5}, {"Voda", 5}, {"Cokoladica", 3}
    };
            apartmani[2].StanjeMinibara rrrrr= new Dictionary<string, int> {
        {"Pivo", 5}, {"Voda", 3}, {"Cokoladica", 5}  // Ovde voda ima 3, što je ispod praga 5
    };

            apartmani[0].Stanje = StanjeApartmana.PotrebnoCiscenje;  // Apartman 101 čeka čišćenje
            apartmani[1].Alarm = StanjeAlarma.Aktivirano;
        */
        #endregion
    }

    public async Task Start()
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

        Console.WriteLine("[DEBUG] Server je pokrenut i ulazi u glavnu petlju...");

        while (true)
        {
            #region UDP primanje rezervacija
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

                //
                if (poruka.StartsWith("AKCIJA"))
                {
                    int brojA = int.Parse(delovi[1].Split('=')[1]);
                    Apartman11 apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojA);

                    if (apartman != null)
                    {
                        string akcija = delovi[0].Split('=')[1];
                        string odgovor = "";

                        if (akcija == "ALARM")
                        {
                            apartman.Alarm = StanjeAlarma.Aktivirano;
                            odgovor = "Alarm aktiviran.";
                        }
                        else if (akcija == "MINIBAR")
                        {
                            foreach (var key in apartman.StanjeMinibara.Keys.ToList())
                            {
                                apartman.StanjeMinibara[key] = Math.Max(0, apartman.StanjeMinibara[key] - 1);
                            }
                            odgovor = "Preuzeli ste artikle iz minibara.";
                        }
                        else if (akcija == "CISCENJE")
                        {
                            apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                            odgovor = "Zatraženo čišćenje sobe.";
                        }
                        else
                        {
                            odgovor = "Nepoznata akcija.";
                        }

                        byte[] odgovorBytes = Encoding.UTF8.GetBytes(odgovor);
                        udpSocket.SendTo(odgovorBytes, remoteEP);
                        Console.WriteLine($"[SERVER] {odgovor} za apartman {brojA}");
                    }
                    continue;
                }


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

                        vremePrijaveGosta[brojApartmana] = DateTime.Now;

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
                }
            }
            #endregion

            #region smanjivanje dana
            foreach (var apartman in apartmani)
            {
                if (apartman.Stanje == StanjeApartmana.Zauzet)
                {
                    if (apartman.PreostaleNoci > 0)
                    {
                        apartman.PreostaleNoci--;
                    }

                    if (apartman.PreostaleNoci == 0)
                    {
                        apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                        apartman.TrenutniBrojGostiju = 0;
                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada prelazi u PotrebnoCiscenje.");
                    }
                }
            }
            #endregion

            //zakomentarisano dok pokusavam drugi nacin
            //problem-osoblje odma vraca da nema zad,
            //bez da ceka da se gost prijavi
            #region prihvatanje TCP konekcija
            /*
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
                             // Provera da li je proslo vreme boravka (u sekundama)
                             if (vremePrijaveGosta.TryGetValue(apartman.BrojApartmana, out DateTime vremePrijave))
                             {
                                 double sekundeOdPrijave = (DateTime.Now - vremePrijave).TotalSeconds;
                                 // Praznjenje treba da se javi tek posle trajanja boravka u sekundama (tj. broj noći na prijavi)
                                 if (sekundeOdPrijave >= apartman.PreostaleNoci)
                                 {
                                     string zadatak = $"ZADATAK=Ciscenje;APARTMAN={apartman.BrojApartmana}";
                                     byte[] data = Encoding.UTF8.GetBytes(zadatak);
                                     client.Send(data);
                                     Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");
                                     zadatakPoslat = true;
                                     break;
                                 }
                             }
                             else
                             {
                                 // Ako nemamo podatak o vremenu, šalji odmah
                                 string zadatak = $"ZADATAK=Ciscenje;APARTMAN={apartman.BrojApartmana}";
                                 byte[] data = Encoding.UTF8.GetBytes(zadatak);
                                 client.Send(data);
                                 Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");
                                 zadatakPoslat = true;
                                 break;
                             }
                         }
                         else if (apartman.Alarm == StanjeAlarma.Aktivirano)
                         {
                             string zadatak = $"ZADATAK=SanacijaAlarma;APARTMAN={apartman.BrojApartmana}";
                             byte[] data = Encoding.UTF8.GetBytes(zadatak);
                             client.Send(data);
                             Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");
                             zadatakPoslat = true;
                             break;
                         }
                         else if (apartman.StanjeMinibara.Values.Any(v => v < 5))
                         {
                             string zadatak = $"ZADATAK=UpravljanjeMinibarem;APARTMAN={apartman.BrojApartmana}";
                             byte[] data = Encoding.UTF8.GetBytes(zadatak);
                             client.Send(data);
                             Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");
                             zadatakPoslat = true;
                             break;
                         }
                     }

                     if (!zadatakPoslat)
                     {
                         //await Task.Delay(1000);

                         byte[] data = Encoding.UTF8.GetBytes("NEMA_ZADATAKA");
                         client.Send(data);
                         Console.WriteLine($"[TCP] Trenutno nema zadataka za osoblje.");

                     }

                 }*/
            #endregion

            #region pokusaj 2 prihvatanje tcp konekcija

            if (tcpListenSocket.Poll(0, SelectMode.SelectRead))
            {
                Socket client = tcpListenSocket.Accept();
                client.Blocking = false;
                tcpClients.Add(client);
                Console.WriteLine($"[TCP] Novo osoblje povezano: {client.RemoteEndPoint}");
            }
            PosaljiZadatkeOsobljuAkoPostoje();

            #endregion

            #region TCP obrada odgovora osoblja

            // TCP - obrada odgovora osoblja
            for (int i = tcpClients.Count - 1; i >= 0; i--)
                {
                    Socket client = tcpClients[i];
                    if (client.Available > 0)
                    {
                        //Console.WriteLine($"[DEBUG] Podaci dostupni za čitanje od {client.RemoteEndPoint}");
                        int received2 = client.Receive(tcpBuffer);

                    if (received2 == 0) {
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        tcpClients.RemoveAt(i);
                        continue;
                    }
                    string response = Encoding.UTF8.GetString(tcpBuffer, 0, received2);
                    Console.WriteLine("$\"[TCP] Primljena potvrda od osoblja: {response}\"");
                    Console.WriteLine($"[TCP] Received from {client.RemoteEndPoint}: {response}");

                        if (response.Contains("Zadatak primljen i izvrsen"))
                        {
                            foreach (var apartman in apartmani)
                            {
                                if (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje)
                                {
                                    apartman.Stanje = StanjeApartmana.Prazan;
                                    Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada je Prazan.");
                                }
                                if (apartman.Alarm == StanjeAlarma.Aktivirano)
                                {
                                    apartman.Alarm = StanjeAlarma.Normalno;
                                    Console.WriteLine($"[SERVER] Alarm u apartmanu {apartman.BrojApartmana} sada je deaktiviran.");
                                }
                                if (apartman.StanjeMinibara.Values.Any(v => v < 5))
                                {
                                    foreach (var key in apartman.StanjeMinibara.Keys.ToList())
                                    {
                                        apartman.StanjeMinibara[key] = 5;
                                    }
                                    Console.WriteLine($"[SERVER] Minibar u apartmanu {apartman.BrojApartmana} dopunjen.");
                                }
                            }
                        }
                    }
                /*client.Shutdown(SocketShutdown.Both);
                client.Close();
                tcpClients.RemoveAt(i);*/

            }
            #endregion

            #region Trenutno stanje apartmana
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
            #endregion
        }
    }
    private void PosaljiZadatkeOsobljuAkoPostoje()
    {
        for (int i = tcpClients.Count - 1; i >= 0; i--)
        {
            Socket client = tcpClients[i];
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
                    break;
                }
                else if (apartman.Alarm == StanjeAlarma.Aktivirano)
                {
                    string zadatak = $"ZADATAK=SanacijaAlarma;APARTMAN={apartman.BrojApartmana}";
                    byte[] data = Encoding.UTF8.GetBytes(zadatak);
                    client.Send(data);
                    Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");
                    zadatakPoslat = true;
                    break;
                }
                else if (apartman.StanjeMinibara.Values.Any(v => v < 5))
                {
                    string zadatak = $"ZADATAK=UpravljanjeMinibarem;APARTMAN={apartman.BrojApartmana}";
                    byte[] data = Encoding.UTF8.GetBytes(zadatak);
                    client.Send(data);
                    Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");
                    zadatakPoslat = true;
                    break;
                }
            }

            if (zadatakPoslat)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                tcpClients.RemoveAt(i);
            }
            // Ako nema zadatka, konekcija ostaje otvorena i osoblje nastavlja da čeka
        }
    }

}
