#include "Server.h"
#include <iostream>
#include <cstring>  // za strcpy, itd
#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")  // Linkovanje Winsock biblioteke

Server::Server(int udpPort, int tcpPort) : udpPort(udpPort), tcpPort(tcpPort) {
    // Ovde možeš inicijalizovati apartmane ako želiš
}

void Server::pokreniUDPServer() {
    //cout << "UDP server startovan na portu " << udpPort << std::endl;

    WSADATA wsaData;
    SOCKET udpSocket = INVALID_SOCKET;
    sockaddr_in serverAddr;

    // Inicijalizacija Winsock-a
    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
    {
        cout << "WSAStartup failed.\n";
        return;
    }

    // Kreiranje UDP socket-a
    udpSocket = socket(AF_INET, SOCK_DGRAM, IPPROTO_UDP);
    if (udpSocket == INVALID_SOCKET)
    {
        cout << "Failed to create UDP socket.\n";
        WSACleanup();
        return;
    }

    // Podešavanje adrese servera
    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = INADDR_ANY; // bilo koja lokalna adresa
    serverAddr.sin_port = htons(12345);      // port 12345

    // Bind socket-a na adresu
    if (bind(udpSocket, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
    {
        cout << "Bind failed.\n";
        closesocket(udpSocket);
        WSACleanup();
        return;
    }

    cout << "UDP server startovan na portu 12345\n";

    // Za sada samo sleep da ne zatvori odmah (kasnije ide recvfrom)
    //Sleep(5000);
    char buffer[512];
    sockaddr_in clientAddr;
    int clientAddrSize = sizeof(clientAddr);

    int bytesReceived = recvfrom(udpSocket, buffer, sizeof(buffer) - 1, 0,
        (SOCKADDR*)&clientAddr, &clientAddrSize);

    if (bytesReceived > 0) {
        buffer[bytesReceived] = '\0'; // dodaj null terminator
        cout << "Primljena UDP poruka: " << buffer << endl;


        //za vracanje potvrde
        const char* odgovor = "POTVRDA=OK";

        int sendResult = sendto(udpSocket, odgovor, (int)strlen(odgovor), 0,
            (SOCKADDR*)&clientAddr, clientAddrSize);

        if (sendResult == SOCKET_ERROR) {
            std::cout << "Slanje potvrde nije uspelo.\n";
        }
        else {
            std::cout << "Potvrda poslata gostu.\n";
        }
    }

    // Čišćenje
    closesocket(udpSocket);
    WSACleanup();
}

void Server::pokreniTCPServer() {
    //cout << "TCP server startovan na portu " << tcpPort << std::endl;
    //cout << "Pokušavam da pokrenem TCP server na portu: " << tcpPort << endl;

    WSADATA wsaData;
    SOCKET listenSocket = INVALID_SOCKET, clientSocket = INVALID_SOCKET;
    sockaddr_in serverAddr;

    if (WSAStartup(MAKEWORD(2, 2), &wsaData) != 0)
    {
        cout << "WSAStartup failed.\n";
        return;
    }

    listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
    if (listenSocket == INVALID_SOCKET)
    {
        cout << "Failed to create TCP socket.\n";
        WSACleanup();
        return;
    }

    serverAddr.sin_family = AF_INET;
    serverAddr.sin_addr.s_addr = INADDR_ANY;
    serverAddr.sin_port = htons(12346);

    if (bind(listenSocket, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR)
    {
        cout << "Bind failed.\n";
        closesocket(listenSocket);
        WSACleanup();
        return;
    }

    if (listen(listenSocket, SOMAXCONN) == SOCKET_ERROR)
    {
        cout << "Listen failed.\n";
        closesocket(listenSocket);
        WSACleanup();
        return;
    }

    cout << "TCP server startovan na portu 12346\n";

    // Za sada prihvatamo samo jednu konekciju i odmah zatvaramo
    clientSocket = accept(listenSocket, NULL, NULL);
    if (clientSocket == INVALID_SOCKET)
    {
        cout << "Accept failed.\n";
        closesocket(listenSocket);
        WSACleanup();
        return;
    }

    cout << "Primljena TCP konekcija.\n";

    // Šaljemo poruku ka osoblju
    const char* poruka = "Zadatak: Ocistiti apartman 101";
    int poslato = send(clientSocket, poruka, (int)strlen(poruka), 0);
    if (poslato == SOCKET_ERROR) {
        cout << "Slanje poruke nije uspelo.\n";
    }
    else {
        cout << "Poruka poslata ka osoblju.\n";


        //potvrda od osoblja
        char buffer[512];
        int primljeno = recv(clientSocket, buffer, sizeof(buffer) - 1, 0);

        if (primljeno > 0) {
            buffer[primljeno] = '\0';
            std::cout << "Potvrda od osoblja: " << buffer << "\n";
        }
        else {
            std::cout << "Nije primljena potvrda od osoblja.\n";
        }
    }

    closesocket(clientSocket);
    closesocket(listenSocket);
    WSACleanup();
}

//MORA OVA METODE JER AKO SE POZOVU 2 U MAINU, PREPLITACE SE I TCP SE NIKAD NECE POZVATI
void Server::pokreni() {
    pokreniUDPServer();
    pokreniTCPServer();
}






/*
void Server::pokreniUDPServer() {
    int sockfd = socket(AF_INET, SOCK_DGRAM, 0);
    if (sockfd < 0) {
        std::cerr << "Greska pri kreiranju UDP socket-a\n";
        return;
    }

    sockaddr_in servaddr{};
    servaddr.sin_family = AF_INET;
    servaddr.sin_addr.s_addr = INADDR_ANY;
    servaddr.sin_port = htons(udpPort);

    if (bind(sockfd, (struct sockaddr*)&servaddr, sizeof(servaddr)) < 0) {
        std::cerr << "Greska pri bind-u UDP socket-a\n";
        closesocket(sockfd);
        return;
    }

    char buffer[1024];
    sockaddr_in cliaddr{};
    socklen_t len = sizeof(cliaddr);

    std::cout << "UDP server pokrenut na portu " << udpPort << std::endl;

    while (true) {
        int n = recvfrom(sockfd, buffer, sizeof(buffer) - 1, 0, (struct sockaddr*)&cliaddr, &len);
        if (n > 0) {
            buffer[n] = '\0';
            std::string data(buffer);

            char ipStr[INET_ADDRSTRLEN];
            inet_ntop(AF_INET, &(cliaddr.sin_addr), ipStr, INET_ADDRSTRLEN);
            std::string gostKey = std::string(ipStr) + ":" + std::to_string(ntohs(cliaddr.sin_port));

            std::cout << "Primljena rezervacija od " << gostKey << ": " << data << std::endl;

            primiRezervaciju(data, cliaddr);
        }
    }
    closesocket(sockfd);
}

void Server::primiRezervaciju(const std::string& data, sockaddr_in gostAdresa) {
    // Ovde parsiraj podatke i napravi novog gosta
    // Primer: data format: "apartman;brojGostiju;brojNoci"
    int apartman, brojGostiju, brojNoci;
    sscanf(data.c_str(), "%d;%d;%d", &apartman, &brojGostiju, &brojNoci);

    char ipStr[INET_ADDRSTRLEN];
    inet_ntop(AF_INET, &(gostAdresa.sin_addr), ipStr, INET_ADDRSTRLEN);
    std::string gostKey = std::string(ipStr) + ":" + std::to_string(ntohs(gostAdresa.sin_port));

    Gost noviGost("Gost", apartman, brojGostiju, brojNoci, gostAdresa);
    gosti[gostKey] = noviGost;

    std::cout << "Rezervacija zabeležena za gosta: " << gostKey << std::endl;

    // Update stanja apartmana itd.
}

void Server::pokreniTCPServer() {
    int sockfd = socket(AF_INET, SOCK_STREAM, 0);
    if (sockfd < 0) {
        std::cerr << "Greska pri kreiranju TCP socket-a\n";
        return;
    }

    sockaddr_in servaddr{};
    servaddr.sin_family = AF_INET;
    servaddr.sin_addr.s_addr = INADDR_ANY;
    servaddr.sin_port = htons(tcpPort);

    if (bind(sockfd, (struct sockaddr*)&servaddr, sizeof(servaddr)) < 0) {
        std::cerr << "Greska pri bind-u TCP socket-a\n";
        close(sockfd);
        return;
    }

    if (listen(sockfd, 5) < 0) {
        std::cerr << "Greska pri listen-u\n";
        close(sockfd);
        return;
    }

    std::cout << "TCP server pokrenut na portu " << tcpPort << std::endl;

    while (true) {
        sockaddr_in cliaddr{};
        socklen_t len = sizeof(cliaddr);
        int newsockfd = accept(sockfd, (struct sockaddr*)&cliaddr, &len);
        if (newsockfd < 0) {
            std::cerr << "Greska pri prihvatanju TCP konekcije\n";
            continue;
        }

        char buffer[1024];
        int n = read(newsockfd, buffer, sizeof(buffer) - 1);
        if (n > 0) {
            buffer[n] = '\0';
            std::string zadatak(buffer);
            std::cout << "Primljen zadatak za osoblje: " << zadatak << std::endl;

            // Ovde možeš poslati potvrdu nazad
            std::string odgovor = "Zadatak primljen";
            write(newsockfd, odgovor.c_str(), odgovor.size());
        }

        closesocket(newsockfd);
    }

    closesocket(sockfd);
}

void Server::posaljiZadatakOsoblju(const std::string& zadatak) {
    // Implementacija slanja TCP poruke osoblju (klijentima)
    // Ovo je samo skelet, moraš znati IP i port osoblja da bi slao
    // Za sada može biti prazno ili demo verzija
}

void Server::zavrsiRezervaciju(const std::string& gostKey) {
    if (gosti.find(gostKey) != gosti.end()) {
        Gost& gost = gosti[gostKey];
        int racun = gost.izracunajRacun();
        std::cout << "Gost " << gostKey << " treba da plati: " << racun << std::endl;
        // Očisti apartman, obavesti osoblje itd.
    }
}
*/
