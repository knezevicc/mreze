using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

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
        public double TrenutnoZaduzenje { get; set; }

        public Apartman11()
        {
            ListaGostiju = new List<string>();
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
            ListaGostiju = new List<string>();
            StanjeMinibara = new Dictionary<string, int>()
            {
                {"Pivo", 5},
                {"Voda", 5},
                {"Cokoladica", 5 }
            };
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
                DodajUslugu($"Minibar-{artikal}", 2.5);
                Console.WriteLine($"[APARTMAN {BrojApartmana}] Iskorišćen artikal '{artikal}' iz minibara.");
            }
            else
            {
                Console.WriteLine($"[APARTMAN {BrojApartmana}] Artikal '{artikal}' nije dostupan u minibaru.");
            }
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
            string gosti = ListaGostiju.Count > 0 ? string.Join(", ", ListaGostiju) : "Nema gostiju";
            return $"Apartman {BrojApartmana} | Sprat: {Sprat}, Klasa: {Klasa}, MaxGostiju: {MaksimalanBrojGostiju}, " +
                   $"TrenutnoGostiju: {TrenutniBrojGostiju}, PreostaleNoci: {PreostaleNoci}, " +
                   $"Stanje: {Stanje}, Alarm: {Alarm}, Zaduzenje: {TrenutnoZaduzenje} EUR, " +
                   $"Minibar: {minibarStanje}, Gosti: {gosti}";
        }
    }
}
