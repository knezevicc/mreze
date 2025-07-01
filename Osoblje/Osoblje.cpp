#include "Osoblje.h"
#include <winsock2.h>
#include <iostream>
#include <ws2tcpip.h>
#define _WINSOCK_DEPRECATED_NO_WARNINGS


//proba

Osoblje::Osoblje(string ime, int id) : ime(ime), id(id) {}

void Osoblje::poveziSeSaServerom(const char* ip, int port) {
    WSADATA wsaData;
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        std::cout << "Neuspešna inicijalizacija Winsock-a.\n";
        return;
    }

    sock = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (sock == INVALID_SOCKET) {
        cout << "Neuspešno kreiranje TCP socketa.\n";
        WSACleanup();
        return;
    }

    sockaddr_in serverAddr;
    serverAddr.sin_family = AF_INET;
    InetPton(AF_INET, L"127.0.0.1", &serverAddr.sin_addr);
    serverAddr.sin_port = htons(port);


    if (connect(sock, (sockaddr*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
        cout << "Neuspešna konekcija na server.\n";
        closesocket(sock);
        WSACleanup();
        return;
    }
    cout << "Uspešno povezano sa serverom putem TCP.\n";
}

void Osoblje::primiZadatke() {
    char buffer[1024];
    int bytesReceived;

    DWORD timeout = 10000; 
    setsockopt(sock, SOL_SOCKET, SO_RCVTIMEO, (const char*)&timeout, sizeof(timeout));

    while (true) {
        bytesReceived = recv(sock, buffer, sizeof(buffer) - 1, 0);
        if (bytesReceived == SOCKET_ERROR || bytesReceived == 0) {
            cout << "Konekcija prekinuta ili greška u prijemu.\n";
            break;
        }
        buffer[bytesReceived] = '\0';
        cout << "Primljen zadatak: " << buffer << endl;
        

      //ovo radi ali se odma zatvori TCP prozor 
        // Posalji potvrdu nazad serveru
        const char* potvrda = "Zadatak primljen i izvrsen";
        int poslato = send(sock, potvrda, (int)strlen(potvrda), 0);
        if (poslato == SOCKET_ERROR) {
            cout << "Slanje potvrde nije uspelo.\n";
        }
        else {
            cout << "Potvrda poslata serveru.\n";
        }
      //

    }
    closesocket(sock);
    WSACleanup();
}

/*
void HotelskoOsoblje::cistiApartman(int brojApartmana) {
    std::cout << "Osoblje " << ime << " cisti apartman broj " << brojApartmana << std::endl;
    // Logika za čišćenje...
}

void HotelskoOsoblje::sanirajAlarm(int brojApartmana) {
    std::cout << "Osoblje " << ime << " sanira alarm u apartmanu broj " << brojApartmana << std::endl;
    // Logika za sanaciju alarma...
}

void HotelskoOsoblje::azurirajMinibar(int brojApartmana, int stanjeMinibara) {
    std::cout << "Osoblje " << ime << " azurira minibar u apartmanu broj " << brojApartmana
        << " na stanje " << stanjeMinibara << std::endl;
    // Logika za ažuriranje minibara...
}
*/