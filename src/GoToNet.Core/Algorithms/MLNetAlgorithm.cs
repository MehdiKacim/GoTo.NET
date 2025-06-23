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
    /// Implements a prediction algorithm using ML.NET for sophisticated, data-driven predictions.
    /// It trains a multi-class classification model to predict the next probable page/feature.
    /// </summary>
    public class MLNetAlgorithm : IPredictionAlgorithm
    {
        public string Name => "ML.NET";
        public double Weight { get; set; } = 2.0; // Typically give AI a higher weight due to its complexity

        private readonly MLContext _mlContext;
        private ITransformer? _trainedModel;
        private PredictionEngine<NavigationInput, NavigationPrediction>? _predictionEngine;

        // Stores the input schema that the model expects. (Used for model saving/loading).
        private DataViewSchema? _modelInputSchema; // Holds the *input* schema from Load/Fit

        // Used to map ML.NET's internal label indices back to readable page names.
        private IReadOnlyList<KeyValuePair<string, float>>? _slotNames;

        // Path to save/load the trained model. This allows the model to persist across app restarts.
        private readonly string _modelPath = Path.Combine(AppContext.BaseDirectory, "mlnet_navigation_model.zip");

        // Reference to the catalog of all possible navigation items in the application.
        // This is crucial for the ML model to predict probabilities for every known destination.
        private IAppNavigationCatalog? _appNavigationCatalog;

        public MLNetAlgorithm()
        {
            _mlContext = new MLContext(seed: 0); // Initialize MLContext with a fixed seed for reproducibility
            LoadModel(); // Try to load an existing model on startup
        }

        /// <summary>
        /// Sets the application's navigation catalog for this algorithm. This allows the algorithm to
        /// get a complete list of all possible navigation items in the application for prediction purposes.
        /// </summary>
        /// <param name="catalog">The instance of the application navigation catalog.</param>
        public void SetAppNavigationCatalog(IAppNavigationCatalog catalog)
        {
            _appNavigationCatalog = catalog;
            Console.WriteLine("[MLNetAlgorithm] App Navigation Catalog set.");
        }


        /// <inheritdoc />
        public Task TrainAsync(IEnumerable<NavigationEvent> historicalData, IAlgorithmProgressReporter progressReporter) // Takes progress reporter
        {
            Console.WriteLine("[MLNetAlgorithm] Starting training...");
            string algoName = Name;

            progressReporter.ReportProgress(algoName, "Début de la préparation des données", 0); // Report start

            // 1. Prepare data into ML.NET's expected format (NavigationInput)
            var trainingData = PrepareTrainingData(historicalData);
            if (!trainingData.Any())
            {
                Console.WriteLine("[MLNetAlgorithm] Insufficient data for training. Skipping training.");
                _trainedModel = null;
                _predictionEngine = null;
                _modelInputSchema = null;
                _slotNames = null;
                progressReporter.ReportProgress(algoName, "Données insuffisantes", 100, true);
                return Task.CompletedTask;
            }

            progressReporter.ReportProgress(algoName, "Chargement des données dans ML.NET", 20);
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
            _modelInputSchema = dataView.Schema; // Store the input schema directly after loading data

            progressReporter.ReportProgress(algoName, "Définition du pipeline d'entraînement", 40);
            // 2. Define the ML.NET training pipeline
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("NextPageOrFeature", "NextPageOrFeature")
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("UserId"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("CurrentPageOrFeature"))
                .Append(_mlContext.Transforms.Categorical.OneHotEncoding("PreviousPageOrFeature"))
                .Append(_mlContext.Transforms.Concatenate("Features", "UserId", "CurrentPageOrFeature", "PreviousPageOrFeature", "HourOfDay", "DayOfWeek"))
                .Append(_mlContext.MulticlassClassification.Trainers.SdcaMaximumEntropy("NextPageOrFeature", "Features"))
                .Append(_mlContext.Transforms.Conversion.MapKeyToValue("PredictedLabel"));

            progressReporter.ReportProgress(algoName, "Entraînement du modèle", 60);
            // 3. Train the model
            _trainedModel = pipeline.Fit(dataView);

            // 4. (Optional) Evaluate the model performance
            var predictions = _trainedModel.Transform(dataView);
            var metrics = _mlContext.MulticlassClassification.Evaluate(predictions, "NextPageOrFeature");
            Console.WriteLine($"[MLNetAlgorithm] Model trained. MicroAccuracy: {metrics.MicroAccuracy:P2}");

            progressReporter.ReportProgress(algoName, "Sauvegarde du modèle", 80);
            // 5. Save the trained model to disk for persistence.
            SaveModel(_trainedModel, _modelInputSchema);

            // 6. Create a prediction engine for fast predictions at runtime.
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<NavigationInput, NavigationPrediction>(_trainedModel);

            // 7. Get the mapping of label names from the *output schema* of the prediction engine.
            GetSlotNamesFromSchema(_predictionEngine.OutputSchema);

            progressReporter.ReportProgress(algoName, "Modèle prêt", 100, true);
            Console.WriteLine("[MLNetAlgorithm] Training complete and model ready for predictions.");
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
                Console.WriteLine("[MLNetAlgorithm] Model not trained, slot names missing, or current context is null/empty. Cannot predict.");
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            var suggestions = new List<SuggestedItem>();

            // Get all possible destinations from the app catalog.
            IEnumerable<string> allPossibleDestinations = _appNavigationCatalog?.GetAllAvailableNavigationItems() ?? Enumerable.Empty<string>();

            // If the app catalog is empty, fall back to the labels the model was trained on.
            if (!allPossibleDestinations.Any() && _slotNames != null)
            {
                allPossibleDestinations = _slotNames.Select(s => s.Key).ToList();
            }


            if (!allPossibleDestinations.Any())
            {
                Console.WriteLine("[MLNetAlgorithm] No possible destinations available for prediction from AppNavigationCatalog or model slots.");
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            // For each potential next page, create an input and get its prediction score.
            foreach (var potentialNextPage in allPossibleDestinations)
            {
                var input = new NavigationInput
                {
                    UserId = userId,
                    CurrentPageOrFeature = currentContext,
                    PreviousPageOrFeature = contextData != null && contextData.TryGetValue("PreviousPage", out var prevPage) ? prevPage : "NONE",
                    HourOfDay = DateTimeOffset.Now.Hour,
                    DayOfWeek = (float)DateTimeOffset.Now.DayOfWeek,
                    NextPageOrFeature = potentialNextPage // This is the target label we are scoring for
                };

                var prediction = _predictionEngine.Predict(input);

                // Find the index of the 'potentialNextPage' within the model's known labels (_slotNames)
                // to retrieve its specific probability score.
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
                        Score = prediction.Scores[labelIndex] * Weight,
                        Reason = Name
                    });
                }
            }

            var finalSuggestions = suggestions
                .OrderByDescending(s => s.Score)
                .Take(numberOfSuggestions)
                .ToList();

            Console.WriteLine($"[MLNetAlgorithm] Predicted {finalSuggestions.Count} items for user '{userId}' (context: {currentContext}).");
            return Task.FromResult<IEnumerable<SuggestedItem>>(finalSuggestions.AsEnumerable());
        }

        /// <summary>
        /// Saves the trained ML.NET model to disk.
        /// </summary>
        /// <param name="model">The trained ITransformer model.</param>
        /// <param name="schema">The input schema used to train the model. This is needed by ML.NET's Save method.</param>
        private void SaveModel(ITransformer model, DataViewSchema schema)
        {
            _mlContext.Model.Save(model, schema, _modelPath);
            Console.WriteLine($"[MLNetAlgorithm] Model saved to '{_modelPath}'.");
        }

        /// <summary>
        /// Attempts to load an existing ML.NET model from disk.
        /// </summary>
        private void LoadModel()
        {
            if (File.Exists(_modelPath))
            {
                try
                {
                    // Load the model. The *input* schema is loaded into _modelInputSchema.
                    _trainedModel = _mlContext.Model.Load(_modelPath, out _modelInputSchema);
                    // Create prediction engine from the loaded model
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<NavigationInput, NavigationPrediction>(_trainedModel);

                    // Re-establish the slot names (label mapping) from the *output schema* of the prediction engine.
                    // CRITICAL FIX: Ensure _predictionEngine is not null before accessing its OutputSchema
                    if (_predictionEngine != null)
                    {
                        GetSlotNamesFromSchema(_predictionEngine.OutputSchema);
                    }
                    else
                    {
                        Console.WriteLine("[MLNetAlgorithm] PredictionEngine is null after loading model. Cannot get output schema.");
                        _slotNames = null; // Ensure slot names are cleared
                    }


                    Console.WriteLine($"[MLNetAlgorithm] Model loaded successfully from '{_modelPath}'.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MLNetAlgorithm] Error loading model from '{_modelPath}': {ex.Message}. A new model will be trained.");
                    _trainedModel = null;
                    _predictionEngine = null;
                    _modelInputSchema = null;
                    _slotNames = null;
                }
            }
            else
            {
                Console.WriteLine($"[MLNetAlgorithm] No existing model file found at '{_modelPath}'. A new model will be trained on startup data.");
            }
        }

        /// <summary>
        /// Extracts and stores the label slot names from the model's output schema.
        /// These names correspond to the indices in the prediction's 'Scores' array.
        /// </summary>
        /// <param name="schema">The DataViewSchema of the model's output (e.g., from PredictionEngine.OutputSchema).</param>
        private void GetSlotNamesFromSchema(DataViewSchema schema)
        {
            _slotNames = null; // Reset in case

            // Use GetColumnOrNull to safely access the "Score" column.
            DataViewSchema.Column? scoreColumn = schema.GetColumnOrNull("Score");

            // Check if the column was found and has a value.
            if (scoreColumn.HasValue)
            {
                VBuffer<ReadOnlyMemory<char>> slotNamesBuffer = default;
                // Access Annotations property on the DataViewSchema.Column struct's Value
                // and use GetValue to retrieve the annotation by its well-known name "SlotNames".
                scoreColumn.Value.Annotations.GetValue("SlotNames", ref slotNamesBuffer);

                // Convert VBuffer<ReadOnlyMemory<char>> to List<KeyValuePair<string, float>>.
                // GetValues() returns ReadOnlySpan<ReadOnlyMemory<char>>.
                // Using .ToArray() on the ReadOnlySpan to reliably enable LINQ's Select method.
                _slotNames = slotNamesBuffer.GetValues().ToArray()
                                            .Select((s, i) => new KeyValuePair<string, float>(s.ToString(), i))
                                            .ToList();

                Console.WriteLine($"[MLNetAlgorithm] Retrieved {_slotNames.Count} slot names from schema.");
            }
            else
            {
                Console.WriteLine("[MLNetAlgorithm] 'Score' column not found in schema for slot names annotation.");
            }
        }


        /// <summary>
        /// Prepares raw NavigationEvent data into a format (NavigationInput) suitable for ML.NET training.
        /// This creates pairs of (CurrentEvent, NextEvent) where CurrentEvent features predict NextEvent.
        /// </summary>
        private IEnumerable<NavigationInput> PrepareTrainingData(IEnumerable<NavigationEvent> historicalData)
        {
            var trainingSamples = new List<NavigationInput>();

            // Group events by user and sort them by timestamp to reconstruct sequences.
            var userGroupedHistory = historicalData
                .GroupBy(e => e.UserId)
                .Select(g => g.OrderBy(e => e.Timestamp).ToList());

            foreach (var userHistory in userGroupedHistory)
            {
                // We need at least two events to form a (current -> next) sequence.
                for (int i = 0; i < userHistory.Count - 1; i++)
                {
                    var currentEvent = userHistory[i];
                    var nextEvent = userHistory[i + 1];

                    trainingSamples.Add(new NavigationInput
                    {
                        UserId = currentEvent.UserId,
                        CurrentPageOrFeature = currentEvent.CurrentPageOrFeature,
                        PreviousPageOrFeature = currentEvent.PreviousPageOrFeature ?? "NONE", // Handle null previous page
                        HourOfDay = currentEvent.Timestamp.Hour,
                        DayOfWeek = (float)currentEvent.Timestamp.DayOfWeek,
                        NextPageOrFeature = nextEvent.CurrentPageOrFeature // This is the LABEL (what we want to predict)
                    });
                }
            }
            Console.WriteLine($"[MLNetAlgorithm] Prepared {trainingSamples.Count} training samples from historical data.");
            return trainingSamples;
        }
    }
}