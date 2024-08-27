# Getting Started

This guide will help you set up your development environment, run the project, and understand the basic configuration options.

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
  - TO BE WRITTEN

## Setting Up the Development Environment

### 1. Configuring your IDE

Depending on your IDE, please see the following guides from Microsoft

- [Visual Studio]()
- [Visual Studio Code](https://learn.microsoft.com/en-us/shows/visual-studio-toolbox/getting-started-with-maui-in-visual-studio-code)

### 2. Clone the Repository

First, clone the project repository to your local machine using Git:

```bash
git clone https://github.com/yourusername/yourprojectname.git
cd yourprojectname
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

## Running the Project

Once your environment is set up, you can run the project on your desired platform.

### 1. Run on Android

To run the project on an Android device or emulator:

```bash
dotnet build -t:Run -f net6.0-android
```

Or, use the "Run" button in Visual Studio, ensuring the Android emulator is selected.

### 2. Run on iOS

To run the project on an iOS device or simulator (macOS only):

```bash
dotnet build -t:Run -f net6.0-ios
```

Or, use the "Run" button in Visual Studio, ensuring the iOS simulator is selected.

### Platform-Specific Configurations

Each platform have specific configuration files or settings:

- **Android**: `AndroidManifest.xml`
- **iOS**: `Info.plist`

Refer to the specific platform documentation for more details on how to manage these files.

---

By following these steps, you should have your development environment set up and your project running on your desired platform. If you encounter any issues, please refer to the [Troubleshooting](troubleshooting.md)