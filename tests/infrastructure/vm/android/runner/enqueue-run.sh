#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"
runtime_root="${NEURO_TEST_ROOT:-/opt/neuro-test}"
job_id="${1:-}"
require_safe_id "$job_id"
incoming_job="$runtime_root/incoming/$job_id"
queued_job="$runtime_root/queue/$job_id"
require_file "$incoming_job/job.env"; require_file "$incoming_job/tests.apk"
if [[ ! -f "$incoming_job/app.apk" && ! -f "$incoming_job/app-x86.apk" && ! -f "$incoming_job/app-x86_64.apk" ]]; then
    die "The job does not contain an app APK"
fi
mkdir -p "$runtime_root/queue" "$runtime_root/runs"
[[ ! -e "$queued_job" && ! -e "$runtime_root/runs/$job_id" ]] || die "Job already exists: $job_id"
mv -- "$incoming_job" "$queued_job"
nohup bash "$SCRIPT_DIR/process-queue.sh" >/dev/null 2>&1 &
printf '%s\n' "$job_id"
