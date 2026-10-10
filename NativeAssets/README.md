# Asset nativi

Questa directory viene popolata da `tools/fetch-native-assets.sh` durante il packaging per il target Linux amd64.

Il plugin distribuito deve contenere `yt-dlp`, `ffmpeg`, `ffprobe` e le relative informazioni di versione. Verificare e distribuire anche le licenze dei binari effettivamente scaricati. Non committiamo binari nel repository: il packaging li scarica da URL versionati/controllabili prima della pubblicazione.
