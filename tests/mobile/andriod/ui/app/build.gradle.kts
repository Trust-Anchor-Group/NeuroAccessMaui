import java.io.ByteArrayOutputStream
import javax.inject.Inject
import org.gradle.api.DefaultTask
import org.gradle.api.file.RegularFileProperty
import org.gradle.api.tasks.InputFile
import org.gradle.api.tasks.Exec
import org.gradle.api.tasks.TaskAction
import org.gradle.process.ExecOperations
import java.time.LocalDateTime
import java.time.format.DateTimeFormatter
import java.util.Properties

plugins {
    alias(libs.plugins.android.application)
}

val registrationUsernameTimestamp = LocalDateTime.now().format(DateTimeFormatter.ofPattern("yyyyMMddHHmmss"))
val testEnvironmentArguments = mapOf(
    "testPhoneNumber" to "NEUROACCESS_TEST_PHONE_NUMBER",
    "testPin" to "NEUROACCESS_TEST_PIN",
    "testNewPin" to "NEUROACCESS_TEST_NEW_PIN",
    "testOtpEndpoint" to "NEUROACCESS_TEST_OTP_ENDPOINT",
    "testQuickLoginPageUrl" to "NEUROACCESS_TEST_QUICK_LOGIN_PAGE_URL",
    "personalNumberAgeGroup" to "NEUROACCESS_TEST_PERSONAL_NUMBER_AGE_GROUP",
    "testSocialSecurityNumber" to "NEUROACCESS_TEST_SSN"
)
val dotenvProperties = Properties().apply {
    val dotenvFile = rootProject.file(".env")
    if (dotenvFile.isFile) {
        dotenvFile.inputStream().use { inputStream ->
            load(inputStream)
        }
    }
}



android {

    namespace = "com.tag.neuroaccess.neuroaccessespressoautomationtests"
	compileSdk {
		version = release(37)
	}

	defaultConfig {
        applicationId = "com.tag.neuroaccess.neuroaccessespressoautomationtests"
        minSdk = 23
        targetSdk = 37
        versionCode = 1
        versionName = "1.0"

        testInstrumentationRunner = "androidx.test.runner.AndroidJUnitRunner"
        testInstrumentationRunnerArguments["registrationUsernameTimestamp"] = registrationUsernameTimestamp
        testEnvironmentArguments.forEach { (argumentName, environmentVariableName) ->
            val argumentValue = providers.environmentVariable(environmentVariableName)
                .orNull
                ?: dotenvProperties.getProperty(environmentVariableName)

            argumentValue
                ?.trim()
                ?.removeSurrounding("\"")
                ?.removeSurrounding("'")
                ?.takeIf { value -> value.isNotBlank() }
                ?.let { value -> testInstrumentationRunnerArguments[argumentName] = value }
        }
    }

    signingConfigs {
        getByName("debug") {
            val localAppDataDirectory = System.getenv("LOCALAPPDATA")
                ?: error("LOCALAPPDATA must be set to locate the MAUI Android debug keystore.")
            storeFile = File(localAppDataDirectory, "Xamarin/Mono for Android/debug.keystore")
            storePassword = "android"
            keyAlias = "androiddebugkey"
            keyPassword = "android"
        }
    }
    buildTypes {
        getByName("debug") {
            signingConfig = signingConfigs.getByName("debug")
        }

        release {
            optimization {
                enable = false
            }
        }
    }
    compileOptions {
        sourceCompatibility = JavaVersion.VERSION_11
        targetCompatibility = JavaVersion.VERSION_11
    }

}

dependencies {
    implementation(libs.androidx.appcompat)
    implementation(libs.androidx.core.ktx)
    implementation(libs.material)
    testImplementation(libs.junit)
    androidTestImplementation(libs.androidx.espresso.core)
    androidTestImplementation(libs.androidx.espresso.intents)
    androidTestImplementation(libs.androidx.junit)
}
val mauiDebugApk = rootProject.file(
    "../../../../NeuroAccessMaui/bin/Debug/net10.0-android/com.tag.NeuroAccess-Signed.apk"
)

val packagedAndroidTestManifest = layout.buildDirectory.file(
    "intermediates/packaged_manifests/debugAndroidTest/processDebugAndroidTestManifest/AndroidManifest.xml"
)
val androidSdkProperties = Properties().apply {
    rootProject.file("local.properties").inputStream().use { inputStream ->
        this.load(inputStream)
    }
}

val androidSdkDirectory = File(
    checkNotNull(androidSdkProperties.getProperty("sdk.dir")) {
        "local.properties must define sdk.dir."
    }
)

tasks.configureEach {
    if (name != "processDebugAndroidTestManifest") return@configureEach
    outputs.upToDateWhen { false }

    doLast {
        val testManifest = packagedAndroidTestManifest.get().asFile
        val manifestContents = testManifest.readText()
        val testApplicationPackage = "com.tag.neuroaccess.neuroaccessespressoautomationtests"
        val mauiApplicationPackage = "com.tag.NeuroAccess"

        check(manifestContents.contains("android:targetPackage=\"$testApplicationPackage\"")) {
            "The generated instrumentation manifest does not contain the expected test target package."
        }

        testManifest.writeText(
            manifestContents.replace(
                "android:targetPackage=\"$testApplicationPackage\"",
                "android:targetPackage=\"$mauiApplicationPackage\""
            )
        )
    }
}

abstract class InstallMauiDebugApk @Inject constructor(
    private val execOperations: ExecOperations
) : DefaultTask() {
    @get:InputFile
    abstract val apkFile: RegularFileProperty

    @get:InputFile
    abstract val adbExecutableFile: RegularFileProperty

    @TaskAction
    fun install() {
        val devicesOutput = ByteArrayOutputStream()
        execOperations.exec {
            commandLine(adbExecutableFile.get().asFile.path, "devices")
            standardOutput = devicesOutput
        }.assertNormalExitValue()

        val deviceSerials = devicesOutput.toString()
            .lineSequence()
            .drop(1)
            .map(String::trim)
            .map { line -> line.split(Regex("\\s+")) }
            .filter { columns -> columns.size >= 2 && columns[1] == "device" }
            .map { columns -> columns[0] }
            .toList()

        check(deviceSerials.isNotEmpty()) {
            "No authorized Android devices or emulators are connected."
        }

        deviceSerials.forEach { deviceSerial ->
            execOperations.exec {
                commandLine(
                    adbExecutableFile.get().asFile.path,
                    "-s",
                    deviceSerial,
                    "install",
                    "-r",
                    apkFile.get().asFile.path
                )
            }.assertNormalExitValue()
        }
    }
}

tasks.register<InstallMauiDebugApk>("installMauiDebugApk") {
    apkFile.set(mauiDebugApk)
    adbExecutableFile.set(File(androidSdkDirectory, "platform-tools/adb.exe"))
}

tasks.register<Exec>("languageOptionsColdStartTest") {
    group = "verification"
    description = "Runs every language with cleared storage and a real force-stop before verification."
    dependsOn("assembleDebugAndroidTest")

    val adbExecutable = File(androidSdkDirectory, "platform-tools/adb.exe")
    val testApk = layout.buildDirectory.file("outputs/apk/androidTest/debug/app-debug-androidTest.apk")
    val runnerScript = rootProject.file("scripts/run-language-options-cold-start.ps1")
    val commonRunnerScript = rootProject.file("scripts/TestRunner.Common.ps1")
    val reportDirectory = layout.buildDirectory.dir("reports/androidTests/language-options-cold-start")

    inputs.file(runnerScript)
    inputs.file(commonRunnerScript)
    inputs.file(testApk)
    outputs.dir(reportDirectory)
    outputs.upToDateWhen { false }

    doFirst {
        val commandArguments = mutableListOf(
            "powershell.exe",
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            runnerScript.absolutePath,
            "-AdbPath",
            adbExecutable.absolutePath,
            "-TestApkPath",
            testApk.get().asFile.absolutePath,
            "-ReportDirectory",
            reportDirectory.get().asFile.absolutePath
        )
        providers.gradleProperty("deviceSerial").orNull?.let { deviceSerial ->
            commandArguments.add("-DeviceSerial")
            commandArguments.add(deviceSerial)
        }
        commandLine(commandArguments)
    }
}

tasks.register<Exec>("fullAndroidTestSuite") {
    group = "verification"
    description = "Runs all Android flows with explicit clean-state and dependent-state grouping."
    dependsOn("assembleDebugAndroidTest")

    val adbExecutable = File(androidSdkDirectory, "platform-tools/adb.exe")
    val testApk = layout.buildDirectory.file("outputs/apk/androidTest/debug/app-debug-androidTest.apk")
    val runnerScript = rootProject.file("scripts/run-full-test-suite.ps1")
    val commonRunnerScript = rootProject.file("scripts/TestRunner.Common.ps1")
    val reportDirectory = layout.buildDirectory.dir("reports/androidTests/full-suite")

    inputs.file(runnerScript)
    inputs.file(commonRunnerScript)
    inputs.file(testApk)
    outputs.dir(reportDirectory)
    outputs.upToDateWhen { false }

    doFirst {
        val commandArguments = mutableListOf(
            "powershell.exe",
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            runnerScript.absolutePath,
            "-AdbPath",
            adbExecutable.absolutePath,
            "-TestApkPath",
            testApk.get().asFile.absolutePath,
            "-ReportDirectory",
            reportDirectory.get().asFile.absolutePath
        )
        providers.gradleProperty("deviceSerial").orNull?.let { deviceSerial ->
            commandArguments.add("-DeviceSerial")
            commandArguments.add(deviceSerial)
        }

        testEnvironmentArguments.forEach { (_, environmentVariableName) ->
            val argumentValue = providers.environmentVariable(environmentVariableName)
                .orNull
                ?: dotenvProperties.getProperty(environmentVariableName)
            argumentValue
                ?.trim()
                ?.removeSurrounding("\"")
                ?.removeSurrounding("'")
                ?.takeIf { value -> value.isNotBlank() }
                ?.let { value -> environment(environmentVariableName, value) }
        }
        environment(
            "NEUROACCESS_REGISTRATION_USERNAME_TIMESTAMP",
            registrationUsernameTimestamp
        )
        commandLine(commandArguments)
    }
}

tasks.register<Exec>("mutualContactsTest") {
    group = "verification"
    description = "Adds contacts on two Android devices, verifies the saved identities and exchanges unique chat messages."
    dependsOn("assembleDebugAndroidTest")
    val runnerScript = rootProject.file("scripts/run-mutual-contacts-test.ps1")
    val testApk = layout.buildDirectory.file("outputs/apk/androidTest/debug/app-debug-androidTest.apk")
    val reportDirectory = layout.buildDirectory.dir("reports/androidTests/mutual-contacts")
    outputs.upToDateWhen { false }
    doFirst {
        fun localSetting(propertyName: String, environmentName: String): String? =
            (providers.gradleProperty(propertyName).orNull
                ?: providers.environmentVariable(environmentName).orNull
                ?: dotenvProperties.getProperty(environmentName))
                ?.trim()?.removeSurrounding("\"")?.removeSurrounding("'")
                ?.takeIf { it.isNotBlank() }

        val serialA = localSetting("deviceSerialA", "NEUROACCESS_TEST_DEVICE_SERIAL_A")
        val serialB = localSetting("deviceSerialB", "NEUROACCESS_TEST_DEVICE_SERIAL_B")
        require(!serialA.isNullOrBlank() && !serialB.isNullOrBlank() && serialA != serialB) {
            "Specify two different Android devices with -PdeviceSerialA/-PdeviceSerialB or the corresponding .env settings."
        }
        val adbHost = localSetting("adbServerHost", "NEUROACCESS_TEST_ADB_SERVER_HOST") ?: "127.0.0.1"
        val adbPort = localSetting("adbServerPort", "NEUROACCESS_TEST_ADB_SERVER_PORT") ?: "5037"
        require(adbHost.matches(Regex("[A-Za-z0-9_.-]+"))) { "Invalid adbServerHost." }
        require(adbPort.toIntOrNull()?.let { it in 1..65535 } == true) { "Invalid adbServerPort." }
        // Includes the PowerShell runner and its parallel Start-Job workers.
        setEnvironment(environment.filterKeys { !it.equals("ANDROID_SERIAL", ignoreCase = true) })
        environment("ADB_SERVER_SOCKET", "tcp:$adbHost:$adbPort")
        environment("ANDROID_ADB_SERVER_ADDRESS", adbHost)
        environment("ANDROID_ADB_SERVER_PORT", adbPort)
        logger.lifecycle("Mutual contacts: using configured ADB server; A=$serialA; B=$serialB")
        listOf("NEUROACCESS_TEST_PIN_A", "NEUROACCESS_TEST_PIN_B").forEach { name ->
            val value = (providers.environmentVariable(name).orNull ?: dotenvProperties.getProperty(name))
                ?.trim()?.removeSurrounding("\"")?.removeSurrounding("'")
            require(value != null && value.matches(Regex("[0-9]{6}"))) { "Set $name in .env to the current six-digit PIN." }
            environment(name, value)
        }
        commandLine("powershell.exe", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", runnerScript.absolutePath,
            "-AdbPath", File(androidSdkDirectory, "platform-tools/adb.exe").absolutePath,
            "-TestApkPath", testApk.get().asFile.absolutePath,
            "-ReportDirectory", reportDirectory.get().asFile.absolutePath,
            "-DeviceSerialA", serialA, "-DeviceSerialB", serialB)
    }
}

tasks.register<Exec>("changePinTest") {
    group = "options"
    description = "Runs only the Change PIN flow while preserving the existing account and identity state."
    dependsOn("assembleDebugAndroidTest")

    val adbExecutable = File(androidSdkDirectory, "platform-tools/adb.exe")
    val testApk = layout.buildDirectory.file("outputs/apk/androidTest/debug/app-debug-androidTest.apk")
    val runnerScript = rootProject.file("scripts/run-change-pin-test.ps1")
    val commonRunnerScript = rootProject.file("scripts/TestRunner.Common.ps1")
    val reportDirectory = layout.buildDirectory.dir("reports/androidTests/change-pin")

    inputs.file(runnerScript)
    inputs.file(commonRunnerScript)
    inputs.file(testApk)
    outputs.dir(reportDirectory)
    outputs.upToDateWhen { false }

    doFirst {
        val commandArguments = mutableListOf(
            "powershell.exe",
            "-NoProfile",
            "-ExecutionPolicy",
            "Bypass",
            "-File",
            runnerScript.absolutePath,
            "-AdbPath",
            adbExecutable.absolutePath,
            "-TestApkPath",
            testApk.get().asFile.absolutePath,
            "-ReportDirectory",
            reportDirectory.get().asFile.absolutePath
        )
        providers.gradleProperty("deviceSerial").orNull?.let { deviceSerial ->
            commandArguments.add("-DeviceSerial")
            commandArguments.add(deviceSerial)
        }

        listOf("NEUROACCESS_TEST_PIN", "NEUROACCESS_TEST_NEW_PIN").forEach { environmentVariableName ->
            val argumentValue = providers.environmentVariable(environmentVariableName)
                .orNull
                ?: dotenvProperties.getProperty(environmentVariableName)
            argumentValue
                ?.trim()
                ?.removeSurrounding("\"")
                ?.removeSurrounding("'")
                ?.takeIf { value -> value.isNotBlank() }
                ?.let { value -> environment(environmentVariableName, value) }
        }
        commandLine(commandArguments)
    }
}
