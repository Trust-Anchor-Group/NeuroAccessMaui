# VM Test Infrastructure

This directory contains the infrastructure used to run Android regression tests on a dedicated VM.

## Overview

The basic flow is:

```text
Developer machine
    ↓
Build MAUI APK
    ↓
Build Espresso tests
    ↓
Upload via SSH/SCP
    ↓
Test VM
    ↓
Start Android emulators
    ↓
Install app + test APK
    ↓
Run Espresso tests
    ↓
Collect logs + test results
    ↓
Download results
```

The VM acts as a permanent Android test runner and hosts the Android SDK, emulators, and AVD profiles.

## Structure

```text
vm/
├── config/
│   ├── devices/
│   │   ├── android/
│   │   ├── samsung/
│   │   ├── huawei/
│   │   └── xiaomi/
│   ├── test-matrix.yaml
│   └── toolchain.env
│
├── setup/
│   ├── setup-vm.sh
│   ├── install-android-sdk.sh
│   ├── create-avds.sh
│   └── verify-vm.sh
│
├── runner/
│   ├── run-tests.sh
│   ├── run-android-pair.sh
│   ├── start-emulator.sh
│   ├── wait-for-android.sh
│   ├── collect-results.sh
│   └── cleanup.sh
│
└── client/
    └── run-remote-tests.ps1
```

## Configuration

`config/devices/` defines the Android versions and device types that can be used by the test infrastructure.

Generic Android emulator definitions live under:

```text
config/devices/android/
```

Vendor-specific devices can later be added under directories such as:

```text
config/devices/samsung/
config/devices/huawei/
config/devices/xiaomi/
```

Android versions, API levels, system images, and device profiles should be defined in configuration files and must not be hardcoded in the runner scripts.

`test-matrix.yaml` defines which combinations should be executed, for example:

```text
smoke
regression
full
```

## VM Runtime

The VM uses a separate runtime structure, for example:

```text
/opt/neuro-test/
├── repo/
├── android-sdk/
├── avds/
├── incoming/
├── runs/
└── tmp/
```

Generated APK files, emulator data, logs, and test results must not be committed to Git.

## Submit and activate a run remotely

The Windows client uploads APKs into a temporary directory. It only moves that directory into the incoming queue after all files have arrived, then activates the Linux queue worker over SSH. The worker holds a `flock` lock while processing the queue, so scenarios remain sequential when several runs are submitted.

```powershell
./android/client/run-remote-tests.ps1 `
    -HostName android-test-host `
    -AppApk ./com.tag.NeuroAccess-Signed.apk `
    -TestApk ./app-debug-androidTest.apk `
    -Serial emulator-5554
```

The checkout is expected at `/opt/neuro-test/repo` and runtime data below `/opt/neuro-test`. Use `-RemoteRoot` when the VM uses another location. The SSH user needs write access to `incoming`, `queue`, and `runs`.

All Android operations use an explicit adb serial. `runner/run-tests.sh` executes a single-device scenario. `runner/run-android-pair.sh` accepts two distinct serials and exposes them to a host-side coordinator as `NEURO_DEVICE_A` and `NEURO_DEVICE_B`. This permits two active emulators within one scenario while the scenario queue itself stays sequential.

## Test Results

Each test execution gets its own run ID and can collect:

```text
JUnit results
HTML reports
Gradle output
Device A logcat
Device B logcat
Emulator logs
Screenshots when needed
```

The goal is to allow the same regression test suite to run against multiple Android versions, screen sizes, and later vendor-specific devices without changing the test code.
