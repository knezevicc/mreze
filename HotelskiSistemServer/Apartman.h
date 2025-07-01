#pragma once
#include <vector>
#include <string>
#include <fstream>
#include "Gost.h"

enum class StanjeApartmana { Prazan, Zauzet, PotrebnoCiscenje };
enum class StanjeAlarma { Normalno, Aktivirano };

class Apartman
{
private:
	int brojApartmana;
	int sprat;
    int klasaApartmana;          
    int maxBrojGostiju;   
    int trenutniBrojGostiju;     
    //int stanjeMinibara;          
    StanjeApartmana stanjeApartmana;      
    StanjeAlarma stanjeAlarma;
    vector<Gost> listaGostiju;

public:
    Apartman();
    Apartman(int broj, int sprat, int klasa, int maxGostiju);

    void dodajGosta(const Gost& gost);
    void ukloniSveGoste();

    void sacuvajUBinarnuDatoteku(const std::string& filename);
    void ucitajIzBinarneDatoteke(const std::string& filename);

    //met za menjanje stanja, azuriranje minibara..
    /*
    void dodajGosta(const Gost& gost);

    void izbaciGoste();

    void dodajMinibar(int iznos);
    
    void postaviAlarm(StanjeAlarma novoStanje);
    
    void ocistiApartman();

    */
};

