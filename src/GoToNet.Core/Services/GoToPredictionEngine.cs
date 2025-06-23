// /src/GoToNet.Core/Services/GoToPredictionEngine.cs
using GoToNet.Core.Algorithms;
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoToNet.Core.Services
{
    /// <summary>
    /// The main prediction engine for GoTo.NET.
    /// It orchestrates navigation event recording, algorithm training, and the generation
    /// of combined navigation suggestions based on various algorithms and user preferences.
    /// </summary>
    public class GoToPredictionEngine : IAlgorithmProgressReporter // NOUVEAU : Implémente cette interface
    {
        private readonly INavigationHistoryStore _historyStore;
        private readonly IUserMenuPreferencesStore _preferencesStore;
        private readonly List<IPredictionAlgorithm> _algorithms;

        // Dependencies for client application integration
        private INavigationNotifier? _navigationNotifier;
        private INavigationActionHandler? _navigationActionHandler;
        private IAppNavigationCatalog? _appNavigationCatalog;

        // Events for training status (maintenant gérés directement par le moteur)
        public event EventHandler? AlgorithmsTrained; // Notifies when a full training cycle completes
        public event EventHandler<TrainingProgressEventArgs>? TrainingProgressUpdated; // Provides granular updates during training

        private TrainingMode _trainingMode;
        private Func<IServiceProvider, Task>? _customTrainingLogic;
        private bool _isTrainingInProgress = false;

        /// <summary>
        /// Initialises une nouvelle instance du <see cref="GoToPredictionEngine"/>.
        /// </summary>
        /// <param name="historyStore">Le store pour les événements de navigation historiques.</param>
        /// <param name="preferencesStore">Le store pour les préférences de menu personnalisées par l'utilisateur.</param>
        public GoToPredictionEngine(
            INavigationHistoryStore historyStore,
            IUserMenuPreferencesStore preferencesStore)
        {
            _historyStore = historyStore;
            _preferencesStore = preferencesStore;
            _algorithms = new List<IPredictionAlgorithm>();
        }

        /// <summary>
        /// Configures the client application integration interfaces for GoTo.NET.
        /// This allows the engine to communicate with the consuming application's UI and navigation system.
        /// </summary>
        /// <param name="notifier">Implementation for notifying the UI about suggestions.</param>
        /// <param name="actionHandler">Implementation for handling real navigation actions.</param>
        /// <param name="catalog">Implementation for providing the catalog of available application pages/features.</param>
        public void ConfigureClientIntegration(
            INavigationNotifier notifier,
            INavigationActionHandler actionHandler,
            IAppNavigationCatalog catalog)
        {
            _navigationNotifier = notifier;
            _navigationActionHandler = actionHandler;
            _appNavigationCatalog = catalog;

            Console.WriteLine("[GoToPredictionEngine] Client integration configured.");

            // IMPORTANT: Pass the IAppNavigationCatalog to algorithms that need it (like ML.NET)
            foreach (var algorithm in _algorithms)
            {
                if (algorithm is MLNetAlgorithm mlNetAlgo)
                {
                    mlNetAlgo.SetAppNavigationCatalog(_appNavigationCatalog);
                }
            }
        }

        /// <summary>
        /// Adds a prediction algorithm to the engine.
        /// Algorithms added here will be used to generate suggestions.
        /// </summary>
        /// <param name="algorithm">The algorithm to add.</param>
        public void AddAlgorithm(IPredictionAlgorithm algorithm)
        {
            _algorithms.Add(algorithm);
            Console.WriteLine($"[GoToPredictionEngine] Algorithm '{algorithm.Name}' added.");
            // If the AppNavigationCatalog is already configured, pass it to the newly added algorithm
            if (_appNavigationCatalog != null && algorithm is MLNetAlgorithm mlNetAlgo)
            {
                mlNetAlgo.SetAppNavigationCatalog(_appNavigationCatalog);
            }
        }

        /// <summary>
        /// Sets the training strategy for the prediction algorithms.
        /// </summary>
        /// <param name="mode">The desired <see cref="TrainingMode"/>.</param>
        /// <param name="customLogic">An optional custom asynchronous logic for <see cref="TrainingMode.Custom"/>.</param>
        public void SetTrainingStrategy(TrainingMode mode, Func<IServiceProvider, Task>? customLogic = null)
        {
            _trainingMode = mode;
            _customTrainingLogic = customLogic;
            Console.WriteLine($"[GoToPredictionEngine] Training strategy set to: {_trainingMode}.");
        }


        /// <summary>
        /// Records a user navigation event into the history store.
        /// Based on the <see cref="TrainingMode"/>, it may trigger a re-training of algorithms.
        /// </summary>
        /// <param name="navigationEvent">The navigation event to record.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        public async Task RecordNavigationAsync(NavigationEvent navigationEvent)
        {
            await _historyStore.AddEventAsync(navigationEvent);

            if (_trainingMode == TrainingMode.ContinuousDevelopment && !_isTrainingInProgress)
            {
                _ = TrainAlgorithmsAsync(); // Fire-and-forget
            }
        }

        /// <summary>
        /// Trains all registered prediction algorithms using the complete navigation history.
        /// This method can be called manually or triggered by the engine based on strategy.
        /// It reports progress via the <see cref="TrainingProgressUpdated"/> event.
        /// </summary>
        /// <returns>A Task representing the asynchronous training operation.</returns>
        public async Task TrainAlgorithmsAsync()
        {
            if (_isTrainingInProgress)
            {
                Console.WriteLine("[GoToPredictionEngine] Training is already in progress. Skipping new request.");
                return;
            }

            _isTrainingInProgress = true;
            Console.WriteLine("[GoToPredictionEngine] Starting full algorithm training process...");
            try
            {
                // Signaler le début de l'entraînement global
                OnTrainingProgressUpdated(new TrainingProgressEventArgs(
                    "Global", "Démarrage de l'entraînement global", 0, false, "Préparation des données historiques..."
                ));

                // For Custom mode, execute the provided custom logic
                if (_trainingMode == TrainingMode.Custom && _customTrainingLogic != null)
                {
                    await _customTrainingLogic.Invoke(null!); // Pass null if customLogic doesn't need it, or a real SP
                    OnTrainingProgressUpdated(new TrainingProgressEventArgs("CustomLogic", "Exécution terminée", 100, true));
                }
                else // For predefined modes, execute standard training
                {
                    var allHistory = await _historyStore.GetAllHistoryAsync();
                    int totalAlgorithms = _algorithms.Count;
                    int completedAlgorithms = 0;

                    foreach (var algorithm in _algorithms)
                    {
                        Console.WriteLine($"[GoToPredictionEngine] Training algorithm: {algorithm.Name}...");
                        
                        // Passer 'this' (le moteur) comme rapporteur de progression à l'algorithme.
                        // L'algorithme appellera ensuite ReportProgress sur 'this'.
                        await algorithm.TrainAsync(allHistory, this); // MODIFICATION CLÉ ICI

                        completedAlgorithms++;
                        // Signaler la progression globale après chaque algorithme
                        OnTrainingProgressUpdated(new TrainingProgressEventArgs(
                            algorithm.Name,
                            "Entraînement terminé",
                            (int)((double)completedAlgorithms / totalAlgorithms * 100),
                            true,
                            $"Algorithme {algorithm.Name} terminé."
                        ));
                    }
                }

                Console.WriteLine($"[GoToPredictionEngine] All algorithms retrained successfully.");
                
                // Déclenchement de l'événement AlgorithmsTrained
                // Utilisation du pattern de déclenchement d'événement sécurisé
                EventHandler? handler = AlgorithmsTrained;
                handler?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoToPredictionEngine] Erreur lors de l'entraînement des algorithmes: {ex.Message}");
                // Signaler une erreur de progression
                OnTrainingProgressUpdated(new TrainingProgressEventArgs(
                    "Global", "Erreur d'entraînement", 100, true, $"Erreur: {ex.Message}"
                ));
                // Vous pourriez vouloir un événement séparé pour les erreurs d'entraînement: TrainingErrorOccurred?.Invoke(this, new TrainingErrorEventArgs(ex));
            }
            finally
            {
                _isTrainingInProgress = false;
            }
        }

        /// <summary>
        /// Raises the TrainingProgressUpdated event. This method is called by the engine itself
        /// and by the algorithms (via IAlgorithmProgressReporter).
        /// </summary>
        /// <param name="e">The <see cref="TrainingProgressEventArgs"/> containing progress details.</param>
        protected virtual void OnTrainingProgressUpdated(TrainingProgressEventArgs e)
        {
            // Déclenchement de l'événement TrainingProgressUpdated
            // Utilisation du pattern de déclenchement d'événement sécurisé
            EventHandler<TrainingProgressEventArgs>? handler = TrainingProgressUpdated;
            handler?.Invoke(this, e);
        }

        // Implémentation explicite de la méthode ReportProgress de IAlgorithmProgressReporter.
        // Cette méthode est appelée par les algorithmes individuels.
        void IAlgorithmProgressReporter.ReportProgress(string algorithmName, string currentStep, int progressPercentage, bool isCompleted, string? message)
        {
            OnTrainingProgressUpdated(new TrainingProgressEventArgs(algorithmName, currentStep, progressPercentage, isCompleted, message));
        }

        /// <summary>
        /// Generates and returns a combined list of navigation suggestions,
        /// taking into account prediction algorithms and user's custom preferences.
        /// Also notifies the registered INavigationNotifier if one is set.
        /// </summary>
        /// <param name="userId">The ID of the user for whom to generate suggestions.</param>
        /// <param name="currentContext">The user's current page or feature (for contextual predictions).</param>
        /// <param name="count">The maximum number of suggestions to return.</param>
        /// <param name="contextData">Additional context data to refine predictions (optional).</param>
        /// <returns>A Task containing a collection of suggested items.</returns>
        public async Task<IEnumerable<SuggestedItem>> GetSuggestionsAsync(
            string userId,
            string? currentContext = null,
            int count = 5,
            IDictionary<string, string>? contextData = null)
        {
            var allSuggestions = new Dictionary<string, SuggestedItem>();

            // 1. Get suggestions from each prediction algorithm
            foreach (var algorithm in _algorithms)
            {
                var algorithmSuggestions = await algorithm.PredictAsync(userId, currentContext, count, contextData);
                foreach (var suggestion in algorithmSuggestions)
                {
                    // Keep the suggestion with the highest score if an item with the same name is already present
                    if (!allSuggestions.ContainsKey(suggestion.Name) || allSuggestions[suggestion.Name].Score < suggestion.Score)
                    {
                        allSuggestions[suggestion.Name] = suggestion;
                    }
                }
            }

            // 2. Integrate user's custom menu items
            var customMenuItems = await _preferencesStore.GetCustomMenuItemsAsync(userId);
            foreach (var customItem in customMenuItems)
            {
                // Custom items have very high priority.
                // We give them a very high score, potentially modulated by their order.
                var customSuggestedItem = new SuggestedItem
                {
                    Name = customItem.ItemName,
                    Score = 1000.0 + (customMenuItems.Count() - customItem.Order), // Example: base 1000 + bonus for higher priority (lower order value)
                    Reason = "UserCustom"
                };

                // Add or update the suggestion if it doesn't exist or if the score is higher
                if (!allSuggestions.ContainsKey(customSuggestedItem.Name) || allSuggestions[customSuggestedItem.Name].Score < customSuggestedItem.Score)
                {
                    allSuggestions[customSuggestedItem.Name] = customSuggestedItem;
                }
            }

            // 3. Sort the final suggestions by score in descending order and take the requested count
            var finalSuggestions = allSuggestions.Values
                .OrderByDescending(s => s.Score)
                .Take(count)
                .ToList();

            Console.WriteLine($"[GoToPredictionEngine] Final suggestions for '{userId}' ({finalSuggestions.Count} items): {string.Join(", ", finalSuggestions.Select(s => $"{s.Name} ({s.Score:F0}, {s.Reason})"))}");

            // 4. Notify the registered INavigationNotifier with the updated suggestions
            _navigationNotifier?.UpdateSuggestionsDisplay(userId, finalSuggestions);

            return finalSuggestions;
        }

        /// <summary>
        /// Requests the registered navigation action handler to perform a navigation to a suggested item.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="itemName">The name of the item to navigate to.</param>
        /// <returns>True if the navigation was handled and successful by the UI Provider, False otherwise.</returns>
        public bool PerformSuggestedNavigation(string userId, string itemName)
        {
            if (_navigationActionHandler == null)
            {
                Console.WriteLine("[GoToPredictionEngine] No Navigation Action Handler registered to perform navigation.");
                return false;
            }
            return _navigationActionHandler.PerformNavigation(userId, itemName);
        }
    }
}