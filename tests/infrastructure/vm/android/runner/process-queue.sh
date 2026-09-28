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
        case "${MODE:-}" in
            single)
                common_args=(--app "$run_dir/app.apk" --tests "$run_dir/tests.apk" --results "$run_dir/results")
                [[ -z "${TEST_CLASS:-}" ]] || common_args+=(--class "$TEST_CLASS")
                [[ -z "${TEST_PHONE_NUMBER:-}" ]] || common_args+=(--arg "testPhoneNumber=$TEST_PHONE_NUMBER")
                [[ -z "${TEST_PIN:-}" ]] || common_args+=(--arg "testPin=$TEST_PIN")
                [[ -z "${TEST_OTP_ENDPOINT:-}" ]] || common_args+=(--arg "testOtpEndpoint=$TEST_OTP_ENDPOINT")
                [[ -z "${REGISTRATION_USERNAME_TIMESTAMP:-}" ]] || common_args+=(--arg "registrationUsernameTimestamp=$REGISTRATION_USERNAME_TIMESTAMP")
                if [[ "${DEVICE_MODE:-existing}" == "managed" ]]; then
                    [[ "${API_LEVEL:-}" =~ ^[0-9]{2}$ ]] || die "API_LEVEL is required for a managed emulator"
                    managed_args=(--api "$API_LEVEL" --device "${DEVICE_PROFILE:-pixel_6}"
                        --image "${SYSTEM_IMAGE:-google_apis}" --abi "${SYSTEM_IMAGE_ABI:-x86_64}"
                        --storage-policy "${STORAGE_POLICY:-cache}")
                    [[ -z "${AVD_NAME:-}" ]] || managed_args+=(--name "$AVD_NAME")
                    [[ "${SHOW_EMULATOR:-false}" == "true" ]] && managed_args+=(--show-window)
                    bash "$SCRIPT_DIR/run-managed-tests.sh" "${managed_args[@]}" "${common_args[@]}"
                else
                    [[ -n "${SERIAL:-}" ]] || die "SERIAL is required for an existing device"
                    bash "$SCRIPT_DIR/run-tests.sh" --serial "$SERIAL" "${common_args[@]}"
                fi
                ;;
            matrix)
                require_file "$run_dir/matrix.tsv"
                matrix_args=(--matrix "$run_dir/matrix.tsv" --tests "$run_dir/tests.apk" --results "$run_dir/results")
                [[ ! -f "$run_dir/app.apk" ]] || matrix_args+=(--app "$run_dir/app.apk")
                [[ ! -f "$run_dir/app-x86.apk" ]] || matrix_args+=(--app-x86 "$run_dir/app-x86.apk")
                [[ ! -f "$run_dir/app-x86_64.apk" ]] || matrix_args+=(--app-x86-64 "$run_dir/app-x86_64.apk")
                [[ "${STOP_ON_FAILURE:-false}" == "true" ]] && matrix_args+=(--stop-on-failure)
                [[ -z "${TEST_PHONE_NUMBER:-}" ]] || matrix_args+=(--arg "testPhoneNumber=$TEST_PHONE_NUMBER")
                [[ -z "${TEST_PIN:-}" ]] || matrix_args+=(--arg "testPin=$TEST_PIN")
                [[ -z "${TEST_OTP_ENDPOINT:-}" ]] || matrix_args+=(--arg "testOtpEndpoint=$TEST_OTP_ENDPOINT")
                [[ -z "${REGISTRATION_USERNAME_TIMESTAMP:-}" ]] || matrix_args+=(--arg "registrationUsernameTimestamp=$REGISTRATION_USERNAME_TIMESTAMP")
                bash "$SCRIPT_DIR/run-device-matrix.sh" "${matrix_args[@]}"
                ;;
            mutual-contacts)
                [[ -n "${SERIAL_A:-}" && -n "${SERIAL_B:-}" ]] || die "SERIAL_A and SERIAL_B are required"
                [[ -n "${TEST_PIN_A:-}" && -n "${TEST_PIN_B:-}" ]] || die "TEST_PIN_A and TEST_PIN_B are required"
                bash "$SCRIPT_DIR/run-android-pair.sh" \
                    --serial-a "$SERIAL_A" --serial-b "$SERIAL_B" \
                    --app "$run_dir/app.apk" --tests "$run_dir/tests.apk" \
                    --results "$run_dir/results" --coordinator "$SCRIPT_DIR/run-mutual-contacts.sh" \
                    --arg "--pin-a=$TEST_PIN_A" --arg "--pin-b=$TEST_PIN_B"
                ;;
            *) die "Unsupported queued mode: ${MODE:-}" ;;
        esac
    ) >"$run_dir/run.log" 2>&1
    exit_code=$?
    set -e
    printf '%s\n' "$exit_code" >"$run_dir/exit-code.txt"
    if ((exit_code == 0)); then printf 'passed\n' >"$run_dir/status"; else printf 'failed\n' >"$run_dir/status"; fi
done
