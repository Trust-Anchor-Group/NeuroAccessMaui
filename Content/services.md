# Services

This document provides an overview of services included with **Neuro-Access**

## Table of Contents

- [TagProfile](#tagprofile)
- [UiService](#uiservice)
- [XmppService](#xmppservice)
- [CryptoService](#cryptoservice)
- [NetworkService](#networkservice)
- [LogService](#logservice)
- [StorageService](#storageservice)

## TagProfile

The [TagProfile](../NeuroAccessMaui/Services/Tag/ITagProfile.cs) represents and makes it possible to interact with the the user profile. The user profile holds data about a user's identity, account, as well as Jabber Ids for the services discovered for the domain the user is connected to.

## UiService

The [UiService](../NeuroAccessMaui/Services/UI/IUiService.cs) is responsible for everything related to the UI, this includes:

- Navigation
- Popups
- Screenshots

### XmppService ###

The [XmppService](../NeuroAccessMaui/Services/Xmpp/IXmppService.cs) represents the live connection to a Neuron server.
It provides event handlers for managing connected state, as well as methods to access the various services the Neuron server provides (Contract handling, Chats et.c)

### CryptoService ###

The [CryptoService](../NeuroAccessMaui/Services/ICryptoService.cs) has methods to help with cryptographic tasks like creating a random password et.c.

### NetworkService ###

The [NetworkService](../NeuroAccessMaui/Services/Network/INetworkService.cs) allows you to check for network access, and it also provides helper methods
to make arbitrary requests with consise error handling built-in. That's what the `TryRequest` methods are for.
If any call fails, they will catch errors, log them and display alerts to the user. You don't have to use these, but they are provided for convenience.

### LogService ###

The [LogService](../NeuroAccessMaui/Services/EventLog/ILogService.cs) handles **Neuro-Access** logging, allows logging of exceptions and messages of different severity, which can be reported back to the Neuron server. You can also subscribe to certain events and redirect them to other sources.

### StorageService ###

The [StorageService](../NeuroAccessMaui/Services/Storage/IStorageService.cs) represents persistent storage, i.e. a Database. The content is encrypted. In order to
store any object in the database, the type of object to store must be made known to the Types dependency resolution system. This is what the parameters to `Types.Initialize` in the App constructor is for.

Pass in one or more assemblies that contain the type(s) you need stored in the database.
The type you want stored should have a collection name. Set one using an attribute like this:

```csharp
[CollectionName("Orders")]
public sealed class CustomerOrders
{
}
```

Once that is done, add an `Id` property and specify the attribute as follows:

```csharp
[CollectionName("Orders")]
public sealed class CustomerOrders
{
    [ObjectId]
    public string ObjectId { get; set; }
}
```

This will be the primary key for the object in the database. No need to set it, just declare it like this.

### SettingsService ###

The [SettingsService](../NeuroAccessMaui/Services/Settings/ISettingsService.cs) is for storing user specific settings, like what they last typed into an 
`Entry` field or similar. It is typically used for loading and saving UI state in the view models. The 
[BaseViewModel](../NeuroAccessMaui/UI/Pages/BaseViewModel.cs) class has a helper method for this callled `GetSettingsKey`:

```csharp
this.SettingsService.SaveState(GetSettingsKey(nameof(FirstName)), this.FirstName);
```

It's easy to work with, and as you can see, refactor friendly as it doesn't use string literals anywhere.

### A Note on Dispose ###

In general, Dispose is used to handle _unmanaged_ resources. This is usually not needed, but _can_ of course be implemented
in various parts of the app. However, it should _not_ be done in the `Service` instances, as these are **re-used** during restarts.
When the app shuts down, all the services' `Unload()` method is called. When the app starts, the services' `Load()` method is called.
This is especially important to know during soft restarts, like when you're switching apps. In this case the pages and view models are kept around,
which means they reference the same services. If you dispose a service and then recreate it, the app will fail.
