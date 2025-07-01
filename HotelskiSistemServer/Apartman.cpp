#include "Apartman.h"

/*
Apartman::Apartman()
    : brojApartmana(0), sprat(0), klasaApartmana(1), maxBrojGostiju(0),
    trenutniBrojGostiju(0), stanjeApartmana(StanjeApartmana::Prazan),
    stanjeAlarma(StanjeAlarma::Normalno) {}
    */

Apartman::Apartman(int broj, int sprat, int klasa, int maxGostiju)
    : brojApartmana(broj),
    sprat(sprat),
    klasaApartmana(klasa),
    maxBrojGostiju(maxGostiju),
    trenutniBrojGostiju(0),
    //stanjeMinibara(0),
    stanjeApartmana(StanjeApartmana::Prazan),
    stanjeAlarma(StanjeAlarma::Normalno)
{}

void Apartman::dodajGosta(const Gost& gost) {
    if (trenutniBrojGostiju < maxBrojGostiju) {
        listaGostiju.push_back(gost);
        trenutniBrojGostiju++;
        stanjeApartmana = StanjeApartmana::Zauzet;
    }
}

void Apartman::ukloniSveGoste() {
    listaGostiju.clear();
    trenutniBrojGostiju = 0;
    stanjeApartmana = StanjeApartmana::Prazan;
}
/*
void Apartman::sacuvajUBinarnuDatoteku(const std::string& filename) {
    std::ofstream out(filename, std::ios::binary);
    if (!out) return;

    out.write((char*)&brojApartmana, sizeof(brojApartmana));
    out.write((char*)&sprat, sizeof(sprat));
    out.write((char*)&klasaApartmana, sizeof(klasaApartmana));
    out.write((char*)&maxBrojGostiju, sizeof(maxBrojGostiju));
    out.write((char*)&trenutniBrojGostiju, sizeof(trenutniBrojGostiju));

    int stanjeA = static_cast<int>(stanjeApartmana);
    out.write((char*)&stanjeA, sizeof(stanjeA));

    int stanjeAl = static_cast<int>(stanjeAlarma);
    out.write((char*)&stanjeAl, sizeof(stanjeAl));

    int brojGostiju = (int)listaGostiju.size();
    out.write((char*)&brojGostiju, sizeof(brojGostiju));

    for (const auto& gost : listaGostiju) {
        gost.serijalizuj(out);
    }

    out.close();
}

void Apartman::ucitajIzBinarneDatoteke(const std::string& filename) {
    std::ifstream in(filename, std::ios::binary);
    if (!in) return;

    in.read((char*)&brojApartmana, sizeof(brojApartmana));
    in.read((char*)&sprat, sizeof(sprat));
    in.read((char*)&klasaApartmana, sizeof(klasaApartmana));
    in.read((char*)&maxBrojGostiju, sizeof(maxBrojGostiju));
    in.read((char*)&trenutniBrojGostiju, sizeof(trenutniBrojGostiju));

    int stanjeA;
    in.read((char*)&stanjeA, sizeof(stanjeA));
    stanjeApartmana = static_cast<StanjeApartmana>(stanjeA);

    int stanjeAl;
    in.read((char*)&stanjeAl, sizeof(stanjeAl));
    stanjeAlarma = static_cast<StanjeAlarma>(stanjeAl);

    int brojGostiju = 0;
    in.read((char*)&brojGostiju, sizeof(brojGostiju));

    listaGostiju.clear();
    for (int i = 0; i < brojGostiju; i++) {
        Gost g;
        g.deserijalizuj(in);  
        listaGostiju.push_back(g);
    }

    in.close();
}*/
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