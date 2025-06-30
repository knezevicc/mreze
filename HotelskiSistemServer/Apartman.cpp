#include "Apartman.h"

Apartman::Apartman(int broj, int sprat, int klasa, int maxGostiju)
    : brojApartmana(broj),
    sprat(sprat),
    klasaApartmana(klasa),
    maksimalanBrojGostiju(maxGostiju),
    trenutniBrojGostiju(0),
    stanjeMinibara(0),
    stanje(StanjeApartmana::Prazan),
    stanjeAlarma(StanjeAlarma::Normalno)
{}
/*
void Apartman::dodajGoste(int broj) {
    trenutniBrojGostiju += broj;
    if (trenutniBrojGostiju > 0)
        stanje = StanjeApartmana::Zauzet;
}

void Apartman::izbaciGoste() {
    trenutniBrojGostiju = 0;
    stanje = StanjeApartmana::PotrebnoCiscenje;
}

void Apartman::dodajMinibar(int iznos) {
    stanjeMinibara += iznos;
}

void Apartman::postaviAlarm(StanjeAlarma novoStanje) {
    stanjeAlarma = novoStanje;
}

void Apartman::ocistiApartman() {
    stanje = StanjeApartmana::Prazan;
    stanjeMinibara = 0;
    stanjeAlarma = StanjeAlarma::Normalno;
}
*/