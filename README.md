# CTU60GLib

Stručný popis pro odevzdání semestrálního projektu do předmětu BPC-OOP

pro komunikaci s portálem 60ghz.ctu.cz je určena net core knihovna CTU60Lib, 
která obsahuje nástroje pro komunikaci s portálem, 
žurnálování stavu registrace(zda anténa byla publikována, čeká se na její publikování nebo je jen ve fázi návrhu)
a sadě vyjímek pro signalizaci problému. 

## Přihlášení k portálu
asynchronní funkce **LoginAsync** s parametry login a pass umožňuje přihlášení k portálu, CTUCient poté obdrží cookie s platností [session] v případě že přihlašovací data jsou neplatné je vrácena vyjímka InvalidMailOrPasswordException při jiných potížích WebServerException v případě,že by uživatel zavolal např registraci antén ještě před přihlášením bude také potrestán vyhozenou vyjímkou.

## Registrace spoje  FS Point to Point
asynchronní funkce **AddPTPConnectionAsync** s parametry FixedP2PPair umožňuje registraci p2p spojení. Funkce je rozdělena na 3 sekce stejně jako tomu je i na portálu

**PTPLocalisation** - funkce zajišťuje vytvoření záznamu, záznam obsahuje jména a  gps souřadnice obou antén. při úspěšném vytvoření záznam obdrží registrační id a je veden jako ***návrh***, při problému je vyhozena vyjímka.

**PTPTechnicalSpec** - funkce doplňuje záznam o technické informace jako: zisk antény, šířka kanálu, vyzařovaný výkon, modulační schéma, frekvenci mac/sn. Při úspěšném vytvoření je záznam převeden z ***návrh*** na stav ***čeká***, při problému je vyhozena vyjímka.

**PTPCollisionAndPublishing** - funkce zjištuje zda by mohlo dojít k rušení s nějakou z již publikovaných stanic, v případě že k rušení nedojde je spojení publikováno a záznam je ve stavu **publikováno**. V případě, že k rušení dojde zůstává záznam ve stavu **čeká**, v případě problému je vyhozena vyjímka.



## Registrace spoje Wigig PtP/ Point to Multipoint
asynchronní funkce **AddWIGIG_PTP_PTMPConnectionAsync** s parametry WigigPTMPUnitInfo umožňuje registraci p2p s vysokým výkonem, nebo ptmp spojení.
Funkce je rozdělena na 3 sekce stejně jako tomu je i na portálu

**WigigLocalisation** - funkce zajišťuje vytvoření záznamu, záznam obsahuje jméno, gps souřadnice, a azimut vysílací stanice. při úspěšném vytvoření záznam obdrží registrační id a je veden jako ***návrh***, při problému je vyhozena vyjímka.

**WigigTechnicalSpec**  - funkce doplňuje záznam o technické informace jako: EIRP, zisk antény, vysílaný výkon, šířka kanálu a mac/sn. Při úspěšném vytvoření je záznam převeden z ***návrh*** na stav ***čeká***, při problému je vyhozena vyjímka.

**WigigCollisionAndPublishing** - funkce zjištuje zda by mohlo dojít k rušení s nějakou z již publikovaných stanic, v případě že k rušení nedojde je spojení publikováno a záznam je ve stavu ***publikováno***. V případě, že k rušení dojde zůstává záznam ve stavu ***čeká***, v případě problému je vyhozena vyjímka.

po (ne)úspěšné registraci spojení je vrácen žurnálovací objekt.

## Vyčítání spojů
**GetAllStationsAsync** - funkce vrací výpis všech publikovaných spojů na celém portálu.
**GetMyStationsAsync** - funkce vrací výpis všech spojů vlastněných účtem.

## Mazání spojů
**DeleteConnectionAsync** - funkce s parametrem id umožňuje smazání spoje vlastněného účtem na základě id spoje.

## Žurnálování
třída **RegistrationJournal** - zaznamenává stav záznamu při procesu registrace spojení, v jakém stádiu registrace skončila, jaká byla vyhozena vyjímka, jaké id záznam obdržel, jaké stanice jsou v okolí a jaké stanice by případně byly rušeny.

## Vyjímky

Vyjímky jsou zatím stále ve stadiu vývoje a pravděpodbně budou výrazně přepracovány.

**WebServerException** - používány pokud nastane neočekávaná odpověď portálu
**MissingParameterException** - používané při instanciování abstrakcí stanic při prázdných nebo null hodnotách
**InvalidPropertyValueException** - používané v případě, že je hodnota špatného formátu nebo hodnota porušuje podmínky např hodnota je mimo dovolenou frekvenci. Vyjímka informuje jaké hodnoty očekává.
**InvalidMailOrPasswordException** - pužívané v případě, že portál neumožní přihlášení.
**CollisionDetectedException** - používané v případě, kdy je zjištěné možné rušení.


---

## License

[![License](http://img.shields.io/:license-mit-blue.svg?style=flat-square)](http://badges.mit-license.org)

- **[MIT license](https://choosealicense.com/licenses/mit/)**
- Copyright 2020 © <a href="https://github.com/GabrielMastny" target="_blank">Gabriel Mastný</a>.
