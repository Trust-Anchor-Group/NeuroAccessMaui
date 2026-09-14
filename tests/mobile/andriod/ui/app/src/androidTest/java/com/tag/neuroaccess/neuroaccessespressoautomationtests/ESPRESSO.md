# Espresso Test Guide

This guide explains how to open, configure, and run the Android UI tests with Espresso.

## Prerequisites

The following must be installed:

* Android Studio
* Android SDK
* JDK
* An Android emulator or physical Android device

## Open the project

The test project is located at:

```text
C:\dev\Neuro-Access\NeuroAccessMaui\tests\mobile\andriod\ui\app
```

### Android Studio

1. Open **Android Studio**.
2. Select **Open**.
3. Navigate to:

```text
C:\dev\Neuro-Access\NeuroAccessMaui\tests\mobile\andriod\ui\app
```

4. Open the project.
5. Wait for the Gradle sync to complete.

> Open the Android project that contains the `app` module. The Espresso tests are located under `src/androidTest`.

## Where are the Espresso tests?

The tests are located under:

```text
app/src/androidTest/java/com/tag/neuroaccess/neuroaccessespressoautomationtests
```

Example:

```text
app/
└── src/
    └── androidTest/
        └── java/
            └── com/
                └── tag/
                    └── neuroaccess/
                        └── neuroaccessespressoautomationtests/
```

## Run tests from Android Studio

### Run a single test

1. Open the test class.
2. Locate the test method.
3. Click the green ▶ icon next to the test.
4. Select an emulator or connected Android device.

### Run an entire test class

Click ▶ next to the class name.

### Run all Android UI tests

Right-click:

```text
src/androidTest
```

or the test package:

```text
com.tag.neuroaccess.neuroaccessespressoautomationtests
```

and select:

```text
Run Tests
```

## Run tests with Gradle

Android instrumentation tests can also be run with Gradle.

Example:

```bash
./gradlew connectedAndroidTest
```

On Windows, use:

```powershell
gradlew.bat connectedAndroidTest
```

This requires an available Android device or emulator.

## Emulator

An Android device or emulator is required to run Espresso tests.

In Android Studio, open:

```text
Device Manager
```

Start the emulator you want to use before running the tests.

## Espresso

Espresso is used to find and interact with UI elements.

Example:

```kotlin
onView(AutomationIdMatcher.withAutomationId("input_username"))
    .perform(typeText("testuser"))
```

Click a button:

```kotlin
onView(AutomationIdMatcher.withAutomationId("button_continue_username"))
    .perform(click())
```

Verify that a screen is displayed:

```kotlin
onView(AutomationIdMatcher.withAutomationId("screen_username"))
    .check(matches(isDisplayed()))
```

## UI IDs

The tests should primarily use stable UI IDs instead of searching for visible text.

All available IDs are listed in:

```text
UI_IDS.md
```

Example:

```text
screen_username
input_username
button_continue_username
button_back_username
```

## Troubleshooting

### The test cannot find the UI element

Verify:

1. That the correct screen is displayed.
2. That the UI element has the correct ID.
3. That the ID matches `UI_IDS.md`.
4. That the test is running against the correct version of the application.
5. That the emulator or device is connected correctly.

### The emulator is not detected

Run:

```bash
adb devices
```

A connected device should appear in the list.

Example:

```text
List of devices attached
emulator-5554    device
```

### Gradle issues

If the project has just been opened in Android Studio, run a Gradle sync first:

```text
File → Sync Project with Gradle Files
```
