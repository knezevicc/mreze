#include <winsock2.h>
#include <ws2tcpip.h>
#include <iostream>
#include <windows.h>
#include "Server.h"
#pragma comment(lib, "Ws2_32.lib")
using namespace std;
#include<thread>

int main()
{
    Server server(12345,12346);
    //server.pokreni();
    thread udpThread(&Server::pokreniUDPServer, &server);
    std::thread tcpThread(&Server::pokreniTCPServer, &server);
    udpThread.join();
    tcpThread.join();

    return 0;
}


