#pragma once
#include <string>
#include <iostream>
#include <winsock2.h>
#pragma comment(lib, "ws2_32.lib")
using namespace std;

class Osoblje
{
private:
    SOCKET sock = INVALID_SOCKET;
public:
    string ime;
    int id;

    Osoblje(string ime, int id);

    void poveziSeSaServerom(const char* ip, int port);
    void primiZadatke();

    /*
    void cistiApartman(int brojApartmana);
    void sanirajAlarm(int brojApartmana);
    void azurirajMinibar(int brojApartmana, int stanjeMinibara);
    */
};
