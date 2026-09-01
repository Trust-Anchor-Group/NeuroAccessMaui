import java.util.Properties

plugins {
    alias(libs.plugins.android.application)
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
    }

    signingConfigs {
        getByName("debug") {
            storeFile = File("C:/Users/AlbinKoppared/AppData/Local/Xamarin/Mono for Android/debug.keystore")
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

tasks.register<Exec>("installMauiDebugApk") {
    val adbExecutable = File(androidSdkDirectory, "platform-tools/adb.exe")

    inputs.file(mauiDebugApk)
    commandLine(adbExecutable.path, "install", "-r", mauiDebugApk.path)
}

tasks.configureEach {
    if (name == "connectedDebugAndroidTest") {
        dependsOn("installMauiDebugApk")
    }
}
