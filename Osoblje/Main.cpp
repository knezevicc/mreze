#include "Osoblje.h"

int main() {
    Osoblje o("Marko",1);
    o.poveziSeSaServerom("127.0.0.1", 12346);  // IP i port servera TCP
    o.primiZadatke();

    return 0;
}
