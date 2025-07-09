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

namespace Osoblje1
{   
        [Serializable]
        public class Osoblje11
        {
            public int ID { get; set; }
            public string Ime { get; set; }
            public string Prezime { get; set; }
            public Pol Pol { get; set; }
            public string Funkcija { get; set; }

            public Osoblje11() { }

            public Osoblje11(int id, string ime, string prezime, Pol pol, string funkcija)
            {
                ID = id;
                Ime = ime;
                Prezime = prezime;
                Pol = pol;
                Funkcija = funkcija;
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

            public static Osoblje11 Deserijalizuj(byte[] data)
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    return (Osoblje11)bf.Deserialize(ms);
                }
            }

            public override string ToString()
            {
                return $"Osoblje: {Ime} {Prezime}, Pol: {Pol}, Funkcija: {Funkcija}, ID: {ID}";
            }
        }
    }
