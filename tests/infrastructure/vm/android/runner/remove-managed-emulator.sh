#!/usr/bin/env bash
set -Eeuo pipefail
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"
avd_name=""; results_dir=""
while (($#)); do
    case "$1" in
        --name) avd_name="${2:-}"; shift 2 ;;
        --results) results_dir="${2:-}"; shift 2 ;;
        *) die "Unknown cleanup argument: $1" ;;
    esac
done
[[ "$avd_name" =~ ^neuro_appium_[A-Za-z0-9][A-Za-z0-9._-]*$ ]] || die "Only managed Appium AVDs can be removed"
[[ -n "$results_dir" ]] || die "A results directory is required"
require_command avdmanager
avdmanager delete avd --name "$avd_name" >>"$results_dir/emulator-setup.txt" 2>&1
