import java.io.ByteArrayOutputStream
import javax.inject.Inject
import org.gradle.api.DefaultTask
import org.gradle.api.file.RegularFileProperty
import org.gradle.api.tasks.InputFile
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
    "testOtpEndpoint" to "NEUROACCESS_TEST_OTP_ENDPOINT"
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
        minSdk = 25
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
tasks.configureEach {
    if (name == "connectedDebugAndroidTest") {
        dependsOn("installMauiDebugApk")
    }
}




