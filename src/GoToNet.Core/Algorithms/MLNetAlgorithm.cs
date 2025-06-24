// /src/GoToNet.Core/Algorithms/MLNetAlgorithm.cs
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using GoToNet.Core.Models.MlNet;
using Microsoft.ML;
using Microsoft.ML.Data; // Essential for DataViewSchema, VBuffer, etc.
using Microsoft.ML.Trainers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq; // Essential for Select, Any, OrderByDescending
using System.Threading.Tasks;

namespace GoToNet.Core.Algorithms
{
    /// <summary>
    /// Implémente un algorithme de prédiction utilisant ML.NET pour des prédictions basées sur l'apprentissage automatique.
    /// Il entraîne un modèle de classification multi-classes pour prédire la prochaine page/fonctionnalité probable.
    /// </summary>
    public class MLNetAlgorithm : IPredictionAlgorithm
    {
        public string Name => "ML.NET";
        public double Weight { get; set; } = 2.0; // Poids typiquement plus élevé pour l'IA

        private readonly MLContext _mlContext;
        private ITransformer? _trainedModel;
        private PredictionEngine<NavigationInput, NavigationPrediction>? _predictionEngine;

        // Stocke le schéma d'entrée que le modèle attend. (Utilisé pour la sauvegarde/chargement du modèle).
        private DataViewSchema? _modelInputSchema; // Contient le schéma *d'entrée* de Load/Fit

        // Utilisé pour mapper les indices de label internes de ML.NET aux noms de pages lisibles.
        private IReadOnlyList<KeyValuePair<string, float>>? _slotNames;

        // Chemin pour sauvegarder/charger le modèle entraîné.
        private readonly string _modelPath = Path.Combine(AppContext.BaseDirectory, "mlnet_navigation_model.zip");

        // Référence au catalogue de tous les éléments de navigation possibles dans l'application.
        private IAppNavigationCatalog? _appNavigationCatalog;

        public MLNetAlgorithm()
        {
            _mlContext = new MLContext(seed: 0); // Initialise MLContext avec une graine fixe pour la reproductibilité
            LoadModel(); // Tente de charger un modèle existant au démarrage
        }

        /// <summary>
        /// Définit le catalogue de navigation de l'application pour cet algorithme.
        /// </summary>
        /// <param name="catalog">L'instance du catalogue de navigation de l'application.</param>
        public void SetAppNavigationCatalog(IAppNavigationCatalog catalog)
        {
            _appNavigationCatalog = catalog;
            Console.WriteLine("[MLNetAlgorithm] Catalogue de navigation de l'application défini.");
        }


        /// <inheritdoc />
        public Task TrainAsync(IEnumerable<NavigationEvent> historicalData, IAlgorithmProgressReporter progressReporter) // MODIFIED HERE
        {
            Console.WriteLine("[MLNetAlgorithm] Démarrage de l'entraînement...");
            string algoName = Name;

            progressReporter.ReportProgress(algoName, "Début de la préparation des données", 0); // Signale le début

            // 1. Prépare les données au format attendu par ML.NET (NavigationInput)
            var trainingData = PrepareTrainingData(historicalData);
            if (!trainingData.Any())
            {
                Console.WriteLine("[MLNetAlgorithm] Données insuffisantes pour l'entraînement. Entraînement ignoré.");
                _trainedModel = null;
                _predictionEngine = null;
                _modelInputSchema = null;
                _slotNames = null;
                progressReporter.ReportProgress(algoName, "Données insuffisantes", 100, true);
                return Task.CompletedTask;
            }

            progressReporter.ReportProgress(algoName, "Chargement des données dans ML.NET", 20);
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
            _modelInputSchema = dataView.Schema; // Stocke le schéma d'entrée directement après le chargement des données

            progressReporter.ReportProgress(algoName, "Définition du pipeline d'entraînement", 40);
            // 2. Définit le pipeline d'entraînement ML.NET
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("NextPageOrFeature", "NextPageOrFeature")
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("UserId"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("CurrentPageOrFeature"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("PreviousPageOrFeature"))
                .Append(_mlContext.Transforms.Concatenate("Features", "UserId", "CurrentPageOrFeature", "PreviousPageOrFeature", "HourOfDay", "DayOfWeek"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("NextPageOrFeature", "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            progressReporter.ReportProgress(algoName, "Entraînement du modèle", 60);
            // 3. Entraîne le modèle
            _trainedModel = pipeline.Fit(dataView);

            // 4. (Optionnel) Évalue la performance du modèle
            var predictions = _trainedModel.Transform(dataView);
            var metrics = _mlContext.MulticlassClassification.Evaluate(predictions, "NextPageOrFeature");
            Console.WriteLine($"[MLNetAlgorithm] Modèle entraîné. MicroAccuracy: {metrics.MicroAccuracy:P2}");

            progressReporter.ReportProgress(algoName, "Sauvegarde du modèle", 80);
            // 5. Sauvegarde le modèle entraîné sur le disque pour la persistance.
            SaveModel(_trainedModel, _modelInputSchema);

            // 6. Crée un moteur de prédiction pour des prédictions rapides au runtime.
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<NavigationInput, NavigationPrediction>(_trainedModel);

            // 7. Obtient le mappage des noms de labels à leurs indices internes à partir du schéma de sortie du moteur de prédiction.
            GetSlotNamesFromSchema(_predictionEngine.OutputSchema);

            progressReporter.ReportProgress(algoName, "Modèle prêt", 100, true);
            Console.WriteLine("[MLNetAlgorithm] Entraînement terminé et modèle prêt pour les prédictions.");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IEnumerable<SuggestedItem>> PredictAsync(
            string userId,
            string? currentContext,
            int numberOfSuggestions,
            IDictionary<string, string>? contextData = null)
        {
            if (_predictionEngine == null || _slotNames == null || string.IsNullOrEmpty(currentContext))
            {
                Console.WriteLine("[MLNetAlgorithm] Modèle non entraîné, noms de slots manquants ou contexte actuel est nul/vide. Impossible de prédire.");
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            var suggestions = new List<SuggestedItem>();

            // Pour obtenir les scores pour toutes les destinations possibles, nous utilisons le _appNavigationCatalog.
            IEnumerable<string> allPossibleDestinations = _appNavigationCatalog?.GetAllAvailableNavigationItems() ?? Enumerable.Empty<string>();

            // Si le catalogue de l'application est vide, on se rabat sur les labels sur lesquels le modèle a été entraîné.
            if (!allPossibleDestinations.Any() && _slotNames != null)
            {
                allPossibleDestinations = _slotNames.Select(s => s.Key).ToList();
            }

            if (!allPossibleDestinations.Any())
            {
                Console.WriteLine("[MLNetAlgorithm] Aucune destination possible disponible pour la prédiction depuis AppNavigationCatalog ou les slots du modèle.");
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            // Pour chaque page suivante potentielle, crée une entrée et obtient son score de prédiction.
            foreach (var potentialNextPage in allPossibleDestinations)
            {
                var input = new NavigationInput
                {
                    UserId = userId,
                    CurrentPageOrFeature = currentContext,
                    PreviousPageOrFeature = contextData != null && contextData.TryGetValue("PreviousPage", out var prevPage) ? prevPage : "NONE",
                    HourOfDay = DateTimeOffset.Now.Hour,
                    DayOfWeek = (float)DateTimeOffset.Now.DayOfWeek,
                    NextPageOrFeature = potentialNextPage // C'est le label cible que nous cherchons à prédire
                };

                var prediction = _predictionEngine.Predict(input);

                // Trouve l'indice de 'potentialNextPage' parmi les labels connus du modèle (_slotNames)
                // pour récupérer son score de probabilité spécifique.
                int labelIndex = -1;
                for (int i = 0; i < _slotNames.Count; i++)
                {
                    if (_slotNames[i].Key.Equals(potentialNextPage, StringComparison.OrdinalIgnoreCase))
                    {
                        labelIndex = i;
                        break;
                    }
                }

                if (labelIndex != -1 && prediction.Scores.Length > labelIndex)
                {
                    suggestions.Add(new SuggestedItem
                    {
                        Name = potentialNextPage,
                        Score = prediction.Scores[labelIndex] * Weight, // Le score brut de ML.NET, pondéré par l'influence de l'algorithme.
                        Reason = Name // "ML.NET"
                    });
                }
            }

            // Trie et prend le nombre de suggestions demandé.
            var finalSuggestions = suggestions
                .OrderByDescending(s => s.Score)
                .Take(numberOfSuggestions)
                .ToList();

            Console.WriteLine($"[MLNetAlgorithm] A prédit {finalSuggestions.Count} éléments pour l'utilisateur '{userId}' (contexte: {currentContext}).");
            return Task.FromResult<IEnumerable<SuggestedItem>>(finalSuggestions.AsEnumerable());
        }

        /// <summary>
        /// Sauvegarde le modèle ML.NET entraîné sur le disque.
        /// </summary>
        /// <param name="model">Le modèle ITransformer entraîné.</param>
        /// <param name="schema">Le schéma d'entrée utilisé pour entraîner le modèle. Ceci est nécessaire par la méthode Save de ML.NET.</param>
        private void SaveModel(ITransformer model, DataViewSchema schema)
        {
            _mlContext.Model.Save(model, schema, _modelPath);
            Console.WriteLine($"[MLNetAlgorithm] Modèle sauvegardé vers '{_modelPath}'.");
        }

        /// <summary>
        /// Tente de charger un modèle ML.NET existant depuis le disque.
        /// </summary>
        private void LoadModel()
        {
            if (File.Exists(_modelPath))
            {
                try
                {
                    // Charge le modèle. Le schéma *d'entrée* est chargé dans _modelInputSchema.
                    _trainedModel = _mlContext.Model.Load(_modelPath, out _modelInputSchema);
                    // Crée le moteur de prédiction à partir du modèle chargé.
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<NavigationInput, NavigationPrediction>(_trainedModel);

                    // Rétablit le mappage des noms de labels (slots) à partir du schéma *de sortie* du moteur de prédiction.
                    // IMPORTANT: Assurez-vous que _predictionEngine n'est pas nul avant d'accéder à son OutputSchema.
                    if (_predictionEngine != null)
                    {
                        GetSlotNamesFromSchema(_predictionEngine.OutputSchema);
                    }
                    else
                    {
                        Console.WriteLine("[MLNetAlgorithm] PredictionEngine est null après le chargement du modèle. Impossible d'obtenir le schéma de sortie.");
                        _slotNames = null; // S'assurer que les noms de slots sont effacés
                    }


                    Console.WriteLine($"[MLNetAlgorithm] Modèle chargé avec succès depuis '{_modelPath}'.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MLNetAlgorithm] Erreur lors du chargement du modèle depuis '{_modelPath}': {ex.Message}. Un nouveau modèle sera entraîné.");
                    _trainedModel = null;
                    _predictionEngine = null;
                    _modelInputSchema = null;
                    _slotNames = null;
                }
            }
            else
            {
                Console.WriteLine($"[MLNetAlgorithm] Aucun fichier de modèle existant trouvé à '{_modelPath}'. Un nouveau modèle sera entraîné au démarrage.");
            }
        }

        /// <summary>
        /// Extrait et stocke les noms de labels (slots) du schéma de sortie du modèle.
        /// Ces noms correspondent aux indices dans le tableau 'Scores' de la prédiction.
        /// </summary>
        /// <param name="schema">Le DataViewSchema du schéma de sortie du modèle (ex: depuis PredictionEngine.OutputSchema).</param>
        private void GetSlotNamesFromSchema(DataViewSchema schema)
        {
            _slotNames = null; // Réinitialise au cas où

            // Utilise GetColumnOrNull pour accéder en toute sécurité à la colonne "Score".
            DataViewSchema.Column? scoreColumn = schema.GetColumnOrNull("Score");

            // Vérifie si la colonne a été trouvée et a une valeur.
            if (scoreColumn.HasValue)
            {
                VBuffer<ReadOnlyMemory<char>> slotNamesBuffer = default;
                // Accède à la propriété Annotations sur la structure Value de DataViewSchema.Column
                // et utilise GetValue pour récupérer l'annotation par son nom bien connu "SlotNames".
                scoreColumn.Value.Annotations.GetValue("SlotNames", ref slotNamesBuffer);

                // Convertit VBuffer<ReadOnlyMemory<char>> en List<KeyValuePair<string, float>>.
                // GetValues() retourne ReadOnlySpan<ReadOnlyMemory<char>>.
                // Utiliser .ToArray() sur ReadOnlySpan pour activer de manière fiable la méthode Select de LINQ.
                _slotNames = slotNamesBuffer.GetValues().ToArray() 
                                            .Select((s, i) => new KeyValuePair<string, float>(s.ToString(), i))
                                            .ToList();

                Console.WriteLine($"[MLNetAlgorithm] Récupéré {_slotNames.Count} noms de slots à partir du schéma.");
            }
            else
            {
                Console.WriteLine("[MLNetAlgorithm] La colonne 'Score' est introuvable dans le schéma pour l'annotation des noms de slots.");
            }
        }


        /// <summary>
        /// Prépare les données brutes NavigationEvent dans un format (NavigationInput) adapté à l'entraînement ML.NET.
        /// Cela crée des paires (CurrentEvent, NextEvent) où les caractéristiques de CurrentEvent prédisent NextEvent.
        /// </summary>
        private IEnumerable<NavigationInput> PrepareTrainingData(IEnumerable<NavigationEvent> historicalData)
        {
            var trainingSamples = new List<NavigationInput>();

            // Regroupe les événements par utilisateur et les trie par timestamp pour reconstruire les séquences.
            var userGroupedHistory = historicalData
                .GroupBy(e => e.UserId)
                .Select(g => g.OrderBy(e => e.Timestamp).ToList());

            foreach (var userHistory in userGroupedHistory)
            {
                // Nous avons besoin d'au moins deux événements pour former une séquence (courant -> suivant).
                for (int i = 0; i < userHistory.Count - 1; i++)
                {
                    var currentEvent = userHistory[i];
                    var nextEvent = userHistory[i + 1];

                    trainingSamples.Add(new NavigationInput
                    {
                        UserId = currentEvent.UserId,
                        CurrentPageOrFeature = currentEvent.CurrentPageOrFeature,
                        PreviousPageOrFeature = currentEvent.PreviousPageOrFeature ?? "NONE", // Gère les pages précédentes nulles
                        HourOfDay = currentEvent.Timestamp.Hour,
                        DayOfWeek = (float)currentEvent.Timestamp.DayOfWeek,
                        NextPageOrFeature = nextEvent.CurrentPageOrFeature // Ceci est le LABEL (ce que nous voulons prédire)
                    });
                }
            }
            Console.WriteLine($"[MLNetAlgorithm] Préparé {trainingSamples.Count} échantillons d'entraînement à partir des données historiques.");
            return trainingSamples;
        }
    }
}