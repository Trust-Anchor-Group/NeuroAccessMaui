#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"

serial=""; timeout_seconds=240
while (($#)); do
    case "$1" in
        --serial) serial="${2:-}"; shift 2 ;;
        --timeout) timeout_seconds="${2:-}"; shift 2 ;;
        *) die "Unknown wait-for-android argument: $1" ;;
    esac
done
[[ "$serial" =~ ^emulator-[0-9]+$ ]] || die "A managed emulator serial is required"
[[ "$timeout_seconds" =~ ^[0-9]+$ ]] || die "Boot timeout must be an integer"

deadline=$((SECONDS + timeout_seconds))
while ((SECONDS < deadline)); do
    if [[ "$(adb -s "$serial" get-state 2>/dev/null || true)" == "device" ]] &&
       [[ "$(adb -s "$serial" shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" == "1" ]]; then
        adb -s "$serial" shell input keyevent 82 >/dev/null 2>&1 || true
        adb -s "$serial" shell settings put global window_animation_scale 0 >/dev/null
        adb -s "$serial" shell settings put global transition_animation_scale 0 >/dev/null
        adb -s "$serial" shell settings put global animator_duration_scale 0 >/dev/null
        printf 'Android boot completed on %s\n' "$serial"
        exit 0
    fi
    sleep 2
done
die "Android did not finish booting on $serial within $timeout_seconds seconds"
