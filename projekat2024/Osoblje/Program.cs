﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Osoblje
{
    public class Program
    {
        static void Main(string[] args)
        {
            OsobljeKlasa osoblje = new OsobljeKlasa();
            osoblje.SendRequest();
            Console.ReadLine();
        }
    }
}
