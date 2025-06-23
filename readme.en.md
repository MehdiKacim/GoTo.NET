# GoTo.NET : Intelligent Navigation Prediction Engine


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