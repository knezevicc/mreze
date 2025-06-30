#pragma once
#include <string>
#include <winsock2.h> 
using namespace std;

class Gost {
public:
    string ime;
    int brojApartmana;
    int brojGostiju;
    int brojNoci;
    sockaddr_in adresaGosta; // IP adresa i port
    int minibarZaduzenje;
    bool alarmAktiviran;

    Gost(string ime, int apartman, int gosti, int noci, sockaddr_in adr);
    void posaljiRezervaciju(string serverIp, int serverPort);

    /*
    // Metoda za smanjenje broja noći (npr. posle svakog dana)
    void smanjiBrojNoci();

    // Provera da li je vreme za odjavu
    bool jeZavrsioBoravak() const;

    bool jeIstekaoBrojNoci() const;
    int izracunajRacun();
    */
};
