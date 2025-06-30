#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <windows.h>
#include "Gost.h"
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
    g.posaljiRezervaciju("127.0.0.1", 12345); // šalje serveru

    return 0;
}
