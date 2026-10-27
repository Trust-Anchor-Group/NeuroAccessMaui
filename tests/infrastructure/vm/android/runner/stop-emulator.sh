#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"

serial="${1:-}"
[[ "$serial" =~ ^emulator-[0-9]+$ ]] || die "A managed emulator serial is required"
adb -s "$serial" emu kill >/dev/null 2>&1 || true
deadline=$((SECONDS + 30))
while ((SECONDS < deadline)); do
    adb devices | awk 'NR>1 {print $1}' | grep -qx "$serial" || exit 0
    sleep 1
done
die "Emulator did not stop: $serial"
