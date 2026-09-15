#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"
runtime_root="${NEURO_TEST_ROOT:-/opt/neuro-test}"
mkdir -p "$runtime_root/queue" "$runtime_root/runs"
exec 9>"$runtime_root/queue.lock"
flock -n 9 || exit 0

while true; do
    queued_job="$(find "$runtime_root/queue" -mindepth 1 -maxdepth 1 -type d -printf '%T@ %p\n' | sort -n | head -n 1 | cut -d' ' -f2-)"
    [[ -n "$queued_job" ]] || exit 0
    job_id="$(basename -- "$queued_job")"; require_safe_id "$job_id"
    run_dir="$runtime_root/runs/$job_id"
    mv -- "$queued_job" "$run_dir"
    printf 'running\n' >"$run_dir/status"
    set +e
    (
        set -Eeuo pipefail
        source <(tr -d '\r' <"$run_dir/job.env")
        [[ "${MODE:-}" == "single" ]] || die "Unsupported queued mode: ${MODE:-}"
        [[ -n "${SERIAL:-}" ]] || die "SERIAL is required"
        args=(--serial "$SERIAL" --app "$run_dir/app.apk" --tests "$run_dir/tests.apk" --results "$run_dir/results")
        [[ -z "${TEST_CLASS:-}" ]] || args+=(--class "$TEST_CLASS")
        [[ -z "${TEST_PHONE_NUMBER:-}" ]] || args+=(--arg "testPhoneNumber=$TEST_PHONE_NUMBER")
        [[ -z "${TEST_PIN:-}" ]] || args+=(--arg "testPin=$TEST_PIN")
        [[ -z "${TEST_OTP_ENDPOINT:-}" ]] || args+=(--arg "testOtpEndpoint=$TEST_OTP_ENDPOINT")
        [[ -z "${REGISTRATION_USERNAME_TIMESTAMP:-}" ]] || args+=(--arg "registrationUsernameTimestamp=$REGISTRATION_USERNAME_TIMESTAMP")
        bash "$SCRIPT_DIR/run-tests.sh" "${args[@]}"
    ) >"$run_dir/run.log" 2>&1
    exit_code=$?
    set -e
    printf '%s\n' "$exit_code" >"$run_dir/exit-code.txt"
    if ((exit_code == 0)); then printf 'passed\n' >"$run_dir/status"; else printf 'failed\n' >"$run_dir/status"; fi
done
