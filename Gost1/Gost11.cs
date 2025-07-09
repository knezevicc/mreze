using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public enum Pol
{
    Muski,
    Zenski,
    Drugi
}

namespace Gost1
{
    [Serializable]
    public class Gost11
    {
        public string Ime { get; set; }
        public string Prezime { get; set; }
        public Pol Pol { get; set; }
        public DateTime DatumRodjenja { get; set; }
        public string BrojPasosa { get; set; }

        public Gost11() { }

        public Gost11(string ime, string prezime, Pol pol, DateTime datumRodjenja, string brojPasosa)
        {
            Ime = ime;
            Prezime = prezime;
            Pol = pol;
            DatumRodjenja = datumRodjenja;
            BrojPasosa = brojPasosa;
        }

        // Serijalizacija objekta u niz bajtova
        public byte[] Serijalizuj()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ms, this);
                return ms.ToArray();
            }
        }

        // Deserijalizacija objekta iz niza bajtova
        public static Gost11 Deserijalizuj(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                BinaryFormatter bf = new BinaryFormatter();
                return (Gost11)bf.Deserialize(ms);
            }
        }

        public override string ToString()
        {
            return $"Gost: {Ime} {Prezime}, Pol: {Pol}, Datum rođenja: {DatumRodjenja.ToShortDateString()}, Broj pasoša: {BrojPasosa}";
        }
    }
}
