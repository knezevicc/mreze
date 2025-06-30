#pragma once
#include <string>

enum class StanjeApartmana { Prazan, Zauzet, PotrebnoCiscenje };
enum class StanjeAlarma { Normalno, Aktivirano };

class Apartman
{
public:
	int brojApartmana;
	int sprat;
    int klasaApartmana;          
    int maksimalanBrojGostiju;   
    int trenutniBrojGostiju;     
    int stanjeMinibara;          
    StanjeApartmana stanje;      
    StanjeAlarma stanjeAlarma;

    Apartman(int broj, int sprat, int klasa, int maxGostiju);

    //met za menjanje stanja, azuriranje minibara..
    /*
    void dodajGoste(int broj);
    void izbaciGoste();
    void dodajMinibar(int iznos);
    void postaviAlarm(StanjeAlarma novoStanje);
    void ocistiApartman();
    */
};

