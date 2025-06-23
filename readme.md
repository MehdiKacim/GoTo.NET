![GoTo](./assets/gotologo.png)
# GoTo.NET : Moteur de Prédiction de Navigation Intelligent


---

## Table des matières

1.  [Introduction](#1-introduction)
2.  [Fonctionnalités Clés](#2-fonctionnalités-clés)
3.  [Démarrage Rapide](#3-démarrage-rapide)
    * [Installation](#installation)
    * [Configuration Essentielle](#configuration-essentielle)
    * [Implémentations Client Essentielles](#implémentations-client-essentielles)
    * [Utilisation du Moteur](#utilisation-du-moteur)
4.  [Concepts Fondamentaux](#4-concepts-fondamentaux)
    * [Événements de Navigation (`NavigationEvent`)](#événements-de-navigation-navigationevent)
    * [Suggestions (`SuggestedItem`)](#suggestions-suggesteditem)
    * [Gestion de la Persistance (Stores)](#gestion-de-la-persistance-stores)
    * [Algorithmes de Prédiction](#algorithmes-de-prédiction)
    * [Le Moteur de Prédiction (`GoToPredictionEngine`)](#le-moteur-de-prédiction-gotopredictionengine)
    * [Personnalisation du Menu par l'Utilisateur](#personnalisation-du-menu-par-lutilisateur)
    * [Intégration avec votre Application](#intégration-avec-votre-application)
    * [Stratégies d'Entraînement des Algorithmes](#stratégies-dentraînement-des-algorithmes)
5.  [Algorithmes de Prédiction Détaillés](#5-algorithmes-de-prédiction-détaillés)
    * [Approche Hybride : La Force de la Combinaison](#approche-hybride--la-force-de-la-combinaison)
    * [Fréquence (`FrequencyAlgorithm`)](#fréquence-frequencyalgorithm)
    * [Chaînes de Markov (`MarkovChainAlgorithm`)](#chaînes-de-markov-markovchainalgorithm)
    * [Flux de Conception (`DesignFlowAlgorithm` et `IDesignFlowAlgorithm`)](#flux-de-conception-designflowalgorithm-et-idesignflowalgorithm)
    * [Intelligence Artificielle (`MLNetAlgorithm`)](#intelligence-artificielle-mlnetalgorithm)
6.  [Gérer les Prédictions : Individuelles ou Globales](#6-gérer-les-prédictions--individuelles-ou-globales)
    * [Prédictions pour un Utilisateur Spécifique](#prédictions-pour-un-utilisateur-spécifique)
    * [Prédictions Globales ou Basées sur un Rôle](#prédictions-globales-ou-basées-sur-un-rôle)
7.  [Configuration Avancée](#7-configuration-avancée)
    * [Ajout d'Algorithmes Spécifiques](#ajout-dalgorithmes-spécifiques)
    * [Personnalisation des Fournisseurs (Stores et Règles)](#personnalisation-des-fournisseurs-stores-et-règles)
    * [Modes d'Entraînement Personnalisés](#modes-dentraînement-personnalisés)
8.  [Exemples et Démonstrations](#8-exemples-et-démonstrations)
9.  [Contribution](#9-contribution)
10. [Licence](#10-licence)

---

## 1. Introduction

**GoTo.NET** est une **bibliothèque .NET robuste et flexible**, conçue pour vous permettre d'intégrer des **menus de navigation de raccourcis intelligents et prédictifs** dans vos applications. En analysant le comportement des utilisateurs, GoTo.NET suggère les actions les plus pertinentes au bon moment, améliorant ainsi l'efficacité et l'expérience utilisateur.

Que votre application soit une API backend ASP.NET Core, un client lourd WPF, ou toute autre application .NET, GoTo.NET s'intègre de manière transparente grâce à son architecture agnostique à l'interface utilisateur et basée sur l'injection de dépendances.

---

## 2. Fonctionnalités Clés

* **Prédictions Multi-algorithmes :** Combinez les forces de plusieurs algorithmes de prédiction (fréquence, chaînes de Markov, logique de conception métier, et Intelligence Artificielle via ML.NET) pour des suggestions complètes et pertinentes.
* **Menu Personnalisable :** Offrez à vos utilisateurs la possibilité d'ajouter, de supprimer et de prioriser manuellement leurs propres raccourcis.
* **Architecture Modulaire :** Étendez ou remplacez facilement les composants internes (algorithmes, stockages de données) pour répondre à vos besoins spécifiques.
* **Agnostique à l'UI :** La bibliothèque gère la logique de prédiction. Vous contrôlez l'affichage et les actions de navigation réelles, quelle que soit votre technologie d'interface utilisateur (WPF, ASP.NET Core, Blazor, MAUI, etc.).
* **Contrôle de l'Entraînement :** Gérez la manière et le moment où les modèles d'apprentissage apprennent, avec des modes adaptés au développement et à la production.
* **Rapports de Progression d'Entraînement :** Recevez des mises à jour en temps réel sur l'avancement de l'apprentissage des algorithmes.

---

## 3. Démarrage Rapide

Intégrer **GoTo.NET** dans votre projet .NET est simple grâce à son extension `IServiceCollection`.

### Installation

1.  **Créez votre projet .NET** (par exemple, un projet ASP.NET Core Web API, une application WPF, ou une application console .NET 8).
2.  **Ajoutez le package NuGet `GoToNet.Core` :**
    Ouvrez la console du gestionnaire de packages NuGet dans Visual Studio ou utilisez la CLI .NET :
    ```bash
    dotnet add package GoToNet.Core
    ```
    *Note : Selon les algorithmes que vous choisissez d'utiliser (en particulier ML.NET), vous devrez peut-être ajouter des packages `Microsoft.ML.*` supplémentaires à votre projet.*

### Configuration Essentielle

La configuration de **GoTo.NET** se fait au démarrage de votre application, généralement dans la méthode `ConfigureServices` de votre `Startup.cs` (pour ASP.NET Core) ou `Program.cs` / `App.xaml.cs` (pour les applications console/WPF utilisant l'hôte générique).

```csharp
// Exemple de configuration dans Program.cs (pour une application console ou ASP.NET Core)
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
                    // 1. Enregistrez vos implémentations pour les dépendances de GoTo.NET
                    // GoTo.NET est agnostique à la persistance et à l'UI, vous devez lui fournir comment interagir.

                    // Stores de données (obligatoires)
                    // Pour le prototypage, nous utilisons les implémentations en mémoire par défaut.
                    // En production, vous devriez remplacer ces services par vos propres implémentations
                    // (ex: SqliteHistoryStore, DbUserPreferencesStore) pour une persistance réelle.
                    services.AddSingleton<INavigationHistoryStore, InMemoryHistoryStore>();
                    services.AddSingleton<IUserMenuPreferencesStore, InMemoryUserMenuPreferencesStore>();
                    services.AddSingleton<UserMenuBuilder>(); // Le constructeur de menu utilisateur

                    // Implémentations pour l'intégration avec votre application (obligatoires)
                    // Vous DEVEZ fournir des implémentations pour ces interfaces.
                    // Placez ces classes dans votre projet consommateur (ex: MonProjet/Services/).
                    services.AddSingleton<INavigationNotifier, MyAppNavigationNotifier>(); // Votre classe qui gère l'affichage des suggestions
                    services.AddSingleton<INavigationActionHandler, MyAppNavigationActionHandler>(); // Votre classe qui gère les actions de navigation
                    services.AddSingleton<IAppNavigationCatalog, MyAppNavigationCatalog>(); // Votre classe qui liste toutes les pages de votre app

                    // Si vous utilisez l'algorithme DesignFlowAlgorithm, vous DEVEZ enregistrer un fournisseur de règles de conception.
                    // GoTo.NET ne fournit PAS d'implémentation par défaut en production, car les règles sont spécifiques à votre app.
                    // DefaultDesignFlowRulesProvider est fourni pour le prototypage/démonstration.
                    services.AddSingleton<IDesignFlowRulesProvider, DefaultDesignFlowRulesProvider>(); // Ou votre propre fournisseur de règles

                    // 2. Enregistrez les algorithmes de prédiction que vous souhaitez utiliser
                    // Chaque algorithme DOIT être enregistré ici pour que le conteneur de DI puisse l'instancier.
                    // Ceux-ci sont enregistrés en tant que Singletons (car les algorithmes conservent un état entraîné).
                    services.AddSingleton<IPredictionAlgorithm, FrequencyAlgorithm>();
                    services.AddSingleton<IPredictionAlgorithm, MarkovChainAlgorithm>();
                    services.AddSingleton<IDesignFlowAlgorithm, DesignFlowAlgorithm>(); // Enregistré comme IDesignFlowAlgorithm
                    services.AddSingleton<IPredictionAlgorithm, MLNetAlgorithm>();

                    // 3. Appelez l'extension AddGoToNet pour configurer le moteur
                    services.AddGoToNet(options =>
                    {
                        // Ajoutez les algorithmes que vous souhaitez que le moteur utilise.
                        // Ils seront résolus par le conteneur de DI.
                        options.AddAlgorithm<FrequencyAlgorithm>();
                        options.AddAlgorithm<MarkovChainAlgorithm>();
                        options.AddDesignFlowAlgorithm<DesignFlowAlgorithm>(); // Utiliser la surcharge spécifique
                        options.AddAlgorithm<MLNetAlgorithm>();

                        // Lier les implémentations des interfaces d'intégration client
                        options.SetNavigationNotifier<MyAppNavigationNotifier>();
                        options.SetNavigationActionHandler<MyAppNavigationActionHandler>();
                        options.SetAppNavigationCatalog<MyAppNavigationCatalog>();

                        // Lier le fournisseur de règles de design flow (si vous voulez le rendre explicite)
                        options.SetDesignFlowRulesProvider<DefaultDesignFlowRulesProvider>();

                        // Définissez la stratégie d'entraînement du moteur.
                        options.TrainingMode = TrainingMode.OnStartupOnce; // Exemple: entraînement une fois au démarrage
                        // options.TrainingMode = TrainingMode.ContinuousDevelopment; // Pour le développement/démo
                        // options.TrainingMode = TrainingMode.Manual; // Pour déclencher l'entraînement manuellement
                        // options.TrainingMode = TrainingMode.Custom; // Pour une logique d'entraînement personnalisée
                    });

                    // Ajoutez ici d'autres services spécifiques à votre application.
                })
                .Build();

            // Démarrage de l'hôte (nécessaire pour que les services soient créés et que l'entraînement démarre)
            await host.StartAsync();

            // --- Exécutez ici la logique de votre application ---
            // Vous pouvez récupérer le moteur et le constructeur de menu depuis le service provider
            var predictionEngine = host.Services.GetRequiredService<GoToPredictionEngine>();
            var menuBuilder = host.Services.GetRequiredService<UserMenuBuilder>();

            // S'abonner aux événements d'entraînement pour suivre la progression (optionnel)
            predictionEngine.AlgorithmsTrained += (sender, e) => Console.WriteLine("\nGoTo.NET: Entraînement global terminé !");
            predictionEngine.TrainingProgressUpdated += (sender, e) =>
                Console.WriteLine("GoTo.NET Progrès: {0} - {1} ({2}%) - {3}", e.AlgorithmName, e.CurrentStep, e.ProgressPercentage, e.Message);

            Console.WriteLine("Application démarrée. Simulez des événements...");
            // Exemples d'utilisation (voir section ci-dessous)
            await predictionEngine.RecordNavigationAsync(new NavigationEvent { UserId = "user1", CurrentPageOrFeature = "Dashboard" });
            var suggestions = await predictionEngine.GetSuggestionsAsync("user1", "Dashboard");

            // Pour les applications console, assurez-vous de garder l'application en cours d'exécution
            Console.WriteLine("Appuyez sur une touche pour arrêter l'application.");
            Console.ReadKey();

            await host.StopAsync();
        }
    }
}
````

### Implémentations Client Essentielles

Ces classes sont les implémentations concrètes des interfaces de GoTo.NET que votre application doit fournir. Elles devraient être placées dans un dossier de votre projet consommateur (ex: `MonProjet/Services/` ou `MonProjet/Infrastructure/`).

```csharp
// Implémentation pour INavigationNotifier
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyAppNavigationNotifier : INavigationNotifier
{
    public void UpdateSuggestionsDisplay(string userId, IEnumerable<SuggestedItem> suggestedItems)
    {
        Console.WriteLine("\n--- Suggestions pour {0} ---", userId);
        if (!suggestedItems.Any())
        {
            Console.WriteLine("Aucune suggestion.");
            return;
        }
        foreach (var item in suggestedItems)
        {
            Console.WriteLine("- {0} (Score: {1:F2}, Raison: {2})", item.Name, item.Score, item.Reason);
        }
        Console.WriteLine("----------------------------------");
    }
}

// Implémentation pour INavigationActionHandler
using GoToNet.Core.Interfaces;
using System;

public class MyAppNavigationActionHandler : INavigationActionHandler
{
    public bool PerformNavigation(string userId, string itemName)
    {
        Console.WriteLine("[APP] {0} a demandé la navigation vers : {1}", userId, itemName);
        // Ici, vous ajouteriez la logique de navigation réelle de votre application.
        // Exemple WPF: Window.GetWindow(App.Current.MainWindow).FindName("MainFrame") as Frame)?.Navigate(new Uri($"/{itemName}.xaml", UriKind.Relative));
        // Exemple ASP.NET Core: return RedirectToAction(itemName); (dans un contrôleur)
        return true;
    }
}

// Implémentation pour IAppNavigationCatalog
using GoToNet.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

public class MyAppNavigationCatalog : IAppNavigationCatalog
{
    private readonly List<string> _allAvailablePages = new List<string>
    {
        "Dashboard", "Projects", "Tasks", "Reports", "Users", "Settings", "Help",
        "Invoices", "Payments", "Create Project", "User Profile",
        "Details/ProjectX", "Admin/Users", "Preferences" // Exemple d'autres pages plus spécifiques
    };
    public IEnumerable<string> GetAllAvailableNavigationItems()
    {
        return _allAvailablePages;
    }
}
```

### Utilisation du Moteur

Une fois **GoTo.NET** configuré, vous interagissez principalement avec deux services injectés : `GoToPredictionEngine` et `UserMenuBuilder`.

1.  **Enregistrer les actions de navigation :**
    Chaque fois qu'un utilisateur effectue une action significative dans votre application, enregistrez-la pour que **GoTo.NET** puisse apprendre.

    ```csharp
    // Dans votre code d'application (ex: un contrôleur ASP.NET Core, un ViewModel WPF)
    // Assurez-vous d'injecter GoToPredictionEngine.
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

2.  **Obtenir des suggestions de raccourcis :**
    Lorsque vous avez besoin d'afficher le menu de raccourcis, demandez au moteur des suggestions.

    ```csharp
    // Dans votre code d'application
    public async Task<IEnumerable<SuggestedItem>> GetMyShortcuts(string userId, string currentPageContext)
    {
        // Le moteur calculera et notifiera votre INavigationNotifier.
        // Vous n'avez pas besoin de récupérer directement les suggestions de cette méthode,
        // car elles seront "poussées" vers votre UI via UpdateSuggestionsDisplay.
        await _predictionEngine.GetSuggestionsAsync(userId, currentPageContext, 5);

        // Si vous avez besoin de les récupérer directement pour d'autres usages,
        // GetSuggestionsAsync retourne aussi la liste.
        var directSuggestions = await _predictionEngine.GetSuggestionsAsync(userId, currentPageContext, 5);
        return directSuggestions;
    }
    ```

3.  **Gérer les raccourcis personnalisés :**
    Permettez à vos utilisateurs d'ajouter ou de supprimer des éléments de leur menu de raccourcis.

    ```csharp
    // Dans votre code d'application, après avoir injecté UserMenuBuilder
    public async Task AddUserShortcut(string userId, string itemName)
    {
        await _menuBuilder.AddItemToUserMenuAsync(userId, itemName, 0); // 0 pour haute priorité
        // Après l'ajout, vous voudrez peut-être rafraîchir les suggestions :
        // await _predictionEngine.GetSuggestionsAsync(userId, "current_page");
    }

    public async Task RemoveUserShortcut(string userId, string itemName)
    {
        await _menuBuilder.RemoveItemFromUserMenuAsync(userId, itemName);
        // Après la suppression, rafraîchir les suggestions
    }
    ```

4.  **Déclencher la navigation à partir d'une suggestion :**
    Lorsque l'utilisateur clique sur un raccourci suggéré par **GoTo.NET**, demandez au moteur de déclencher l'action de navigation.

    ```csharp
    // Dans votre code d'application (ex: une commande WPF, un gestionnaire de clic Angular)
    public async Task OnShortcutClicked(string userId, SuggestedItem item)
    {
        bool navigated = _predictionEngine.PerformSuggestedNavigation(userId, item.Name);
        if (navigated)
        {
            // IMPORTANT: Enregistrez cette action aussi, pour que GoTo.NET apprenne de ce clic.
            await _predictionEngine.RecordNavigationAsync(new NavigationEvent
            {
                UserId = userId,
                CurrentPageOrFeature = item.Name,
                PreviousPageOrFeature = "page_où_le_menu_était_ouvert", // Contexte
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }
    ```

-----

## 4\. Concepts Fondamentaux

### Événements de Navigation (`NavigationEvent`)

```csharp
public class NavigationEvent
{
    public string UserId { get; set; } // L'identifiant unique de l'utilisateur
    public string CurrentPageOrFeature { get; set; } // Nom de la page/fonctionnalité visitée
    public string? PreviousPageOrFeature { get; set; } // Page précédente (pour les séquences)
    public DateTimeOffset Timestamp { get; set; } // Moment de l'événement
    // ... d'autres champs optionnels comme SessionId, ContextData
}
```

### Suggestions (`SuggestedItem`)

```csharp
public class SuggestedItem
{
    public string Name { get; set; } // Le nom de la page/fonctionnalité suggérée
    public double Score { get; set; } // Score de pertinence (plus c'est élevé, plus c'est pertinent)
    public string Reason { get; set; } // La raison de la suggestion ("Frequency", "ML.NET", "UserCustom", "DesignFlow", "MarkovChain")
}
```

### Gestion de la Persistance (Stores)

**GoTo.NET** utilise des interfaces pour interagir avec les données. Vous devez fournir des implémentations de :

* `INavigationHistoryStore` : Pour stocker et récupérer les `NavigationEvent` (historique).
* `IUserMenuPreferencesStore` : Pour stocker et récupérer les `UserCustomMenuItem` (préférences personnalisées).

**GoTo.NET** fournit des implémentations en mémoire (`InMemoryHistoryStore`, `InMemoryUserMenuPreferencesStore`) pour le prototypage. En production, vous implémenteriez des stores basés sur une base de données (SQL, NoSQL).

### Algorithmes de Prédiction

Le cœur de l'intelligence de **GoTo.NET**. Chaque algorithme implémente `IPredictionAlgorithm` et propose des suggestions selon son propre paradigme.

* `FrequencyAlgorithm`
* `MarkovChainAlgorithm`
* `DesignFlowAlgorithm`
* `MLNetAlgorithm`

Vous ajoutez les algorithmes souhaités lors de la configuration de **GoTo.NET**.

### Le Moteur de Prédiction (`GoToPredictionEngine`)

C'est la façade principale de **GoTo.NET**. Il orchestre :

* L'enregistrement des événements de navigation.
* L'entraînement des algorithmes (gérant les stratégies et la progression).
* La collecte et la combinaison des suggestions de tous les algorithmes actifs.
* L'intégration des raccourcis personnalisés de l'utilisateur.
* La notification de votre application cliente des suggestions mises à jour.
* La demande d'exécution d'actions de navigation réelles.

### Personnalisation du Menu par l'Utilisateur

Le service `UserMenuBuilder` permet à vos utilisateurs finaux d'avoir un contrôle explicite sur leur menu de raccourcis.

```csharp
public class UserCustomMenuItem
{
    public string UserId { get; set; }
    public string ItemName { get; set; }
    public int Order { get; set; } // Plus petit = plus haute priorité
}
```

### Intégration avec votre Application

**GoTo.NET** est agnostique à votre technologie UI. Il communique via ces interfaces que votre application doit implémenter et enregistrer dans le conteneur de DI :

* `INavigationNotifier` : Reçoit les listes de `SuggestedItem` à afficher.
* `INavigationActionHandler` : Exécute la logique de navigation réelle de votre application.
* `IAppNavigationCatalog` : Fournit à **GoTo.NET** (en particulier à `MLNetAlgorithm`) la liste complète de toutes les pages/fonctionnalités navigables de votre application.

### Stratégies d'Entraînement des Algorithmes

Vous contrôlez la manière dont les modèles sont entraînés via les `GoToNetOptions.TrainingMode` :

* `TrainingMode.OnStartupOnce` : Entraînement complet une seule fois au démarrage de l'application (en arrière-plan, non bloquant). Idéal pour la production si les modèles peuvent être entraînés hors ligne ou si les données ne changent pas trop souvent.
* `TrainingMode.ContinuousDevelopment` : L'entraînement est déclenché en arrière-plan après chaque `RecordNavigationAsync` (pour le développement/démo). **Non recommandé pour la production avec un volume élevé d'événements.**
* `TrainingMode.Manual` : L'application cliente doit explicitement appeler `GoToPredictionEngine.TrainAlgorithmsAsync()` pour déclencher l'entraînement.
* `TrainingMode.Custom` : Vous fournissez une logique personnalisée pour déclencher l'entraînement (par exemple, toutes les heures, après X nouveaux événements, etc.).

Vous pouvez suivre la progression de l'entraînement via les événements `AlgorithmsTrained` (entraînement global terminé) et `TrainingProgressUpdated` (mises à jour granulaires par algorithme) du `GoToPredictionEngine`.

-----

## 5\. Algorithmes de Prédiction Détaillés

**GoTo.NET** utilise une approche **hybride**, combinant les forces de plusieurs algorithmes pour offrir des suggestions les plus pertinentes possibles. Chaque algorithme a un `Weight` que vous pouvez configurer pour ajuster son influence dans le score final.

### Approche Hybride : La Force de la Combinaison

Aucun algorithme seul n'est parfait pour tous les scénarios. La combinaison permet à **GoTo.NET** de :

* **Gérer la nouveauté :** Si un utilisateur est nouveau, les algorithmes basés sur l'historique n'auront rien à dire. Le **DesignFlowAlgorithm** peut alors prendre le relais.
* **Apporter du contexte :** Le **MarkovChainAlgorithm** et l'**MLNetAlgorithm** sont excellents pour des prédictions basées sur le contexte actuel de navigation.
* **Découvrir des motifs complexes :** L'**MLNetAlgorithm** peut identifier des corrélations subtiles (heure, jour, historique combiné) que les règles simples manquent.
* **Garantir la pertinence métier :** Le **DesignFlowAlgorithm** assure que les actions clés définies par votre application sont toujours suggérées si nécessaire.
* **Offrir la robustesse :** Si un algorithme échoue (pas assez de données, modèle non entraîné), les autres peuvent compenser.

### Fréquence (`FrequencyAlgorithm`)

* **Paradigme :** Le plus simple. "Ce que l'utilisateur fait le plus souvent, il le voudra probablement encore."
* **Valeur ajoutée :** Identifie rapidement les "favoris" implicites et les habitudes d'utilisation les plus fortes.
* **Utilisation :** Utile comme base pour la popularité générale des éléments pour un utilisateur donné.

### Chaînes de Markov (`MarkovChainAlgorithm`)

* **Paradigme :** Prédiction séquentielle du premier ordre. "Après être allé à la page X, les utilisateurs vont le plus souvent à la page Y."
* **Valeur ajoutée :** Apporte une dimension **contextuelle forte**. Idéal pour prédire la prochaine étape logique dans un flux de travail (`Factures` -\> `Payer une facture`).
* **Utilisation :** Très efficace lorsque les actions des utilisateurs suivent des séquences prévisibles.

### Flux de Conception (`DesignFlowAlgorithm` et `IDesignFlowAlgorithm`)

* **Paradigme :** Intègre la **logique métier** et les **flux définis par le design de votre application**.
* **Valeur ajoutée :** Permet de suggérer des actions **logiques ou importantes** (nouvelles fonctionnalités, étapes critiques d'un workflow, actions de "démarrage") même si elles ne sont pas encore fréquemment utilisées ou n'apparaissent pas dans les séquences passées. Il permet à votre application de **guider proactivement** l'utilisateur.
* **Comment ça marche :** Cet algorithme dépend d'un fournisseur de règles, l'`IDesignFlowRulesProvider`. Vous définissez des règles comme "quand l'utilisateur est sur 'Projets', suggérer 'Créer un nouveau projet' et 'Gérer les équipes'".
* **Votre rôle (`IDesignFlowAlgorithm`) :** Pour que cet algorithme fonctionne, vous devez :
    1.  Fournir une implémentation de **`IDesignFlowRulesProvider`** (par exemple, chargeant les règles depuis un fichier JSON, une base de données, ou codées en dur pour le prototypage).
    2.  Enregistrer cette implémentation dans le conteneur de DI (`services.AddSingleton<IDesignFlowRulesProvider, MyDesignRulesProvider>();`).
    3.  Ajouter l'algorithme à **GoTo.NET** en utilisant la surcharge `options.AddDesignFlowAlgorithm<DesignFlowAlgorithm>();` qui garantit que sa dépendance (`IDesignFlowRulesProvider`) est bien résolue.

### Intelligence Artificielle (`MLNetAlgorithm`)

* **Paradigme :** Utilise le Machine Learning (via ML.NET) pour découvrir des **motifs complexes et non évidents** dans les données de navigation. Il peut prendre en compte des caractéristiques variées (utilisateur, page actuelle, page précédente, heure du jour, jour de la semaine, etc.) pour prédire la probabilité de visiter n'importe quelle autre page.
* **Valeur ajoutée :** Apporte l'**intelligence adaptative**, capable de repérer des tendances subtiles et de fournir des prédictions plus personnalisées et précises. Gère des scénarios plus complexes que les algorithmes basés sur des règles.
* **Utilisation :** Idéal pour des prédictions à haute valeur ajoutée où les règles simples ne suffisent pas. Nécessite une quantité suffisante de données d'historique pour l'entraînement. Dépend de `IAppNavigationCatalog` pour connaître toutes les pages possibles.

-----

## 6\. Gérer les Prédictions : Individuelles ou Globales

**GoTo.NET** est conçu pour la flexibilité. Vous pouvez obtenir des prédictions qui sont :

### Prédictions pour un Utilisateur Spécifique

C'est le cas d'usage le plus courant. La prédiction est hautement personnalisée en fonction de l'historique et des préférences d'un utilisateur donné.

* **Comment faire :** Vous utilisez simplement l'`UserId` de l'utilisateur actuel dans les méthodes `RecordNavigationAsync` et `GetSuggestionsAsync`.
  ```csharp
  // Enregistrer une action pour "Alice"
  await predictionEngine.RecordNavigationAsync(new NavigationEvent { UserId = "Alice", CurrentPageOrFeature = "Rapports RH" });

  // Obtenir des suggestions pour "Alice"
  var suggestionsForAlice = await predictionEngine.GetSuggestionsAsync("Alice", "Dashboard");
  ```
  Les algorithmes de **Fréquence** et **Markov** fonctionneront sur l'historique d'Alice. L'**MLNetAlgorithm** entraînera un modèle qui inclut l'`UserId` comme caractéristique, lui permettant de prédire le comportement unique d'Alice. Les raccourcis personnalisés seront bien sûr ceux d'Alice.

### Prédictions Globales ou Basées sur un Rôle

Vous pourriez vouloir un menu de raccourcis qui est le même pour tous les utilisateurs, ou pour tous les utilisateurs ayant un certain rôle (ex: tous les "Administrateurs").

* **Comment faire :**
    1.  **Utiliser un `UserId` générique :** Lors de l'enregistrement des `NavigationEvent`, utilisez un `UserId` générique (par exemple, "GLOBAL\_USER" ou "ADMIN\_ROLE"). Tous les événements enregistrés sous cet ID générique construiront un historique "global". Ensuite, demandez des suggestions pour cet ID générique.
        ```csharp
        // Enregistrer une action qui devrait influencer les suggestions globales
        await predictionEngine.RecordNavigationAsync(new NavigationEvent { UserId = "GLOBAL_USER", CurrentPageOrFeature = "Page Marketing" });

        // Obtenir des suggestions "globales" pour tous
        var globalSuggestions = await predictionEngine.GetSuggestionsAsync("GLOBAL_USER", "Landing Page");
        ```
    2.  **Exploiter `DesignFlowAlgorithm` :** Cet algorithme est idéal pour les suggestions basées sur des règles métier ou de conception qui sont universelles ou spécifiques à un rôle.
    3.  **Filtrage ou Post-traitement :** Vous pouvez toujours obtenir les suggestions pour un utilisateur spécifique, puis les filtrer ou les augmenter avec des règles globales ou spécifiques à un rôle *après* que **GoTo.NET** ait retourné ses prédictions.
    4.  **Contexte Étendu :** L'**MLNetAlgorithm** peut utiliser des données de contexte additionnelles (`ContextData` dans `NavigationEvent`, par exemple le `Role`). Vous pouvez entraîner le modèle pour qu'il tienne compte du rôle, permettant des prédictions différentes pour différents rôles sans changer l'`UserId` principal.
        ```csharp
        // Enregistrer avec un rôle
        await predictionEngine.RecordNavigationAsync(new NavigationEvent { UserId = "user1", CurrentPageOrFeature = "Admin Panel", ContextData = new Dictionary<string, string>{ {"Role", "Admin"} } });

        // Obtenir des suggestions pour un rôle spécifique via le contexte
        var suggestionsForAdmin = await predictionEngine.GetSuggestionsAsync("user1", "Admin Panel", contextData: new Dictionary<string, string>{ {"Role", "Admin"} });
        ```

-----

## 7\. Configuration Avancée

La méthode `services.AddGoToNet()` vous offre un contrôle granulaire sur la configuration de votre moteur de prédiction.

### Ajout d'Algorithmes Spécifiques

Vous devez enregistrer chaque algorithme dans le conteneur de DI, puis l'ajouter aux options de **GoTo.NET**.

```csharp
// Dans Program.cs / App.xaml.cs de votre application consommatrice

// Enregistrement des algorithmes (en tant que Singletons, car ils conservent un état entraîné)
services.AddSingleton<IPredictionAlgorithm, FrequencyAlgorithm>();
services.AddSingleton<IPredictionAlgorithm, MarkovChainAlgorithm>();
// Note : DesignFlowAlgorithm implémente IDesignFlowAlgorithm et a une dépendance à IDesignFlowRulesProvider
services.AddSingleton<IDesignFlowAlgorithm, DesignFlowAlgorithm>();
services.AddSingleton<IPredictionAlgorithm, MLNetAlgorithm>();

// Configuration de GoTo.NET
services.AddGoToNet(options =>
{
    // Ajoutez uniquement les algorithmes que vous souhaitez utiliser.
    options.AddAlgorithm<FrequencyAlgorithm>(); // Algorithme général
    options.AddAlgorithm<MarkovChainAlgorithm>(); // Algorithme général

    // Pour DesignFlowAlgorithm, utilisez la surcharge spécifique pour plus de clarté.
    // Sa dépendance IDesignFlowRulesProvider sera résolue automatiquement par DI si elle est enregistrée.
    options.AddDesignFlowAlgorithm<DesignFlowAlgorithm>();

    options.AddAlgorithm<MLNetAlgorithm>(); // Algorithme ML.NET

    // ... (autres configurations comme SetNavigationNotifier, SetAppNavigationCatalog) ...
});
```

### Personnalisation des Fournisseurs (Stores et Règles)

Remplacez les implémentations par défaut par les vôtres si vous avez besoin d'une persistance spécifique (base de données, fichiers) ou de règles de design flow personnalisées.

```csharp
// Dans Program.cs / App.xaml.cs

// Remplace l'InMemoryHistoryStore par votre SqliteHistoryStore
services.AddSingleton<INavigationHistoryStore, MySqliteHistoryStore>();

// Remplace le DefaultDesignFlowRulesProvider par votre CustomJsonDesignFlowRulesProvider
// Ceci est OBLIGATOIRE si vous utilisez DesignFlowAlgorithm et ne voulez pas le provider par défaut.
services.AddSingleton<IDesignFlowRulesProvider, MyJsonFileDesignFlowRulesProvider>();

services.AddGoToNet(options => {
    // ...
    // Pas besoin de changer l'appel AddDesignFlowAlgorithm<DesignFlowAlgorithm>();
    // Il utilisera automatiquement l'implémentation de IDesignFlowRulesProvider que vous avez enregistrée.
});
```

### Modes d'Entraînement Personnalisés

Le mode `TrainingMode.Custom` vous permet de définir une logique d'entraînement entièrement personnalisée.

```csharp
// Dans Program.cs / App.xaml.cs
services.AddGoToNet(options => {
    options.TrainingMode = TrainingMode.Custom;
    options.CustomTrainingLogic = async (serviceProvider) => {
        // Obtenez le moteur pour déclencher l'entraînement
        var engine = serviceProvider.GetRequiredService<GoToPredictionEngine>();
        // Vous pouvez obtenir d'autres services ici si votre logique personnalisée en a besoin, ex:
        // var historyStore = serviceProvider.GetRequiredService<INavigationHistoryStore>();

        Console.WriteLine("[GoToNet Custom Training] Déclenchement de l'entraînement personnalisé...");
        // Vous pouvez définir votre propre logique ici :
        // - Entraîner à des intervalles spécifiques.
        // - Entraîner uniquement si un certain volume de nouvelles données est atteint.
        // - Entraîner un sous-ensemble d'algorithmes.
        await engine.TrainAlgorithmsAsync(); // Appelez l'entraînement complet quand vous le décidez
        Console.WriteLine("[GoToNet Custom Training] Entraînement personnalisé terminé.");
    };
});
```

-----

## 8\. Exemples et Démonstrations

Pour voir **GoTo.NET** en action, explorez les projets de démonstration dans le dépôt GitHub :

* **`GoToNet.ConsoleTest` :**  Todo : Un projet console simple pour tester toutes les fonctionnalités de la bibliothèque et observer les logs d'entraînement.
* **`GoToNet.ApiDemo` :** Todo :  Une application ASP.NET Core Web API montrant comment exposer GoTo.NET via des endpoints HTTP.
* **`GoToNet.WpfDemo` :*  ToDo : Une application WPF démontrant l'intégration de GoTo.NET dans un client lourd avec une interface utilisateur dynamique.

-----

## 9\. Contribution

Nous accueillons les contributions \! Si vous souhaitez améliorer **GoTo.NET**, corriger un bug ou ajouter une nouvelle fonctionnalité, n'hésitez pas à ouvrir une [issue](https://www.google.com/search?q=https://github.com/votre_repo/GoToNet/issues) ou à soumettre une [pull request](https://www.google.com/search?q=https://github.com/votre_repo/GoToNet/pulls) sur notre dépôt GitHub.

-----

## 10\. Licence

Ce projet est sous licence MIT. Voir le fichier [LICENSE](https://www.google.com/search?q=LICENSE) pour plus de détails.
