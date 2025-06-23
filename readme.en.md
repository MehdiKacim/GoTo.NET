# GoTo.NET : Intelligent Navigation Prediction Engine

[![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/votre_repo/GoToNet/actions)
[![NuGet Version](https://img.shields.io/nuget/v/GoToNet.Core.svg?style=flat-square)](https://www.nuget.org/packages/GoToNet.Core/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## Table of Contents

1.  [Introduction](#1-introduction)
2.  [Key Features](#2-key-features)
3.  [Quick Start](#3-quick-start)
    * [Installation](#installation)
    * [Essential Configuration](#essential-configuration)
    * [Essential Client Implementations](#essential-client-implementations)
    * [Engine Usage](#engine-usage)
4.  [Fundamental Concepts](#4-fundamental-concepts)
    * [Navigation Events (`NavigationEvent`)](#navigation-events-navigationevent)
    * [Suggestions (`SuggestedItem`)](#suggestions-suggesteditem)
    * [Persistence Management (Stores)](#persistence-management-stores)
    * [Prediction Algorithms](#prediction-algorithms)
    * [The Prediction Engine (`GoToPredictionEngine`)](#the-prediction-engine-gotopredictionengine)
    * [User Menu Customization](#user-menu-customization)
    * [Application Integration](#application-integration)
    * [Algorithm Training Strategies](#algorithm-training-strategies)
5.  [Detailed Prediction Algorithms](#5-detailed-prediction-algorithms)
    * [Hybrid Approach: The Power of Combination](#hybrid-approach-the-power-of-combination)
    * [Frequency (`FrequencyAlgorithm`)](#frequency-frequencyalgorithm)
    * [Markov Chains (`MarkovChainAlgorithm`)](#markov-chains-markovchainalgorithm)
    * [Design Flow (`DesignFlowAlgorithm` and `IDesignFlowAlgorithm`)](#design-flow-designflowalgorithm-and-idesignflowalgorithm)
    * [Artificial Intelligence (`MLNetAlgorithm`)](#artificial-intelligence-mlnetalgorithm)
6.  [Managing Predictions: Individual or Global](#6-managing-predictions-individual-or-global)
    * [Predictions for a Specific User](#predictions-for-a-specific-user)
    * [Global or Role-Based Predictions](#global-or-role-based-predictions)
7.  [Advanced Configuration](#7-advanced-configuration)
    * [Adding Specific Algorithms](#adding-specific-algorithms)
    * [Customizing Providers (Stores and Rules)](#customizing-providers-stores-and-rules)
    * [Custom Training Modes](#custom-training-modes)
8.  [Examples and Demonstrations](#8-examples-and-demonstrations)
9.  [Contribution](#9-contribution)
10. [License](#10-license)

---

## 1. Introduction

**GoTo.NET** is a **robust and flexible .NET library** designed to help you integrate **intelligent and predictive shortcut navigation menus** into your applications. By analyzing user behavior, GoTo.NET suggests the most relevant actions at the right time, enhancing user efficiency and experience.

Whether your application is an ASP.NET Core backend API, a WPF desktop client, or any other .NET application, GoTo.NET integrates seamlessly thanks to its UI-agnostic and dependency injection-based architecture.

---

## 2. Key Features

* **Multi-Algorithm Predictions:** Combine the strengths of several prediction algorithms (frequency, Markov chains, business design logic, and Artificial Intelligence via ML.NET) for comprehensive and relevant suggestions.
* **Customizable Menu:** Offer your users the ability to manually add, remove, and prioritize their own shortcuts.
* **Modular Architecture:** Easily extend or replace internal components (algorithms, data storage) to meet your specific needs.
* **UI-Agnostic:** The library handles the prediction logic. You control the display and actual navigation actions, regardless of your user interface technology (WPF, ASP.NET Core, Blazor, MAUI, etc.).
* **Training Control:** Manage how and when learning models train, with modes adapted for development and production.
* **Training Progress Reports:** Receive real-time updates on the advancement of algorithm training.

---

## 3. Quick Start

Integrating **GoTo.NET** into your .NET project is straightforward thanks to its `IServiceCollection` extension.

### Installation

1.  **Create your .NET project** (e.g., an ASP.NET Core Web API project, a WPF application, or a .NET 8 console application).
2.  **Add the `GoToNet.Core` NuGet package:**
    Open the NuGet package manager console in Visual Studio or use the .NET CLI:
    ```bash
    dotnet add package GoToNet.Core
    ```
    *Note: Depending on the algorithms you choose to use (especially ML.NET), you may need to add additional `Microsoft.ML.*` packages to your project.*

### Essential Configuration

**GoTo.NET** configuration happens at your application's startup, typically in the `ConfigureServices` method of your `Startup.cs` (for ASP.NET Core) or `Program.cs` / `App.xaml.cs` (for console/WPF applications using the generic host).

```csharp
// Example configuration in Program.cs (for a console or ASP.NET Core application)
using GoToNet.Core.Algorithms;
using GoToNet.Core.Extensions;
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YourAppName
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    // 1. Register your implementations for GoTo.NET's dependencies
                    // GoTo.NET is persistence and UI agnostic; you must provide how it interacts.

                    // Data Stores (required)
                    // For prototyping, we use in-memory default implementations.
                    // In production, you should replace these services with your own implementations
                    // (e.g., SqliteHistoryStore, DbUserPreferencesStore) for real persistence.
                    services.AddSingleton<INavigationHistoryStore, InMemoryHistoryStore>();
                    services.AddSingleton<IUserMenuPreferencesStore, InMemoryUserMenuPreferencesStore>();
                    services.AddSingleton<UserMenuBuilder>(); // The user menu builder

                    // Implementations for integration with your application (required)
                    // You MUST provide implementations for these interfaces.
                    // Example for a console application (mocks):
                    services.AddSingleton<INavigationNotifier, MyAppNavigationNotifier>(); // Your class that handles displaying suggestions
                    services.AddSingleton<INavigationActionHandler, MyAppNavigationActionHandler>(); // Your class that handles navigation actions
                    services.AddSingleton<IAppNavigationCatalog, MyAppNavigationCatalog>(); // Your class that lists all pages in your app

                    // If you use the DesignFlowAlgorithm, you MUST register a design rule provider.
                    // GoTo.NET does NOT provide a default implementation in production, as rules are app-specific.
                    // DefaultDesignFlowRulesProvider is provided for prototyping/demonstration.
                    services.AddSingleton<IDesignFlowRulesProvider, DefaultDesignFlowRulesProvider>(); // Or your own rule provider

                    // 2. Register the prediction algorithms you want to use
                    // Each algorithm MUST be registered here so the DI container can instantiate it.
                    // These are registered as Singletons (because algorithms maintain a trained state).
                    services.AddSingleton<IPredictionAlgorithm, FrequencyAlgorithm>();
                    services.AddSingleton<IPredictionAlgorithm, MarkovChainAlgorithm>();
                    services.AddSingleton<IDesignFlowAlgorithm, DesignFlowAlgorithm>(); // Registered as IDesignFlowAlgorithm
                    services.AddSingleton<IPredictionAlgorithm, MLNetAlgorithm>();

                    // 3. Call the AddGoToNet extension to configure the engine
                    services.AddGoToNet(options =>
                    {
                        // Add the algorithms you want the engine to use.
                        // Their dependencies will be resolved automatically by the DI container.
                        options.AddAlgorithm<FrequencyAlgorithm>();
                        options.AddAlgorithm<MarkovChainAlgorithm>();
                        options.AddDesignFlowAlgorithm<DesignFlowAlgorithm>(); // Use the specific overload
                        options.AddAlgorithm<MLNetAlgorithm>();

                        // Bind the client integration interface implementations
                        options.SetNavigationNotifier<MyAppNavigationNotifier>();
                        options.SetNavigationActionHandler<MyAppNavigationActionHandler>();
                        options.SetAppNavigationCatalog<MyAppNavigationCatalog>();

                        // Bind the design flow rules provider (if you want to make it explicit)
                        options.SetDesignFlowRulesProvider<DefaultDesignFlowRulesProvider>();

                        // Define the engine's training strategy.
                        options.TrainingMode = TrainingMode.OnStartupOnce; // Example: train once on startup
                        // options.TrainingMode = TrainingMode.ContinuousDevelopment; // For dev/demo
                        // options.TrainingMode = TrainingMode.Manual; // For manual training triggering
                        // options.TrainingMode = TrainingMode.Custom; // For custom training logic
                    });

                    // Add other application-specific services here.
                })
                .Build();

            // Start the host (necessary for services to be created and training to begin)
            await host.StartAsync();

            // --- Execute your application logic here ---
            // You can retrieve the engine and menu builder from the service provider
            var predictionEngine = host.Services.GetRequiredService<GoToPredictionEngine>();
            var menuBuilder = host.Services.GetRequiredService<UserMenuBuilder>();

            // Subscribe to training events to monitor progress (optional)
            predictionEngine.AlgorithmsTrained += (sender, e) => Console.WriteLine("\nGoTo.NET: Global training completed!");
            predictionEngine.TrainingProgressUpdated += (sender, e) =>
                Console.WriteLine("GoTo.NET Progress: {0} - {1} ({2}%) - {3}", e.AlgorithmName, e.CurrentStep, e.ProgressPercentage, e.Message);

            Console.WriteLine("Application started. Simulate events...");
            // Example usage (see section below)
            await predictionEngine.RecordNavigationAsync(new NavigationEvent { UserId = "user1", CurrentPageOrFeature = "Dashboard" });
            var suggestions = await predictionEngine.GetSuggestionsAsync("user1", "Dashboard");

            // For console applications, ensure the application keeps running
            Console.WriteLine("Press any key to stop the application.");
            Console.ReadKey();

            await host.StopAsync();
        }
    }
}
````

### Essential Client Implementations

These are the concrete implementations of GoTo.NET's interfaces that your application must provide. They should be placed in a suitable folder within your consuming project (e.g., `MyProject/Services/` or `MyProject/Infrastructure/`).

```csharp
// Implementation for INavigationNotifier
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyAppNavigationNotifier : INavigationNotifier
{
    public void UpdateSuggestionsDisplay(string userId, IEnumerable<SuggestedItem> suggestedItems)
    {
        Console.WriteLine($"\n--- Suggestions for {userId} ---");
        if (!suggestedItems.Any())
        {
            Console.WriteLine("No suggestions.");
            return;
        }
        foreach (var item in suggestedItems)
        {
            Console.WriteLine($"- {item.Name} (Score: {item.Score:F2}, Reason: {item.Reason})");
        }
        Console.WriteLine("----------------------------------");
    }
}

// Implementation for INavigationActionHandler
using GoToNet.Core.Interfaces;
using System;

public class MyAppNavigationActionHandler : INavigationActionHandler
{
    public bool PerformNavigation(string userId, string itemName)
    {
        Console.WriteLine($"[APP] {userId} requested navigation to: {itemName}");
        // Here, you would add your application's actual navigation logic.
        // Example WPF: Window.GetWindow(App.Current.MainWindow).FindName("MainFrame") as Frame)?.Navigate(new Uri($"/{itemName}.xaml", UriKind.Relative));
        // Example ASP.NET Core: return RedirectToAction(itemName); (in a controller)
        return true;
    }
}

// Implementation for IAppNavigationCatalog
using GoToNet.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

public class MyAppNavigationCatalog : IAppNavigationCatalog
{
    private readonly List<string> _allAvailablePages = new List<string>
    {
        "Dashboard", "Projects", "Tasks", "Reports", "Users", "Settings", "Help",
        "Invoices", "Payments", "Create Project", "User Profile",
        "Details/ProjectX", "Admin/Users", "Preferences" // Example of more specific pages
    };
    public IEnumerable<string> GetAllAvailableNavigationItems()
    {
        return _allAvailablePages;
    }
}
```

### Engine Usage

Once **GoTo.NET** is configured, you primarily interact with two injected services: `GoToPredictionEngine` and `UserMenuBuilder`.

1.  **Record navigation actions:**
    Each time a user performs a significant action in your application, record it so **GoTo.NET** can learn.

    ```csharp
    // In your application code (e.g., an ASP.NET Core controller, a WPF ViewModel)
    // Make sure to inject GoToPredictionEngine.
    public class MyService
    {
        private readonly GoToPredictionEngine _predictionEngine;
        public MyService(GoToPredictionEngine predictionEngine)
        {
            _predictionEngine = predictionEngine;
        }

        public async Task UserVisitedPage(string userId, string currentPage, string? previousPage = null)
        {
            await _predictionEngine.RecordNavigationAsync(new NavigationEvent
            {
                UserId = userId,
                CurrentPageOrFeature = currentPage,
                PreviousPageOrFeature = previousPage,
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }
    ```

2.  **Get shortcut suggestions:**
    When you need to display the shortcut menu, ask the engine for suggestions.

    ```csharp
    // In your application code
    public async Task<IEnumerable<SuggestedItem>> GetMyShortcuts(string userId, string currentPageContext)
    {
        // The engine will calculate and notify your INavigationNotifier.
        // You don't need to retrieve suggestions directly from this method,
        // because they will be "pushed" to your UI via UpdateSuggestionsDisplay.
        await _predictionEngine.GetSuggestionsAsync(userId, currentPageContext, 5);

        // If you need to retrieve them directly for other uses,
        // GetSuggestionsAsync also returns the list.
        var directSuggestions = await _predictionEngine.GetSuggestionsAsync(userId, currentPageContext, 5);
        return directSuggestions;
    }
    ```

3.  **Manage custom shortcuts:**
    Allow your users to add or remove items from their shortcut menu.

    ```csharp
    // In your application code, after injecting UserMenuBuilder
    public async Task AddUserShortcut(string userId, string itemName)
    {
        await _menuBuilder.AddItemToUserMenuAsync(userId, itemName, 0); // 0 for high priority
        // After adding, you might want to refresh suggestions:
        // await _predictionEngine.GetSuggestionsAsync(userId, "current_page");
    }

    public async Task RemoveUserShortcut(string userId, string itemName)
    {
        await _menuBuilder.RemoveItemFromUserMenuAsync(userId, itemName);
        // After removal, refresh suggestions
    }
    ```

4.  **Trigger navigation from a suggestion:**
    When the user clicks a shortcut suggested by **GoTo.NET**, ask the engine to trigger the navigation action.

    ```csharp
    // In your application code (e.g., a WPF command, an Angular click handler)
    public async Task OnShortcutClicked(string userId, SuggestedItem item)
    {
        bool navigated = _predictionEngine.PerformSuggestedNavigation(userId, item.Name);
        if (navigated)
        {
            // IMPORTANT: Also record this action, so GoTo.NET learns from this click.
            await _predictionEngine.RecordNavigationAsync(new NavigationEvent
            {
                UserId = userId,
                CurrentPageOrFeature = item.Name,
                PreviousPageOrFeature = "page_where_menu_was_opened", // Context
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }
    ```

-----

## 4\. Fundamental Concepts

### Navigation Events (`NavigationEvent`)

```csharp
public class NavigationEvent
{
    public string UserId { get; set; } // The unique identifier of the user
    public string CurrentPageOrFeature { get; set; } // Name of the visited page/feature
    public string? PreviousPageOrFeature { get; set; } // Previous page (for sequences)
    public DateTimeOffset Timestamp { get; set; } // Time of the event
    // ... other optional fields like SessionId, ContextData
}
```

### Suggestions (`SuggestedItem`)

```csharp
public class SuggestedItem
{
    public string Name { get; set; } // The name of the suggested page/feature
    public double Score { get; set; } // Relevance score (higher is more relevant)
    public string Reason { get; set; } // Reason for the suggestion ("Frequency", "ML.NET", "UserCustom", "DesignFlow", "MarkovChain")
}
```

### Persistence Management (Stores)

**GoTo.NET** uses interfaces to interact with data. You must provide implementations for:

  * `INavigationHistoryStore`: To store and retrieve `NavigationEvent` (history).
  * `IUserMenuPreferencesStore`: To store and retrieve `UserCustomMenuItem` (custom preferences).

**GoTo.NET** provides in-memory implementations (`InMemoryHistoryStore`, `InMemoryUserMenuPreferencesStore`) for prototyping. In production, you would implement database-backed stores (SQL, NoSQL).

### Prediction Algorithms

The core intelligence of **GoTo.NET**. Each algorithm implements `IPredictionAlgorithm` and offers suggestions based on its specific paradigm.

  * `FrequencyAlgorithm`
  * `MarkovChainAlgorithm`
  * `DesignFlowAlgorithm`
  * `MLNetAlgorithm`

You add the desired algorithms during **GoTo.NET**'s configuration.

### The Prediction Engine (`GoToPredictionEngine`)

This is the main facade of **GoTo.NET**. It orchestrates:

  * Recording navigation events.
  * Training algorithms (managing strategies and progress).
  * Collecting and combining suggestions from all active algorithms.
  * Integrating user's custom shortcuts.
  * Notifying your client application of updated suggestions.
  * Requesting execution of actual navigation actions.

### User Menu Customization

The `UserMenuBuilder` service allows your end-users explicit control over their shortcut menu.

```csharp
public class UserCustomMenuItem
{
    public string UserId { get; set; }
    public string ItemName { get; set; }
    public int Order { get; set; } // Lower value = higher priority
}
```

### Integration with your Application

**GoTo.NET** is UI-agnostic. It communicates via these interfaces that your application must implement and register in the DI container:

  * `INavigationNotifier`: Receives lists of `SuggestedItem` to display.
  * `INavigationActionHandler`: Executes your application's actual navigation logic.
  * `IAppNavigationCatalog`: Provides **GoTo.NET** (especially `MLNetAlgorithm`) with the complete list of all navigable pages/features in your application.

### Algorithm Training Strategies

You control how models are trained via `GoToNetOptions.TrainingMode`:

  * `TrainingMode.OnStartupOnce`: Full training once at application startup (in the background, non-blocking). Ideal for production if models can be trained offline or if data doesn't change too often.
  * `TrainingMode.ContinuousDevelopment`: Training is triggered in the background after each `RecordNavigationAsync` (for development/demo). **Not recommended for production with high event volume.**
  * `TrainingMode.Manual`: The client application must explicitly call `GoToPredictionEngine.TrainAlgorithmsAsync()` to trigger training.
  * `TrainingMode.Custom`: You provide custom logic to trigger training (e.g., every hour, after X new events, etc.).

You can track training progress via the `AlgorithmsTrained` (global training completed) and `TrainingProgressUpdated` (granular updates per algorithm) events of the `GoToPredictionEngine`.

-----

## 5\. Detailed Prediction Algorithms

**GoTo.NET** uses a **hybrid approach**, combining the strengths of multiple algorithms to provide the most relevant suggestions possible. Each algorithm has a `Weight` you can configure to adjust its influence on the final score.

### Hybrid Approach: The Power of Combination

No single algorithm is perfect for all scenarios. The combination allows **GoTo.NET** to:

  * **Handle Novelty:** If a user is new, history-based algorithms won't have much to say. The **DesignFlowAlgorithm** can then take over.
  * **Provide Context:** **MarkovChainAlgorithm** and **MLNetAlgorithm** are excellent for predictions based on the current navigation context.
  * **Discover Complex Patterns:** **MLNetAlgorithm** can identify subtle correlations (time, day, combined history) that simple rules miss.
  * **Ensure Business Relevance:** The **DesignFlowAlgorithm** ensures key actions defined by your application are always suggested when necessary.
  * **Offer Robustness:** If one algorithm fails (insufficient data, untrained model), others can compensate.

### Frequency (`FrequencyAlgorithm`)

  * **Paradigm:** The simplest. "What the user does most often, they'll probably want to do again."
  * **Value Added:** Quickly identifies implicit "favorites" and strongest usage habits.
  * **Usage:** Useful as a baseline for the general popularity of items for a given user.

### Markov Chains (`MarkovChainAlgorithm`)

  * **Paradigm:** First-order sequential prediction. "After going to page X, users most often go to page Y."
  * **Value Added:** Provides a **strong contextual dimension**. Ideal for predicting the next logical step in a workflow (`Invoices` -\> `Pay Invoice`).
  * **Usage:** Very effective when user actions follow predictable sequences.

### Design Flow (`DesignFlowAlgorithm` and `IDesignFlowAlgorithm`)

  * **Paradigm:** Integrates **business logic** and **flows defined by your application's design**.
  * **Value Added:** Allows suggesting **logical or important actions** (new features, critical workflow steps, "getting started" actions) even if they are not yet frequently used or don't appear in past sequences. It enables your application to **proactively guide** the user.
  * **How it works:** This algorithm depends on a rules provider, the `IDesignFlowRulesProvider`. You define rules like "when the user is on 'Projects', suggest 'Create New Project' and 'Manage Teams'".
  * **Your Role (`IDesignFlowAlgorithm`)**: For this algorithm to work, you must:
    1.  Provide an implementation of **`IDesignFlowRulesProvider`** (e.g., loading rules from a JSON file, a database, or hardcoded for prototyping).
    2.  Register this implementation in the DI container (`services.AddSingleton<IDesignFlowRulesProvider, MyDesignRulesProvider>();`).
    3.  Add the algorithm to **GoTo.NET** using the `options.AddDesignFlowAlgorithm<DesignFlowAlgorithm>();` overload, which ensures its dependency (`IDesignFlowRulesProvider`) is correctly resolved.

### Artificial Intelligence (`MLNetAlgorithm`)

  * **Paradigm:** Uses Machine Learning (via ML.NET) to discover **complex, non-obvious patterns** in navigation data. It can consider various features (user, current page, previous page, hour of day, day of week, etc.) to predict the probability of visiting any other page.
  * **Value Added:** Brings **adaptive intelligence**, capable of spotting subtle trends and providing more personalized and accurate predictions. Handles more complex scenarios than rule-based algorithms.
  * **Usage:** Ideal for high-value predictions where simple rules are insufficient. Requires a sufficient amount of historical data for training. Depends on `IAppNavigationCatalog` to know all possible pages.

-----

## 6\. Managing Predictions: Individual or Global

**GoTo.NET** is designed for flexibility. You can obtain predictions that are:

### Predictions for a Specific User

This is the most common use case. The prediction is highly personalized based on a given user's history and preferences.

  * **How to do it:** Simply use the current user's `UserId` in the `RecordNavigationAsync` and `GetSuggestionsAsync` methods.
    ```csharp
    // Record an action for "Alice"
    await predictionEngine.RecordNavigationAsync(new NavigationEvent { UserId = "Alice", CurrentPageOrFeature = "HR Reports" });

    // Get suggestions for "Alice"
    var suggestionsForAlice = await predictionEngine.GetSuggestionsAsync("Alice", "Dashboard");
    ```
    The **Frequency** and **Markov** algorithms will operate on Alice's history. The **MLNetAlgorithm** will train a model that includes `UserId` as a feature, allowing it to predict Alice's unique behavior. Custom shortcuts will, of course, be Alice's.

### Global or Role-Based Predictions

You might want a shortcut menu that's the same for all users, or for all users with a certain role (e.g., all "Administrators").

  * **How to do it:**
    1.  **Use a generic `UserId`:** When recording `NavigationEvent`s, use a generic `UserId` (e.g., "GLOBAL\_USER" or "ADMIN\_ROLE"). All events recorded under this generic ID will build a "global" history. Then, request suggestions for this generic ID.
        ```csharp
        // Record an action that should influence global suggestions
        await predictionEngine.RecordNavigationAsync(new NavigationEvent { UserId = "GLOBAL_USER", CurrentPageOrFeature = "Marketing Page" });

        // Get "global" suggestions for everyone
        var globalSuggestions = await predictionEngine.GetSuggestionsAsync("GLOBAL_USER", "Landing Page");
        ```
    2.  **Leverage `DesignFlowAlgorithm`:** This algorithm is ideal for suggestions based on business or design rules that are universal or role-specific.
    3.  **Filtering or Post-processing:** You can always get suggestions for a specific user, then filter or augment them with global or role-specific rules *after* **GoTo.NET** has returned its predictions.
    4.  **Extended Context:** The **MLNetAlgorithm** can use additional context data (`ContextData` in `NavigationEvent`, e.g., the `Role`). You can train the model to consider the role, allowing different predictions for different roles without changing the main `UserId`.
        ```csharp
        // Record with a role
        await predictionEngine.RecordNavigationAsync(new NavigationEvent { UserId = "user1", CurrentPageOrFeature = "Admin Panel", ContextData = new Dictionary<string, string>{ {"Role", "Admin"} } });

        // Get suggestions for a specific role via context
        var suggestionsForAdmin = await predictionEngine.GetSuggestionsAsync("user1", "Admin Panel", contextData: new Dictionary<string, string>{ {"Role", "Admin"} });
        ```

-----

## 7\. Advanced Configuration

The `services.AddGoToNet()` method offers granular control over your prediction engine's configuration.

### Adding Specific Algorithms

You must register each algorithm in the DI container, then add it to the **GoTo.NET** options.

```csharp
// In Program.cs / App.xaml.cs of your consuming application

// Register algorithms (as Singletons, because they maintain trained state)
services.AddSingleton<IPredictionAlgorithm, FrequencyAlgorithm>();
services.AddSingleton<IPredictionAlgorithm, MarkovChainAlgorithm>();
// Note: DesignFlowAlgorithm implements IDesignFlowAlgorithm and has a dependency on IDesignFlowRulesProvider
services.AddSingleton<IDesignFlowAlgorithm, DesignFlowAlgorithm>();
services.AddSingleton<IPredictionAlgorithm, MLNetAlgorithm>();

// GoTo.NET Configuration
services.AddGoToNet(options =>
{
    // Add only the algorithms you want the engine to use.
    options.AddAlgorithm<FrequencyAlgorithm>(); // General algorithm
    options.AddAlgorithm<MarkovChainAlgorithm>(); // General algorithm

    // For DesignFlowAlgorithm, use the specific overload for clarity.
    // Its IDesignFlowRulesProvider dependency will be automatically resolved by DI if registered.
    options.AddDesignFlowAlgorithm<DesignFlowAlgorithm>();

    options.AddAlgorithm<MLNetAlgorithm>(); // ML.NET algorithm

    // ... (other configurations like SetNavigationNotifier, SetAppNavigationCatalog) ...
});
```

### Customizing Providers (Stores and Rules)

Replace default implementations with your own if you need specific persistence (database, files) or custom design flow rules.

```csharp
// In Program.cs / App.xaml.cs

// Replace InMemoryHistoryStore with your SqliteHistoryStore
services.AddSingleton<INavigationHistoryStore, MySqliteHistoryStore>();

// Replace DefaultDesignFlowRulesProvider with your CustomJsonDesignFlowRulesProvider
// This is MANDATORY if you use DesignFlowAlgorithm and don't want the default provider.
services.AddSingleton<IDesignFlowRulesProvider, MyJsonFileDesignFlowRulesProvider>();

services.AddGoToNet(options => {
    // ...
    // No need to change the AddDesignFlowAlgorithm<DesignFlowAlgorithm>() call;
    // It will automatically use the IDesignFlowRulesProvider implementation you registered.
});
```

### Custom Training Modes

The `TrainingMode.Custom` mode allows you to define completely custom training logic.

```csharp
// In Program.cs / App.xaml.cs
services.AddGoToNet(options => {
    options.TrainingMode = TrainingMode.Custom;
    options.CustomTrainingLogic = async (serviceProvider) => {
        // Get the engine to trigger training
        var engine = serviceProvider.GetRequiredService<GoToPredictionEngine>();
        // You can get other services here if your custom logic needs them, e.g.:
        // var historyStore = serviceProvider.GetRequiredService<INavigationHistoryStore>();

        Console.WriteLine("[GoToNet Custom Training] Starting custom training logic...");
        // You can define your own logic here:
        // - Train at specific intervals.
        // - Train only if a certain volume of new data is reached.
        // - Train a subset of algorithms.
        await engine.TrainAlgorithmsAsync(); // Call the full training when you decide
        Console.WriteLine("[GoToNet Custom Training] Custom training completed.");
    };
});
```

-----

## 8\. Examples and Demonstrations

To see **GoTo.NET** in action, explore the demonstration projects in the GitHub repository:

  * **`GoToNet.ConsoleTest`:** A simple console project to test all library functionalities and observe training logs.
  * **`GoToNet.ApiDemo`:** An ASP.NET Core Web API application showing how to expose GoTo.NET via HTTP endpoints.
  * **`GoToNet.WpfDemo`:** A WPF application demonstrating GoTo.NET integration in a desktop client with a dynamic user interface.

-----

## 9\. Contribution

We welcome contributions\! If you wish to improve **GoTo.NET**, fix a bug, or add a new feature, feel free to open an [issue](https://www.google.com/search?q=https://github.com/votre_repo/GoToNet/issues) or submit a [pull request](https://www.google.com/search?q=https://github.com/votre_repo/GoToNet/pulls) on our GitHub repository.

-----

## 10\. License

This project is licensed under MIT. See the [LICENSE](https://www.google.com/search?q=https://github.com/votre_repo/GoToNet/blob/main/LICENSE) file for more details.
