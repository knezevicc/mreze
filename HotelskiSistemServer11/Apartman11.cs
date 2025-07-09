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

        public Apartman11()
        {
            ListaGostiju = new List<string>();
            Stanje = StanjeApartmana.Prazan;
            Alarm = StanjeAlarma.Normalno;
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
            return $"Apartman {BrojApartmana} (Sprat: {Sprat}, Klasa: {Klasa}), " +
                   $"Max gostiju: {MaksimalanBrojGostiju}, Trenutno gostiju: {TrenutniBrojGostiju}, " +
                   $"Stanje: {Stanje}, Alarm: {Alarm}, Gostiju: {ListaGostiju.Count}";
        }
    }
}
