# Download audio da YouTube in Jellyfin

Questo contesto definisce il linguaggio del plugin Jellyfin che acquisisce contenuti audio da video e playlist YouTube per uso personale.

## Sorgenti e contenuti

**Video sorgente**:
Una singola risorsa video YouTube identificata da un URL, dalla quale il plugin ricava una traccia audio.
_Avoid_: video scaricato, file video

**Playlist sorgente**:
Una raccolta ordinata di video YouTube identificata da un URL, i cui elementi vengono elaborati tutti senza selezione parziale.
_Avoid_: playlist parziale

**Traccia audio**:
Il risultato audio prodotto dal plugin per un video sorgente.
_Avoid_: download, video convertito

**Categoria di contenuto**:
La scelta obbligatoria effettuata prima dell’avvio che determina se la traccia audio è trattata come Musica o Podcast.
_Avoid_: tipo file

**Musica**:
Una traccia audio destinata alla libreria musicale Jellyfin, prodotta in formato Opus.

**Podcast**:
Una traccia audio destinata alla raccolta podcast, prodotta in formato MP3 standard.

## Operazioni e stato

**Richiesta di download**:
L’operazione avviata dall’utente fornendo un URL e una categoria di contenuto; può riguardare un video sorgente o tutti gli elementi di una playlist sorgente.
_Avoid_: download sincrono

**Elemento della coda**:
La lavorazione individuale di un video sorgente all’interno di una richiesta di download.
_Avoid_: box, card

**Scheda di avanzamento**:
La rappresentazione nell’interfaccia Jellyfin di un elemento della coda, comprensiva del titolo del video e dello stato di avanzamento.
_Avoid_: box, card

**Stato di download**:
Per l’MVP, un elemento può essere in attesa con barra al 0%, in elaborazione con percentuale aggiornata, Completato oppure Fallito.

## Destinazioni

**Destinazione musicale**:
La cartella esistente assegnata alle tracce classificate come Musica; nella prima versione l’utente può occuparsi personalmente dello spostamento successivo dei file.

**Destinazione podcast**:
La cartella esistente assegnata alle tracce classificate come Podcast; nella prima versione l’utente può occuparsi personalmente dello spostamento successivo dei file.

**Versionamento del file**:
La modifica del nome di un file già presente aggiungendo un suffisso numerico progressivo, ad esempio `video.mp3`, `video(1).mp3`.

**Nome del file audio**:
Il titolo YouTube usato come base del nome della traccia audio prodotta, con l’estensione coerente con la categoria di contenuto; i caratteri non validi per il filesystem vengono sostituiti.

**Autenticazione YouTube**:
Una capacità prevista per un’evoluzione successiva; l’MVP supporta solo video e playlist pubblici.
