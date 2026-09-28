#!/usr/bin/env bash

set -Eeuo pipefail

die() { printf 'error: %s\n' "$*" >&2; exit 1; }
require_command() { command -v "$1" >/dev/null 2>&1 || die "Required command is missing: $1"; }
require_file() { [[ -f "$1" ]] || die "Required file does not exist: $1"; }
require_safe_id() { [[ "$1" =~ ^[A-Za-z0-9][A-Za-z0-9._-]{0,127}$ ]] || die "Invalid identifier: $1"; }
adb_for() { local serial="$1"; shift; adb -s "$serial" "$@"; }
require_device() {
    local serial="$1"
    local state
    state="$(adb -s "$serial" get-state 2>/dev/null || true)"
    [[ "$state" == "device" ]] || die "Android device is unavailable: $serial"
}
