# Guida di sviluppo del plugin Jellyfin

Questa è la guida operativa del progetto per realizzare il plugin **YouTube Audio Downloader** come plugin nativo di Jellyfin. È stata ricavata dal repository ufficiale [`jellyfin-plugin-template`](https://github.com/jellyfin/jellyfin-plugin-template), revisionato al commit [`c93225a0a5a76d3843db05b4b5b77fcfc482fba3`](https://github.com/jellyfin/jellyfin-plugin-template/tree/c93225a0a5a76d3843db05b4b5b77fcfc482fba3).

> Il template è il punto di partenza per la struttura e il packaging del plugin; non implementa il download da YouTube. La parte applicativa descritta in questa guida deriva dai requisiti in `SPEC.md` e dal dominio in `CONTEXT.md`.

## 1. Vincoli tecnici del progetto

| Voce | Decisione |
|---|---|
| Server target | Jellyfin `10.11.10` |
| Immagine target | `linuxserver/jellyfin:10.11.10` |
| Architettura target | `amd64` |
| SDK | .NET SDK 9.0 |
| Target framework | `net9.0` |
| Dipendenze Jellyfin | `Jellyfin.Controller` e `Jellyfin.Model`, con runtime escluso |
| Sorgenti supportate | Video e playlist YouTube pubblici |
| Elaborazione | Un elemento alla volta |
| Output Musica | Audio Opus |
| Output Podcast | Audio MP3 |
| Distribuzione | DLL del plugin nella directory `plugins/<nome-plugin>` |

Il template richiede che la versione dei pacchetti Jellyfin corrisponda alla versione del server; altrimenti il plugin può risultare `NotSupported`. Per questo progetto non bisogna copiare ciecamente il valore `10.11.5` presente nell'esempio del template: va usata e verificata la versione compatibile con Jellyfin `10.11.10`.

## 2. Struttura iniziale del repository

La struttura minima da mantenere è:

```text
Jellyfin.Plugin.YouTubeAudio/
├── Configuration/
│   ├── PluginConfiguration.cs
│   └── configPage.html
├── Controllers/
├── Download/
├── Models/
├── Services/
├── Tests/
├── Jellyfin.Plugin.YouTubeAudio.csproj
├── Jellyfin.Plugin.YouTubeAudio.slnx
├── Plugin.cs
├── build.yaml
└── LICENSE
```

I nomi delle cartelle applicative sono una convenzione del progetto. La struttura obbligatoria del template è invece:

- una classe di configurazione derivata da `BasePluginConfiguration`;
- una classe principale derivata da `BasePlugin<PluginConfiguration>`;
- una pagina web incorporata se il plugin espone una configurazione o un'interfaccia nel dashboard;
- un `build.yaml` con i metadati del plugin.

## 3. Creazione del progetto

### Opzione raccomandata: partire dal template

Clonare o scaricare il template ufficiale e rinominare:

- assembly e namespace `Jellyfin.Plugin.Template` in `Jellyfin.Plugin.YouTubeAudio`;
- nome visualizzato in `YouTube Audio Downloader`;
- file e riferimenti che contengono il nome `Template`;
- identificativo GUID del plugin, generandone uno nuovo.

### Opzione da zero

```bash
dotnet new classlib -f net9.0 -n Jellyfin.Plugin.YouTubeAudio
cd Jellyfin.Plugin.YouTubeAudio
dotnet add package Jellyfin.Model --version <versione-compatibile>
dotnet add package Jellyfin.Controller --version <versione-compatibile>
```

Nel file `.csproj` le dipendenze Jellyfin devono escludere gli asset runtime:

```xml
<ItemGroup>
  <PackageReference Include="Jellyfin.Controller" Version="<versione-compatibile>">
    <ExcludeAssets>runtime</ExcludeAssets>
  </PackageReference>
  <PackageReference Include="Jellyfin.Model" Version="<versione-compatibile>">
    <ExcludeAssets>runtime</ExcludeAssets>
  </PackageReference>
</ItemGroup>
```

L'esclusione è necessaria perché il runtime Jellyfin viene fornito dal server e il template segnala che ometterla può impedire la registrazione corretta del plugin.

## 4. Classe principale del plugin

`Plugin.cs` deve:

1. ereditare da `BasePlugin<PluginConfiguration>`;
2. ricevere nel costruttore `IApplicationPaths` e `IXmlSerializer`;
3. esporre un `Name` stabile e leggibile;
4. esporre un `Id` GUID stabile e unico;
5. implementare `IHasWebPages` per pubblicare la pagina interna del plugin;
6. registrare l'istanza solo se serve davvero; la logica applicativa deve preferire la dependency injection.

Schema minimo:

```csharp
public sealed class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
    }

    public override string Name => "YouTube Audio Downloader";
    public override Guid Id => Guid.Parse("<guid-stabile>");

    public IEnumerable<PluginPageInfo> GetPages() =>
    [
        new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath =
                "Jellyfin.Plugin.YouTubeAudio.Configuration.configPage.html"
        }
    ];
}
```

Il GUID non deve cambiare tra build o release: Jellyfin lo usa per identificare il plugin e la sua configurazione.

## 5. Configurazione persistente

`PluginConfiguration.cs` deve derivare da `BasePluginConfiguration` e contenere almeno:

```csharp
public sealed class PluginConfiguration : BasePluginConfiguration
{
    public string MusicDestination { get; set; } = string.Empty;
    public string PodcastDestination { get; set; } = string.Empty;
}
```

Regole:

- le due destinazioni sono path assoluti già esistenti;
- Musica usa esclusivamente `MusicDestination`;
- Podcast usa esclusivamente `PodcastDestination`;
- il salvataggio deve essere effettuato tramite l'API della configurazione del plugin;
- non memorizzare password Google, cookie YouTube o credenziali nell'MVP;
- validare path vuoti, non raggiungibili o non scrivibili prima di avviare una richiesta.

La pagina HTML deve essere una risorsa embedded nel `.csproj`:

```xml
<ItemGroup>
  <None Remove="Configuration\configPage.html" />
  <EmbeddedResource Include="Configuration\configPage.html" />
</ItemGroup>
```

La pagina può usare `ApiClient.getPluginConfiguration`, valorizzare i campi e salvare con `ApiClient.updatePluginConfiguration`, come nell'esempio ufficiale del template. Per il nostro plugin dovrà esporre almeno i due path e una pagina operativa con URL, categoria e stato delle richieste.

## 6. Architettura applicativa

La pagina Jellyfin non deve eseguire il download nella richiesta HTTP. Il flusso deve essere:

```text
Pagina plugin
  → endpoint del plugin
  → creazione della Richiesta di download persistita
  → worker IHostedService
  → analisi URL
  → coda ordinata di Elementi della coda
  → yt-dlp + FFmpeg
  → scrittura file temporaneo e rename atomico
  → stato Completato/Fallito
  → aggiornamento libreria una sola volta
```

Componenti consigliati:

- `ControllerBase`: endpoint REST per creare richieste e leggere lo stato;
- `IHostedService`: worker di background che processa la coda;
- `IPluginServiceRegistrator`: registrazione del worker, repository e adapter nel container DI;
- `IDownloadEngine`: astrazione su yt-dlp/FFmpeg;
- `IRequestStore`: persistenza della richiesta e degli elementi, necessaria per la ripresa dopo riavvio;
- `IAudioFileWriter`: scelta del nome, sanificazione e versionamento;
- adapter Jellyfin per richiedere la scansione della libreria.

Il template documenta esplicitamente l'uso di `ControllerBase`, `IHostedService`, `IPluginServiceRegistrator` e delle interfacce Jellyfin per estendere il server. La classe principale del plugin **non** deve implementare anche `IHostedService`.

### Contratto della coda

Ogni elemento deve avere almeno:

- identificativo della richiesta;
- URL o identificativo della sorgente;
- titolo YouTube, quando disponibile;
- indice nella playlist;
- categoria (`Musica` o `Podcast`);
- stato (`In attesa`, `In elaborazione`, `Completato`, `Fallito`);
- percentuale `0..100`;
- messaggio di errore sanitizzato, se presente;
- path finale, se completato.

Vincoli:

- una playlist viene elaborata integralmente e nell'ordine originale;
- un solo elemento può essere attivo in un dato momento;
- il fallimento di un elemento non blocca i successivi;
- un URL non valido deve produrre uno stato visibile `Fallito`;
- dopo riavvio, gli elementi `Completato` non devono essere ripetuti;
- gli elementi `In elaborazione` al momento del riavvio devono essere riportati a `In attesa` oppure recuperati in modo sicuro.

## 7. Endpoint e interfaccia

Gli endpoint REST devono essere piccoli e orientati alle azioni osservabili, per esempio:

```text
POST /YouTubeAudio/requests
GET  /YouTubeAudio/requests/{requestId}
GET  /YouTubeAudio/requests
```

La `POST` deve ricevere URL e categoria obbligatoria, validare la configurazione e restituire l'identificativo della richiesta. L'analisi della sorgente deve proseguire nel worker.

La pagina deve:

- richiedere URL e categoria prima dell'avvio;
- distinguere input video e playlist senza introdurre selezione parziale;
- mostrare una scheda per ogni elemento individuato;
- mostrare `0%` per gli elementi in attesa;
- aggiornare percentuale e stato durante l'elaborazione;
- mostrare `Completato` o `Fallito` in modo esplicito;
- non esporre segreti o comandi shell all'utente.

## 8. yt-dlp e FFmpeg

Il motore deve essere dietro un'interfaccia, così i test non dipendono da Internet o dai binari reali. L'adapter reale deve:

- accettare solo sorgenti pubbliche nel perimetro MVP;
- estrarre esclusivamente audio;
- produrre Opus per Musica;
- produrre MP3 per Podcast;
- inoltrare gli aggiornamenti di progresso;
- propagare errori con un messaggio utile ma senza segreti;
- usare argomenti di processo senza concatenare input utente in una shell;
- applicare timeout e cancellazione;
- scrivere in un file temporaneo e completare con rename, evitando file finali parziali.

Il template Jellyfin non distribuisce yt-dlp o FFmpeg. La loro inclusione nel plugin è quindi una decisione specifica di questo progetto e deve essere verificata nel container `linuxserver/jellyfin:10.11.10` amd64. Prima dell'implementazione definitiva occorre validare:

1. licenze e redistribuzione dei binari;
2. percorso di esecuzione nel plugin installato;
3. permessi nel container;
4. compatibilità glibc/architettura;
5. aggiornamento dei binari;
6. comportamento quando un binario manca o non è eseguibile.

## 9. Nomi e filesystem

Il titolo YouTube è la base del nome. Il writer deve:

1. sostituire i caratteri non validi per il filesystem;
2. gestire nomi vuoti, riservati o troppo lunghi;
3. aggiungere l'estensione coerente (`.opus` o `.mp3`);
4. non sovrascrivere mai un file esistente;
5. usare `video.mp3`, `video(1).mp3`, `video(2).mp3`, ecc.;
6. creare il file nella destinazione associata alla categoria.

Il controllo dell'esistenza e la creazione del nome devono essere sicuri rispetto a richieste concorrenti, anche se il worker MVP è sequenziale.

## 10. Aggiornamento della libreria

Quando tutti gli elementi della richiesta sono `Completato` o `Fallito`, il plugin deve richiedere **un solo** aggiornamento della libreria Jellyfin. Non bisogna avviare una scansione dopo ogni file.

La richiesta deve essere idempotente: dopo riavvio o retry interno non deve generare più aggiornamenti per la stessa richiesta conclusa.

## 11. Test obbligatori

Il seam principale è:

```text
URL + categoria → analisi → coda → download audio → file → aggiornamento libreria
```

Usare:

- fake del motore di acquisizione;
- filesystem temporaneo;
- fake del servizio di aggiornamento libreria;
- clock/cancellation controllabili, se necessari.

Test minimi:

1. video singolo come Musica produce Opus nella destinazione musicale;
2. video singolo come Podcast produce MP3 nella destinazione podcast;
3. playlist crea tutti gli elementi nell'ordine originale;
4. al massimo un download è attivo contemporaneamente;
5. il fallimento di un elemento non blocca i successivi;
6. URL non valido produce `Fallito` visibile;
7. titolo con caratteri non validi viene sanificato;
8. file esistente viene versionato senza sovrascrittura;
9. l'aggiornamento libreria avviene una sola volta a fine richiesta;
10. un riavvio non ripete gli elementi già completati;
11. il fake restituisce correttamente percentuali e titoli;
12. la cancellazione e il timeout non lasciano file finali parziali.

A parte, eseguire test di integrazione nel container target per verificare DLL, dipendenze, yt-dlp, FFmpeg, permessi e compatibilità con Jellyfin `10.11.10`.

## 12. Build, installazione locale e debugging

Build locale:

```bash
dotnet restore
dotnet build --configuration Release
dotnet test
dotnet publish --configuration Release
```

Il template copia l'output pubblicato nella directory dati di Jellyfin:

```text
<jellyfin-data>/plugins/YouTubeAudio/
```

Copiare lì la DLL e gli eventuali file necessari, quindi riavviare Jellyfin. Per il debug:

1. compilare in Debug;
2. creare la directory del plugin;
3. copiare DLL e PDB;
4. avviare Jellyfin con la sua working directory;
5. collegare il debugger al processo.

Il template include configurazioni VS Code per concatenare build, creazione della directory e copia degli artefatti. I percorsi devono essere adattati all'ambiente locale.

## 13. Packaging e pubblicazione

`build.yaml` deve descrivere almeno:

```yaml
name: "YouTube Audio Downloader"
guid: "<stesso-guid-di-Plugin.cs>"
version: "1.0.0.0"
targetAbi: "10.11.10.0"
framework: "net9.0"
overview: "Scarica l'audio pubblico di video e playlist YouTube"
category: "General"
owner: "<owner>"
artifacts:
- "Jellyfin.Plugin.YouTubeAudio.dll"
```

Il `guid` del manifest deve coincidere con quello della classe `Plugin`. La versione e il `targetAbi` devono essere aggiornati consapevolmente a ogni release compatibile.

Il template usa workflow riutilizzabili di `jellyfin-meta-plugins` per build, test e publish. Il workflow di pubblicazione è attivato da una release GitHub e richiede i secret di deploy previsti dal repository Jellyfin. Prima di adottarlo nel repository del progetto bisogna sostituire i riferimenti al repository template e configurare i secret.

## 14. Licenza

Il template include GPLv3. La guida ufficiale avverte che il plugin viene collegato ai pacchetti binari Jellyfin, distribuiti sotto GPLv3; la licenza del plugin deve quindi essere scelta tenendo conto di questo vincolo. Per una distribuzione pubblica non usare una licenza proprietaria o incompatibile con il collegamento a Jellyfin. Mantenere `LICENSE` coerente con il codice e con le eventuali licenze di yt-dlp, FFmpeg e altri artefatti inclusi.

## 15. Checklist prima del primo merge

- [ ] Namespace, assembly, nome e GUID sono stati sostituiti.
- [ ] `Plugin.cs` implementa `BasePlugin<PluginConfiguration>` e `IHasWebPages`.
- [ ] `PluginConfiguration` contiene le due destinazioni.
- [ ] `configPage.html` è embedded e salva tramite `ApiClient`.
- [ ] Package Jellyfin e `targetAbi` sono compatibili con `10.11.10`.
- [ ] Controller, worker e registrazione DI non eseguono lavoro nella richiesta HTTP.
- [ ] Lo stato della coda è persistente e ripristinabile.
- [ ] L'elaborazione è sequenziale e resistente al fallimento di un elemento.
- [ ] yt-dlp e FFmpeg sono disponibili e avviabili nel container amd64.
- [ ] Non esiste concatenazione insicura dell'URL in una shell.
- [ ] Nomi, estensioni, versionamento e file temporanei sono testati.
- [ ] La libreria viene aggiornata una sola volta a fine richiesta.
- [ ] Test deterministici e test nel container target sono separati.
- [ ] `build.yaml`, workflow, licenza e metadati sono pronti per la pubblicazione.

## Fonti primarie consultate

- [README ufficiale del template](https://github.com/jellyfin/jellyfin-plugin-template/blob/c93225a0a5a76d3843db05b4b5b77fcfc482fba3/README.md) — prerequisiti, creazione del progetto, classi obbligatorie, interfacce, debugging e licenza.
- [Progetto di esempio](https://github.com/jellyfin/jellyfin-plugin-template/tree/c93225a0a5a76d3843db05b4b5b77fcfc482fba3/Jellyfin.Plugin.Template) — `Plugin.cs`, configurazione e pagina HTML embedded.
- [`Jellyfin.Plugin.Template.csproj`](https://github.com/jellyfin/jellyfin-plugin-template/blob/c93225a0a5a76d3843db05b4b5b77fcfc482fba3/Jellyfin.Plugin.Template/Jellyfin.Plugin.Template.csproj) — target framework, package reference ed esclusione degli asset runtime.
- [`build.yaml`](https://github.com/jellyfin/jellyfin-plugin-template/blob/c93225a0a5a76d3843db05b4b5b77fcfc482fba3/build.yaml) — metadati e artefatti del plugin.
- [Workflow del template](https://github.com/jellyfin/jellyfin-plugin-template/tree/c93225a0a5a76d3843db05b4b5b77fcfc482fba3/.github/workflows) — build, test, changelog e pubblicazione.
- [`CONTEXT.md`](CONTEXT.md) e [`SPEC.md`](SPEC.md) — linguaggio di dominio, requisiti e criteri di test di questo progetto.
