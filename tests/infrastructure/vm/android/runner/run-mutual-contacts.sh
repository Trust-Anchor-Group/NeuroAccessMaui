#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
source "$SCRIPT_DIR/lib/common.sh"
serial_a="${NEURO_DEVICE_A:-}"; serial_b="${NEURO_DEVICE_B:-}"; results_dir="${NEURO_RESULTS_DIR:-}"
pin_a=""; pin_b=""
while (($#)); do
    case "$1" in
        --pin-a=*) pin_a="${1#*=}"; shift ;;
        --pin-b=*) pin_b="${1#*=}"; shift ;;
        *) die "Unknown mutual contacts argument: $1" ;;
    esac
done
[[ -n "$serial_a" && -n "$serial_b" && -n "$results_dir" ]] || die "Pair runner environment is incomplete"
[[ "$pin_a" =~ ^[0-9]{6}$ && "$pin_b" =~ ^[0-9]{6}$ ]] || die "Both contact accounts require a six-digit PIN"
require_command adb; require_command base64; require_command od; require_command timeout

test_selector="com.tag.neuroaccess.neuroaccessespressoautomationtests.contacts.MutualContactsFlowTest#runDevicePhase"
test_runner="com.tag.neuroaccess.neuroaccessespressoautomationtests.test/androidx.test.runner.AndroidJUnitRunner"
temporary_dir="$(mktemp -d)"
cleanup() {
    local active_pids=()
    mapfile -t active_pids < <(jobs -pr)
    ((${#active_pids[@]} == 0)) || kill "${active_pids[@]}" 2>/dev/null || true
    rm -rf -- "$temporary_dir"
}
trap cleanup EXIT
phase_output() { printf '%s/%s-%s.txt' "$temporary_dir" "$1" "$2"; }
encode_value() { printf '%s' "$1" | base64 | tr -d '\n'; }

run_phase() {
    local role="$1" serial="$2" pin="$3" phase="$4" output_file="$5"
    shift 5
    local arguments=(shell am instrument -w -r -e class "$test_selector" -e contactsCoordinator true -e contactsPhase "$phase" -e testPin "$pin")
    local pair
    for pair in "$@"; do
        [[ "$pair" == *=* ]] || die "Coordinator value must use KEY=VALUE"
        arguments+=(-e "${pair%%=*}" "${pair#*=}")
    done
    arguments+=("$test_runner")
    adb_for "$serial" shell am force-stop com.tag.NeuroAccess >/dev/null 2>&1 || true
    if ! timeout 300 adb -s "$serial" "${arguments[@]}" >"$output_file" 2>&1; then
        sed -E 's/^(INSTRUMENTATION_STATUS: (contactIdentity|contactLink|petitionState|contactAdded|contactVerified|chatVerified)=).*/\1[REDACTED]/' "$output_file" >&2
        return 1
    fi
    if ! grep -Eq '^OK \(1 test\)\r?$' "$output_file" || grep -Eq 'FAILURES!!!|INSTRUMENTATION_FAILED|INSTRUMENTATION_ABORTED' "$output_file"; then
        sed -E 's/^(INSTRUMENTATION_STATUS: (contactIdentity|contactLink|petitionState|contactAdded|contactVerified|chatVerified)=).*/\1[REDACTED]/' "$output_file" >&2
        return 1
    fi
    printf '[%s / %s] %s passed\n' "$role" "$serial" "$phase"
}

read_result() {
    local output_file="$1" key="$2"; local matches=()
    mapfile -t matches < <(sed -nE "s/^INSTRUMENTATION_STATUS: ${key}=([A-Za-z0-9+\/=]+)\r?$/\\1/p" "$output_file")
    ((${#matches[@]} == 1)) || die "Expected one $key result from device phase"
    printf '%s' "${matches[0]}"
}
read_identity() {
    local encoded="$1" identity
    identity="$(printf '%s' "$encoded" | base64 --decode 2>/dev/null)" || die "Device returned an invalid encoded identity"
    [[ "$identity" =~ ^[A-Za-z0-9-]+@[A-Za-z0-9.-]+$ ]] || die "Device returned an unexpected Neuro-ID"
    printf '%s' "$identity"
}
wait_for_pair() {
    local pid_a="$1" pid_b="$2" status_a status_b
    set +e; wait "$pid_a"; status_a=$?; wait "$pid_b"; status_b=$?; set -e
    ((status_a == 0 && status_b == 0)) || die "A coordinated contacts phase failed"
}
wait_for_test_start() {
    local output_file="$1" deadline=$((SECONDS + 30))
    while ((SECONDS < deadline)); do
        grep -Eq '^INSTRUMENTATION_STATUS_CODE: 1\r?$' "$output_file" 2>/dev/null && return 0
        sleep 1
    done
    die "Timed out waiting for the receiving device test to start"
}
wait_for_result() {
    local output_file="$1" key="$2" deadline=$((SECONDS + 90))
    while ((SECONDS < deadline)); do
        grep -Eq "^INSTRUMENTATION_STATUS: ${key}=" "$output_file" 2>/dev/null && return 0
        sleep 1
    done
    die "Timed out waiting for $key from the sending device"
}

identify_a="$(phase_output a identify)"; identify_b="$(phase_output b identify)"
run_phase A "$serial_a" "$pin_a" identify "$identify_a"
run_phase B "$serial_b" "$pin_b" identify "$identify_b"
identity_a="$(read_identity "$(read_result "$identify_a" contactIdentity)")"
identity_b="$(read_identity "$(read_result "$identify_b" contactIdentity)")"
[[ "$identity_a" != "$identity_b" ]] || die "Both devices use the same Neuro-ID"
printf 'Precheck passed: two different approved identities found\n'

export_a="$(phase_output a export)"; run_phase A "$serial_a" "$pin_a" export "$export_a" "ownIdentity=$identity_a"
link_a="$(read_result "$export_a" contactLink)"
petition_b="$(phase_output b petition-a)"; accept_a="$(phase_output a accept)"; add_b="$(phase_output b add-a)"
run_phase A "$serial_a" "$pin_a" accept "$accept_a" & pid_a=$!
wait_for_test_start "$accept_a"
run_phase B "$serial_b" "$pin_b" sendPetition "$petition_b" "ownIdentity=$identity_b" "peerIdentity=$identity_a" "peerLink=$link_a"
petition_state="$(read_result "$petition_b" petitionState)"
if [[ "$petition_state" == "$(encode_value sent)" ]]; then
    set +e; wait "$pid_a"; status_a=$?; set -e
    ((status_a == 0)) || die "Device A did not accept and save B's contact request"
elif [[ "$petition_state" == "$(encode_value approved)" ]]; then
    kill "$pid_a" 2>/dev/null || true
    adb_for "$serial_a" shell am force-stop com.tag.NeuroAccess >/dev/null 2>&1 || true
    wait "$pid_a" 2>/dev/null || true
    export_b="$(phase_output b export)"
    run_phase B "$serial_b" "$pin_b" export "$export_b" "ownIdentity=$identity_b"
    link_b="$(read_result "$export_b" contactLink)"
    add_a="$(phase_output a add-b)"
    run_phase A "$serial_a" "$pin_a" addAccepted "$add_a" "ownIdentity=$identity_a" "peerIdentity=$identity_b" "peerLink=$link_b"
    [[ "$(read_result "$add_a" contactAdded)" == "$(encode_value "$identity_b")" ]] || die "A did not confirm B as a contact"
else
    die "B returned an unexpected petition state"
fi
run_phase B "$serial_b" "$pin_b" addAccepted "$add_b" "ownIdentity=$identity_b" "peerIdentity=$identity_a" "peerLink=$link_a"
[[ "$(read_result "$add_b" contactAdded)" == "$(encode_value "$identity_a")" ]] || die "B did not confirm A as a contact"

verify_a="$(phase_output a verify)"; verify_b="$(phase_output b verify)"
run_phase A "$serial_a" "$pin_a" verifyContact "$verify_a" "ownIdentity=$identity_a" "peerIdentity=$identity_b"
run_phase B "$serial_b" "$pin_b" verifyContact "$verify_b" "ownIdentity=$identity_b" "peerIdentity=$identity_a"
[[ "$(read_result "$verify_a" contactVerified)" == "$(encode_value "$identity_b")" ]] || die "A could not verify B"
[[ "$(read_result "$verify_b" contactVerified)" == "$(encode_value "$identity_a")" ]] || die "B could not verify A"
printf 'Contacts passed: both devices verified their saved peer\n'

chat_run_id="$(od -An -N16 -tx1 /dev/urandom | tr -d ' \n')"
chat_a="$(phase_output a chat)"; chat_b="$(phase_output b chat)"
run_phase B "$serial_b" "$pin_b" chatResponder "$chat_b" "ownIdentity=$identity_b" "peerIdentity=$identity_a" "chatRunId=$chat_run_id" & pid_b=$!
run_phase A "$serial_a" "$pin_a" chatInitiator "$chat_a" "ownIdentity=$identity_a" "peerIdentity=$identity_b" "chatRunId=$chat_run_id" & pid_a=$!
set +e; wait "$pid_a"; status_a=$?; set -e
((status_a == 0)) || die "Device A did not complete the chat exchange"
adb_for "$serial_b" shell run-as com.tag.NeuroAccess touch "cache/mutual-chat-$chat_run_id.release" >/dev/null
set +e; wait "$pid_b"; status_b=$?; set -e
((status_b == 0)) || die "Device B did not complete the chat exchange"
expected_chat="$(encode_value "$chat_run_id")"
[[ "$(read_result "$chat_a" chatVerified)" == "$expected_chat" ]] || die "Device A did not confirm the chat run"
[[ "$(read_result "$chat_b" chatVerified)" == "$expected_chat" ]] || die "Device B did not confirm the chat run"
printf 'Mutual contacts and chat passed on both devices\n'
