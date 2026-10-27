#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"
usage() { printf 'Usage: %s --serial-a SERIAL --serial-b SERIAL --app APK --tests APK --results DIR --coordinator SCRIPT [--arg VALUE]\n' "$0"; }
serial_a=""; serial_b=""; app_apk=""; test_apk=""; results_dir=""; coordinator=""; coordinator_args=()
while (($#)); do
    case "$1" in
        --serial-a) serial_a="${2:-}"; shift 2 ;;
        --serial-b) serial_b="${2:-}"; shift 2 ;;
        --app) app_apk="${2:-}"; shift 2 ;;
        --tests) test_apk="${2:-}"; shift 2 ;;
        --results) results_dir="${2:-}"; shift 2 ;;
        --coordinator) coordinator="${2:-}"; shift 2 ;;
        --arg) coordinator_args+=("${2:-}"); shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) usage >&2; die "Unknown argument: $1" ;;
    esac
done
[[ -n "$serial_a" && -n "$serial_b" && -n "$app_apk" && -n "$test_apk" && -n "$results_dir" && -n "$coordinator" ]] || { usage >&2; exit 2; }
[[ "$serial_a" != "$serial_b" ]] || die "The pair requires two different adb serials"
require_command adb; require_file "$app_apk"; require_file "$test_apk"; require_file "$coordinator"
require_device "$serial_a"; require_device "$serial_b"
mkdir -p "$results_dir/device-a" "$results_dir/device-b"

logcat_a_pid=""; logcat_b_pid=""
finish() {
    local exit_code=$?
    for pid in "$logcat_a_pid" "$logcat_b_pid"; do [[ -z "$pid" ]] || kill "$pid" 2>/dev/null || true; done
    [[ -z "$logcat_a_pid" ]] || wait "$logcat_a_pid" 2>/dev/null || true
    [[ -z "$logcat_b_pid" ]] || wait "$logcat_b_pid" 2>/dev/null || true
    adb_for "$serial_a" shell am force-stop com.tag.NeuroAccess >/dev/null 2>&1 || true
    adb_for "$serial_b" shell am force-stop com.tag.NeuroAccess >/dev/null 2>&1 || true
    printf '%s\n' "$exit_code" >"$results_dir/exit-code.txt"
}
trap finish EXIT

for serial in "$serial_a" "$serial_b"; do adb_for "$serial" install -r -t "$app_apk"; adb_for "$serial" install -r -t "$test_apk"; adb_for "$serial" logcat -c >/dev/null 2>&1 || true; done
adb_for "$serial_a" logcat -v threadtime >"$results_dir/device-a/logcat.txt" 2>&1 & logcat_a_pid=$!
adb_for "$serial_b" logcat -v threadtime >"$results_dir/device-b/logcat.txt" 2>&1 & logcat_b_pid=$!
NEURO_DEVICE_A="$serial_a" NEURO_DEVICE_B="$serial_b" NEURO_RESULTS_DIR="$results_dir" bash "$coordinator" "${coordinator_args[@]}" | tee "$results_dir/coordinator.txt"
