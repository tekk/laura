# LAURA
## Lifelike Automated User-friendly Radio Assistant
#### Automatizovaný digitálny asistent, dolovač dát z internetu, oznamovač a pripomienkovač s veľmi naturálnym syntetickým Slovenským hlasom.

## Funkcie
- Dvojúrovňová HW vrstva - MCU a PC
- MCU - mikrokontrolérová jednotka
	- ATmega32U4
	- USB
	- 18 MHz
	- 32 kB Flash
	- Detekcia nosnej vlny / aktívneho príjmu
	- Pomocou komparátora LM393
	- Rátanie času od posledného príjmu signálu
	- Komunikácia s PC cez USB
	- USB sériový port, 115200 baud, Parity none
	- Ovládanie vysielania z PC
	- Ovládanie vysielania z MCU HW jednotky (tlačítkom) - manuálne PTT
	- Signalizácia udalostí
	- Jednotka zapnutá
	- Príjem signálu
	- Signalizácia vysielania
	- PC Link
	- DNTT (do not transmit timer)
	- Pripojenie ku koncovému transcieveru cez K-type konektor
	- Audio výstup na manuálne monitorovanie prijímaného signálu
	- Prepojenie so zvukovou kartou
	- Line Out / Line In
	- (optional) Mic in
	
- PC
	- Vysokoúrovňové spracovanie vstupného audio signálu
	- Rozpoznávanie DTMF príkazov
	- Rozpoznávanie reči (HMM) !TODO!
	- Obslužný Software pre Windows
	- TTS Slovenský hlas "Laura" spol. Nuance tech.
- Identifikácia
	- Značka
	- Lokátor
	- Čas
	- Každú celú hodinu automaticky
	- Aktuálne počasie a predpoveď počasia
	- Podmienky šírenia rádiových vĺn
	- Ionosféra otvorená / uzavretá
	- Sporadická Es vrstva
- Automatická notifikácia pri otvorení
- Dolovanie dát
- DX Cluster (TODO) *
- OM Callbook (TODO) *
- Prevádzka detekovaná na inej frekvencii
	- Sledovanie blízkych prevádzačov
	- Plne konfigurovateľné na diaľku *2
- Možnosť dekódovať digitálne protokoly (multimon-ng)
	- POCSAG512 POCSAG1200 POCSAG2400o FLEX
	- EAS
	- UFSK1200 CLIPFSK AFSK1200 AFSK2400 AFSK2400_2 AFSK2400_3
	- HAPN4800
	- FSK9600 
	- DTMF
	- ZVEI1 ZVEI2 ZVEI3 DZVEI PZVEI
	- EEA EIA CCIR
	- MORSE CW
- Spracovanie vstupného audia (sox)
- Compander hlasu
- Odstránenie šumu z nekvalitného signálu
- Zábavné funkcie (TODO chrániť heslom)

#### Poznámky k funkciám
- *1 - nutné rozpoznávanie hlasu
- Obmedzené na EN vo väčšine prípadov
	- HMM rozpoznávanie
	- Prebieha na PC - nutný silnejší HW
	- Nezávislé na hlasu hovoriaceho
	- Natrénované príkazy ako ÁNO/NIE/UKONČI atď.
	- Nezávislé na hovoriacom
- *2 - nutné dorobiť interface
	- Teamviewer
	- Arduino reprog HW
	- Nutný reštart transcieveru (niekedy)
	- Riešiť cez pridaný digit. pin na reset komparátor