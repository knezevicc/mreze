using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Gost1;

namespace HotelskiSistemServer11
{
    [Serializable]
    public enum StanjeApartmana
    {
        Prazan,
        Zauzet,
        PotrebnoCiscenje
    }

    [Serializable]
    public enum StanjeAlarma
    {
        Normalno,
        Aktivirano
    }
    [Serializable]
    public class Apartman11
    {
        public int BrojApartmana { get; set; }
        public int Sprat { get; set; }
        public int Klasa { get; set; }  // 1,2,3
        public int MaksimalanBrojGostiju { get; set; }
        public int TrenutniBrojGostiju { get; set; }
        public StanjeApartmana Stanje { get; set; }
        public StanjeAlarma Alarm { get; set; }
        public List<string> ListaGostiju { get; set; }  // ovde možeš staviti stringove sa imenima gostiju
        public Dictionary<string, int> StanjeMinibara { get; set; }
        public int PreostaleNoci { get; set; }
        public int UkupnoNocenja { get; set; } = 0; // NOVO za pamcenje ukupnih nocenja
        public bool ZavrsenoCiscenje { get; set; } = false;
        public double TrenutnoZaduzenje { get; set; } = 0;
        public double UtrosenoMinibar { get; set; } = 0;


        public static readonly Dictionary<string, double> CenaMinibara = new Dictionary<string, double> {
            {"Pivo", 3.0},
            {"Voda", 1.5},
            {"Cokoladica", 2.0}
        };

        public List<Gost11> Gosti { get; set; } = new List<Gost11>();
        public bool alarmProvera { get; set; } = false;

        public Apartman11()
        {
            //ListaGostiju = new List<string>();
            StanjeMinibara = new Dictionary<string, int>();
            Stanje = StanjeApartmana.Prazan;
            Alarm = StanjeAlarma.Normalno;
            PreostaleNoci = 0;
            TrenutnoZaduzenje = 0;
        }

        public Apartman11(int brojApartmana, int sprat, int klasa, int maxGostiju)
        {
            BrojApartmana = brojApartmana;
            Sprat = sprat;
            Klasa = klasa;
            MaksimalanBrojGostiju = maxGostiju;
            TrenutniBrojGostiju = 0;
            Stanje = StanjeApartmana.Prazan;
            Alarm = StanjeAlarma.Normalno;
            // ListaGostiju = new List<string>();
            StanjeMinibara = new Dictionary<string, int>()
            {
                {"Pivo", 5},
                {"Voda", 5},
                {"Cokoladica", 5 }
            };
            PreostaleNoci = 0;
            TrenutnoZaduzenje = 0;
        }
        public void Reset()
        {
            Gosti.Clear();
            TrenutniBrojGostiju = 0;
            ZavrsenoCiscenje = false;
            alarmProvera = false;
            UtrosenoMinibar = 0;
            UkupnoNocenja = 0;
            Stanje = StanjeApartmana.Prazan;
            Alarm = StanjeAlarma.Normalno;
            PreostaleNoci = 0;
            TrenutnoZaduzenje = 0;
        }
        public void DodajUslugu(string naziv, double cena)
        {
            TrenutnoZaduzenje += cena;
            Console.WriteLine($"[APARTMAN {BrojApartmana}] Dodata usluga '{naziv}' ({cena} EUR). Novo zaduženje: {TrenutnoZaduzenje} EUR.");
        }

        public void IskoristiMinibarArtikal(string artikal)
        {
            if (StanjeMinibara.ContainsKey(artikal) && StanjeMinibara[artikal] > 0)
            {
                StanjeMinibara[artikal]--;
                double cena = CenaMinibara.ContainsKey(artikal) ? CenaMinibara[artikal] : 2.5; // podrazumevana cena ako nema u rečniku
                DodajUslugu($"Minibar-{artikal}", cena);
                UtrosenoMinibar += cena; // VAŽNO: pamti za prikaz u VratiListuTroskova
                Console.WriteLine($"[APARTMAN {BrojApartmana}] Iskorišćen artikal '{artikal}' iz minibara po ceni {cena} EUR.");
            }
            else
            {
                Console.WriteLine($"[APARTMAN {BrojApartmana}] Artikal '{artikal}' nije dostupan u minibaru.");
            }
        }

        public void NaplatiNocenja()
        {
            double cenaPoNoci;
            switch (Klasa) {
                case 1:
                    cenaPoNoci = 20;
                    break;
                case 2:
                    cenaPoNoci = 30;
                    break;
                case 3:
                    cenaPoNoci = 50;
                    break;
                default:
                    cenaPoNoci = 20;
                    break;
            }
            //TrenutnoZaduzenje += UkupnoNocenja * cenaPoNoci;
            //DodajUslugu($"Noćenje x{UkupnoNocenja}:", TrenutnoZaduzenje);
            
            //double cenaNocenja = UkupnoNocenja * cenaPoNoci;
            //double ukupnaCena = TrenutnoZaduzenje + cenaNocenja;
            //return ukupnaCena;

            double ukupno = UkupnoNocenja * cenaPoNoci;
            DodajUslugu($"Noćenje x{UkupnoNocenja} :", ukupno);

            //double ukupno = cenaPoNoci * PreostaleNoci * TrenutniBrojGostiju;
            //DodajUslugu($"Noćenje x{PreostaleNoci} za {TrenutniBrojGostiju} gosta", ukupno);
            //DodajUslugu($"Noćenje x{UkupnoNocenja} za {TrenutniBrojGostiju} gosta", TrenutnoZaduzenje);
        }
        
        public void NaplatiAlarm()
        {
            DodajUslugu("Aktivacija alarma", 5);
        }

        public void NaplatiTrazenoCiscenje()
        {
            DodajUslugu("Traženo čišćenje", 15);
        }

        public void IspisiUkupnuCenu()
        {
            Console.WriteLine($"[APARTMAN {BrojApartmana}] Ukupna cena boravka: {TrenutnoZaduzenje} EUR.");
            TrenutnoZaduzenje = 0;
            TrenutniBrojGostiju = 0;
        }

        public string VratiListuTroskova()
        {
            double ukupnoMinibar = 0;
            foreach (var artikal in StanjeMinibara)
            {
                double cenaPoArtiklu = CenaMinibara.ContainsKey(artikal.Key) ? CenaMinibara[artikal.Key] : 0;

                ukupnoMinibar += artikal.Value * cenaPoArtiklu;
                /*double cenaArtikla = artikal.Value * cenaPoArtiklu;

                                Console.WriteLine($"Artikal: {artikal.Key}, Količina: {artikal.Value}, Cena po artiklu: {cenaPoArtiklu}, Ukupno: {cenaArtikla}");
                                ukupnoMinibar += cenaArtikla;*/
            }
            double cenaAlarma = 0;
            if (alarmProvera)
                cenaAlarma = 5;

            double cenaCiscenja = 0;
            if (ZavrsenoCiscenje)
                cenaCiscenja = 15;
            
            double cenaPoNoci = 0;

            // Izračunaj cenu noćenja po klasi i broju noći
            switch (Klasa)
            {
                case 1:
                    cenaPoNoci = 20;
                    break;
                case 2:
                    cenaPoNoci = 30;
                    break;
                case 3:
                    cenaPoNoci = 50;
                    break;
                default:
                    cenaPoNoci = 20;
                    break;
            }
            double cenaNocenja = cenaPoNoci * UkupnoNocenja;

            return $"Minibar: {UtrosenoMinibar}.00 EUR, Alarm: {cenaAlarma}.00 EUR, Čišćenje: {cenaCiscenja}.00 EUR, Noćenja: {cenaNocenja}.00 EUR";
        }


        public byte[] Serijalizuj()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, this);
                return ms.ToArray();
            }
        }

        public static Apartman11 Deserijalizuj(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                return (Apartman11)bf.Deserialize(ms);
            }
        }

        public override string ToString()
        {
            string minibarStanje = string.Join(", ", StanjeMinibara.Select(x => $"{x.Key}-{x.Value}"));
            string gosti = Gosti.Count > 0 ? string.Join(", ", Gosti.Select(g => $"{g.Ime} {g.Prezime}")) : "Nema gostiju";
            return $"Apartman {BrojApartmana} | Sprat: {Sprat}, Klasa: {Klasa}, MaxGostiju: {MaksimalanBrojGostiju}, " +
                   $"TrenutnoGostiju: {TrenutniBrojGostiju}, PreostaleNoci: {PreostaleNoci}, " +
                   $"Stanje: {Stanje}, Alarm: {Alarm}, Zaduzenje: {TrenutnoZaduzenje} EUR, " +
                   $"Minibar: {minibarStanje}, Gosti: {gosti}";
        }
    }
}
