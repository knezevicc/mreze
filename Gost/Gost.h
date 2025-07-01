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



    Gost(): ime(""), brojApartmana(0), brojGostiju(0), brojNoci(0), minibarZaduzenje(0), alarmAktiviran(false)
    {
        memset(&adresaGosta, 0, sizeof(adresaGosta)); // inicijalizacija adrese na 0
    }
    Gost(string ime, int apartman, int gosti, int noci, sockaddr_in adr);
    void posaljiRezervaciju(string serverIp, int serverPort);

    /*
    void serijalizuj(std::ofstream& out) const;
    void deserijalizuj(std::ifstream& in);
    */

    /*
    // Metoda za smanjenje broja noći (npr. posle svakog dana)
    void smanjiBrojNoci();

    // Provera da li je vreme za odjavu
    bool jeZavrsioBoravak() const;

    bool jeIstekaoBrojNoci() const;
    int izracunajRacun();
    */
};
