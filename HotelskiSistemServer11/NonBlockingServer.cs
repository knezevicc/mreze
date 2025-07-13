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
    private Dictionary<int, DateTime> poslednjeSmanjenje = new Dictionary<int, DateTime>();

    //sprecava beskonacnu petlju slanja yadataka
    private HashSet<int> poslatiZadaci = new HashSet<int>();

    private IPEndPoint lastGuestEndPoint;

    public NonBlockingServer(int udpPort, int tcpPort)
    {
        this.udpPort = udpPort;
        this.tcpPort = tcpPort;

        //brAp,sprat,klasa,maxGostiju
        apartmani.Add(new Apartman11(101, 1, 3, 4));
        apartmani.Add(new Apartman11(102, 1, 2, 2));
        apartmani.Add(new Apartman11(201, 2, 1, 5));

   
        foreach (var a in apartmani)
        {
            a.StanjeMinibara = new Dictionary<string, int> { { "Pivo", 5 }, { "Voda", 5 }, { "Cokoladica", 5 } };
        }
    }

    public async Task Start()
    {
        #region podesavanje konekcije
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

        //Console.WriteLine("[DEBUG] Server je pokrenut i ulazi u glavnu petlju...");
        #endregion

        while (true)
        {
            #region UDP primanje rezervacija
            if (udpSocket.Available > 0)
            {
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                int received = udpSocket.ReceiveFrom(udpBuffer, ref remoteEP);
                lastGuestEndPoint = (IPEndPoint)remoteEP;

                byte[] receivedData = new byte[received];
                Array.Copy(udpBuffer, receivedData, received);

                //string poruka = Encoding.UTF8.GetString(udpBuffer, 0, received);
                string poruka = Encoding.UTF8.GetString(receivedData);
                string[] delovi = poruka.Split(';');

               

                // Klijent traži listu slobodnih apartmana
                if (poruka == "ZAHTEV=LISTA")
                {
                    string lista = VratiListuSlobodnihApartmana();
                    byte[] listaBytes = Encoding.UTF8.GetBytes(lista);
                    udpSocket.SendTo(listaBytes, remoteEP);
                    Console.WriteLine($"[SERVER] Poslao listu slobodnih apartmana klijentu.");
                    continue;
                }

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
                            PosaljiZadatkeOsobljuAkoPostoje();
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
                            PosaljiZadatkeOsobljuAkoPostoje();
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
                else if (poruka.StartsWith("ZAHTEV"))
                {
                    string lista = VratiListuSlobodnihApartmana();
                    byte[] listaBytes = Encoding.UTF8.GetBytes(lista);
                    udpSocket.SendTo(listaBytes, remoteEP);
                    Console.WriteLine($"[UDP] Poslat odgovor sa listom slobodnih apartmana.");
                    continue;
                }
                else if (poruka.StartsWith("REZERVACIJA"))
                {
                    //var delovi1 = poruka.Split(';');
                    int brojApartmana1 = -1, brojGostiju1 = -1, brojNoci1 = -1;

                    foreach (var deo in delovi)
                    {
                        if (deo.StartsWith("APARTMAN="))
                            int.TryParse(deo.Split('=')[1], out brojApartmana1);
                        else if (deo.StartsWith("GOSTIJU="))
                            int.TryParse(deo.Split('=')[1], out brojGostiju1);
                        else if (deo.StartsWith("NOCI="))
                            int.TryParse(deo.Split('=')[1], out brojNoci1);
                    }

                    if (brojApartmana1 != -1 && brojGostiju1 != -1 && brojNoci1 != -1)
                    {
                        Apartman11 apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojApartmana1);
                        if (apartman != null && apartman.Stanje == StanjeApartmana.Prazan &&
                            brojGostiju1 <= apartman.MaksimalanBrojGostiju)
                        {
                            apartman.TrenutniBrojGostiju = brojGostiju1;
                            apartman.PreostaleNoci = brojNoci1;
                            apartman.Stanje = StanjeApartmana.Zauzet;
                            vremePrijaveGosta[brojApartmana1] = DateTime.Now;

                            string potvrda = $"POTVRDA=OK;APARTMAN={brojApartmana1}";
                            byte[] potvrdaBytes = Encoding.UTF8.GetBytes(potvrda);
                            udpSocket.SendTo(potvrdaBytes, remoteEP);

                            Console.WriteLine($"[SERVER] Gost prijavljen: Apartman={brojApartmana1}, Gosti={brojGostiju1}, Noci={brojNoci1}");
                        }
                        else
                        {
                            string error = "GRESKA=Nema slobodnog apartmana ili apartman ne postoji";
                            byte[] errorBytes = Encoding.UTF8.GetBytes(error);
                            udpSocket.SendTo(errorBytes, remoteEP);
                        }
                    }
                    else
                    {
                        string error = "GRESKA=Nevalidan zahtev (nedostaju podaci)";
                        byte[] errorBytes = Encoding.UTF8.GetBytes(error);
                        udpSocket.SendTo(errorBytes, remoteEP);
                    }

                }
                else { string error = "ne pocinje sa rez/akc/zaht "; udpSocket.SendTo(Encoding.UTF8.GetBytes(error), remoteEP); }

            }
            #endregion
            
            #region smanjivanje dana
            foreach (var apartman in apartmani)
            {
                if (apartman.Stanje == StanjeApartmana.Zauzet)
                {
                    if (vremePrijaveGosta.TryGetValue(apartman.BrojApartmana, out DateTime vremePrijave))
                    {
                        double sekundeOdPrijave = (DateTime.Now - vremePrijave).TotalSeconds;

                        if (sekundeOdPrijave >= 10)  // fiksno 10 sekundi od prijave
                        {
                            apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                            apartman.TrenutniBrojGostiju = 0;
                            Console.WriteLine($"[SERVER] Nakon 10 sekundi, apartman {apartman.BrojApartmana} prelazi u PotrebnoCiscenje.");
                        }
                    }
                    /* ovo je aok ne pratim preosale dane
                    if (vremePrijaveGosta.TryGetValue(apartman.BrojApartmana, out DateTime vremePrijave))
                    {
                        double sekundeOdPrijave = (DateTime.Now - vremePrijave).TotalSeconds;
                        if (sekundeOdPrijave >= apartman.PreostaleNoci)
                        {
                            apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                            apartman.TrenutniBrojGostiju = 0;
                            Console.WriteLine($"[SERVER] Gost je odjavio apartman {apartman.BrojApartmana}, prelazi u PotrebnoCiscenje.");
                        }
                    }*/
                    /* odbrojavanje
                    if (!poslednjeSmanjenje.ContainsKey(apartman.BrojApartmana))
                    {
                        poslednjeSmanjenje[apartman.BrojApartmana] = DateTime.Now;
                    }

                    TimeSpan proteklo = DateTime.Now - poslednjeSmanjenje[apartman.BrojApartmana];
                    if (proteklo.TotalSeconds >= 1)
                    {
                        if (apartman.PreostaleNoci > 0)
                        {
                            apartman.PreostaleNoci--;
                            poslednjeSmanjenje[apartman.BrojApartmana] = DateTime.Now;
                            Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} preostale noci: {apartman.PreostaleNoci}");
                        }

                        if (apartman.PreostaleNoci == 0)
                        {
                            apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                            apartman.TrenutniBrojGostiju = 0;
                            Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada prelazi u PotrebnoCiscenje.");
                        }
                    }*/
                }
            }
            #endregion

            #region GLEDAJ-prihvatanje TCP konekcija
            if (tcpListenSocket.Poll(0, SelectMode.SelectRead))
            {
                Socket client = tcpListenSocket.Accept();
                client.Blocking = false;
                tcpClients.Add(client);
                Console.WriteLine($"[TCP] Novo osoblje povezano: {client.RemoteEndPoint}");

            }

            PosaljiZadatkeOsobljuAkoPostoje();

            ProveriBoravakGostiju();
            
            #endregion

            #region TCP obrada odgovora osoblja
            // TCP - obrada odgovora osoblja

            for (int i = tcpClients.Count - 1; i >= 0; i--)
            {
                Socket client = tcpClients[i];

                if (client.Poll(1000, SelectMode.SelectRead))
                {
                    try
                    {
                        int received2 = client.Receive(tcpBuffer);

                        if (received2 == 0)
                        {
                            Console.WriteLine("[TCP] Klijent zatvorio konekciju.");
                            client.Shutdown(SocketShutdown.Both);
                            client.Close();
                            tcpClients.RemoveAt(i);
                            continue;
                        }

                        string response = Encoding.UTF8.GetString(tcpBuffer, 0, received2);
                        Console.WriteLine($"[TCP] Primljena potvrda od osoblja: {response}\"");

                        if (response.Contains("Zadatak primljen i izvrsen"))
                        {
                            string[] delovi = response.Split(';');
                            int brojApartmanaPotvrda = -1;
                            string vrstaZadatka = "";


                            foreach (var deo in delovi)
                            {

                                if (deo.StartsWith("APARTMAN="))
                                {
                                    int.TryParse(deo.Split('=')[1], out brojApartmanaPotvrda);
                                    break;
                                }
                                else if (deo.StartsWith("VRSTA="))
                                {
                                    vrstaZadatka = deo.Split('=')[1];
                                }

                            }

                            if (brojApartmanaPotvrda != -1)
                            {
                                poslatiZadaci.Remove(brojApartmanaPotvrda * 100 + GetZadatakKod(vrstaZadatka)); // vidi napomenu dole

                                Apartman11 apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojApartmanaPotvrda);
                                if (apartman != null)
                                {
                                    switch (vrstaZadatka)
                                    {
                                        case "Ciscenje":
                                            apartman.Stanje = StanjeApartmana.Prazan;
                                            Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada je Prazan.");
                                            break;

                                        case "SanacijaAlarma":
                                            apartman.Alarm = StanjeAlarma.Normalno;
                                            Console.WriteLine($"[SERVER] Alarm u apartmanu {apartman.BrojApartmana} sada je deaktiviran.");
                                            break;

                                        case "UpravljanjeMinibarem":
                                            foreach (var key in apartman.StanjeMinibara.Keys.ToList())
                                            {
                                                apartman.StanjeMinibara[key] = 5;
                                            }
                                            Console.WriteLine($"[SERVER] Minibar u apartmanu {apartman.BrojApartmana} dopunjen.");
                                            break;
                                    }
                                }
                            }

                        }

                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"[TCP] SocketException: {ex.Message}");
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        tcpClients.RemoveAt(i);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[TCP] Neočekivana greška: {ex.Message}");
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        tcpClients.RemoveAt(i);
                    }/*
                    if (response.Contains("Zadatak primljen i izvrsen"))
                    {
                        string[] delovi = response.Split(';');
                        int brojApartmanaPotvrda = -1;
                        string vrstaZadatka = "";

                        
                        foreach (var deo in delovi)
                        {

                            if (deo.StartsWith("APARTMAN="))
                            {
                                int.TryParse(deo.Split('=')[1], out brojApartmanaPotvrda);
                                break;
                            } else if (deo.StartsWith("VRSTA=")) {
                                vrstaZadatka = deo.Split('=')[1];
                            }
                            
                        }

                        if (brojApartmanaPotvrda != -1)
                        {
                            poslatiZadaci.Remove(brojApartmanaPotvrda * 100 + GetZadatakKod(vrstaZadatka)); // vidi napomenu dole

                            Apartman11 apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojApartmanaPotvrda);
                            if (apartman != null)
                            {
                                switch (vrstaZadatka)
                                {
                                    case "Ciscenje":
                                        apartman.Stanje = StanjeApartmana.Prazan;
                                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada je Prazan.");
                                        break;

                                    case "SanacijaAlarma":
                                        apartman.Alarm = StanjeAlarma.Normalno;
                                        Console.WriteLine($"[SERVER] Alarm u apartmanu {apartman.BrojApartmana} sada je deaktiviran.");
                                        break;

                                    case "UpravljanjeMinibarem":
                                        foreach (var key in apartman.StanjeMinibara.Keys.ToList())
                                        {
                                            apartman.StanjeMinibara[key] = 5;
                                        }
                                        Console.WriteLine($"[SERVER] Minibar u apartmanu {apartman.BrojApartmana} dopunjen.");
                                        break;
                                }
                            }
                        }
                        */
                    }
                #endregion
            }
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

            Thread.Sleep(1000);
        }
    }
    /*
    private void PosaljiZadatkeOsobljuAkoPostoje()
    {
        for (int i = tcpClients.Count - 1; i >= 0; i--)
        {
            Socket client = tcpClients[i];
            bool zadatakPoslat = false;

            foreach (var apartman in apartmani)
            {
                if (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje && !poslatiZadaci.Contains(apartman.BrojApartmana * 100 +1))
                {
                    string zadatak = $"ZADATAK=Ciscenje;APARTMAN={apartman.BrojApartmana}";

                    byte[] data = Encoding.UTF8.GetBytes(zadatak);
                    client.Send(data);
                    Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");
                    
                    poslatiZadaci.Add(apartman.BrojApartmana*100+1);
                    zadatakPoslat = true;
                    break;
                }
                else if (apartman.Alarm == StanjeAlarma.Aktivirano && !poslatiZadaci.Contains(apartman.BrojApartmana * 100 + 2))
                {
                    string zadatak = $"ZADATAK=SanacijaAlarma;APARTMAN={apartman.BrojApartmana}";
                    byte[] data = Encoding.UTF8.GetBytes(zadatak);
                    client.Send(data);
                    Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");

                    poslatiZadaci.Add(apartman.BrojApartmana * 100 + 2);
                    zadatakPoslat = true;
                    break;
                }
                else if (apartman.StanjeMinibara.Values.Any(v => v < 5) && !poslatiZadaci.Contains(apartman.BrojApartmana * 100 + 3))
                {
                    string zadatak = $"ZADATAK=UpravljanjeMinibarem;APARTMAN={apartman.BrojApartmana}";
                    byte[] data = Encoding.UTF8.GetBytes(zadatak);
                    client.Send(data);
                    Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");

                    poslatiZadaci.Add(apartman.BrojApartmana * 100 + 3);
                    zadatakPoslat = true;
                    break;
                }
            }
            /*
            if (zadatakPoslat)
            {
                client.Shutdown(SocketShutdown.Both);
                client.Close();
                tcpClients.RemoveAt(i);
            }//
            // Ako nema zadatka, konekcija ostaje otvorena i osoblje nastavlja da čeka
        }
    }
*/


    private void PosaljiZadatkeOsobljuAkoPostoje()
    {
        for (int i = tcpClients.Count - 1; i >= 0; i--)
        {
            var client = tcpClients[i];

            foreach (var apartman in apartmani)
            {
                int idCiscenje = apartman.BrojApartmana * 100 + 1;
                if (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje && !poslatiZadaci.Contains(idCiscenje))
                {
                    string zadatak = $"ZADATAK=Ciscenje;APARTMAN={apartman.BrojApartmana}";
                    PosaljiZadatak(client, zadatak);
                    poslatiZadaci.Add(idCiscenje);
                    break; // šaljemo samo jedan zadatak po ciklusu
                }

                int idAlarm = apartman.BrojApartmana * 100 + 2;
                if (apartman.Alarm == StanjeAlarma.Aktivirano && !poslatiZadaci.Contains(idAlarm))
                {
                    string zadatak = $"ZADATAK=SanacijaAlarma;APARTMAN={apartman.BrojApartmana}";
                    PosaljiZadatak(client, zadatak);
                    poslatiZadaci.Add(idAlarm);
                    break;
                }

                int idMinibar = apartman.BrojApartmana * 100 + 3;
                if (apartman.StanjeMinibara.Values.Any(v => v < 5) && !poslatiZadaci.Contains(idMinibar))
                {
                    string zadatak = $"ZADATAK=UpravljanjeMinibarem;APARTMAN={apartman.BrojApartmana}";
                    PosaljiZadatak(client, zadatak);
                    poslatiZadaci.Add(idMinibar);
                    break;
                }
            }
        }
    }

    /*  OVOOOOOO JE ORIG
    private void PosaljiZadatkeOsobljuAkoPostoje()
    {
        for (int i = tcpClients.Count - 1; i >= 0; i--)
        {
            Socket client = tcpClients[i];

            bool zadatakPoslat = false;

            foreach (var apartman in apartmani)
            {
                // 1️⃣ ALARM
                int alarmId = apartman.BrojApartmana * 100 + 2;
                if (apartman.Alarm == StanjeAlarma.Aktivirano && !poslatiZadaci.Contains(alarmId))
                {
                    string zadatak = $"ZADATAK=SanacijaAlarma;APARTMAN={apartman.BrojApartmana}";
                    PosaljiZadatak(client, zadatak);
                    poslatiZadaci.Add(alarmId);
                    zadatakPoslat = true;
                    break; // šaljemo samo jedan zadatak po petlji
                }

                // 2️⃣ MINIBAR
                int minibarId = apartman.BrojApartmana * 100 + 3;
                if (apartman.StanjeMinibara.Values.Any(v => v < 5) && !poslatiZadaci.Contains(minibarId))
                {
                    string zadatak = $"ZADATAK=UpravljanjeMinibarem;APARTMAN={apartman.BrojApartmana}";
                    PosaljiZadatak(client, zadatak);
                    poslatiZadaci.Add(minibarId);
                    zadatakPoslat = true;
                    break;
                }

                // 3️⃣ CISCENJE
                int ciscenjeId = apartman.BrojApartmana * 100 + 1;
                if (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje && !poslatiZadaci.Contains(ciscenjeId))
                {
                    string zadatak = $"ZADATAK=Ciscenje;APARTMAN={apartman.BrojApartmana}";
                    PosaljiZadatak(client, zadatak);
                    poslatiZadaci.Add(ciscenjeId);
                    zadatakPoslat = true;
                    break;
                }
            }

            // Ako nema zadatka, ne šaljemo ništa, konekcija ostaje otvorena
        }
    }*/

    private void PosaljiZadatak(Socket client, string zadatak)
    {
        byte[] data = Encoding.UTF8.GetBytes(zadatak);
        client.Send(data);
        Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");
    }

    private void ProveriBoravakGostiju()
    {
        foreach (var apartman in apartmani)
        {
            if (apartman.Stanje == StanjeApartmana.Zauzet)
            {
                if (apartman.PreostaleNoci > 0)
                {
                    apartman.PreostaleNoci--;

                    Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana}: preostale noći smanjene, sada: {apartman.PreostaleNoci}");
                }

                if (apartman.PreostaleNoci == 0)
                {
                    apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                    apartman.TrenutniBrojGostiju = 0;

                    Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada prelazi u PotrebnoCiscenje nakon isteka boravka.");
                }
            }
        }
    }

    // Metoda koja prevodi vrstu zadatka u kod koji koristiš za set poslatiZadaci
    private int GetZadatakKod(string vrstaZadatka)
    {
        if (vrstaZadatka == "Ciscenje")
            return 1;
        else if (vrstaZadatka == "SanacijaAlarma")
            return 2;
        else if (vrstaZadatka == "UpravljanjeMinibarem")
            return 3;
        else
            return 0;
    }

    private string VratiListuSlobodnihApartmana()
    {
        var slobodni = apartmani.Where(a => a.Stanje == StanjeApartmana.Prazan)
                                 .Select(a => $"Apartman {a.BrojApartmana} | Klasa {a.Klasa} | MaxGostiju {a.MaksimalanBrojGostiju}")
                                 .ToList();

        if (slobodni.Count == 0)
            return "Nema slobodnih apartmana trenutno.";

        return string.Join("\n", slobodni);
    }

    private string ObradiZahtevZaRezervaciju(string klasaStr, string brojGostijuStr)
    {
        if (!int.TryParse(klasaStr, out int klasa) || !int.TryParse(brojGostijuStr, out int brojGostiju))
            return "GRESKA: Neispravan unos.";

        var slobodan = apartmani.FirstOrDefault(a => a.Stanje == StanjeApartmana.Prazan &&
                                                     a.Klasa == klasa &&
                                                     a.MaksimalanBrojGostiju >= brojGostiju);

        if (slobodan == null)
            return "GRESKA: Nema slobodnih apartmana tražene klase za taj broj gostiju.";

        // Rezervacija odobrena
        slobodan.Stanje = StanjeApartmana.Zauzet;
        slobodan.TrenutniBrojGostiju = brojGostiju;

        return $"ODOBRENO;APARTMAN={slobodan.BrojApartmana};MAKS_GOSTIJU={slobodan.MaksimalanBrojGostiju}";
    }

}
