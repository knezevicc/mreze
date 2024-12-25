using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gost
{
    public class Program
    {
        static void Main(string[] args)
        {
            GostKlasa gostKlasa = new GostKlasa();
            gostKlasa.SendReservation();
            Console.ReadLine();
        }
    }
}
