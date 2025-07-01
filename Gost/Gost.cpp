#include "Gost.h"
#pragma comment(lib, "ws2_32.lib")
#include <iostream>
#include <winsock2.h>
#include <ws2tcpip.h>
#include <string>
#include <fstream>


Gost::Gost(string ime, int apartman, int gosti, int noci, sockaddr_in adr)
    : ime(ime), brojApartmana(apartman), brojGostiju(gosti), brojNoci(noci),
    adresaGosta(adr), minibarZaduzenje(0), alarmAktiviran(false) {}

void Gost::posaljiRezervaciju(string serverIp, int serverPort) {
    WSADATA wsaData;
    SOCKET udpSocket = INVALID_SOCKET;
    sockaddr_in serverAddr;

    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0) {
        std::cout << "WSAStartup failed.\n";
        return;
    }

    udpSocket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
    if (udpSocket == INVALID_SOCKET) {
        cout << "Failed to create UDP socket.\n";
        WSACleanup();
        return;
    }

    serverAddr.sin_family = AF_INET;
    inet_pton(AF_INET, serverIp.c_str(), &serverAddr.sin_addr);
    serverAddr.sin_port = htons(serverPort);

    string poruka = "IME=" + ime + ";APARTMAN=" + to_string(brojApartmana) +
        ";GOSTIJU=" + to_string(brojGostiju) +
        ";NOCI=" + to_string(brojNoci);

    int sendResult = sendto(udpSocket, poruka.c_str(), (int)poruka.size(), 0, (sockaddr*)&serverAddr, sizeof(serverAddr));
    if (sendResult == SOCKET_ERROR) {
        cout << "Send failed with error: " << WSAGetLastError() << "\n";
    }
    else {
        cout << "Rezervacija poslata: " << poruka << "\n";


        //primanje poruke od servera
        char recvBuffer[512];
        sockaddr_in fromAddr;
        int fromLen = sizeof(fromAddr);

        int recvResult = recvfrom(udpSocket, recvBuffer, sizeof(recvBuffer) - 1, 0,
            (sockaddr*)&fromAddr, &fromLen);

        if (recvResult == SOCKET_ERROR) {
            std::cout << "Primanje potvrde nije uspelo: " << WSAGetLastError() << "\n";
        }
        else {
            recvBuffer[recvResult] = '\0'; // Null-terminiraj string
            std::cout << "Potvrda od servera: " << recvBuffer << "\n";
        }
    }

    closesocket(udpSocket);
    WSACleanup();
}
/*
void Gost::serijalizuj(std::ofstream& out) const {
    // Serijalizuj niz podataka o gostu u binarni fajl
    int imeLen = (int)ime.size();
    out.write((char*)&imeLen, sizeof(imeLen));
    out.write(ime.c_str(), imeLen);

    out.write((char*)&brojApartmana, sizeof(brojApartmana));
    out.write((char*)&brojGostiju, sizeof(brojGostiju));
    out.write((char*)&brojNoci, sizeof(brojNoci));


}

void Gost::deserijalizuj(std::ifstream& in) {
    // Očitaj podatke iz binarnog fajla u polja klase

    int imeLen = 0;
    in.read((char*)&imeLen, sizeof(imeLen));

    char* buffer = new char[imeLen + 1];
    in.read(buffer, imeLen);
    buffer[imeLen] = '\0';

    ime = std::string(buffer);
    delete[] buffer;

    in.read((char*)&brojApartmana, sizeof(brojApartmana));
    in.read((char*)&brojGostiju, sizeof(brojGostiju));
    in.read((char*)&brojNoci, sizeof(brojNoci));

}
*/
/*
int Gost::izracunajRacun() {
    int cenaPoNoci = 3000;
    int ukupno = brojNoci * cenaPoNoci;
    ukupno += minibarZaduzenje;
    if (alarmAktiviran)
        ukupno += 1000;
    return ukupno;
}

bool Gost::jeZavrsioBoravak() const {
    return brojNoci <= 0;
}

void Gost::smanjiBrojNoci() {
    if (brojNoci > 0)
        brojNoci--;
}

bool Gost::jeZavrsioBoravak() const {
    return brojNoci <= 0;
}

bool Gost::jeIstekaoBrojNoci() const {
    return brojNoci == 0;
}
*/
