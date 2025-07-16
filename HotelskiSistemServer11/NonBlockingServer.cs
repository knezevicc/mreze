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
            //if (udpSocket.Available > 0)
            if (udpSocket.Poll(0, SelectMode.SelectRead))
            {
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                int received = udpSocket.ReceiveFrom(udpBuffer, ref remoteEP);
                lastGuestEndPoint = (IPEndPoint)remoteEP;

                byte[] receivedData = new byte[received];
                Array.Copy(udpBuffer, receivedData, received);

                string poruka = Encoding.UTF8.GetString(udpBuffer, 0, received);
                //string poruka = Encoding.UTF8.GetString(receivedData);
                string[] delovi = poruka.Split(';').Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();

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
                    //int brojA = int.Parse(delovi[1].Split('=')[1]);
                    //Apartman11 apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojA);

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
                            PosaljiZadatkeOsobljuAkoPostoje();
                        }
                        else if (akcija == "MINIBAR")
                        {
                            //ubaceno 16.7-zbog naplate
                            string artikal = delovi.FirstOrDefault(s => s.StartsWith("ARTIKAL="))?.Split('=')[1];
                            if (!string.IsNullOrEmpty(artikal))
                            {
                                apartman.IskoristiMinibarArtikal(artikal);
                                odgovor = $"Iskorišćen artikal '{artikal}' iz minibara.";
                            }
                            else
                            {
                                odgovor = "Nije naveden artikal za MINIBAR akciju.";
                            }
                            /*foreach (var deoUslova in delovi)
                            {
                                if (deoUslova.StartsWith("ARTIKAL="))
                                {
                                    artikal = deoUslova.Split('=')[1];
                                    break;
                                }
                            }

                            if (artikal != null)
                            {
                                apartman.IskoristiMinibarArtikal(artikal);
                                odgovor = $"Preuzeli ste artikl '{artikal}' iz minibara.";
                            }
                            else
                            {
                                odgovor = "Nije naveden artikal za minibar.";
                            }*/
                            /*
                            foreach (var key in apartman.StanjeMinibara.Keys.ToList())
                            {
                                apartman.StanjeMinibara[key] = Math.Max(0, apartman.StanjeMinibara[key] - 1);
                            }
                            odgovor = "Preuzeli ste artikle iz minibara.";
                            */
                        }
                        else if (akcija == "CISCENJE")
                        {
                            apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                            odgovor = "Zatraženo čišćenje sobe.";
                            PosaljiZadatkeOsobljuAkoPostoje();
                        }
                        else if (akcija == "CISCENJE2")
                        {
                            apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                          //apartman.TrenutniBrojGostiju = 0;
                            odgovor = "Zatraženo čišćenje sobe u medjuboravku!!(4).";
                            PosaljiZadatkeOsobljuAkoPostoje();

                            //dodaj slanje BORAVAK_ZAVRSEN klijentu:**
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
                        }/*
                        else if (poruka.StartsWith("ZAHTEV=UKUPNA_CENA"))
                        {
                            //int brojApartmana = -1;
                            foreach (var deo in delovi)
                            {
                                if (deo.StartsWith("APARTMAN="))
                                {
                                    int.TryParse(deo.Split('=')[1], out brojA);
                                    break;
                                }
                            }

                            //Apartman11 apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojApartmana);

                            if (apartman != null)
                            {
                                apartman.NaplatiNocenja(); // osigurava da su noćenja naplaćena
                                apartman.IspisiUkupnuCenu(); // ispis na serveru

                                double ukupnaCena = apartman.TrenutnoZaduzenje;

                                Console.WriteLine($"[SERVER] Ukupna cena boravka za apartman {apartman.BrojApartmana} je {ukupnaCena} EUR.");

                                string porukaCena = $"UKUPNA_CENA;APARTMAN={apartman.BrojApartmana};IZNOS={ukupnaCena}";
                                byte[] data = Encoding.UTF8.GetBytes(porukaCena);
                                udpSocket.SendTo(data, remoteEP);
                            }
                            else
                            {
                                string error = $"GRESKA=Apartman {brojA} ne postoji";
                                udpSocket.SendTo(Encoding.UTF8.GetBytes(error), remoteEP);
                            }

                            continue;
                        }
                        else
                        {
                            odgovor = "Nepoznata akcija.";
                        }*/
                        //ZAKOMENTARISANO 15.7
                        
                        byte[] odgovorBytes = Encoding.UTF8.GetBytes(odgovor);
                        udpSocket.SendTo(odgovorBytes, remoteEP);
                        
                        /*string odgovorFinal = $"POTVRDA={akcija};APARTMAN={apartman.BrojApartmana};MSG={odgovor}";
                        byte[] odgovorBytes = Encoding.UTF8.GetBytes(odgovorFinal);
                        udpSocket.SendTo(odgovorBytes, remoteEP);
                        Console.WriteLine($"[SERVER] Poslata potvrda: {odgovorFinal}");
                        */
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
                    int brojApartmana1 = -1;
                    int brojGostiju1 = -1;

                    foreach (var deo in delovi)
                    {
                        if (deo.StartsWith("APARTMAN="))
                            int.TryParse(deo.Split('=')[1], out brojApartmana1);
                        else if (deo.StartsWith("GOSTIJU="))
                            int.TryParse(deo.Split('=')[1], out brojGostiju1);
                    }

                    if (brojApartmana1 != -1 && brojGostiju1 != -1)
                    {
                        var apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojApartmana1);
                        if (apartman != null && apartman.Stanje == StanjeApartmana.Prazan &&
                            brojGostiju1 <= apartman.MaksimalanBrojGostiju)
                        {
                            apartman.TrenutniBrojGostiju = brojGostiju1;
                            apartman.Stanje = StanjeApartmana.Zauzet;

                            string potvrdaRez = $"POTVRDA=OK;APARTMAN={brojApartmana1};GOSTIJU={brojGostiju1}";
                            udpSocket.SendTo(Encoding.UTF8.GetBytes(potvrdaRez), lastGuestEndPoint);
                            Console.WriteLine($"[SERVER] Poslata potvrda rezervacije: {potvrdaRez}");

                        }
                        /*Apartman11 apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojApartmana1);
                        if (apartman != null && apartman.Stanje == StanjeApartmana.Prazan &&
                            brojGostiju1 <= apartman.MaksimalanBrojGostiju)
                        {
                            apartman.TrenutniBrojGostiju = brojGostiju1;
                           
                            apartman.Stanje = StanjeApartmana.Zauzet;
                            vremePrijaveGosta[brojApartmana1] = DateTime.Now;

                            string potvrda = $"POTVRDA=OK;APARTMAN={brojApartmana1}";
                            byte[] potvrdaBytes = Encoding.UTF8.GetBytes(potvrda);
                            udpSocket.SendTo(potvrdaBytes, remoteEP);

                            Console.WriteLine($"[SERVER] Gost prijavljen: Apartman={brojApartmana1}, Gosti={brojGostiju1}");
                        }
                        else
                        {
                            string error = "GRESKA=Nema slobodnog apartmana ili apartman ne postoji";
                            byte[] errorBytes = Encoding.UTF8.GetBytes(error);
                            udpSocket.SendTo(errorBytes, remoteEP);
                        }*/
                    }
                    else
                    {
                        string error = "GRESKA=Nevalidan zahtev (nedostaju podaci)";
                        byte[] errorBytes = Encoding.UTF8.GetBytes(error);
                        udpSocket.SendTo(errorBytes, remoteEP);
                    }

                }
                else if (poruka.StartsWith("GOSTI"))
                {
                    Console.WriteLine($"[SERVER] Primljena GOSTI poruka: {poruka}");

                    int brojApartmana = -1, brojNoci = -1;
                    var gostiPodaci = new List<string>();

                    foreach (var deo in delovi)
                    {
                        if (deo.StartsWith("APARTMAN="))
                            int.TryParse(deo.Split('=')[1], out brojApartmana);
                        else if (deo.StartsWith("NOCI="))
                            int.TryParse(deo.Split('=')[1], out brojNoci);
                        else if (deo.StartsWith("GOST") && deo.Contains("="))
                            gostiPodaci.Add(deo.Split('=')[1]);
                    }

                    var apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojApartmana);
                    if (apartman != null && apartman.Stanje == StanjeApartmana.Zauzet && brojNoci > 0 && gostiPodaci.Count == apartman.TrenutniBrojGostiju)
                    {
                        apartman.PreostaleNoci = brojNoci;
                        apartman.UkupnoNocenja = brojNoci;
                        apartman.Gosti.Clear();

                        foreach (var gostStr in gostiPodaci)
                        {
                            var p = gostStr.Split(',');
                            apartman.Gosti.Add(new Gost11
                            {
                                Ime = p[0],
                                Prezime = p[1],
                                Pol = p[2],
                                DatumRodjenja = DateTime.ParseExact(p[3], "dd.MM.yyyy", null),
                                BrojPasosa = p[4]
                            });
                        }

                        //pamti br apartmana da zna ciji ce boravak biti zavrsen
                        apartmanEndPoints[brojApartmana] = (IPEndPoint)remoteEP;

                        // Start odbrojavanja
                        vremePrijaveGosta[brojApartmana] = DateTime.Now;

                        // **SADA šaljemo potpunu potvrdu rezervacije**
                        string potvrda = $"POTVRDA=OK;APARTMAN={brojApartmana};GOSTIJU={apartman.TrenutniBrojGostiju};NOCI={brojNoci}";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(potvrda), lastGuestEndPoint);
                        Console.WriteLine($"[SERVER] POTVRDA rezervacije poslat za apartman {brojApartmana}");


                        // Pošalji zadatak čišćenja osoblju kad poželiš, npr. odmah ili tek kad istekne boravak
                    }
                    continue;

                    /*
                    if (brojApartmana != -1 && brojNoci != -1 && gostiPodaci.Count > 0)
                    {
                        Apartman11 apartman = apartmani.FirstOrDefault(a => a.BrojApartmana == brojApartmana);
                        if (apartman != null && apartman.Stanje == StanjeApartmana.Zauzet)
                        {
                            apartman.PreostaleNoci = brojNoci;
                            apartman.Gosti.Clear();
                            foreach (var gostStr in gostiPodaci)
                            {
                                var podaci = gostStr.Split(',');
                                if (podaci.Length == 5)
                                {
                                    Gost11 gost = new Gost11
                                    {
                                        Ime = podaci[0],
                                        Prezime = podaci[1],
                                        Pol = podaci[2],
                                        DatumRodjenja = DateTime.Parse(podaci[3]),
                                        BrojPasosa = podaci[4]
                                    };
                                    apartman.Gosti.Add(gost);
                                }
                            }
                            vremePrijaveGosta[brojApartmana] = DateTime.Now;

                            string potvrda = $"POTVRDA=GOSTI;APARTMAN={brojApartmana}";
                            udpSocket.SendTo(Encoding.UTF8.GetBytes(potvrda), remoteEP);

                            Console.WriteLine($"[SERVER] Gost(i) evidentirani za apartman {brojApartmana} na {brojNoci} noći.");
                        }
                        else
                        {
                            string error = "GRESKA=Apartman ne postoji ili nije zauzet";
                            udpSocket.SendTo(Encoding.UTF8.GetBytes(error), remoteEP);
                        }
                    }
                    else
                    {
                        string error = "GRESKA=Nevalidan zahtev za goste";
                        udpSocket.SendTo(Encoding.UTF8.GetBytes(error), remoteEP);
                    }

                    continue;*/
                }
                else
                {
                    string error = "ne pocinje sa rez/akc/zaht "; udpSocket.SendTo(Encoding.UTF8.GetBytes(error), remoteEP);
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
                        if (!apartman.ZavrsenoCiscenje)
                        {
                            apartman.NaplatiNocenja();
                            apartman.ZavrsenoCiscenje = true; // Obeleži da je obračun urađen
                        }

                        apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                        apartman.TrenutniBrojGostiju = 0;

                        apartman.StanjeMinibara.Clear();       // resetovanje minibara
                        apartman.Alarm = StanjeAlarma.Normalno; // reset alarma ako je potrebno
                        apartman.ZavrsenoCiscenje = false;

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

                    /*
                    if (apartman.PreostaleNoci == 0 && apartman.Stanje == StanjeApartmana.Zauzet)
                    {
                        //if (apartman.PreostaleNoci == 0 && apartman.Stanje == StanjeApartmana.Zauzet && !apartman.ZavrsenoCiscenje)
                        //apartman.NaplatiNocenja(); // ➡️ obračun cena pre završetka
                      
                        if (!apartman.ZavrsenoCiscenje) // dodaš bool ZavrsenoCiscenje u Apartman11
                        {
                            apartman.NaplatiNocenja();
                            //apartman.ZavrsenoCiscenje = true;

                        }
                        // Slanje ukupne cene klijentu
                        if (apartmanEndPoints.TryGetValue(apartman.BrojApartmana, out IPEndPoint ep))
                        {
                            string cenaPoruka = $"UKUPNA_CENA;APARTMAN={apartman.BrojApartmana};IZNOS={apartman.TrenutnoZaduzenje}";
                            byte[] data = Encoding.UTF8.GetBytes(cenaPoruka);
                            udpSocket.SendTo(data, ep);
                            apartman.ZavrsenoCiscenje = true; // obeleži da je završeno
                            Console.WriteLine($"[SERVER] Poslata ukupna cena gostu u apartmanu {apartman.BrojApartmana}.");
                    }
                        else
                        {
                            Console.WriteLine($"[SERVER] Nema sačuvan EndPoint za apartman {apartman.BrojApartmana}, nije poslata UKUPNA_CENA.");
                        }

                        apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                        apartman.TrenutniBrojGostiju = 0;
                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada prelazi u PotrebnoCiscenje nakon isteka boravka.");

                        //
                        string listaTroskova = apartman.VratiListuTroskova();
                        string porukaZavrsetak = $"BORAVAK_ZAVRSEN;APARTMAN={apartman.BrojApartmana};UKUPNO={apartman.TrenutnoZaduzenje};TROSKOVI={listaTroskova}";
                        byte[] porukaBytes = Encoding.UTF8.GetBytes(porukaZavrsetak);

                        /*
                        if (apartmanEndPoints.TryGetValue(apartman.BrojApartmana, out IPEndPoint ep))
                        {
                            udpSocket.SendTo(porukaBytes, ep);
                            Console.WriteLine($"[SERVER] Poslata detaljna poruka BORAVAK_ZAVRSEN za apartman {apartman.BrojApartmana}.");
                        }
                        else
                        {
                            Console.WriteLine($"[SERVER] Nema sačuvan EndPoint za apartman {apartman.BrojApartmana}, nije poslata poruka BORAVAK_ZAVRSEN.");
                        }*/
                    //
                    /*RADIIIIIII
                     string porukaZavrsetak = $"BORAVAK_ZAVRSEN;APARTMAN={apartman.BrojApartmana}";
                    byte[] porukaBytes = Encoding.UTF8.GetBytes(porukaZavrsetak);

                    udpSocket.SendTo(porukaBytes, ep);
                    Console.WriteLine($"[SERVER] Poslata poruka BORAVAK_ZAVRSEN za apartman {apartman.BrojApartmana} klijentu na {ep}");
              */
                    // }

                    /* ZAKOM 16.7 UVECE
                      if (apartman.PreostaleNoci == 0 && apartman.Stanje == StanjeApartmana.Zauzet)
                      {
                          apartman.Stanje = StanjeApartmana.PotrebnoCiscenje;
                          apartman.TrenutniBrojGostiju = 0;
                          Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada prelazi u PotrebnoCiscenje nakon isteka boravka.");

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
                    */
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
                                        //apartman.NaplatiZavrsnoCiscenje();
                                        //apartman.NaplatiNocenja();

                                        apartman.IspisiUkupnuCenu();
                                        //da ne bi slao 2 puta cenu
                                        /*string cenaPoruka = $"UKUPNA_CENA;APARTMAN={apartman.BrojApartmana};IZNOS={apartman.TrenutnoZaduzenje}";
                                        if (apartmanEndPoints.TryGetValue(apartman.BrojApartmana, out IPEndPoint gostEp))
                                        {
                                            byte[] data = Encoding.UTF8.GetBytes(cenaPoruka);
                                            udpSocket.SendTo(data, gostEp);
                                            Console.WriteLine($"[SERVER] Poslata ukupna cena gostu u apartmanu {apartman.BrojApartmana}.");
                                        }
                                        */

                                        apartman.Stanje = StanjeApartmana.Prazan;
                                        apartman.TrenutniBrojGostiju = 0;
                                        apartman.UkupnoNocenja = 0; // reset za sledeći boravak
                                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} sada je Prazan.");
                                        
                                        
                                        apartman.ZavrsenoCiscenje = true; // ➡️ OVO DODAŠ

                                    }
                                    else if (vrstaZadatka == "CiscenjeTrazeno")
                                    {
                                        apartman.NaplatiTrazenoCiscenje();
                                        apartman.Stanje = StanjeApartmana.Zauzet;
                                        Console.WriteLine($"[SERVER] Apartman {apartman.BrojApartmana} je očišćen na zahtev gosta.");
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
                        Console.WriteLine($"[TCP] Neočekivana greška: {ex.Message}");
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
