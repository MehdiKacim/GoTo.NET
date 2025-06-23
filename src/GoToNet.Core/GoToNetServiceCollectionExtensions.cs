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
    /// Provides extension methods for IServiceCollection to easily configure
    /// GoTo.NET library services.
    /// </summary>
    public static class GoToNetServiceCollectionExtensions
    {
        /// <summary>
        /// Adds and configures GoTo.NET library services to the dependency injection container.
        /// </summary>
        /// <param name="services">The instance of IServiceCollection.</param>
        /// <param name="configureOptions">An action to configure advanced GoTo.NET options.</param>
        /// <returns>The IServiceCollection instance for chaining.</returns>
        public static IServiceCollection AddGoToNet(
            this IServiceCollection services,
            Action<GoToNetOptions>? configureOptions = null)
        {
            // Register default in-memory stores.
            services.AddSingleton<INavigationHistoryStore, InMemoryHistoryStore>();
            services.AddSingleton<IUserMenuPreferencesStore, InMemoryUserMenuPreferencesStore>();

            // IMPORTANT: IDesignFlowRulesProvider is NOT registered by default here.
            // It must be registered explicitly by the consuming application if DesignFlowAlgorithm is used.

            // Register GoTo.NET's core services
            services.AddSingleton<UserMenuBuilder>();

            // Create and configure options
            var options = new GoToNetOptions();
            configureOptions?.Invoke(options);

            // Register the main prediction engine
            services.AddSingleton<GoToPredictionEngine>(serviceProvider =>
            {
                var historyStore = serviceProvider.GetRequiredService<INavigationHistoryStore>();
                var preferencesStore = serviceProvider.GetRequiredService<IUserMenuPreferencesStore>();

                var engine = new GoToPredictionEngine(historyStore, preferencesStore);

                // Configure client integration interfaces if factories are provided in options
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

                // Instantiate and add algorithms via their factories
                if (options.PredictionAlgorithmFactories.Any())
                {
                    foreach (var algoFactory in options.PredictionAlgorithmFactories)
                    {
                        var algo = algoFactory(serviceProvider); // Resolve the algorithm using the serviceProvider
                        engine.AddAlgorithm(algo);
                    }
                }
                else
                {
                    Console.WriteLine("[GoToNet] Avertissement: Aucun algorithme de prédiction n'a été ajouté à GoTo.NET. Le moteur ne fournira que des suggestions basées sur les raccourcis personnalisés (s'il y en a).");
                }


                // Set the training strategy on the engine
                engine.SetTrainingStrategy(options.TrainingMode, options.CustomTrainingLogic);

                // Trigger initial training based on the chosen strategy (if applicable)
                if (options.TrainingMode == TrainingMode.OnStartupOnce)
                {
                    _ = Task.Run(async () => await engine.TrainAlgorithmsAsync()); // Fire-and-forget
                }

                return engine;
            });

            return services;
        }
    }

    /// <summary>
    /// Options class for configuring GoTo.NET services.
    /// </summary>
    public class GoToNetOptions
    {
        // Now holds factories for all prediction algorithms
        public List<Func<IServiceProvider, IPredictionAlgorithm>> PredictionAlgorithmFactories { get; } = new List<Func<IServiceProvider, IPredictionAlgorithm>>();

        public TrainingMode TrainingMode { get; set; } = TrainingMode.ContinuousDevelopment;
        public Func<IServiceProvider, Task>? CustomTrainingLogic { get; set; }
        public Func<IServiceProvider, INavigationNotifier>? NavigationNotifierFactory { get; set; }
        public Func<IServiceProvider, INavigationActionHandler>? NavigationActionHandlerFactory { get; set; }
        public Func<IServiceProvider, IAppNavigationCatalog>? AppNavigationCatalogFactory { get; set; }
        public Func<IServiceProvider, IDesignFlowRulesProvider>? DesignFlowRulesProviderFactory { get; set; } // This is mainly for the factories below

        /// <summary>
        /// Adds a prediction algorithm to the list of algorithms that will be used by the engine.
        /// The algorithm will be instantiated by the dependency injection container.
        /// </summary>
        /// <typeparam name="TAlgorithm">The type of the algorithm to add (must implement IPredictionAlgorithm and be registered in DI).</typeparam>
        /// <returns>The current <see cref="GoToNetOptions"/> instance for chaining.</returns>
        public GoToNetOptions AddAlgorithm<TAlgorithm>() where TAlgorithm : class, IPredictionAlgorithm
        {
            // The factory resolves the algorithm from the service provider.
            PredictionAlgorithmFactories.Add(sp => sp.GetRequiredService<TAlgorithm>());
            return this;
        }

        /// <summary>
        /// Adds a Design Flow prediction algorithm to the list of algorithms, ensuring its specific dependencies are resolved.
        /// Use this overload for algorithms that implement <see cref="IDesignFlowAlgorithm"/>.
        /// </summary>
        /// <typeparam name="TDesignAlgorithm">The type of the design flow algorithm (must implement IDesignFlowAlgorithm and be registered in DI).</typeparam>
        /// <returns>The current <see cref="GoToNetOptions"/> instance for chaining.</returns>
        public GoToNetOptions AddDesignFlowAlgorithm<TDesignAlgorithm>()
            where TDesignAlgorithm : class, IDesignFlowAlgorithm // Constraint: must be a DesignFlowAlgorithm
        {
            // Factory resolves the specific DesignFlowAlgorithm,
            // relying on IServiceProvider to provide its constructor dependencies (like IDesignFlowRulesProvider).
            PredictionAlgorithmFactories.Add(sp => sp.GetRequiredService<TDesignAlgorithm>());
            return this;
        }


        /// <summary>
        /// Adds a prediction algorithm by providing directly an instance of the algorithm.
        /// Use this method if the algorithm does not have complex dependencies to be resolved by DI.
        /// </summary>
        /// <param name="algorithm">The instance of the algorithm to add.</param>
        /// <returns>The current <see cref="GoToNetOptions"/> instance for chaining.</returns>
        public GoToNetOptions AddAlgorithmInstance(IPredictionAlgorithm algorithm)
        {
            PredictionAlgorithmFactories.Add(sp => algorithm);
            return this;
        }
        
    }
}