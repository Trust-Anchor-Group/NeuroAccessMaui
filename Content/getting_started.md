# Getting Started

This guide will help you set up your development environment, run the project, and understand the basic configuration options.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Setting Up the Development Environment](#setting-up-the-development-environment)
- [Running the Project](#running-the-project)
- [Connecting to a Local Development Neuron](#connecting-to-a-local-development-neuron)

## Prerequisites

Before you begin, ensure you have met the following requirements:

- **.NET SDK**: .NET SDK 8.0.401. You can download it from the [official .NET website](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).
- **IDE**: We recommend using [Visual Studio](https://visualstudio.microsoft.com/) or [Visual Studio Code](https://code.visualstudio.com/) for development.
- **Git**: If you plan to clone the repository, you need Git installed on your machine. You can download it from the [official Git website](https://git-scm.com/).
- **Operating System**: .NET MAUI supports development on Windows, macOS, and Linux. However, running iOS projects requires access to macOS computer with Xcode installed.

### Optional Prerequisites

- **Android SDK**: If you plan to run the project on Android devices, ensure that the Android SDK is installed.
- **Xcode**: For iOS development, Xcode is required. You can install it from the Mac App Store.
- **Emulators/Simulators**: You may need Android Emulators or iOS Simulators for testing the application without physical devices.
- **Extensions**: If you are using VS Code the following extensions are recommended
  - TBA

## Setting Up the Development Environment

### 1. Configuring your IDE

Depending on your IDE, please see the following guides from Microsoft

- [Visual Studio]()
- [Visual Studio Code](https://learn.microsoft.com/en-us/shows/visual-studio-toolbox/getting-started-with-maui-in-visual-studio-code)

### 2. Clone the Repository

First, clone the project repository to your local machine using Git:

```bash
git clone https://github.com/trust-anchor-group/NeuroAccessMaui.git
cd NeuroAccessMaui
```

### 3. Install Dependencies

Navigate to the project directory and restore the required dependencies and workloads:

```bash
dotnet restore
dotnet workload restore
```

### 4. Open the Project

- **Visual Studio**: Open the `.sln` solution file in Visual Studio.
- **Visual Studio Code**: Open the project folder in Visual Studio Code.

### 4. Configure the Platform

Select the platform you want to target (e.g., Android, iOS, Windows). Ensure that the correct emulator or simulator is set up in your IDE.

### 5. Build the Project

Build the project to ensure everything is set up correctly:

```bash
dotnet build
```

Note: On the IOS playform you need to setup valid Provisioning profiles and Signing identities.

## Running the Project

Once your environment is set up, you can run the project on your desired platform.

### 1. Run on Android

To run the project on an Android device or emulator:

```bash
dotnet build -t:Run -f net6.0-android
```

Or, use the "Run" button in Visual Studio, ensuring the Android emulator or device is selected.

### 2. Run on iOS

To run the project on an iOS device or simulator (macOS only):

```bash
dotnet build -t:Run -f net6.0-ios
```

Or, use the "Run" button in Visual Studio, ensuring the iOS simulator or device is selected.

### Platform-Specific Configurations

Each platform have specific configuration files or settings:

- **Android**: `AndroidManifest.xml`
- **iOS**: `Info.plist`

Refer to the specific platform documentation for more details on how to manage these files.

## Connecting to a Local Development Neuron

This section provides detailed instructions for connecting to a local development neuron. These steps assume you're using HTTPS with self-signed certificates.

### Prerequisites

- A running local neuron with HTTPS enabled using self-signed certificates
- Debug build of the app
- Android or iOS emulator running on your local machine

### Step 1: Configure the Self-Signed Certificate

1. For detailed instructions on working with self-signed certificates, refer to this guide: [Developing for HTTPS with self-signed certificates](https://lab.tagroot.io/Community/Post/Developing_for_HTTPS_with_self_signed_certificates)

### Step 2: Update the Local IP Address

1. Open the `Constants.cs` file located in the `NeuroAccessMaui` project
2. Update the `Debug.LocalIpAdress` constant to point to your local machine:
   - For localhost: `localhost`
   - For Android emulator: `10.0.2.2` (10.0.2.2 is the loopback address that points to your host machine from the Android emulator)

### Step 3: Configure Platform-Specific Settings

#### For Android:

1. Create a network security configuration file if it doesn't exist:
   - Path: `NeuroAccessMaui/Platforms/Android/Resources/xml/network_security_config.xml`
   - Content:
     ```xml
     <?xml version="1.0" encoding="utf-8"?>
     <network-security-config>
         <domain-config cleartextTrafficPermitted="true">
             <domain includeSubdomains="true">10.0.2.2</domain>
             <domain includeSubdomains="true">localhost</domain>
             <trust-anchors>
                 <!-- Trust user added CAs while debuggable only -->
                 <certificates src="@raw/self_signed_cert"/>
                 <certificates src="user" />
                 <certificates src="system"/>
             </trust-anchors>
         </domain-config>
     </network-security-config>
     ```

2. Add your self-signed certificate to the raw resources:
   - Create a `raw` directory if it doesn't exist: `NeuroAccessMaui/Platforms/Android/Resources/raw/`
   - Copy your certificate to this directory

3. Update the AndroidManifest.xml to use the network security configuration:
   - Path: `NeuroAccessMaui/Platforms/Android/AndroidManifest.xml`
   - Add the following attribute to the `<application>` tag:
     ```xml
     android:networkSecurityConfig="@xml/network_security_config"
     ```

#### For iOS:

1. Add your self-signed certificate to the project and ensure it's included in the app bundle
2. Update Info.plist to allow arbitrary loads (for development only):
   ```xml
   <key>NSAppTransportSecurity</key>
   <dict>
       <key>NSAllowsLocalNetworking</key>
       <true/>
       <key>NSAllowsArbitraryLoads</key>
       <true/>
   </dict>
   ```

### Troubleshooting

- If you encounter SSL/TLS certificate errors, ensure that:
  - The certificate is correctly exported and added to the resources
  - The network security configuration properly references the certificate

- For Android emulators, make sure you're using 10.0.2.2 instead of localhost to access your host machine

- If connections time out, check that:
  - The neuron is running and accessible
  - Any firewalls or proxies are properly configured to allow the connection

---

By following these steps, you should have your development environment set up and your project running on your desired platform. If you encounter any issues, please refer to the [Troubleshooting](troubleshooting.md)