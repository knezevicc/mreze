#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <windows.h>
#include "Gost.h"
#include "fstream"
#pragma comment(lib, "Ws2_32.lib")
using namespace std;
#include<thread>

int main()
{
    sockaddr_in mojaAdresa{};
    mojaAdresa.sin_family = AF_INET;
    InetPton(AF_INET, L"127.0.0.1", &mojaAdresa.sin_addr);
    mojaAdresa.sin_port = htons(50001); // lokalni port gosta (nije obavezno)

    Gost g("Milica", 101, 2, 3, mojaAdresa);

    //proba
    /*
    // Serijalizuj u fajl
    ofstream outFile("gost.bin", std::ios::binary);
    if (!outFile) {
        std::cout << "Ne mogu da otvorim fajl za pisanje!\n";
        return 1;
    }
    g.serijalizuj(outFile);
    outFile.close();

    // Deserijalizuj u novi objekat
    Gost g2("Milan", 103, 6, 10, mojaAdresa);
    ifstream inFile("gost.bin", std::ios::binary);
    if (!inFile) {
        cout << "Ne mogu da otvorim fajl za čitanje!\n";
        return 1;
    }
    g2.deserijalizuj(inFile);
    inFile.close();

    // Provera
    cout << "Provera podataka nakon deserijalizacije:\n";
    cout << "Ime: " << g2.ime << "\n";
    cout << "Broj apartmana: " << g2.brojApartmana << "\n";
    cout << "Broj gostiju: " << g2.brojGostiju << "\n";
    cout << "Broj noći: " << g2.brojNoci << "\n";

    if (g.ime == g2.ime &&
        g.brojApartmana == g2.brojApartmana &&
        g.brojGostiju == g2.brojGostiju &&
        g.brojNoci == g2.brojNoci) {
        cout << "Serijalizacija i deserijalizacija su uspešne!\n";
    }
    else {
        cout << "Nešto nije u redu sa serijalizacijom/deserijalizacijom.\n";
    }
    */
    //proba

    g.posaljiRezervaciju("127.0.0.1", 12345); // šalje serveru

    cout << "Pritisni enter za izlaz..";
    cin.get();

    return 0;
}
