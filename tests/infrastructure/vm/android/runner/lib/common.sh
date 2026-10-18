#!/usr/bin/env bash

set -Eeuo pipefail

die() { printf 'error: %s\n' "$*" >&2; exit 1; }
require_command() { command -v "$1" >/dev/null 2>&1 || die "Required command is missing: $1"; }
require_file() { [[ -f "$1" ]] || die "Required file does not exist: $1"; }
