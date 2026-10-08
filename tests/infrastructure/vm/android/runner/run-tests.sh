#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"

usage() { printf 'Usage: %s --serial SERIAL --app APK --tests APK --results DIR [--class TEST_CLASS] [--arg KEY=VALUE]\n' "$0"; }
serial=""; app_apk=""; test_apk=""; results_dir=""; test_class=""; instrumentation_args=()
while (($#)); do
    case "$1" in
        --serial) serial="${2:-}"; shift 2 ;;
        --app) app_apk="${2:-}"; shift 2 ;;
        --tests) test_apk="${2:-}"; shift 2 ;;
        --results) results_dir="${2:-}"; shift 2 ;;
        --class) test_class="${2:-}"; shift 2 ;;
        --arg) instrumentation_args+=("${2:-}"); shift 2 ;;
        -h|--help) usage; exit 0 ;;
        *) usage >&2; die "Unknown argument: $1" ;;
    esac
done
[[ -n "$serial" && -n "$app_apk" && -n "$test_apk" && -n "$results_dir" ]] || { usage >&2; exit 2; }
require_command adb; require_file "$app_apk"; require_file "$test_apk"; require_device "$serial"
mkdir -p "$results_dir"

logcat_pid=""
finish() {
    local exit_code=$?
    [[ -z "$logcat_pid" ]] || kill "$logcat_pid" 2>/dev/null || true
    [[ -z "$logcat_pid" ]] || wait "$logcat_pid" 2>/dev/null || true
    adb_for "$serial" shell am force-stop com.tag.NeuroAccess >/dev/null 2>&1 || true
    printf '%s\n' "$exit_code" >"$results_dir/exit-code.txt"
}
trap finish EXIT

adb_for "$serial" logcat -c >/dev/null 2>&1 || true
adb_for "$serial" logcat -v threadtime >"$results_dir/logcat.txt" 2>&1 & logcat_pid=$!
adb_for "$serial" install -r -t "$app_apk" | tee "$results_dir/install-app.txt"
adb_for "$serial" install -r -t "$test_apk" | tee "$results_dir/install-tests.txt"

command=(shell am instrument -w -r)
[[ -z "$test_class" ]] || command+=(-e class "$test_class")
for argument in "${instrumentation_args[@]}"; do
    [[ "$argument" == *=* ]] || die "Instrumentation argument must use KEY=VALUE: $argument"
    command+=(-e "${argument%%=*}" "${argument#*=}")
done
command+=(com.tag.neuroaccess.neuroaccessespressoautomationtests.test/androidx.test.runner.AndroidJUnitRunner)
adb_for "$serial" "${command[@]}" | tee "$results_dir/instrumentation.txt"
if ! grep -Eq '^OK \([1-9][0-9]* tests?\)\r?$' "$results_dir/instrumentation.txt"; then
    die "Espresso instrumentation did not report a successful test run"
fi
