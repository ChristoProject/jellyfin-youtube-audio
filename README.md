# YouTube Audio Downloader per Jellyfin

Plugin Jellyfin per acquisire l'audio di video e playlist YouTube pubblici.

## Stato iniziale

È implementato il flusso MVP: pagina dashboard, configurazione delle due destinazioni, endpoint REST, persistenza JSON, worker sequenziale ripristinabile, nomi sicuri/versionati, adapter `yt-dlp`/FFmpeg e refresh unico della libreria a fine richiesta.

I binari `yt-dlp`, FFmpeg e `ffprobe` vengono preparati per il target Linux amd64 tramite `tools/fetch-native-assets.sh` e copiati nel publish sotto `NativeAssets/linux-x64/`. L'adapter non usa una shell e applica timeout, cancellazione e file temporanei. Prima della release occorre verificare le licenze e fissare le versioni degli asset.

## Compatibilità

- Jellyfin 10.11.10
- .NET 9 (`net9.0`)
- Linux amd64 target

## Sviluppo

```bash
dotnet restore
dotnet build --configuration Release
dotnet test
./tools/fetch-native-assets.sh
dotnet publish --configuration Release
```

L'ambiente corrente deve avere il .NET SDK 9 installato per eseguire questi comandi.

## Installazione dalla dashboard Jellyfin

Il repository plugin Jellyfin è disponibile qui:

```text
https://raw.githubusercontent.com/ChristoProject/jellyfin-youtube-audio/main/manifest.json
```

In Jellyfin apri **Dashboard → Plugin → Repository**, aggiungi l'URL precedente, salva, cerca **YouTube Audio Downloader** e installalo. Gli aggiornamenti vengono pubblicati tramite GitHub Releases e compaiono nel catalogo Jellyfin.
