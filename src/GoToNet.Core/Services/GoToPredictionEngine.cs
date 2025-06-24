using GoToNet.Core.Algorithms;
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace GoToNet.Core.Services
{
    public class GoToPredictionEngine : IAlgorithmProgressReporter
    {
        private readonly INavigationHistoryStore _historyStore;
        private readonly IUserMenuPreferencesStore _preferencesStore;
        private readonly List<IPredictionAlgorithm> _algorithms;

        private INavigationNotifier? _navigationNotifier;         
        private INavigationActionHandler? _navigationActionHandler; 
        private IAppNavigationCatalog? _appNavigationCatalog;     

        public event EventHandler? AlgorithmsTrained; 
        public event EventHandler<TrainingProgressEventArgs>? TrainingProgressUpdated; 

        private TrainingMode _trainingMode;
        private Func<IServiceProvider, Task>? _customTrainingLogic;
        private bool _isTrainingInProgress = false;

        private Timer? _trainingTimer;
        private TimeSpan _batchTrainingInterval = TimeSpan.FromMinutes(5);
        private int _newEventsSinceLastTraining = 0;
        private int _batchTrainingThreshold = 50;
        private readonly object _lockObject = new object();

        public GoToPredictionEngine(
            INavigationHistoryStore historyStore,
            IUserMenuPreferencesStore preferencesStore)
        {
            _historyStore = historyStore;
            _preferencesStore = preferencesStore;
            _algorithms = new List<IPredictionAlgorithm>();
        }

        public TrainingMode TrainingMode => _trainingMode; 

        public void ConfigureClientIntegration(
            INavigationNotifier notifier,
            INavigationActionHandler actionHandler,
            IAppNavigationCatalog catalog)
        {
            _navigationNotifier = notifier;
            _navigationActionHandler = actionHandler;
            _appNavigationCatalog = catalog;

            Console.WriteLine("[GoToPredictionEngine] Client integration configured.");

            foreach (var algorithm in _algorithms)
            {
                if (algorithm is MLNetAlgorithm mlNetAlgo) 
                {
                    mlNetAlgo.SetAppNavigationCatalog(_appNavigationCatalog);
                }
            }
        }

        public void AddAlgorithm(IPredictionAlgorithm algorithm)
        {
            _algorithms.Add(algorithm);
            Console.WriteLine($"[GoToPredictionEngine] Algorithm '{algorithm.Name}' added.");
            if (_appNavigationCatalog != null && algorithm is MLNetAlgorithm mlNetAlgo)
            {
                mlNetAlgo.SetAppNavigationCatalog(_appNavigationCatalog);
            }
        }

        public void SetTrainingStrategy(TrainingMode mode, Func<IServiceProvider, Task>? customLogic = null,
                                        TimeSpan? batchInterval = null, int? batchThreshold = null)
        {
            _trainingMode = mode;
            _customTrainingLogic = customLogic;

            if (mode == TrainingMode.BatchScheduled)
            {
                _batchTrainingInterval = batchInterval ?? TimeSpan.FromMinutes(5);
                _batchTrainingThreshold = batchThreshold ?? 50;
                Console.WriteLine($"[GoToPredictionEngine] Mode BatchScheduled configuré. Intervalle: {_batchTrainingInterval}, Seuil: {_batchTrainingThreshold} événements.");

                if (_trainingTimer == null)
                {
                    _trainingTimer = new Timer(async (state) => await TriggerBatchTraining(), null, _batchTrainingInterval, _batchTrainingInterval);
                }
                else
                {
                    _trainingTimer.Change(_batchTrainingInterval, _batchTrainingInterval);
                }
            }
            else
            {
                _trainingTimer?.Dispose();
                _trainingTimer = null;
            }

            Console.WriteLine($"[GoToPredictionEngine] Stratégie d'entraînement définie à: {_trainingMode}.");
        }


        public async Task RecordNavigationAsync(NavigationEvent navigationEvent)
        {
            await _historyStore.AddEventAsync(navigationEvent);

            if (_trainingMode == TrainingMode.ContinuousDevelopment && !_isTrainingInProgress)
            {
                _ = TrainAlgorithmsAsync();
            }
            else if (_trainingMode == TrainingMode.BatchScheduled)
            {
                lock (_lockObject)
                {
                    _newEventsSinceLastTraining++;
                    Console.WriteLine($"[GoToPredictionEngine] Nouvel événement enregistré. Événements totaux depuis le dernier batch: {_newEventsSinceLastTraining}.");
                    if (_newEventsSinceLastTraining >= _batchTrainingThreshold && !_isTrainingInProgress)
                    {
                        Console.WriteLine($"[GoToPredictionEngine] Seuil de batch atteint. Déclenchement immédiat de l'entraînement par lots.");
                        _newEventsSinceLastTraining = 0;
                        _ = TrainAlgorithmsAsync();
                    }
                }
            }
        }

        private async Task TriggerBatchTraining()
        {
            if (_isTrainingInProgress)
            {
                Console.WriteLine("[GoToPredictionEngine] Entraînement par lots ignoré: un autre entraînement est déjà en cours.");
                return;
            }

            lock (_lockObject)
            {
                if (_newEventsSinceLastTraining == 0)
                {
                    Console.WriteLine("[GoToPredictionEngine] Entraînement par lots ignoré: aucun nouvel événement depuis le dernier batch/exécution planifiée.");
                    return;
                }
                _newEventsSinceLastTraining = 0;
            }

            Console.WriteLine("[GoToPredictionEngine] Entraînement par lots planifié déclenché.");
            await TrainAlgorithmsAsync();
        }


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
                OnTrainingProgressUpdated(new TrainingProgressEventArgs(
                    "Global", "Démarrage de l'entraînement global", 0, false, "Préparation des données historiques..."
                ));

                if (_trainingMode == TrainingMode.Custom && _customTrainingLogic != null)
                {
                    await _customTrainingLogic.Invoke(null!);
                    OnTrainingProgressUpdated(new TrainingProgressEventArgs("CustomLogic", "Exécution terminée", 100, true));
                }
                else
                {
                    var allHistory = await _historyStore.GetAllHistoryAsync();
                    int totalAlgorithms = _algorithms.Count;
                    int completedAlgorithms = 0;

                    foreach (var algorithm in _algorithms)
                    {
                        Console.WriteLine($"[GoToPredictionEngine] Training algorithm: {algorithm.Name}...");

                        await algorithm.TrainAsync(allHistory, this); 

                        completedAlgorithms++;
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

                EventHandler? handler = AlgorithmsTrained;
                handler?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[GoToPredictionEngine] Erreur lors de l'entraînement des algorithmes: {ex.Message}");
                OnTrainingProgressUpdated(new TrainingProgressEventArgs(
                    "Global", "Erreur d'entraînement", 100, true, $"Erreur: {ex.Message}"
                ));
            }
            finally
            {
                _isTrainingInProgress = false;
            }
        }

        protected virtual void OnTrainingProgressUpdated(TrainingProgressEventArgs e)
        {
            EventHandler<TrainingProgressEventArgs>? handler = TrainingProgressUpdated;
            handler?.Invoke(this, e);
        }

        void IAlgorithmProgressReporter.ReportProgress(string algorithmName, string currentStep, int progressPercentage, bool isCompleted, string? message)
        {
            OnTrainingProgressUpdated(new TrainingProgressEventArgs(algorithmName, currentStep, progressPercentage, isCompleted, message));
        }

        public async Task<IEnumerable<SuggestedItem>> GetSuggestionsAsync(
            string userId,
            string? currentContext = null,
            int count = 5,
            IDictionary<string, string>? contextData = null)
        {
            var allSuggestions = new Dictionary<string, SuggestedItem>();

            foreach (var algorithm in _algorithms)
            {
                var algorithmSuggestions = await algorithm.PredictAsync(userId, currentContext, count, contextData);
                foreach (var suggestion in algorithmSuggestions)
                {
                    if (!allSuggestions.ContainsKey(suggestion.Name) || allSuggestions[suggestion.Name].Score < suggestion.Score)
                    {
                        allSuggestions[suggestion.Name] = suggestion;
                    }
                }
            }

            var customMenuItems = await _preferencesStore.GetCustomMenuItemsAsync(userId);
            foreach (var customItem in customMenuItems)
            {
                var customSuggestedItem = new SuggestedItem
                {
                    Name = customItem.ItemName,
                    Score = 1000.0 + (customMenuItems.Count() - customItem.Order),
                    Reason = "UserCustom"
                };

                if (!allSuggestions.ContainsKey(customSuggestedItem.Name) || allSuggestions[customSuggestedItem.Name].Score < customSuggestedItem.Score)
                {
                    allSuggestions[customSuggestedItem.Name] = customSuggestedItem;
                }
            }

            var finalSuggestions = allSuggestions.Values
                .OrderByDescending(s => s.Score)
                .Take(count)
                .ToList();

            Console.WriteLine($"[GoToPredictionEngine] Final suggestions pour '{userId}' ({finalSuggestions.Count} éléments): {string.Join(", ", finalSuggestions.Select(s => $"{s.Name} ({s.Score:F0}, {s.Reason})"))}");

            _navigationNotifier?.UpdateSuggestionsDisplay(userId, finalSuggestions);

            return finalSuggestions;
        }

        public bool PerformSuggestedNavigation(string userId, string itemName)
        {
            if (_navigationActionHandler == null)
            {
                Console.WriteLine("[GoToPredictionEngine] Aucun gestionnaire d'action de navigation enregistré pour effectuer la navigation.");
                return false;
            }
            return _navigationActionHandler.PerformNavigation(userId, itemName);
        }
    }
}