#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"

matrix_file=""; app_apk=""; app_x86_apk=""; app_x86_64_apk=""; test_apk=""; results_dir=""; stop_on_failure=false
instrumentation_args=()
while (($#)); do
    case "$1" in
        --matrix) matrix_file="${2:-}"; shift 2 ;;
        --app) app_apk="${2:-}"; shift 2 ;;
        --app-x86) app_x86_apk="${2:-}"; shift 2 ;;
        --app-x86-64) app_x86_64_apk="${2:-}"; shift 2 ;;
        --tests) test_apk="${2:-}"; shift 2 ;;
        --results) results_dir="${2:-}"; shift 2 ;;
        --arg) instrumentation_args+=("${2:-}"); shift 2 ;;
        --stop-on-failure) stop_on_failure=true; shift ;;
        *) die "Unknown device matrix argument: $1" ;;
    esac
done
require_file "$matrix_file"; require_file "$test_apk"
[[ -z "$app_apk" ]] || require_file "$app_apk"
[[ -z "$app_x86_apk" ]] || require_file "$app_x86_apk"
[[ -z "$app_x86_64_apk" ]] || require_file "$app_x86_64_apk"
[[ -n "$results_dir" ]] || die "A matrix results directory is required"
mkdir -p "$results_dir/scenarios"
events_log="$results_dir/matrix.log"
summary_tsv="$results_dir/summary.tsv"
summary_txt="$results_dir/summary.txt"
printf 'scenario\tapi_level\tdevice_profile\tabi\tstorage_policy\tstatus\tduration_seconds\n' >"$summary_tsv"
log_event() {
    printf '%s %s\n' "$(date -u +%Y-%m-%dT%H:%M:%SZ)" "$*" | tee -a "$events_log"
}

base_phone_number=""
shared_instrumentation_args=()
for argument in "${instrumentation_args[@]}"; do
    case "$argument" in
        testPhoneNumber=*) base_phone_number="${argument#*=}" ;;
        registrationUsernameTimestamp=*) ;;
        *) shared_instrumentation_args+=("$argument") ;;
    esac
done
passed=0; failed=0; scenario_index=0
expected_scenarios="$(tr -d '\r' <"$matrix_file" | awk 'NF {count++} END {print count+0}')"
matrix_started=$SECONDS
while IFS=$'\t' read -r name api_level device_profile image_flavor abi show_emulator storage_policy test_class; do
    [[ -n "$name" ]] || continue
    require_safe_id "$name"
    [[ "$api_level" =~ ^[0-9]{2}$ ]] || die "Invalid API level in matrix scenario $name"
    [[ "$device_profile" =~ ^[A-Za-z0-9._-]+$ ]] || die "Invalid device profile in matrix scenario $name"
    [[ "$image_flavor" =~ ^[A-Za-z0-9._-]+$ && "$abi" =~ ^[A-Za-z0-9._-]+$ ]] || die "Invalid system image in matrix scenario $name"
    [[ "$show_emulator" == "true" || "$show_emulator" == "false" ]] || die "Invalid show_emulator value in $name"
    [[ "$storage_policy" == "cache" || "$storage_policy" == "delete" ]] || die "Invalid storage policy in $name"
    [[ -z "$test_class" || "$test_class" =~ ^[A-Za-z0-9_.$#,-]+$ ]] || die "Invalid test class in $name"

    scenario_index=$((scenario_index + 1))
    case "$abi" in
        x86) scenario_app_apk="${app_x86_apk:-$app_apk}" ;;
        x86_64) scenario_app_apk="${app_x86_64_apk:-$app_apk}" ;;
        *) scenario_app_apk="$app_apk" ;;
    esac
    [[ -n "$scenario_app_apk" ]] || die "No app APK was supplied for ABI $abi in scenario $name"
    require_file "$scenario_app_apk"
    scenario_dir="$results_dir/scenarios/$name"
    mkdir -p "$scenario_dir"
    args=(--api "$api_level" --device "$device_profile" --image "$image_flavor" --abi "$abi"
        --storage-policy "$storage_policy" --app "$scenario_app_apk" --tests "$test_apk" --results "$scenario_dir")
    [[ "$show_emulator" == "false" ]] || args+=(--show-window)
    [[ -z "$test_class" ]] || args+=(--class "$test_class")
    for argument in "${shared_instrumentation_args[@]}"; do args+=(--arg "$argument"); done
    if [[ "$test_class" == *.onboarding.RegistrationFlowTest ]]; then
        [[ "$base_phone_number" =~ ^[0-9]+$ ]] || die "Registration matrix requires a numeric base test phone number"
        phone_width=${#base_phone_number}
        phone_value=$((10#$base_phone_number + scenario_index - 1))
        printf -v scenario_phone_number "%0${phone_width}d" "$phone_value"
        args+=(--arg "testPhoneNumber=$scenario_phone_number"
            --arg "registrationUsernameTimestamp=$(date -u +%Y%m%d%H%M%S)")
    elif [[ -n "$base_phone_number" ]]; then
        args+=(--arg "testPhoneNumber=$base_phone_number")
    fi
    args+=(--arg "matrixScenario=$name" --arg "matrixScenarioIndex=$scenario_index")

    log_event "[$name] START api=$api_level profile=$device_profile image=$image_flavor/$abi storage=$storage_policy"
    scenario_started=$SECONDS
    set +e
    bash "$SCRIPT_DIR/run-managed-tests.sh" "${args[@]}" </dev/null >"$scenario_dir/scenario.log" 2>&1
    scenario_exit=$?
    set -e
    duration=$((SECONDS - scenario_started))
    if ((scenario_exit == 0)); then
        status=passed; passed=$((passed + 1))
    else
        status=failed; failed=$((failed + 1))
    fi
    printf '%s\t%s\t%s\t%s\t%s\t%s\t%s\n' "$name" "$api_level" "$device_profile" "$abi" "$storage_policy" "$status" "$duration" >>"$summary_tsv"
    log_event "[$name] END status=$status duration=${duration}s"
    if [[ "$status" == "failed" && "$stop_on_failure" == true ]]; then break; fi
done < <(tr -d '\r' <"$matrix_file")

if ((scenario_index != expected_scenarios)) && [[ "$stop_on_failure" != true ]]; then
    failed=$((failed + 1))
    log_event "MATRIX ERROR expected=$expected_scenarios executed=$scenario_index"
fi
total_duration=$((SECONDS - matrix_started))
{
    printf 'Device matrix summary\n'
    printf 'Passed: %s\n' "$passed"
    printf 'Failed: %s\n' "$failed"
    printf 'Duration: %ss\n\n' "$total_duration"
    awk -F '\t' 'NR > 1 {printf "%s %s (API %s, %s, %s, %ss)\n", toupper($6), $1, $2, $3, $4, $7}' "$summary_tsv"
} >"$summary_txt"
log_event "MATRIX END passed=$passed failed=$failed duration=${total_duration}s"
((failed == 0))