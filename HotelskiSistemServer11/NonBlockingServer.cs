using Gost1;
using HotelskiSistemServer11;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class NonBlockingServer
{
    #region private
    private Socket udpSocket;
    private Socket tcpListenSocket;
    private List<Socket> tcpClients = new List<Socket>();
    private List<Apartman11> apartmani = new List<Apartman11>();
    private int udpPort, tcpPort;

    private Dictionary<int, DateTime> vremePrijaveGosta = new Dictionary<int, DateTime>();
    private Dictionary<int, DateTime> poslednjeSmanjenje = new Dictionary<int, DateTime>();
    private Dictionary<int, IPEndPoint> apartmanEndPoints = new Dictionary<int, IPEndPoint>();

    //sprecava beskonacnu petlju slanja yadataka
    private HashSet<int> poslatiZadaci = new HashSet<int>();
    private DateTime lastDecrease = DateTime.Now;


    private IPEndPoint lastGuestEndPoint;
    #endregion
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

        await Task.Delay(2000);

        //Console.WriteLine("[DEBUG] Server je pokrenut i ulazi u glavnu petlju...");
        #endregion

        while (true)
        {

            #region UDP primanje rezervacija
            if (udpSocket.Poll(0, SelectMode.SelectRead))
            {
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                int received = udpSocket.ReceiveFrom(udpBuffer, ref remoteEP);
                lastGuestEndPoint = (IPEndPoint)remoteEP;

                byte[] receivedData = new byte[received];
                Array.Copy(udpBuffer, receivedData, received);

                string poruka = Encoding.UTF8.GetString(udpBuffer, 0, received);
                string[] delovi = poruka.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

                if (poruka == "ZAHTEV=LISTA")
                {
                    string lista = VratiListuSlobodnihApartmana();
                    byte[] listaBytes = Encoding.UTF8.GetBytes(lista);
                    udpSocket.SendTo(listaBytes, remoteEP);
                    Console.WriteLine($"[SERVER] Poslao listu slobodnih apartmana klijentu.");
                    continue;
                }
                else if (poruka.Trim() == "ZAHTEV=KLASE")
                {
                    var slobodneKlase = apartmani
                        .Where(a => a.Stanje == StanjeApartmana.Prazan)
                        .Select(a => a.Klasa)
                        .Distinct();

                    string odgovor = "KLASE=" + string.Join(",", slobodneKlase);
                    udpSocket.SendTo(Encoding.UTF8.GetBytes(odgovor), remoteEP);
                    continue;
                }
                if (poruka.StartsWith("AKCIJA"))
                {
                    if (delovi.Length < 2)
                    {
                        string error = "GRESKA=Nevalidan zahtev za AKCIJA";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(error), remoteEP);
                        continue;
                    }

                    int brojA;
                    if (!int.TryParse(delovi[1].Split('=')[1], out brojA))
                    {
                        string error = "GRESKA=Nevalidan broj apartmana u AKCIJA";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(error), remoteEP);
                        continue;
                    }

                    Apartman11 apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojA);

                    if (apartman != null)
                    {
                        string akcija = delovi[0].Split('=')[1];
                        string odgovor = "";

                        if (akcija == "ALARM")
                        {
                            apartman.Alarm = StanjeAlarma.Aktivirano;
                            odgovor = "Alarm aktiviran.";
                            apartman.alarmProvera = true;
                            PosaljiZadatkeOsobljuAkoPostoje();
                        }
                        else if (akcija == "MINIBAR")
                        {
                            //ubaceno 16.7-zbog naplate
                            string artikal = delovi.FirstOrDefault(s => s.StartsWith("ARTIKAL="))?.Split('=')[1];
                            if (!string.IsNullOrEmpty(artikal))
                            {
                                apartman.IskoristiMinibarArtikal(artikal);
                                odgovor = $"Iskoriscen artikal '{artikal}' iz minibara.";
                            }
                            else
                            {
                                odgovor = "Nije naveden artikal za MINIBAR akciju.";
                            }

                        }
                        else if (akcija == "CISCENJE")
                        {
                            apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                            odgovor = "Zatraženo ciscenje sobe(uu medj).";
                            // apartman.TrenutnoZaduzenje += 15;
                            //I OCO 18.7
                            //apartman.NaplatiTrazenoCiscenje();
                            //OBRISANO 18.7
                            //apartman.ZavrsenoCiscenje = true;
                            PosaljiZadatkeOsobljuAkoPostoje();
                        }
                        else if (akcija == "CISCENJE2")
                        {
                            apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                            //apartman.TrenutniBrojGostiju = 0;
                            odgovor = "Zatraženo čiscenje sobe.";

                            PosaljiZadatkeOsobljuAkoPostoje();

                            string porukaZavrsetak = $"BORAVAK_ZAVRSEN;APARTMAN={apartman.BrojApartmana}";
                            byte[] porukaBytes = Encoding.UTF8.GetBytes(porukaZavrsetak);

                            if (apartmanEndPoints.TryGetValue(apartman.BrojApartmana, out IPEndPoint ep))
                            {
                                udpSocket.SendTo(porukaBytes, ep);
                                Console.WriteLine($"[SERVER] Poslata poruka BORAVAK_ZAVRSEN za apartman {apartman.BrojApartmana} klijentu na {ep}");
                            }
                            else
                            {
                                Console.WriteLine($"[SERVER] Nema sačuvan EndPoint za apartman {apartman.BrojApartmana}, nije poslata BORAVAK_ZAVRSEN.");
                            }
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

                    string klasa = "";
                    int brojGostiju1 = -1;
                    Console.WriteLine(poruka);

                    foreach (var deo in delovi)
                    {
                        if (deo.StartsWith("KLASA="))
                            klasa = deo.Split('=')[1];
                        else if (deo.StartsWith("GOSTIJU="))
                            int.TryParse(deo.Split('=')[1], out brojGostiju1);
                    }

                    var slobodanApartman = apartmani.FirstOrDefault(a =>
                        a.Stanje == StanjeApartmana.Prazan &&
                        a.Klasa == int.Parse(klasa));

                    if (slobodanApartman == null)
                    {

                        udpSocket.SendTo(Encoding.UTF8.GetBytes("POTVRDA=NEMA_SLOBODNIH"), remoteEP);
                        continue;
                    }

                    slobodanApartman.Stanje = StanjeApartmana.Zauzet;
                    slobodanApartman.TrenutniBrojGostiju = brojGostiju1;

                    int brojApartmana1 = slobodanApartman.BrojApartmana;

                    if (brojApartmana1 != -1 && brojGostiju1 != -1)
                    {

                        slobodanApartman.TrenutniBrojGostiju = brojGostiju1;
                        slobodanApartman.Stanje = StanjeApartmana.Zauzet;

                        string potvrdaRez = $"POTVRDA=OK;APARTMAN={brojApartmana1};GOSTIJU={brojGostiju1}";

                        udpSocket.SendTo(Encoding.UTF8.GetBytes(potvrdaRez), remoteEP);
                        Console.WriteLine($"[SERVER] Poslata potvrda rezervacije: {potvrdaRez}");
                    }
                    else
                    {
                        Console.WriteLine("er");
                        string error = "GRESKA=Nevalidan zahtev (nedostaju podaci)";
                        byte[] errorBytes = Encoding.UTF8.GetBytes(error);
                        udpSocket.SendTo(errorBytes, remoteEP);
                    }

                }
                else if (poruka.StartsWith("GOSTI"))
                {
                    Console.WriteLine($"[SERVER] Primljena GOSTI poruka: {poruka}");


                    int headerEndIndex = poruka.IndexOf("GOSTI=", StringComparison.Ordinal);
                    if (headerEndIndex == -1)
                    {
                        Console.WriteLine("[SERVER] Neispravna poruka, nema GOSTI=");
                        continue;
                    }

                    headerEndIndex += "GOSTI=".Length;
                    byte[] headerBytes = Encoding.UTF8.GetBytes(poruka.Substring(0, headerEndIndex));

                    // Izdvoji binarni deo
                    byte[] binarniPodaci = new byte[receivedData.Length - headerBytes.Length];
                    Buffer.BlockCopy(receivedData, headerBytes.Length, binarniPodaci, 0, binarniPodaci.Length);




                    int brojApartmana = -1, brojNoci = -1;
                    //var gostiPodaci = new List<string>();

                    foreach (var deo in delovi)
                    {
                        if (deo.StartsWith("APARTMAN="))
                            int.TryParse(deo.Split('=')[1], out brojApartmana);
                        else if (deo.StartsWith("NOCI="))
                            int.TryParse(deo.Split('=')[1], out brojNoci);
                        //else if (deo.StartsWith("GOST") && deo.Contains("="))
                        //  gostiPodaci.Add(deo.Split('=')[1]);
                    }

                    var apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojApartmana);
                    if (apartman != null && apartman.Stanje == StanjeApartmana.Zauzet && brojNoci > 0)
                    {
                        apartman.PreostaleNoci = brojNoci;
                        apartman.UkupnoNocenja = brojNoci;
                        apartman.Gosti.Clear();


                        try
                        {
                            byte[] separator = Encoding.UTF8.GetBytes(",,,");
                            List<byte[]> segmenti = DeserializationHelper.SplitByteArray(binarniPodaci, separator);

                            foreach (var gostBytes in segmenti)
                            {
                                using (MemoryStream ms = new MemoryStream(gostBytes))
                                {
                                    BinaryFormatter bf = new BinaryFormatter();
                                    Gost11 noviGost = (Gost11)bf.Deserialize(ms);
                                    apartman.Gosti.Add(noviGost);
                                    Console.WriteLine($"[SERVER] Dodan gost: {noviGost.Ime} {noviGost.Prezime}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            udpSocket.SendTo(Encoding.UTF8.GetBytes("GRESKA=NEISPRAVNI_PODACI_O_GOSTIMA"), remoteEP);
                            Console.WriteLine($"[SERVER] Greska pri obradi gostiju: {ex.Message}");
                        }


                        //pamti br apartmana da zna ciji ce boravak biti zavrsen
                        apartmanEndPoints[brojApartmana] = (IPEndPoint)remoteEP;

                        // Start odbrojavanja
                        vremePrijaveGosta[brojApartmana] = DateTime.Now;

                        // **SADA šaljemo potpunu potvrdu rezervacije**
                        string potvrda = $"POTVRDA=OK;APARTMAN={brojApartmana};GOSTIJU={apartman.TrenutniBrojGostiju};NOCI={brojNoci}";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(potvrda), lastGuestEndPoint);
                        Console.WriteLine($"[SERVER] POTVRDA rezervacije poslat za apartman {brojApartmana}");
                    }
                    continue;


                }
                else if (poruka.StartsWith("POTVRDA_PLACANJA"))
                {
                    // Parsiranje parametara
                    var delovi3 = poruka.Split(';');
                    int brojApartmana = -1;
                    string brojKartice = "";

                    foreach (var deo in delovi3)
                    {
                        if (deo.StartsWith("APARTMAN="))
                            int.TryParse(deo.Substring("APARTMAN=".Length), out brojApartmana);
                        else if (deo.StartsWith("KARTICA="))
                            brojKartice = deo.Substring("KARTICA=".Length);
                    }

                    if (brojApartmana != -1 && !string.IsNullOrEmpty(brojKartice))
                    {
                        // Ovdje možeš dodati validaciju kartice ako želiš
                        Console.WriteLine($"Primljeno placanje za apartman {brojApartmana} sa karticom {brojKartice}");

                        // Odgovori klijentu da je plaćanje prihvaćeno
                        string odgovor = $"PLACANJE_PRIHVACENO;APARTMAN={brojApartmana}";
                        byte[] odgovorBytes = Encoding.UTF8.GetBytes(odgovor);
                        // udpSocket.Send(odgovorBytes,odgovorBytes.Length,klijentEP);
                        //Console.WriteLine($"[SERVER] Plaćanje prihvaćeno za apartman {brojApartmana}.");

                        //apartmanEndPoints[brojApartmana] = (IPEndPoint)remoteEP;

                        if (apartmanEndPoints.TryGetValue(brojApartmana, out IPEndPoint ep))
                        {
                            udpSocket.SendTo(odgovorBytes, ep);
                        }
                        else
                        {
                            Console.WriteLine($"[SERVER] Nema sacuvan EndPoint za apartman {brojApartmana}, ne salje potvrdu placanja.");
                        }

                        //udpSocket.SendTo(odgovorBytes, remoteEP);
                        Console.WriteLine($"[SERVER] Placanje prihvaceno za apartman {brojApartmana}.");
                        var oslobodjenApartman = apartmani.FirstOrDefault(a =>
                       a.BrojApartmana == brojApartmana);
                        oslobodjenApartman.Reset(); // Resetujemo apartman nakon placanja
                        // Ovde šalješ tačno na klijentov IPEndPoint iz dictionary-ja:
                        /*if (apartmanEndPoints.TryGetValue(brojApartmana, out IPEndPoint klijentEP))
                        {
                            udpSocket.SendTo(odgovorBytes, klijentEP);
                            Console.WriteLine($"[SERVER] Plaćanje prihvaćeno za apartman {brojApartmana}.");
                        }
                        else
                        {
                            Console.WriteLine($"[SERVER] Nije pronađen EndPoint za apartman {brojApartmana}, ne može se poslati potvrda.");
                        }*/
                    }
                    else
                    {
                        string greska = $"GRESKA;PORUKA=Nepotpuni podaci za placanje";
                        byte[] greskaBytes = Encoding.UTF8.GetBytes(greska);
                        udpSocket.SendTo(greskaBytes, lastGuestEndPoint); // ili remoteEP ako imaš
                    }
                    continue;
                }
                else
                {
                    string error = "ne pocinje sa rez/akc/zaht ";
                    udpSocket.SendTo(Encoding.UTF8.GetBytes(error), remoteEP);
                }

            }
            #endregion

            #region smanjivanje dana
            foreach (var apartman in apartmani)
            {
                if (!(apartman.Stanje == StanjeApartmana.Zauzet || (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje && apartman.PreostaleNoci > 0)))
                    continue;
                if (apartman.Gosti.Count == 0)
                    continue;

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
                    if (apartman.PreostaleNoci == 0 && apartman.Stanje == StanjeApartmana.Zauzet)
                    {
                        
                            apartman.NaplatiNocenja();
                            // apartman.ZavrsenoCiscenje = true; // Obeleži da je obračun urađen
                        

                        apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                        apartman.TrenutniBrojGostiju = 0;

                        apartman.StanjeMinibara.Clear();       // resetovanje minibara
                        apartman.Alarm = StanjeAlarma.Normalno; // reset alarma ako je potrebno
                        //apartman.ZavrsenoCiscenje = false;

                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada prelazi u PotrebnoCiscenje nakon isteka boravka.");

                        if (apartmanEndPoints.TryGetValue(apartman.BrojApartmana, out IPEndPoint ep))
                        {
                            string listaTroskova = apartman.VratiListuTroskova();
                            string porukaZavrsetak = $"BORAVAK_ZAVRSEN;APARTMAN={apartman.BrojApartmana};UKUPNO={apartman.TrenutnoZaduzenje};TROSKOVI={listaTroskova}";
                            byte[] porukaBytes = Encoding.UTF8.GetBytes(porukaZavrsetak);

                            udpSocket.SendTo(porukaBytes, ep);
                            Console.WriteLine($"[SERVER] Poslata detaljna poruka BORAVAK_ZAVRSEN za apartman {apartman.BrojApartmana} klijentu na {ep}.");
                        }
                        else
                        {
                            Console.WriteLine($"[SERVER] Nema sačuvan EndPoint za apartman {apartman.BrojApartmana}, nije poslata poruka BORAVAK_ZAVRSEN.");
                        }
                    }
                    if (apartman.PreostaleNoci == 0 && apartman.Stanje == StanjeApartmana.PotrebnoCiscenje)
                    {
                        // finalno čišćenje: pošalji zadatak osoblju
                        int idFinalno = apartman.BrojApartmana * 100 + 1;
                        if (!poslatiZadaci.Contains(idFinalno))
                        {
                            string zadatak = $"ZADATAK=Ciscenje;APARTMAN={apartman.BrojApartmana}";
                            foreach (var client in tcpClients)
                                PosaljiZadatak(client, zadatak);
                            poslatiZadaci.Add(idFinalno);
                            Console.WriteLine($"[SERVER] Poslat zadatak osoblju za FINALNO CISCENJE apartmana {apartman.BrojApartmana}");
                        }
                    }
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
                                    if (vrstaZadatka == "Ciscenje")
                                    {
                                        apartman.IspisiUkupnuCenu();

                                        apartman.Stanje = StanjeApartmana.Prazan;
                                        apartman.TrenutniBrojGostiju = 0;
                                        apartman.UkupnoNocenja = 0; // reset za sledeći boravak
                                        apartman.ZavrsenoCiscenje = false;
                                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada je Prazan.");


                                        //apartman.ZavrsenoCiscenje = true; // ➡️ OVO DODAŠ

                                    }
                                    else if (vrstaZadatka == "CiscenjeTrazeno")
                                    {
                                        apartman.NaplatiTrazenoCiscenje();
                                        apartman.Stanje = StanjeApartmana.Zauzet;
                                        //apartman.ZavrsenoCiscenje = true;
                                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} je ociscen na zahtev gosta.");
                                    }
                                    else if (vrstaZadatka == "SanacijaAlarma")
                                    {
                                        apartman.NaplatiAlarm();
                                        apartman.Alarm = StanjeAlarma.Normalno;
                                        Console.WriteLine($"[SERVER] Alarm u apartmanu {apartman.BrojApartmana} sada je deaktiviran.");
                                    }
                                    else if (vrstaZadatka == "UpravljanjeMinibarem")
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
                        Console.WriteLine($"[TCP] Neočekivana greska: {ex.Message}");
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        tcpClients.RemoveAt(i);
                    }
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

    private void PosaljiZadatkeOsobljuAkoPostoje()
    {
        for (int i = tcpClients.Count - 1; i >= 0; i--)
        {
            var client = tcpClients[i];

            foreach (var apartman in apartmani)
            {//CISCENJE KOJE ZAVRSAVA BROJANJE
                int idFinalno = apartman.BrojApartmana * 100 + 1;
                if (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje
                    && apartman.PreostaleNoci == 0
                    && !poslatiZadaci.Contains(idFinalno))
                {
                    string zadatak = $"ZADATAK=Ciscenje;APARTMAN={apartman.BrojApartmana}";
                    PosaljiZadatak(client, zadatak);
                    poslatiZadaci.Add(idFinalno);
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

                int idTrazeno = apartman.BrojApartmana * 100 + 4;
                if (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje
                    && apartman.PreostaleNoci > 0
                    && !poslatiZadaci.Contains(idTrazeno))
                {
                    string zadatak = $"ZADATAK=CiscenjeTrazeno;APARTMAN={apartman.BrojApartmana}";
                    PosaljiZadatak(client, zadatak);
                    poslatiZadaci.Add(idTrazeno);
                    //break;
                    continue;
                }
            }
        }
    }

    private void PosaljiZadatak(Socket client, string zadatak)
    {
        byte[] data = Encoding.UTF8.GetBytes(zadatak);
        client.Send(data);
        Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");
    }

    private int GetZadatakKod(string vrstaZadatka)
    {
        // Metoda koja prevodi vrstu zadatka u kod koji koristiš za set poslatiZadaci
        if (vrstaZadatka == "Ciscenje")
            return 1;
        else if (vrstaZadatka == "SanacijaAlarma")
            return 2;
        else if (vrstaZadatka == "UpravljanjeMinibarem")
            return 3;
        else if (vrstaZadatka == "CiscenjeTrazeno")
            return 4;
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

}

/*using Gost1;
using HotelskiSistemServer11;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class NonBlockingServer
{
    #region private
    private Socket udpSocket;
    private Socket tcpListenSocket;
    private List<Socket> tcpClients = new List<Socket>();
    private List<Apartman11> apartmani = new List<Apartman11>();
    private int udpPort, tcpPort;

    private Dictionary<int, DateTime> vremePrijaveGosta = new Dictionary<int, DateTime>();
    private Dictionary<int, DateTime> poslednjeSmanjenje = new Dictionary<int, DateTime>();
    private Dictionary<int, IPEndPoint> apartmanEndPoints = new Dictionary<int, IPEndPoint>();

    //sprecava beskonacnu petlju slanja yadataka
    private HashSet<int> poslatiZadaci = new HashSet<int>();
    private DateTime lastDecrease = DateTime.Now;


    private IPEndPoint lastGuestEndPoint;
    #endregion
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

        await Task.Delay(2000);

        //Console.WriteLine("[DEBUG] Server je pokrenut i ulazi u glavnu petlju...");
        #endregion

        while (true)
        {

            #region UDP primanje rezervacija
            if (udpSocket.Poll(0, SelectMode.SelectRead))
            {
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                int received = udpSocket.ReceiveFrom(udpBuffer, ref remoteEP);
                lastGuestEndPoint = (IPEndPoint)remoteEP;

                byte[] receivedData = new byte[received];
                Array.Copy(udpBuffer, receivedData, received);

                string poruka = Encoding.UTF8.GetString(udpBuffer, 0, received);
                string[] delovi = poruka.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

                if (poruka == "ZAHTEV=LISTA")
                {
                    string lista = VratiListuSlobodnihApartmana();
                    byte[] listaBytes = Encoding.UTF8.GetBytes(lista);
                    udpSocket.SendTo(listaBytes, remoteEP);
                    Console.WriteLine($"[SERVER] Poslao listu slobodnih apartmana klijentu.");
                    continue;
                }
                else if (poruka.Trim() == "ZAHTEV=KLASE")
                {
                    var slobodneKlase = apartmani
                        .Where(a => a.Stanje == StanjeApartmana.Prazan)
                        .Select(a => a.Klasa)
                        .Distinct();

                    string odgovor = "KLASE=" + string.Join(",", slobodneKlase);
                    udpSocket.SendTo(Encoding.UTF8.GetBytes(odgovor), remoteEP);
                    continue;
                }
                if (poruka.StartsWith("AKCIJA"))
                {
                    if (delovi.Length < 2)
                    {
                        string error = "GRESKA=Nevalidan zahtev za AKCIJA";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(error), remoteEP);
                        continue;
                    }

                    int brojA;
                    if (!int.TryParse(delovi[1].Split('=')[1], out brojA))
                    {
                        string error = "GRESKA=Nevalidan broj apartmana u AKCIJA";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(error), remoteEP);
                        continue;
                    }

                    Apartman11 apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojA);

                    if (apartman != null)
                    {
                        string akcija = delovi[0].Split('=')[1];
                        string odgovor = "";

                        if (akcija == "ALARM")
                        {
                            apartman.Alarm = StanjeAlarma.Aktivirano;
                            odgovor = "Alarm aktiviran.";
                            apartman.alarmProvera = true;
                            PosaljiZadatkeOsobljuAkoPostoje();
                        }
                        else if (akcija == "MINIBAR")
                        {
                            //ubaceno 16.7-zbog naplate
                            string artikal = delovi.FirstOrDefault(s => s.StartsWith("ARTIKAL="))?.Split('=')[1];
                            if (!string.IsNullOrEmpty(artikal))
                            {
                                apartman.IskoristiMinibarArtikal(artikal);
                                odgovor = $"Iskoriscen artikal '{artikal}' iz minibara.";
                            }
                            else
                            {
                                odgovor = "Nije naveden artikal za MINIBAR akciju.";
                            }
                           
                        }
                        else if (akcija == "CISCENJE")
                        {
                            apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                            odgovor = "Zatraženo ciscenje sobe(uu medj).";
                            // apartman.TrenutnoZaduzenje += 15;
                            //I OCO 18.7
                            //apartman.NaplatiTrazenoCiscenje();
                            //OBRISANO 18.7
                            //apartman.ZavrsenoCiscenje = true;
                            PosaljiZadatkeOsobljuAkoPostoje();
                        }
                        else if (akcija == "CISCENJE2")
                        {
                            apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                            //apartman.TrenutniBrojGostiju = 0;
                            odgovor = "Zatraženo čiscenje sobe.";

                            PosaljiZadatkeOsobljuAkoPostoje();

                            string porukaZavrsetak = $"BORAVAK_ZAVRSEN;APARTMAN={apartman.BrojApartmana}";
                            byte[] porukaBytes = Encoding.UTF8.GetBytes(porukaZavrsetak);

                            if (apartmanEndPoints.TryGetValue(apartman.BrojApartmana, out IPEndPoint ep))
                            {
                                udpSocket.SendTo(porukaBytes, ep);
                                Console.WriteLine($"[SERVER] Poslata poruka BORAVAK_ZAVRSEN za apartman {apartman.BrojApartmana} klijentu na {ep}");
                            }
                            else
                            {
                                Console.WriteLine($"[SERVER] Nema sačuvan EndPoint za apartman {apartman.BrojApartmana}, nije poslata BORAVAK_ZAVRSEN.");
                            }
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

                    string klasa = "";
                    int brojGostiju1 = -1;
                    Console.WriteLine(poruka);

                    foreach (var deo in delovi)
                    {
                        if (deo.StartsWith("KLASA="))
                            klasa = deo.Split('=')[1];
                        else if (deo.StartsWith("GOSTIJU="))
                            int.TryParse(deo.Split('=')[1], out brojGostiju1);
                    }

                    var slobodanApartman = apartmani.FirstOrDefault(a =>
                        a.Stanje == StanjeApartmana.Prazan &&
                        a.Klasa == int.Parse(klasa));

                    if (slobodanApartman == null)
                    {

                        udpSocket.SendTo(Encoding.UTF8.GetBytes("POTVRDA=NEMA_SLOBODNIH"), remoteEP);
                        continue;
                    }

                    slobodanApartman.Stanje = StanjeApartmana.Zauzet;
                    slobodanApartman.TrenutniBrojGostiju = brojGostiju1;

                    int brojApartmana1 = slobodanApartman.BrojApartmana;

                    if (brojApartmana1 != -1 && brojGostiju1 != -1)
                    {

                        slobodanApartman.TrenutniBrojGostiju = brojGostiju1;
                        slobodanApartman.Stanje = StanjeApartmana.Zauzet;

                        string potvrdaRez = $"POTVRDA=OK;APARTMAN={brojApartmana1};GOSTIJU={brojGostiju1}";

                        udpSocket.SendTo(Encoding.UTF8.GetBytes(potvrdaRez), remoteEP);
                        Console.WriteLine($"[SERVER] Poslata potvrda rezervacije: {potvrdaRez}");
                    }
                    else
                    {
                        Console.WriteLine("er");
                        string error = "GRESKA=Nevalidan zahtev (nedostaju podaci)";
                        byte[] errorBytes = Encoding.UTF8.GetBytes(error);
                        udpSocket.SendTo(errorBytes, remoteEP);
                    }

                }
                else if (poruka.StartsWith("GOSTI"))
                {
                    Console.WriteLine($"[SERVER] Primljena GOSTI poruka: {poruka}");


                     int headerEndIndex = poruka.IndexOf("GOSTI=", StringComparison.Ordinal);
                    if (headerEndIndex == -1)
                    {
                        Console.WriteLine("[SERVER] Neispravna poruka, nema GOSTI=");
                        continue;
                    }

                    headerEndIndex += "GOSTI=".Length;
                    byte[] headerBytes = Encoding.UTF8.GetBytes(poruka.Substring(0, headerEndIndex));

                    // Izdvoji binarni deo
                    byte[] binarniPodaci = new byte[receivedData.Length - headerBytes.Length];
                    Buffer.BlockCopy(receivedData, headerBytes.Length, binarniPodaci, 0, binarniPodaci.Length);

                  


                    int brojApartmana = -1, brojNoci = -1;
                    //var gostiPodaci = new List<string>();

                    foreach (var deo in delovi)
                    {
                        if (deo.StartsWith("APARTMAN="))
                            int.TryParse(deo.Split('=')[1], out brojApartmana);
                        else if (deo.StartsWith("NOCI="))
                            int.TryParse(deo.Split('=')[1], out brojNoci);
                        //else if (deo.StartsWith("GOST") && deo.Contains("="))
                          //  gostiPodaci.Add(deo.Split('=')[1]);
                    }

                    var apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojApartmana);
                    if (apartman != null && apartman.Stanje == StanjeApartmana.Zauzet && brojNoci > 0)
                    {
                        apartman.PreostaleNoci = brojNoci;
                        apartman.UkupnoNocenja = brojNoci;
                        apartman.Gosti.Clear();

                        
                         try
                        {
                            byte[] separator = Encoding.UTF8.GetBytes(",,,");
                            List<byte[]> segmenti = DeserializationHelper.SplitByteArray(binarniPodaci, separator);

                            foreach (var gostBytes in segmenti)
                            {
                                using (MemoryStream ms = new MemoryStream(gostBytes))
                                {
                                    BinaryFormatter bf = new BinaryFormatter();
                                    Gost11 noviGost = (Gost11)bf.Deserialize(ms);
                                    apartman.Gosti.Add(noviGost);
                                    Console.WriteLine($"[SERVER] Dodan gost: {noviGost.Ime} {noviGost.Prezime}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            udpSocket.SendTo(Encoding.UTF8.GetBytes("GRESKA=NEISPRAVNI_PODACI_O_GOSTIMA"), remoteEP);
                            Console.WriteLine($"[SERVER] Greska pri obradi gostiju: {ex.Message}");
                        }
                         

                        //pamti br apartmana da zna ciji ce boravak biti zavrsen
                        apartmanEndPoints[brojApartmana] = (IPEndPoint)remoteEP;

                        // Start odbrojavanja
                        vremePrijaveGosta[brojApartmana] = DateTime.Now;

                        // **SADA šaljemo potpunu potvrdu rezervacije**
                        string potvrda = $"POTVRDA=OK;APARTMAN={brojApartmana};GOSTIJU={apartman.TrenutniBrojGostiju};NOCI={brojNoci}";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(potvrda), lastGuestEndPoint);
                        Console.WriteLine($"[SERVER] POTVRDA rezervacije poslat za apartman {brojApartmana}");
                    }
                    continue;


                }
                else if (poruka.StartsWith("POTVRDA_PLACANJA"))
                {
                    // Parsiranje parametara
                    var delovi3 = poruka.Split(';');
                    int brojApartmana = -1;
                    string brojKartice = "";

                    foreach (var deo in delovi3)
                    {
                        if (deo.StartsWith("APARTMAN="))
                            int.TryParse(deo.Substring("APARTMAN=".Length), out brojApartmana);
                        else if (deo.StartsWith("KARTICA="))
                            brojKartice = deo.Substring("KARTICA=".Length);
                    }

                    if (brojApartmana != -1 && !string.IsNullOrEmpty(brojKartice))
                    {
                        // Ovdje možeš dodati validaciju kartice ako želiš
                        Console.WriteLine($"Primljeno placanje za apartman {brojApartmana} sa karticom {brojKartice}");

                        // Odgovori klijentu da je plaćanje prihvaćeno
                        string odgovor = $"PLACANJE_PRIHVACENO;APARTMAN={brojApartmana}";
                        byte[] odgovorBytes = Encoding.UTF8.GetBytes(odgovor);
                        // udpSocket.Send(odgovorBytes,odgovorBytes.Length,klijentEP);
                        //Console.WriteLine($"[SERVER] Plaćanje prihvaćeno za apartman {brojApartmana}.");

                        //apartmanEndPoints[brojApartmana] = (IPEndPoint)remoteEP;
                        
                        if (apartmanEndPoints.TryGetValue(brojApartmana, out IPEndPoint ep))
                        {
                          udpSocket.SendTo(odgovorBytes, ep);
                        }
                        else
                        {
                            Console.WriteLine($"[SERVER] Nema sacuvan EndPoint za apartman {brojApartmana}, ne salje potvrdu placanja.");
                        }

                        //udpSocket.SendTo(odgovorBytes, remoteEP);
                        Console.WriteLine($"[SERVER] Placanje prihvaceno za apartman {brojApartmana}.");
                        // Ovde šalješ tačno na klijentov IPEndPoint iz dictionary-ja:
                        /*if (apartmanEndPoints.TryGetValue(brojApartmana, out IPEndPoint klijentEP))
                        {
                            udpSocket.SendTo(odgovorBytes, klijentEP);
                            Console.WriteLine($"[SERVER] Plaćanje prihvaćeno za apartman {brojApartmana}.");
                        }
                        else
                        {
                            Console.WriteLine($"[SERVER] Nije pronađen EndPoint za apartman {brojApartmana}, ne može se poslati potvrda.");
                        }/////////////////
                    }
                    else
                    {
                        string greska = $"GRESKA;PORUKA=Nepotpuni podaci za placanje";
                        byte[] greskaBytes = Encoding.UTF8.GetBytes(greska);
                        udpSocket.SendTo(greskaBytes, lastGuestEndPoint); // ili remoteEP ako imaš
                    }
                    continue;
                }
                else
                {
                     string error = "ne pocinje sa rez/akc/zaht "; 
                     udpSocket.SendTo(Encoding.UTF8.GetBytes(error), remoteEP);
                }

            }
            #endregion

            #region smanjivanje dana
            foreach (var apartman in apartmani)
            {
                if (!(apartman.Stanje == StanjeApartmana.Zauzet || (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje && apartman.PreostaleNoci > 0)))
                    continue;
                if (apartman.Gosti.Count == 0)
                    continue;

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
                    if (apartman.PreostaleNoci == 0 && apartman.Stanje == StanjeApartmana.Zauzet)
                    {
                       // if (!apartman.ZavrsenoCiscenje)
                        
                            apartman.NaplatiNocenja();
                           //obr
                            //apartman.ZavrsenoCiscenje = true; // Obeleži da je obračun urađen
                        

                        apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                        apartman.TrenutniBrojGostiju = 0;

                        apartman.StanjeMinibara.Clear();       // resetovanje minibara
                        apartman.Alarm = StanjeAlarma.Normalno; // reset alarma ako je potrebno
                        //apartman.ZavrsenoCiscenje = false;

                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada prelazi u PotrebnoCiscenje nakon isteka boravka.");

                        if (apartmanEndPoints.TryGetValue(apartman.BrojApartmana, out IPEndPoint ep))
                        {
                            string listaTroskova = apartman.VratiListuTroskova();
                            string porukaZavrsetak = $"BORAVAK_ZAVRSEN;APARTMAN={apartman.BrojApartmana};UKUPNO={apartman.TrenutnoZaduzenje};TROSKOVI={listaTroskova}";
                            byte[] porukaBytes = Encoding.UTF8.GetBytes(porukaZavrsetak);

                            udpSocket.SendTo(porukaBytes, ep);
                            Console.WriteLine($"[SERVER] Poslata detaljna poruka BORAVAK_ZAVRSEN za apartman {apartman.BrojApartmana} klijentu na {ep}.");
                        }
                        else
                        {
                            Console.WriteLine($"[SERVER] Nema sačuvan EndPoint za apartman {apartman.BrojApartmana}, nije poslata poruka BORAVAK_ZAVRSEN.");
                        }
                        
                    }
                    if (apartman.PreostaleNoci == 0 && apartman.Stanje == StanjeApartmana.PotrebnoCiscenje)
                    {
                        // finalno čišćenje: pošalji zadatak osoblju
                        int idFinalno = apartman.BrojApartmana * 100 + 1;
                        if (!poslatiZadaci.Contains(idFinalno))
                        {
                            string zadatak = $"ZADATAK=Ciscenje;APARTMAN={apartman.BrojApartmana}";
                            foreach (var client in tcpClients)
                                PosaljiZadatak(client, zadatak);
                            poslatiZadaci.Add(idFinalno);
                            Console.WriteLine($"[SERVER] Poslat zadatak osoblju za FINALNO CISCENJE apartmana {apartman.BrojApartmana}");
                        }
                    }
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
                                    if (vrstaZadatka == "Ciscenje")
                                    {
                                        apartman.IspisiUkupnuCenu();

                                        apartman.Stanje = StanjeApartmana.Prazan;
                                        apartman.TrenutniBrojGostiju = 0;
                                        apartman.UkupnoNocenja = 0; // reset za sledeći boravak
                                        apartman.ZavrsenoCiscenje = false;
                                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada je Prazan.");
                                        
                                        
                                        //apartman.ZavrsenoCiscenje = true; // ➡️ OVO DODAŠ

                                    }
                                    else if (vrstaZadatka == "CiscenjeTrazeno")
                                    {
                                        apartman.NaplatiTrazenoCiscenje();
                                        apartman.Stanje = StanjeApartmana.Zauzet;
                                        //apartman.ZavrsenoCiscenje = true;
                                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} je ociscen na zahtev gosta.");
                                    }
                                    else if (vrstaZadatka == "SanacijaAlarma")
                                    {
                                        apartman.NaplatiAlarm();
                                        apartman.Alarm = StanjeAlarma.Normalno;
                                        Console.WriteLine($"[SERVER] Alarm u apartmanu {apartman.BrojApartmana} sada je deaktiviran.");
                                    }
                                    else if (vrstaZadatka == "UpravljanjeMinibarem")
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
                        Console.WriteLine($"[TCP] Neočekivana greska: {ex.Message}");
                        client.Shutdown(SocketShutdown.Both);
                        client.Close();
                        tcpClients.RemoveAt(i);
                    }
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
    }*/
/*
    private void PosaljiZadatkeOsobljuAkoPostoje()
    {
        for (int i = tcpClients.Count - 1; i >= 0; i--)
        {
            var client = tcpClients[i];

            foreach (var apartman in apartmani)
            {//CISCENJE KOJE ZAVRSAVA BROJANJE
                int idFinalno = apartman.BrojApartmana * 100 + 1;
                if (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje 
                    && apartman.PreostaleNoci==0 
                    && !poslatiZadaci.Contains(idFinalno))
                {
                    string zadatak = $"ZADATAK=Ciscenje;APARTMAN={apartman.BrojApartmana}";
                    PosaljiZadatak(client, zadatak);
                    poslatiZadaci.Add(idFinalno);
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

                int idTrazeno = apartman.BrojApartmana * 100 + 4;
                if (apartman.Stanje == StanjeApartmana.PotrebnoCiscenje
                    && apartman.PreostaleNoci >0
                    && !poslatiZadaci.Contains(idTrazeno))
                {
                    string zadatak = $"ZADATAK=CiscenjeTrazeno;APARTMAN={apartman.BrojApartmana}";
                    PosaljiZadatak(client, zadatak);
                    poslatiZadaci.Add(idTrazeno);
                    //break;
                    continue;
                }
            }
        }
    }

    private void PosaljiZadatak(Socket client, string zadatak)
    {
        byte[] data = Encoding.UTF8.GetBytes(zadatak);
        client.Send(data);
        Console.WriteLine($"[TCP] Poslat zadatak osoblju: {zadatak}");
    }

    private int GetZadatakKod(string vrstaZadatka)
    {
        // Metoda koja prevodi vrstu zadatka u kod koji koristiš za set poslatiZadaci
        if (vrstaZadatka == "Ciscenje")
            return 1;
        else if (vrstaZadatka == "SanacijaAlarma")
            return 2;
        else if (vrstaZadatka == "UpravljanjeMinibarem")
            return 3;
        else if(vrstaZadatka == "CiscenjeTrazeno")
            return 4;
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

}
*/