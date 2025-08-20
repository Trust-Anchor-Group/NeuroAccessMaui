# Project Architecture

This document provides an overview of the architecture of **Neuro-Access**. It covers the project structure, the MVVM pattern, the use of dependency injection.

## Table of Contents

- [Project Structure](#project-structure)
- [MVVM Pattern](#mvvm-pattern)
- [Dependency Injection](#dependency-injection-and-dependency-resolution)

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

> The **Model** represents the application's data and business logic. Models are typically plain C# classes that encapsulate the data and methods to manipulate that data. They do not interact directly with the UI or the ViewModel.

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

> The **View** is the XAML-based UI layer. It defines the layout and appearance of the application but also has associated code-behind logic defined in a xaml.cs file. The View is bound to a ViewModel using data binding.

Example MAUI Page (XAML):

```xml
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="ExampleProject.Views.ExamplePage">
    <StackLayout>
        <Label Text="{Binding WelcomeMessage}" 
               VerticalOptions="CenterAndExpand" 
               HorizontalOptions="CenterAndExpand" />
    </StackLayout>
</ContentPage>
```
### BaseContentPage / BaseContentView

> In **Neuro-Access** a base class for pages and views are provided which enables it's corresponding viewModel to access specific lifecycle methods usually only accessable by the view, such as OnAppearing, OnLoaded, etc...

Note: The ViewModel needs to inherit from **BaseViewModel**

Example Neuro-Access Page (XAML):

```xml
<base:BaseContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:base="clr-namespace:NeuroAccessMaui.UI.Pages"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="ExampleProject.Views.ExamplePage">
    <StackLayout>
        <Label Text="{Binding WelcomeMessage}" 
               VerticalOptions="CenterAndExpand" 
               HorizontalOptions="CenterAndExpand" />
    </StackLayout>
</base:BaseContentPage>
```

### ViewModel

> The **ViewModel** acts as the intermediary between the View and the Model. It handles the presentation logic and data binding, exposing data and commands that the View can bind to. ViewModels implement `INotifyPropertyChanged` to notify the View of changes.

Example MAUI ViewModel:

```csharp
public class ExampleViewModel : INotifyPropertyChanged
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

    public ExampleViewModel()
    {
        WelcomeMessage = "Welcome to Your ExampleProject!";
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
```
### BaseViewModel

> Just like how views and pages has a custom base class for it's ViewModels, **BaseViewModel** provides extra helper methods and utilities

See [BaseViewModel](../NeuroAccessMaui/UI/Pages/BaseViewModel.cs)

Note: This base class is not required for the app to work, but is recommended when the view/page inherits from `BaseContentPage` or `BaseContentView` to leverage lifecycle forwarding.

---

### Using the .NET MAUI Community Toolkit

**Neuro-Access** leverages the .NET MAUI Community Toolkit to simplify the implementation of the MVVM pattern. The toolkit provides a variety of utilities and attributes that reduce boilerplate code and make the codebase more maintainable.

#### Observable Properties with `[ObservableProperty]`

The `[ObservableProperty]` attribute provided by the MAUI Community Toolkit allows you to automatically generate properties that raise `INotifyPropertyChanged` notifications. This simplifies the process of creating observable properties in your ViewModel.

Note: the property defined under `[ObservableProperty]` must start with a lowercase character, and a public property starting with uppercase will be generated. The generated property is the one you access in other parts of the code to modify it.

**Example ViewModel using `[ObservableProperty]`:**

```csharp
public partial class ExampleViewModel : BaseViewModel
{
    [ObservableProperty]
    private string welcomeMessage;

    public ExampleViewModel()
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
public partial class ExampleViewModel : BaseViewModel
{
    [ObservableProperty]
    private string welcomeMessage;

    public ExampleViewModel()
    {
        WelcomeMessage = "Welcome to Your ExampleProject!";
    }

    [RelayCommand]
    private void UpdateMessage()
    {
        WelcomeMessage = "The message has been updated!";
    }
}
```

In this example, the `UpdateMessage` method is automatically exposed as an `ICommand` property named `UpdateMessageCommand`, which can be bound to buttons or other interactive elements in the UI.

---

## Dependency Injection and Dependency Resolution

**Neuro-Access** utilizes a combination of the built in dependency injection (DI) of.NET MAUI and a custom implementation for dependency resolution called `Types`.

Note: As a rule of thumb dependency injection is used for injecting ViewModels into their corresponding views, while dependency resolution is used for services

### Setting Up Dependency Injection

In **MauiProgram.cs**, you can register classes to .NET MAUIs dependency system in the `MauiProgram.CreateMauiApp()` method:

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

        // Register pages and its corrensponding viewmodel
        builder.Services.AddTransient<ExamplePage, ExamplePageViewModel>();

        return builder.Build();
    }
}
```
### Injecting Dependencies

In the following example: `ExamplePageViewModel` would be injected into the constructor of ExamplePage automatically

```csharp
public partial class ExamplePage : BaseContentPage
{
    public MainPage(BaseViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
```

### Setting up dependency resolution

**Neuro-Access** uses a custom light-weight `Inversion of Control` implementation for dependency resolution called `Types`.
The reason for this is that the built-in `DependencyService` is just a service locator, not a dependency injection container.
It is rather limited, therefore swapping it out for `Types` is recommended. In order to specify how types should be resolved you can use
two attributes:

1. `DefaultImplementationAttribute` - specifies which class is the default implementation of a certain interface.

**Example:**

```csharp
[DefaultImplementation(typeof(AttachmentCacheService))]
public interface IAttachmentCacheService : ILoadableService
```

2. `SingletonAttribute` - specifies that there should only be one shared instance of this implementation.

**Example:**

``` csharp
[Singleton]
internal sealed class AttachmentCacheService : LoadableService, IAttachmentCacheService
```

The two attributes can be used in combination on an interface and its implementation.

If you need to register types to resolve, make that call to `Types.Initialize` _before_ the call to `Types.Instantiate<T>()`, like this:

```csharp
Assembly appAssembly = this.GetType().Assembly;

if (!Types.IsInitialized)
{
    // Define the scope and reach of Runtime.Inventory (Script, Serialization, Persistence, IoC, etc.):
    Types.Initialize(
        appAssembly,                                // Allows for objects defined in this assembly, to be instantiated and persisted.
        typeof(Database).Assembly,                  // Indexes default attributes
        typeof(ObjectSerializer).Assembly,          // Indexes general serializers
        typeof(FilesProvider).Assembly,             // Indexes special serializers
        typeof(RuntimeSettings).Assembly,           // Allows for persistence of settings in the object database
        typeof(XmppClient).Assembly,                // Serialization of general XMPP objects
        typeof(ContractsClient).Assembly,           // Serialization of XMPP objects related to digital identities and smart contracts
        typeof(Expression).Assembly,                // Indexes basic script functions
        typeof(MarkdownDocument).Assembly,          // Indexes basic Markdown interfaces
        typeof(XmppServerlessMessaging).Assembly,   // Indexes End-to-End encryption mechanisms
        typeof(TagConfiguration).Assembly,          // Indexes persistable objects
        typeof(RegistrationStep).Assembly);         // Indexes persistable objects
}
```

Configure `Types` in the [App.xaml.cs](../NeuroAccessMaui/App.xaml.cs) constructor to be the default resolver like this:

``` csharp
public App()
{
    ...

    // Set the IoC to be the default dependency resolver
    DependencyResolver.ResolveUsing(type =>
	{
        if (Types.GetType(type.FullName) is null)
	        return null;	// Type not managed by Runtime.Inventory. Xamarin.Forms resolves this using its default mechanism.
        return Types.Instantiate(true, type);
    });
}
```

That's all you need to do. And when you need to resolve components later in the code, invoke the built-in `DependencyService` as usual:

```csharp
    var myService = DependencyService.Resolve<IMyService>();
```

This will invoke the lightweight IoC implementation 'under the hood'.

Services can also be accessed using the `ServiceRef` class which contains static references to resolved services in the app. You can read more [here](services.md)

---

