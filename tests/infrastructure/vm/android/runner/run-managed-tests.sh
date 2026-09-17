#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"

api_level=""; device_profile="pixel_6"; image_flavor="google_apis"; abi="x86_64"
avd_name=""; app_apk=""; test_apk=""; results_dir=""; test_class=""; show_window=false; instrumentation_args=()
while (($#)); do
    case "$1" in
        --api) api_level="${2:-}"; shift 2 ;;
        --device) device_profile="${2:-}"; shift 2 ;;
        --image) image_flavor="${2:-}"; shift 2 ;;
        --abi) abi="${2:-}"; shift 2 ;;
        --name) avd_name="${2:-}"; shift 2 ;;
        --app) app_apk="${2:-}"; shift 2 ;;
        --tests) test_apk="${2:-}"; shift 2 ;;
        --results) results_dir="${2:-}"; shift 2 ;;
        --show-window) show_window=true; shift ;;
        --class) test_class="${2:-}"; shift 2 ;;
        --arg) instrumentation_args+=("${2:-}"); shift 2 ;;
        *) die "Unknown managed test argument: $1" ;;
    esac
done
[[ -n "$api_level" && -n "$app_apk" && -n "$test_apk" && -n "$results_dir" ]] ||
    die "Managed tests require API level, app APK, test APK and results directory"
mkdir -p "$results_dir"

prepare_args=(--api "$api_level" --device "$device_profile" --image "$image_flavor" --abi "$abi" --results "$results_dir")
[[ -z "$avd_name" ]] || prepare_args+=(--name "$avd_name")
[[ "$show_window" == true ]] && prepare_args+=(--show-window)
serial="$(bash "$SCRIPT_DIR/prepare-emulator.sh" "${prepare_args[@]}")"
cleanup() {
    local exit_code=$?
    trap - EXIT
    bash "$SCRIPT_DIR/stop-emulator.sh" "$serial" >>"$results_dir/emulator-setup.txt" 2>&1 || true
    exit "$exit_code"
}
trap cleanup EXIT

run_args=(--serial "$serial" --app "$app_apk" --tests "$test_apk" --results "$results_dir/test")
[[ -z "$test_class" ]] || run_args+=(--class "$test_class")
for argument in "${instrumentation_args[@]}"; do run_args+=(--arg "$argument"); done
bash "$SCRIPT_DIR/run-tests.sh" "${run_args[@]}"
