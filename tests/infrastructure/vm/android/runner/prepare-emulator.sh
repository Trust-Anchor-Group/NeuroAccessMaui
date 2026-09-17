#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"

api_level=""; device_profile="pixel_6"; image_flavor="google_apis"; abi="x86_64"
avd_name=""; port=""; results_dir=""; show_window=false
while (($#)); do
    case "$1" in
        --api) api_level="${2:-}"; shift 2 ;;
        --device) device_profile="${2:-}"; shift 2 ;;
        --image) image_flavor="${2:-}"; shift 2 ;;
        --abi) abi="${2:-}"; shift 2 ;;
        --name) avd_name="${2:-}"; shift 2 ;;
        --port) port="${2:-}"; shift 2 ;;
        --results) results_dir="${2:-}"; shift 2 ;;
        --show-window) show_window=true; shift ;;
        *) die "Unknown prepare-emulator argument: $1" ;;
    esac
done

[[ "$api_level" =~ ^[0-9]{2}$ ]] || die "API level must contain two digits"
[[ "$device_profile" =~ ^[A-Za-z0-9._-]+$ ]] || die "Invalid Android hardware profile"
[[ "$image_flavor" =~ ^[A-Za-z0-9._-]+$ ]] || die "Invalid system image flavor"
[[ "$abi" =~ ^[A-Za-z0-9._-]+$ ]] || die "Invalid Android ABI"
[[ -z "$avd_name" || "$avd_name" =~ ^[A-Za-z0-9._-]+$ ]] || die "Invalid AVD name"
[[ -z "$port" || "$port" =~ ^[0-9]+$ ]] || die "Emulator port must be numeric"
[[ -n "$results_dir" ]] || die "A results directory is required"
mkdir -p "$results_dir"

sdk_root="${ANDROID_SDK_ROOT:-${ANDROID_HOME:-}}"
if [[ -z "$sdk_root" ]]; then
    for candidate in /opt/neuro-test/android-sdk "$HOME/Android/Sdk"; do
        if [[ -d "$candidate" ]]; then sdk_root="$candidate"; break; fi
    done
fi
[[ -n "$sdk_root" ]] || die "Set ANDROID_SDK_ROOT to the Android SDK directory"
sdkmanager="$(command -v sdkmanager 2>/dev/null || true)"
avdmanager="$(command -v avdmanager 2>/dev/null || true)"
emulator="$(command -v emulator 2>/dev/null || true)"
if [[ -z "$sdkmanager" && -d "$sdk_root/cmdline-tools" ]]; then
    sdkmanager="$(find "$sdk_root/cmdline-tools" -type f -path '*/bin/sdkmanager' | sort -V | tail -n 1)"
fi
if [[ -z "$avdmanager" && -d "$sdk_root/cmdline-tools" ]]; then
    avdmanager="$(find "$sdk_root/cmdline-tools" -type f -path '*/bin/avdmanager' | sort -V | tail -n 1)"
fi
[[ -n "$sdkmanager" ]] || die "Android Command-line Tools are missing; sdkmanager was not found in PATH or $sdk_root/cmdline-tools"
[[ -n "$avdmanager" ]] || die "Android Command-line Tools are missing; avdmanager was not found in PATH or $sdk_root/cmdline-tools"
require_file "$sdkmanager"; require_file "$avdmanager"; require_command adb

package="system-images;android-${api_level};${image_flavor};${abi}"
[[ -n "$avd_name" ]] || avd_name="neuro_${device_profile}_api${api_level}_${image_flavor}_${abi}"
printf 'Preparing %s (%s)\n' "$avd_name" "$package" | tee "$results_dir/emulator-setup.txt" >&2

image_directory="$sdk_root/system-images/android-${api_level}/${image_flavor}/${abi}"
if [[ ! -f "$image_directory/package.xml" ]]; then
    printf 'Installing Android system image %s\n' "$package" | tee -a "$results_dir/emulator-setup.txt" >&2
    yes | "$sdkmanager" --licenses >/dev/null 2>&1 || true
    "$sdkmanager" "$package" "platform-tools" "emulator" >>"$results_dir/emulator-setup.txt" 2>&1
fi
[[ -f "$image_directory/package.xml" ]] || die "Android system image was not installed correctly: $package"

[[ -n "$emulator" ]] || emulator="$sdk_root/emulator/emulator"
require_file "$emulator"

avd_root="${ANDROID_AVD_HOME:-${ANDROID_EMULATOR_HOME:-$HOME/.android}/avd}"
if [[ ! -d "$avd_root/$avd_name.avd" ]]; then
    printf 'Creating AVD %s with profile %s\n' "$avd_name" "$device_profile" | tee -a "$results_dir/emulator-setup.txt" >&2
    printf 'no\n' | "$avdmanager" create avd --force --name "$avd_name" --package "$package" --device "$device_profile" \
        >>"$results_dir/emulator-setup.txt" 2>&1
fi

if [[ -z "$port" ]]; then
    for candidate in $(seq 5554 2 5680); do
        if ! adb devices | awk 'NR>1 {print $1}' | grep -qx "emulator-$candidate"; then port="$candidate"; break; fi
    done
fi
[[ -n "$port" ]] || die "No free Android emulator port was found"
serial="emulator-$port"
adb devices | awk 'NR>1 {print $1}' | grep -qx "$serial" && die "Selected emulator port is already in use: $port"

emulator_args=(-avd "$avd_name" -port "$port" -gpu swiftshader_indirect -no-audio -no-snapshot-save -no-boot-anim)
if [[ "$show_window" == true ]]; then
    gui_pid="$(pgrep -u "$(id -u)" -n gnome-shell 2>/dev/null || pgrep -u "$(id -u)" -n Xwayland 2>/dev/null || true)"
    if [[ -n "$gui_pid" && -r "/proc/$gui_pid/environ" ]]; then
        gui_display="$(tr '\0' '\n' <"/proc/$gui_pid/environ" | sed -n 's/^DISPLAY=//p' | head -n 1)"
        gui_xauthority="$(tr '\0' '\n' <"/proc/$gui_pid/environ" | sed -n 's/^XAUTHORITY=//p' | head -n 1)"
    else
        gui_display=""
        gui_xauthority=""
    fi
    export DISPLAY="${DISPLAY:-${gui_display:-:0}}"
    if [[ -z "${XAUTHORITY:-}" ]]; then
        for candidate in "$gui_xauthority" "/run/user/$(id -u)/gdm/Xauthority" "$HOME/.Xauthority"; do
            if [[ -n "$candidate" && -f "$candidate" ]]; then export XAUTHORITY="$candidate"; break; fi
        done
    fi
    printf 'Starting visible emulator on DISPLAY=%s with XAUTHORITY=%s\n' \
        "$DISPLAY" "${XAUTHORITY:-unset}" | tee -a "$results_dir/emulator-setup.txt" >&2
else
    emulator_args+=(-no-window)
fi
nohup "$emulator" "${emulator_args[@]}" \
    >"$results_dir/emulator-console.txt" 2>&1 </dev/null &
printf '%s\n' "$!" >"$results_dir/emulator.pid"
bash "$SCRIPT_DIR/wait-for-android.sh" --serial "$serial" --timeout 300 >>"$results_dir/emulator-setup.txt"
printf '%s\n' "$serial"
