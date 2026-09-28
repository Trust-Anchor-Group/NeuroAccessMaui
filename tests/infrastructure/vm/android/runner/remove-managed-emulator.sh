#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"

api_level=""; image_flavor="google_apis"; abi="x86_64"; avd_name=""; results_dir=""
while (($#)); do
    case "$1" in
        --api) api_level="${2:-}"; shift 2 ;;
        --image) image_flavor="${2:-}"; shift 2 ;;
        --abi) abi="${2:-}"; shift 2 ;;
        --name) avd_name="${2:-}"; shift 2 ;;
        --results) results_dir="${2:-}"; shift 2 ;;
        *) die "Unknown cleanup argument: $1" ;;
    esac
done
[[ "$api_level" =~ ^[0-9]{2}$ ]] || die "API level must contain two digits"
[[ "$avd_name" =~ ^[A-Za-z0-9._-]+$ ]] || die "An AVD name is required for cleanup"
[[ -n "$results_dir" ]] || die "A results directory is required"

sdk_root="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-}}"
if [[ -z "$sdk_root" ]]; then
    for candidate in /opt/neuro-test/android-sdk "$HOME/Android/Sdk"; do
        if [[ -d "$candidate" ]]; then sdk_root="$candidate"; break; fi
    done
fi
[[ -n "$sdk_root" ]] || die "Set ANDROID_SDK_ROOT to the Android SDK directory"
sdkmanager="$(command -v sdkmanager 2>/dev/null || true)"
avdmanager="$(command -v avdmanager 2>/dev/null || true)"
[[ -n "$sdkmanager" ]] || sdkmanager="$(find "$sdk_root/cmdline-tools" -type f -path '*/bin/sdkmanager' | sort -V | tail -n 1)"
[[ -n "$avdmanager" ]] || avdmanager="$(find "$sdk_root/cmdline-tools" -type f -path '*/bin/avdmanager' | sort -V | tail -n 1)"
require_file "$sdkmanager"; require_file "$avdmanager"

package="system-images;android-${api_level};${image_flavor};${abi}"
printf 'Storage before cleanup: ' >>"$results_dir/emulator-setup.txt"
du -sh "$sdk_root" 2>/dev/null | awk '{print $1}' >>"$results_dir/emulator-setup.txt" || printf 'unknown\n' >>"$results_dir/emulator-setup.txt"
printf 'Deleting AVD %s\n' "$avd_name" >>"$results_dir/emulator-setup.txt"
"$avdmanager" delete avd --name "$avd_name" >>"$results_dir/emulator-setup.txt" 2>&1 || true
avd_root="${ANDROID_AVD_HOME:-${ANDROID_EMULATOR_HOME:-$HOME/.android}/avd}"
image_path="system-images/android-${api_level}/${image_flavor}/${abi}/"
if [[ -d "$avd_root" ]]; then
    while IFS= read -r config_file; do
        grep -Fqx "image.sysdir.1=$image_path" "$config_file" || continue
        cached_name="$(basename "$(dirname "$config_file")" .avd)"
        printf 'Deleting cached AVD %s for %s\n' "$cached_name" "$package" >>"$results_dir/emulator-setup.txt"
        "$avdmanager" delete avd --name "$cached_name" >>"$results_dir/emulator-setup.txt" 2>&1 || true
    done < <(find "$avd_root" -mindepth 2 -maxdepth 2 -type f -name config.ini -print)
fi
printf 'Uninstalling Android system image %s\n' "$package" >>"$results_dir/emulator-setup.txt"
"$sdkmanager" --uninstall "$package" >>"$results_dir/emulator-setup.txt" 2>&1
printf 'Storage after cleanup: ' >>"$results_dir/emulator-setup.txt"
du -sh "$sdk_root" 2>/dev/null | awk '{print $1}' >>"$results_dir/emulator-setup.txt" || printf 'unknown\n' >>"$results_dir/emulator-setup.txt"