
# Welkom bij Smart Energy

Voordat je begint met het uitbreiden van de applicatie, is het belangrijk dat je deze readme goed doorneemt en alle noodzakelijke stappen uitvoert.

## Configuratie van InfluxDB

Deze applicatie maakt gebruik van InfluxDB, een database die speciaal geschikt is voor het verwerken van grote hoeveelheden gegevens. Om verbinding te maken met deze database heb je een wachtwoord nodig. **We slaan nooit gevoelige gegevens (secrets) op in de broncode** en daarom moet je het wachtwoord eerst op je eigen machine instellen. We gebruiken hiervoor `dotnet user-secrets`. 

### Stappen voor Configuratie

#### 1. Open een Command Line Interface (CLI)

Dit kan CMD, Bash, of PowerShell zijn. De voorbeelden hieronder zijn gebaseerd op CMD, maar ze werken ook in PowerShell.

#### 2. Navigeer naar de map met de SmartEnergy.Client-bestanden

Pas de volgende opdracht aan naar de locatie van jouw projectbestanden voordat je deze uitvoert.
```sh
cd C:\jouw_project_map\SmartEnergy\SmartEnergy.Client
```

#### 3. Initialiseer een dotnet user-secrets-bestand

Voer de volgende opdracht uit om een 'geheim' bestand te genereren. Dit bestand wordt opgeslagen in je 'user directory' en bevat de noodzakelijke gevoelige gegevens voor de applicatie:
```sh
dotnet user-secrets init
```

#### 4. Configureer gevoelige gegevens (secrets)

Gebruik de volgende opdrachten om de secrets in te stellen. De juiste waarden voor de secrets kun je vinden op Brightspace.

```sh
dotnet user-secrets set "InfluxDb:Url" "your_influxdb_url"
dotnet user-secrets set "InfluxDb:Token" "your_influxdb_token"
dotnet user-secrets set "InfluxDb:Org" "your_influxdb_organization"
```

**Tip: kopieer bovenstaande regels eerst in een tekst-editor en voeg de juiste waardes in alvorens je ze plakt in de CLI.**

Na het succesvol uitvoeren van deze stappen zijn je secrets opgeslagen op jouw machine en kan de applicatie hier gebruik van maken.


## Applicatiestructuur

De appplicatie is opgeslitst in twee delen: het gedeelte dat verantwoordelijk is voor de webapplicatie (SmartEnergy.Client) en het gedeelte dat zorgt voor de verbinding met de database van het lectoraat (SmartEnergy.Library).

### SmartEnergy.Client

Dit is de webapplicatie zelf en het enige project binnen deze oplossing dat je kunt starten. Het betreft een .NET Blazor webapplicatie die opgebouwd is doormiddel van van C#, HTML en CSS. **In dit project voer jij de aanpassingen door om je persoonlijke opdracht te voltooien**.

Deze webapplicatie maakt gebruik Bootstrap: een open-source front-end framework dat wordt gebruikt voor het ontwikkelen van responsieve en mobiele-vriendelijke websites en webapplicaties. Het biedt een verzameling van HTML-, CSS- en JavaScript-componenten waarmee ontwikkelaars snel en eenvoudig aantrekkelijke, functionele interfaces kunnen bouwen zonder veel handmatig te hoeven programmeren. Kijk voor voorbeelden en documentatie op https://getbootstrap.com/docs/5.1/getting-started/introduction/. 

### SmartEnergy.Library

Een library bestaat uit code, maar kan niet zelfstandig worden uitgevoerd. In plaats daarvan wordt de code in een library gebruikt door andere applicaties binnen een project.

In dit geval bevat de library bijvoorbeeld code die we hebben geschreven om verbinding te maken met de database van het lectoraat, waarin alle gegevens van de Smart Meters zijn opgeslagen. Je hoeft deze code niet aan te passen om je opdracht succesvol te voltooien, maar je kunt de library wel gebruiken om te debuggen.

## Werken met Visual Studio Code

Wanneer je aan deze applicatie gaat werken, kun je Visual Studio Code gebruiken. Er zijn (helaas) twee verschillende profielen om de applicatie te starten:

### Debug

- **Voordeel:** Een browser wordt geopend met de applicatie, en breakpoints worden geraakt, wat het debuggen van back-end code eenvoudiger maakt.
- **Nadeel:** Na een codewijziging moet je de debug-sessie opnieuw starten.

### Hot Reload

- **Voordeel:** Een browser wordt geopend met de applicatie, en zodra je code aanpast, wordt de browser automatisch ververst.
- **Nadeel:** Breakpoints worden niet geraakt.

## Werken met Visual Studio of JetBrains Rider

Bij het ontwikkelen van onderwijsmateriaal deze periode hebben we ons beperkt tot Visual Studio Code. Momenteel biedt Visual Studio echter de beste ondersteuning voor het ontwikkelen van Blazor-applicaties. Je kunt deze applicatie verder ontwikkelen in de gratis Visual Studio Community-editie. Ook JetBrains Rider is een populair alternatief. Wanneer je één van deze IDE's (Integrated Development Environments) gebruikt kan het zijn dat een docent je niet goed kan ondersteunen wanneer je vast loopt. Dit hoeft natuurlijk geen onoverkomelijk probleem te zijn, er zijn voldoende online bronnen die je verder kunnen helpen. 