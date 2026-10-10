#!/usr/bin/env bash
set -euo pipefail

# Pinned Linux amd64 assets for linuxserver/jellyfin on amd64.
YT_DLP_VERSION="${YT_DLP_VERSION:-2026.08.19}"
FFMPEG_VERSION="${FFMPEG_VERSION:-release}"
FFMPEG_URL="${FFMPEG_URL:-https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
OUT="$ROOT/NativeAssets/linux-x64"
TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT
mkdir -p "$OUT"
XZ_BIN="${XZ_BIN:-$(command -v xz || true)}"
[ -n "$XZ_BIN" ] || { echo "Richiesto xz-utils per estrarre FFmpeg." >&2; exit 1; }

curl --fail --location --retry 3 --output "$OUT/yt-dlp" \
  "https://github.com/yt-dlp/yt-dlp/releases/download/${YT_DLP_VERSION}/yt-dlp_linux"
chmod 755 "$OUT/yt-dlp"
curl --fail --location --retry 3 --output "$OUT/yt-dlp-LICENSE" \
  "https://raw.githubusercontent.com/yt-dlp/yt-dlp/${YT_DLP_VERSION}/LICENSE"

archive="$TMP/ffmpeg.tar.xz"
curl --fail --location --retry 3 --output "$archive" "$FFMPEG_URL"
tar --use-compress-program="$XZ_BIN" -xf "$archive" -C "$TMP"
cp "$TMP"/ffmpeg-*-amd64-static/ffmpeg "$OUT/ffmpeg"
cp "$TMP"/ffmpeg-*-amd64-static/ffprobe "$OUT/ffprobe"
chmod 755 "$OUT"/ffmpeg "$OUT"/ffprobe
if ! cp "$TMP"/ffmpeg-*-amd64-static/LICENSE.txt "$OUT/FFmpeg-LICENSE.txt" 2>/dev/null; then
  {
    echo "FFmpeg static build license references (FFmpeg 7.0.2)."
    echo "The build contains GPL-licensed components; retain these notices with the binary."
    curl --fail --location --retry 3 "https://raw.githubusercontent.com/FFmpeg/FFmpeg/n7.0.2/COPYING.GPLv3"
    curl --fail --location --retry 3 "https://raw.githubusercontent.com/FFmpeg/FFmpeg/n7.0.2/COPYING.LGPLv3"
  } > "$OUT/FFmpeg-LICENSE.txt"
fi

cat > "$OUT/VERSIONS.txt" <<EOF
yt-dlp ${YT_DLP_VERSION}
FFmpeg ${FFMPEG_VERSION} (static amd64 build)
EOF

echo "Asset nativi installati in $OUT"
