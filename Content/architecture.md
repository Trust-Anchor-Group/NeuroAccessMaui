# Project Architecture

This document provides an overview of the architecture of **Neuro-Access**. It covers the project structure, the implementation of the MVVM pattern, the use of dependency injection, and details about the core services and repositories.

## Table of Contents

- [Project Structure](#project-structure)
- [MVVM Pattern](#mvvm-pattern)
- [Dependency Injection](#dependency-injection)
- [Services and Repositories](#services-and-repositories)

## Project Structure

The project is organized into the following primary folders and files:

```bash
NeuroAccessMaui/
├── NeuroAccessMaui.sln
│   NeuroAccessMaui/
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── MainPage.xaml
│   ├── MainPage.xaml.cs
│   ├── MauiProgram.cs
│   │   Exceptions/
│   │   Extensions/
│   │   Helpers/
│   │   Links/
│   │   Platforms/
│   │   Resources/
│   │   │   Images/
│   │   │   Languages/
│   │   │   Styles/
│   │   Services/
│   │   UI/
│   │   │   Behaviours/
│   │   │   Controls/
│   │   │   Converters/
│   │   │   Core/
│   │   │   Pages/
│   │   │   │   Main/
│   │   │   │   ├── AppShell.xaml
│   │   │   │   ├── AppShell.xaml.cs
│   │   │   │   ├── MainPage.xaml
│   │   │   │   ├── MainPage.xaml.cs
│   │   │   Popups/
│   │   │   Rendering/
```

### Key Folders and Files

- **App.xaml & App.xaml.cs**: The entry point of the application where global resources, styles, and app-level configurations are defined. This file is crucial for initializing the main components of the application.
- **MainPage.xaml & MainPage.xaml.cs**: The main page of the application, providing the initial UI and interaction logic. It serves as the starting point for the application's navigation.
- **MauiProgram.cs**: This file contains the setup and configuration logic for the application, including the registration of services and middleware using dependency injection.

- **Exceptions/**: Contains custom exception classes that handle specific error conditions within the application.
- **Extensions/**: Includes extension methods that enhance existing classes.
- **Helpers/**: Utility classes and helper functions.
- **Links/**: Contains implementation for handling different links to resources.

- **Platforms/**: Contains platform-specific code for Android and iOS. This folder includes device-specific implementations and platform-related assets.
  
- **Resources/**: Houses shared resources utilized throughout the application, including:
  - **Images/**: All image assets used within the app, such as icons, logos, and other graphical elements.
  - **Languages/**: Localization files that manage translations and support for multiple languages within the application.
  - **Styles/**: XAML styles and resource dictionaries that define the visual appearance and theming of the application.

- **Services/**: This folder includes service classes responsible for handling business logic, data retrieval, API communication, and other core functionalities of the application.

- **UI/**: Contains the user interface components and related logic, organized into subfolders for better maintainability:
  - **Behaviours/**: Custom behaviors that can be attached to UI elements.
  - **Controls/**: Custom controls.
  - **Converters/**: Value converters used in data binding to transform data between the ViewModel and the View.
  - **Pages/**: Contains XAML pages and their associated code-behind files, aswell as their ViewModels. It’s further organized into subfolders such as:
    - **Main/**: Includes the primary pages of the application, including the main navigation shell and the initial landing page (e.g., `AppShell.xaml`, `MainPage.xaml`).
  - **Popups/**: Contains views and view-models for different custom popups.
  - **Rendering/**: Contains renderers which translate different sources to Maui XAML code.

---

## MVVM Pattern

**Neuro-Access** follows the Model-View-ViewModel (MVVM) architectural pattern, which separates the business logic, user interface, and data binding logic into distinct layers. The following is a brief overview of the MVVM pattern, more information can be found at [Microsoft official docs](https://learn.microsoft.com/en-us/dotnet/maui/xaml/fundamentals/mvvm?view=net-maui-8.0)

### Model

The **Model** represents the application's data and business logic. Models are typically plain C# classes that encapsulate the data and methods to manipulate that data. They do not interact directly with the UI or the ViewModel.

Example Model:

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}
```

### View

The **View** is the XAML-based UI layer. It defines the layout and appearance of the application but contains minimal code-behind logic. The View is bound to a ViewModel using data binding.

Example View (XAML):

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="YourProjectName.Views.MainPage">
    <StackLayout>
        <Label Text="{Binding WelcomeMessage}" 
               VerticalOptions="CenterAndExpand" 
               HorizontalOptions="CenterAndExpand" />
    </StackLayout>
</ContentPage>
```

### ViewModel

The **ViewModel** acts as the intermediary between the View and the Model. It handles the presentation logic and data binding, exposing data and commands that the View can bind to. ViewModels implement `INotifyPropertyChanged` to notify the View of changes.

Example ViewModel:

```csharp
public class MainPageViewModel : INotifyPropertyChanged
{
    private string welcomeMessage;
    
    public string WelcomeMessage
    {
        get => welcomeMessage;
        set
        {
            welcomeMessage = value;
            OnPropertyChanged(nameof(WelcomeMessage));
        }
    }

    public MainPageViewModel()
    {
        WelcomeMessage = "Welcome to Your Project Name!";
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```

---

### Using the .NET MAUI Community Toolkit

**Your Project Name** leverages the .NET MAUI Community Toolkit to simplify the implementation of the MVVM pattern. The toolkit provides a variety of utilities and attributes that reduce boilerplate code and make the codebase more maintainable.

#### Observable Properties with `[ObservableProperty]`

The `[ObservableProperty]` attribute provided by the MAUI Community Toolkit allows you to automatically generate properties that raise `INotifyPropertyChanged` notifications. This simplifies the process of creating observable properties in your ViewModel.

**Example ViewModel using `[ObservableProperty]`:**

```csharp
public partial class MainPageViewModel : INotifyPropertyChanged
{
    [ObservableProperty]
    private string welcomeMessage;

    public MainPageViewModel()
    {
        WelcomeMessage = "Welcome to Your Project Name!";
    }
}
```

In this example, the `welcomeMessage` field is automatically converted into a public property with `INotifyPropertyChanged` support, making it easier to bind to the UI.

#### Relay Commands with `[RelayCommand]`

The `[RelayCommand]` attribute allows you to easily define commands in your ViewModel without having to manually implement `ICommand` interfaces. This is particularly useful for handling user interactions, such as button clicks.

**Example ViewModel using `[RelayCommand]`:**

```csharp
public partial class MainPageViewModel : INotifyPropertyChanged
{
    [ObservableProperty]
    private string welcomeMessage;

    public MainPageViewModel()
    {
        WelcomeMessage = "Welcome to Your Project Name!";
    }

    [RelayCommand]
    private void UpdateMessage()
    {
        WelcomeMessage = "The message has been updated!";
    }
}
```

In this example, the `UpdateMessage` method is automatically exposed as an `ICommand` property named `UpdateMessageCommand`, which can be bound to buttons or other interactive elements in the UI.


## Dependency Injection

**Neuro-Access** utilizes dependency injection (DI) to manage the creation and lifecycle of objects, promoting loose coupling and testability.

### Setting Up Dependency Injection

In **MauiProgram.cs**, services are registered within the `MauiProgram.CreateMauiApp()` method:

```csharp
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Register services
        builder.Services.AddSingleton<MainPageViewModel>();
        builder.Services.AddSingleton<MainPage>();

        builder.Services.AddTransient<IDataService, DataService>();

        return builder.Build();
    }
}
```

### Injecting Dependencies

Dependencies are injected into classes using constructor injection:

```csharp
public class MainPage : ContentPage
{
    private readonly MainPageViewModel _viewModel;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
}
```

## Services and Repositories

Services in **Neuro-Access** encapsulate the application's core logic, such as data access, API communication, and business rules.

### Example Service

```csharp
public interface IDataService
{
    Task<IEnumerable<User>> GetUsersAsync();
}

public class DataService : IDataService
{
    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        // Simulate fetching data from a remote API or database
        await Task.Delay(1000);
        return new List<User>
        {
            new User { Id = 1, Name = "John Doe", Email = "john@example.com" },
            new User { Id = 2, Name = "Jane Doe", Email = "jane@example.com" }
        };
    }
}
```

---

