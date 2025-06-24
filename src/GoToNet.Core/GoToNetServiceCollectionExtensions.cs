// /src/GoToNet.Core/Extensions/GoToNetServiceCollectionExtensions.cs
using GoToNet.Core.Algorithms;
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using GoToNet.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoToNet.Core.Extensions
{
    /// <summary>
    /// Fournit des méthodes d'extension pour IServiceCollection afin de configurer
    /// facilement les services de la bibliothèque GoTo.NET.
    /// </summary>
    public static class GoToNetServiceCollectionExtensions
    {
        /// <summary>
        /// Ajoute et configure les services de la bibliothèque GoTo.NET au conteneur de dépendances.
        /// </summary>
        /// <param name="services">L'instance de IServiceCollection.</param>
        /// <param name="configureOptions">Une action pour configurer les options avancées de GoTo.NET.</param>
        /// <returns>L'instance de IServiceCollection pour le chaînage.</returns>
        public static IServiceCollection AddGoToNet(
            this IServiceCollection services,
            Action<GoToNetOptions>? configureOptions = null)
        {
            // Enregistrement des stores par défaut (en mémoire pour le prototype).
            services.AddSingleton<INavigationHistoryStore, InMemoryHistoryStore>();
            services.AddSingleton<IUserMenuPreferencesStore, InMemoryUserMenuPreferencesStore>();

            // IMPORTANT: IDesignFlowRulesProvider n'est PAS enregistré par défaut ici.
            // Il doit être enregistré explicitement par l'application consommatrice si DesignFlowAlgorithm est utilisé.

            // Enregistrement des services cœur de GoTo.NET
            services.AddSingleton<UserMenuBuilder>();

            // Crée et configure les options
            var options = new GoToNetOptions();
            configureOptions?.Invoke(options);

            // Enregistre le moteur de prédiction principal
            services.AddSingleton<GoToPredictionEngine>(serviceProvider =>
            {
                var historyStore = serviceProvider.GetRequiredService<INavigationHistoryStore>();
                var preferencesStore = serviceProvider.GetRequiredService<IUserMenuPreferencesStore>();

                var engine = new GoToPredictionEngine(historyStore, preferencesStore);

                // Configure les interfaces d'intégration client si les factories sont fournies dans les options.
                if (options.NavigationNotifierFactory != null &&
                    options.NavigationActionHandlerFactory != null &&
                    options.AppNavigationCatalogFactory != null)
                {
                    var notifier = options.NavigationNotifierFactory(serviceProvider);
                    var actionHandler = options.NavigationActionHandlerFactory(serviceProvider);
                    var catalog = options.AppNavigationCatalogFactory(serviceProvider);

                    engine.ConfigureClientIntegration(notifier, actionHandler, catalog);
                }
                else
                {
                    Console.WriteLine("[GoToNet] Avertissement: Les interfaces d'intégration client (INavigationNotifier, INavigationActionHandler, IAppNavigationCatalog) ne sont pas toutes configurées via AddGoToNet.Set...(). Certaines fonctionnalités (ML.NET, notifications UI) peuvent être limitées.");
                }

                // Instancie et ajoute les algorithmes via leurs factories.
                if (options.PredictionAlgorithmFactories.Any())
                {
                    foreach (var algoFactory in options.PredictionAlgorithmFactories)
                    {
                        // Résout l'algorithme en utilisant le serviceProvider, permettant la résolution de ses propres dépendances.
                        var algo = algoFactory(serviceProvider); 
                        engine.AddAlgorithm(algo);
                    }
                }
                else
                {
                    Console.WriteLine("[GoToNet] Avertissement: Aucun algorithme de prédiction n'a été ajouté à GoTo.NET. Le moteur ne fournira que des suggestions basées sur les raccourcis personnalisés (s'il y en a).");
                }


                // Définit la stratégie d'entraînement sur le moteur
                engine.SetTrainingStrategy(options.TrainingMode, options.CustomTrainingLogic);

                // Déclenche l'entraînement initial basé sur la stratégie choisie (si applicable).
                if (options.TrainingMode == TrainingMode.OnStartupOnce)
                {
                    _ = Task.Run(async () => await engine.TrainAlgorithmsAsync()); // "Fire-and-forget"
                }

                return engine;
            });

            return services;
        }

        // --- MÉTHODES D'EXTENSION POUR GoToNetOptions ---

        /// <summary>
        /// Ajoute un algorithme de prédiction à la liste des algorithmes qui seront utilisés par le moteur.
        /// L'algorithme sera instancié par le conteneur d'injection de dépendances.
        /// </summary>
        /// <typeparam name="TAlgorithm">Le type de l'algorithme à ajouter (doit implémenter IPredictionAlgorithm et être enregistré dans DI).</typeparam>
        /// <param name="options">L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</param>
        /// <returns>L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</returns>
        public static GoToNetOptions AddAlgorithm<TAlgorithm>(this GoToNetOptions options) where TAlgorithm : class, IPredictionAlgorithm
        {
            options.PredictionAlgorithmFactories.Add(sp => sp.GetRequiredService<TAlgorithm>());
            return options;
        }

        /// <summary>
        /// Ajoute un algorithme de prédiction de flux de conception, en s'assurant que ses dépendances spécifiques sont résolues.
        /// Utilisez cette surcharge pour les algorithmes qui implémentent <see cref="IDesignFlowAlgorithm"/>.
        /// </summary>
        /// <typeparam name="TDesignAlgorithm">Le type de l'algorithme de flux de conception (doit implémenter IDesignFlowAlgorithm et être enregistré dans DI).</typeparam>
        /// <param name="options">L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</param>
        /// <returns>L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</returns>
        public static GoToNetOptions AddDesignFlowAlgorithm<TDesignAlgorithm>(this GoToNetOptions options)
            where TDesignAlgorithm : class, IDesignFlowAlgorithm // Contrainte: doit implémenter IDesignFlowAlgorithm
        {
            options.PredictionAlgorithmFactories.Add(sp => sp.GetRequiredService<TDesignAlgorithm>());
            return options;
        }


        /// <summary>
        /// Ajoute un algorithme de prédiction en fournissant directement une instance de l'algorithme.
        /// Utilisez cette méthode si l'algorithme n'a pas de dépendances complexes à résoudre par DI.
        /// </summary>
        /// <param name="options">L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</param>
        /// <param name="algorithm">L'instance de l'algorithme à ajouter.</param>
        /// <returns>L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</returns>
        public static GoToNetOptions AddAlgorithmInstance(this GoToNetOptions options, IPredictionAlgorithm algorithm)
        {
            options.PredictionAlgorithmFactories.Add(sp => algorithm);
            return options;
        }

        /// <summary>
        /// Configure le fournisseur de notification de l'interface utilisateur.
        /// </summary>
        /// <typeparam name="TNotifier">Le type implémentant <see cref="INavigationNotifier"/>.</typeparam>
        /// <param name="options">L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</param>
        /// <returns>L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</returns>
        public static GoToNetOptions SetNavigationNotifier<TNotifier>(this GoToNetOptions options) where TNotifier : INavigationNotifier
        {
            options.NavigationNotifierFactory = (sp) => sp.GetRequiredService<TNotifier>();
            return options;
        }

        /// <summary>
        /// Configure le gestionnaire d'actions de navigation.
        /// </summary>
        /// <typeparam name="THandler">Le type implémentant <see cref="INavigationActionHandler"/>.</typeparam>
        /// <param name="options">L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</param>
        /// <returns>L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</returns>
        public static GoToNetOptions SetNavigationActionHandler<THandler>(this GoToNetOptions options) where THandler : INavigationActionHandler
        {
            options.NavigationActionHandlerFactory = (sp) => sp.GetRequiredService<THandler>();
            return options;
        }

        /// <summary>
        /// Configure le catalogue de navigation de l'application.
        /// </summary>
        /// <typeparam name="TCatalog">Le type implémentant <see cref="IAppNavigationCatalog"/>.</typeparam>
        /// <param name="options">L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</param>
        /// <returns>L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</returns>
        public static GoToNetOptions SetAppNavigationCatalog<TCatalog>(this GoToNetOptions options) where TCatalog : IAppNavigationCatalog
        {
            options.AppNavigationCatalogFactory = (sp) => sp.GetRequiredService<TCatalog>();
            return options;
        }

        /// <summary>
        /// Configure le fournisseur de règles de flux de conception.
        /// </summary>
        /// <typeparam name="TProvider">Le type implémentant <see cref="IDesignFlowRulesProvider"/>.</typeparam>
        /// <param name="options">L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</param>
        /// <returns>L'instance actuelle de <see cref="GoToNetOptions"/> pour le chaînage.</returns>
        public static GoToNetOptions SetDesignFlowRulesProvider<TProvider>(this GoToNetOptions options) where TProvider : IDesignFlowRulesProvider
        {
            options.DesignFlowRulesProviderFactory = (sp) => sp.GetRequiredService<TProvider>();
            return options;
        }
    }

    /// <summary>
    /// Classe d'options pour configurer les services de GoTo.NET.
    /// </summary>
    public class GoToNetOptions
    {
        // Contient les factories pour tous les algorithmes de prédiction.
        public List<Func<IServiceProvider, IPredictionAlgorithm>> PredictionAlgorithmFactories { get; } = new List<Func<IServiceProvider, IPredictionAlgorithm>>();

        // Mode d'entraînement des algorithmes.
        public TrainingMode TrainingMode { get; set; } = TrainingMode.ContinuousDevelopment;

        // Logique personnalisée pour le mode d'entraînement Custom.
        public Func<IServiceProvider, Task>? CustomTrainingLogic { get; set; }
        
        // Factories pour les interfaces d'intégration client (publiques pour les méthodes d'extension).
        public Func<IServiceProvider, INavigationNotifier>? NavigationNotifierFactory { get; set; }
        public Func<IServiceProvider, INavigationActionHandler>? NavigationActionHandlerFactory { get; set; }
        public Func<IServiceProvider, IAppNavigationCatalog>? AppNavigationCatalogFactory { get; set; }
        public Func<IServiceProvider, IDesignFlowRulesProvider>? DesignFlowRulesProviderFactory { get; set; }
    }
}