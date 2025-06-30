#pragma once
#ifndef SERVER_H
#define SERVER_H
#include <vector>
#include <map>
#include "Apartman.h"
#include <string>
#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")  // automatski linkuje Winsock biblioteku
using namespace std;

class Server {
private:
    int udpPort;
    int tcpPort;

    //vector<Apartman> apartmani;
    //map<string, Gost> gosti; // ključ: IP + port u stringu

public:
    Server(int udpPort, int tcpPort);

    void pokreniUDPServer();
    void pokreniTCPServer();

    void pokreni();

    /*
    void primiRezervaciju(const string& data, sockaddr_in gostAdresa);
    void posaljiZadatakOsoblju(const string& zadatak);

    void zavrsiRezervaciju(const string& gostKey);
    */
};
#endif
