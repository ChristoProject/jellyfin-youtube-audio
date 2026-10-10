# Jellyfin YouTube Audio Downloader MVP

## Problem Statement

Come amministratore di un’istanza Jellyfin installata in un container Docker su ZimaOS, voglio acquisire il solo audio da un singolo video YouTube o da una playlist YouTube completa senza installare servizi, applicazioni o web app separati. Attualmente non esiste nel repository un plugin che permetta di inserire un URL, scegliere la categoria del contenuto e seguire l’elaborazione direttamente da Jellyfin.

L’MVP deve essere compatibile con Jellyfin `10.11.10`, immagine Docker `linuxserver/jellyfin:10.11.10`, su architettura `amd64`.

## Solution

Realizzare un plugin Jellyfin che offra una pagina interna con:

- un campo per l’URL di un video o di una playlist YouTube;
- una scelta obbligatoria tra **Musica** e **Podcast**;
- una CTA per analizzare e avviare la richiesta di download;
- una scheda di avanzamento per ogni video sorgente individuato.

Il plugin deve analizzare l’URL in background, scaricare un solo elemento alla volta e produrre esclusivamente una traccia audio:

- **Musica** → formato Opus;
- **Podcast** → formato MP3.

La destinazione viene determinata dalla categoria selezionata e configurata tramite due path distinti: Destinazione musicale e Destinazione podcast. Le cartelle sono già esistenti. Al termine dell’intera richiesta, il plugin deve chiedere a Jellyfin di aggiornare la libreria.

L’MVP supporta soltanto video e playlist pubblici. Il motore di acquisizione e conversione deve essere incluso nel plugin, senza servizi o container separati; la soluzione raccomandata include yt-dlp e FFmpeg.

## User Stories

1. Come amministratore Jellyfin, voglio aprire una pagina del plugin dentro Jellyfin, così da svolgere l’intero flusso senza una web app esterna.
2. Come amministratore Jellyfin, voglio inserire l’URL di un video YouTube, così da richiedere l’acquisizione della sua traccia audio.
3. Come amministratore Jellyfin, voglio inserire l’URL di una playlist YouTube, così da acquisire tutti i suoi video in un’unica richiesta.
4. Come amministratore Jellyfin, voglio scegliere obbligatoriamente Musica o Podcast prima di avviare una richiesta, così da determinare formato e destinazione.
5. Come amministratore Jellyfin, voglio che una richiesta relativa a una playlist utilizzi una sola categoria per tutti i suoi elementi, così da mantenere coerenti formato e destinazione.
6. Come amministratore Jellyfin, voglio che l’analisi dell’URL avvenga in background, così da non bloccare la pagina o il processo principale di Jellyfin.
7. Come amministratore Jellyfin, voglio vedere una scheda per ogni video individuato nell’URL, così da conoscere l’estensione della richiesta prima e durante l’elaborazione.
8. Come amministratore Jellyfin, voglio che ogni scheda mostri il titolo YouTube, così da riconoscere il video associato.
9. Come amministratore Jellyfin, voglio che una scheda in attesa mostri una barra allo 0%, così da distinguere un elemento non ancora elaborato da uno in errore.
10. Come amministratore Jellyfin, voglio vedere la percentuale di avanzamento durante il download, così da sapere se il lavoro sta procedendo.
11. Come amministratore Jellyfin, voglio che una scheda mostri Completato quando il file è stato scritto correttamente, così da distinguere i risultati disponibili.
12. Come amministratore Jellyfin, voglio che una scheda mostri Fallito quando l’analisi o il download del video non riescono, così da individuare i problemi senza ambiguità.
13. Come amministratore Jellyfin, voglio che il fallimento di un elemento non interrompa gli altri elementi della playlist, così da ottenere comunque tutti i file disponibili.
14. Come amministratore Jellyfin, voglio che il plugin scarichi un solo elemento alla volta, così da mantenere prevedibile il carico sul container e sulla connessione.
15. Come amministratore Jellyfin, voglio ottenere solo l’audio del video, così da non occupare spazio con il contenuto video completo.
16. Come amministratore Jellyfin, voglio che i contenuti classificati come Musica vengano prodotti in Opus, così da usare il formato deciso per la libreria musicale.
17. Come amministratore Jellyfin, voglio che i contenuti classificati come Podcast vengano prodotti in MP3, così da usare il formato standard scelto per i podcast.
18. Come amministratore Jellyfin, voglio che il nome del file derivi dal titolo YouTube, così da riconoscere il contenuto senza doverlo rinominare immediatamente.
19. Come amministratore Jellyfin, voglio che i caratteri incompatibili con il filesystem vengano sostituiti, così da permettere la creazione del file anche con titoli problematici.
20. Come amministratore Jellyfin, voglio che un file con lo stesso nome non venga sovrascritto, così da preservare il risultato precedente.
21. Come amministratore Jellyfin, voglio che i duplicati vengano versionati con suffissi numerici, così da ottenere nomi come `video.mp3`, `video(1).mp3` e `video(2).mp3`.
22. Come amministratore Jellyfin, voglio configurare una Destinazione musicale, così da stabilire dove scrivere le tracce Musica.
23. Come amministratore Jellyfin, voglio configurare una Destinazione podcast, così da stabilire dove scrivere le tracce Podcast.
24. Come amministratore Jellyfin, voglio che la scelta Musica selezioni automaticamente la Destinazione musicale, così da non dover scegliere il path per ogni download.
25. Come amministratore Jellyfin, voglio che la scelta Podcast selezioni automaticamente la Destinazione podcast, così da non dover scegliere il path per ogni download.
26. Come amministratore Jellyfin, voglio che il plugin aggiorni la libreria una sola volta dopo la conclusione dell’intera richiesta, così da rendere disponibili i risultati senza avviare una scansione dopo ogni file.
27. Come amministratore Jellyfin, voglio che una richiesta in background possa riprendere dopo un riavvio di Jellyfin, così da non perdere il lavoro già avviato.
28. Come amministratore Jellyfin, voglio che un URL non valido produca una scheda Fallito, così da avere un feedback visibile anche quando l’errore avviene prima dell’identificazione del video.
29. Come amministratore Jellyfin, voglio che l’MVP supporti sorgenti YouTube pubbliche senza autenticazione, così da poterlo provare senza configurare credenziali.
30. Come amministratore Jellyfin, voglio che il plugin non memorizzi password Google, così da non esporre credenziali personali nel server Jellyfin.
31. Come amministratore Jellyfin, voglio che il plugin funzioni con l’installazione Docker indicata su ZimaOS amd64, così da poterlo testare nell’ambiente previsto.
32. Come amministratore Jellyfin, voglio che il plugin includa il motore necessario all’acquisizione e alla conversione, così da non dover installare un container o servizio aggiuntivo.

## Implementation Decisions

- Il prodotto è un plugin Jellyfin con interfaccia interna e un worker in background per le richieste di download.
- L’interfaccia espone un campo URL, la selezione obbligatoria della Categoria di contenuto e una CTA di analisi/avvio.
- La selezione della categoria è una proprietà della Richiesta di download e si applica a tutti gli Elementi della coda della richiesta.
- L’URL viene analizzato prima della creazione della coda operativa. Il plugin distingue un Video sorgente da una Playlist sorgente e crea un Elemento della coda per ogni video della playlist.
- Una Playlist sorgente viene elaborata interamente; non è prevista la selezione parziale degli elementi.
- I video vengono elaborati in sequenza: in ogni momento è attivo al massimo un download.
- Il worker deve essere eseguito fuori dal ciclo di richiesta HTTP e deve conservare abbastanza stato da riprendere gli elementi non completati dopo il riavvio di Jellyfin.
- La macchina degli stati minima della coda è: In attesa (0%), In elaborazione con percentuale, Completato, Fallito.
- Un errore di un elemento non interrompe gli altri elementi della stessa playlist.
- Un URL non valido produce un risultato visibile come Fallito; non viene trattato come un completamento silenzioso.
- Il plugin scrive esclusivamente il flusso audio nella destinazione determinata dalla Categoria di contenuto.
- Musica produce file Opus; Podcast produce file MP3.
- Il nome del file deriva dal titolo YouTube. I caratteri non validi per il filesystem vengono sostituiti senza introdurre una tassonomia di nomi alternativa.
- Se il nome risultante esiste già, il plugin crea una variante con suffisso numerico progressivo e non sovrascrive il file esistente.
- Le Destinazioni musicale e podcast sono due impostazioni distinte e configurabili. Le cartelle devono esistere già.
- Il plugin non organizza automaticamente i file in artisti, album, stagioni o altre sottocartelle.
- Non vengono incorporati metadati audio nella prima versione.
- Al termine di tutti gli elementi, inclusi quelli falliti, il plugin richiede un singolo aggiornamento della libreria Jellyfin.
- Il motore di acquisizione/conversione è incluso nel plugin; la soluzione prevista usa yt-dlp e FFmpeg senza servizio esterno o container separato.
- L’MVP supporta soltanto sorgenti YouTube pubbliche. L’autenticazione tramite cookie è un’estensione futura e non fa parte del contratto attuale.
- Target di compatibilità: Jellyfin 10.11.10, immagine LinuxServer corrispondente e architettura amd64.
- Non vengono introdotti servizi esterni, web app o ulteriori app operative.

### Test seams

Il seam principale deve essere un test end-to-end del flusso del plugin, con dipendenze esterne sostituite da un fake controllabile:

`URL + categoria → analisi → coda → download audio → scrittura destinazione → aggiornamento libreria`

Il fake del motore di acquisizione deve permettere di simulare video singolo, playlist, percentuali di avanzamento, titolo, errore di un elemento e ripresa dopo riavvio. Un filesystem temporaneo deve verificare formati, nomi, versionamento e destinazioni. Un fake del servizio di aggiornamento libreria deve verificare che la scansione venga richiesta una sola volta al termine.

Questo seam è preferibile a testare direttamente classi interne: verifica il comportamento osservabile dell’MVP e consente di mantenere yt-dlp, FFmpeg, Jellyfin e Docker fuori dai test deterministici. Devono inoltre esistere almeno test di compatibilità/integrazione sul container target per provare il binario incluso e il collegamento reale con Jellyfin 10.11.10.

## Testing Decisions

- I test devono verificare comportamento esterno: stato delle Schede di avanzamento, file prodotti, destinazioni, nomi, versionamento, gestione degli errori e richiesta di aggiornamento della libreria.
- Non devono dipendere dall’implementazione interna del worker, da dettagli di threading o da nomi di classi private.
- Il percorso principale deve coprire un Video sorgente pubblico scelto come Musica e uno scelto come Podcast.
- Un test di playlist deve verificare che tutti gli elementi vengano creati, che venga elaborato un solo elemento alla volta e che l’ordine della playlist sia rispettato.
- Un test deve verificare che l’elemento fallito non blocchi i successivi e che la richiesta termini con un aggiornamento unico della libreria.
- Un test deve verificare URL non valido con scheda Fallito.
- Un test deve verificare titolo con caratteri non validi e il nome file risultante.
- Un test deve verificare il versionamento quando il file di destinazione esiste già.
- Un test deve verificare che Musica produca Opus e Podcast produca MP3.
- Un test deve verificare che i metadati non vengano richiesti come parte del contratto MVP.
- Un test deve verificare che una richiesta non venga persa dopo il riavvio simulato del worker e che gli elementi già Completati non vengano duplicati.
- Un test deve verificare che l’aggiornamento della libreria avvenga soltanto dopo che tutti gli elementi sono Completati o Falliti.
- I test deterministici devono usare un fake del motore audio/download e un filesystem temporaneo.
- I test di integrazione devono essere eseguiti separatamente nel container target per validare l’inclusione e l’avvio di yt-dlp/FFmpeg e la compatibilità con la versione Jellyfin dichiarata.
- Non essendoci ancora codice nel repository, non esiste al momento prior art locale per i test; il seam descritto diventa il primo pattern di test del progetto.

## Out of Scope

- Playlist o video privati.
- Autenticazione YouTube, cookie, OAuth o gestione di password Google.
- Servizi esterni, container aggiuntivi o web app separate.
- Download parziale di una playlist.
- Download concorrenti o configurazione del numero di worker.
- Metadati incorporati, copertine, tag artista, album o numero traccia.
- Organizzazione automatica nelle cartelle finali dell’utente.
- Scelta della destinazione per singolo Elemento della coda.
- Scelta del codec da parte dell’utente.
- Supporto a formati musicali diversi da Opus e MP3 per Podcast.
- Retry manuale o configurabile.
- Annullamento di singoli elementi o dell’intera richiesta.
- Operazioni avanzate di gestione della coda e cronologia.
- Sovrascrittura dei file esistenti.
- Indicizzazione Jellyfin dopo ogni singolo file.
- Supporto a piattaforme diverse dall’ambiente target dichiarato, salvo compatibilità accidentale.

## Further Notes

- Il repository contiene attualmente il glossario di dominio in `CONTEXT.md`, ma non contiene ancora codice, configurazione di progetto, test, ADR o integrazioni con un issue tracker.
- La decisione di includere il motore di acquisizione e conversione nel plugin è un vincolo architetturale rilevante e dovrà essere validata tecnicamente prima dell’implementazione completa su Jellyfin 10.11.10.
- La compatibilità reale con il packaging del plugin, il runtime .NET e l’esecuzione di binari Linux inclusi deve essere verificata nel container amd64 target.
- La richiesta è pronta per essere trasformata in lavoro implementativo dopo la pubblicazione nel tracker e la verifica tecnica del seam end-to-end.
