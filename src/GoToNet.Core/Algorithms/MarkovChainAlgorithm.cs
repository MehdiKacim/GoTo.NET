// /src/GoToNet.Core/Algorithms/MarkovChainAlgorithm.cs
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoToNet.Core.Algorithms
{
    /// <summary>
    /// Implements a prediction algorithm based on first-order Markov chains.
    /// It predicts the next most probable page/feature based on the current page.
    /// </summary>
    public class MarkovChainAlgorithm : IPredictionAlgorithm
    {
        public string Name => "MarkovChain";
        public double Weight { get; set; } = 1.5; // Default weight, usually higher than Frequency as it's more contextual

        // Stores observed transitions for each user:
        // UserId -> PageSource -> PageDestination -> Count
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, int>>> _userTransitions = new();

        /// <inheritdoc />
        public Task TrainAsync(IEnumerable<NavigationEvent> historicalData, IAlgorithmProgressReporter progressReporter) // MODIFIED HERE
        {
            string algoName = Name;
            progressReporter.ReportProgress(algoName, "Démarrage du calcul des transitions", 0); // Report start

            _userTransitions.Clear(); // Reset for a full re-train

            // Ordering by timestamp is crucial for reconstructing sequences correctly
            foreach (var interaction in historicalData.OrderBy(e => e.Timestamp))
            {
                if (string.IsNullOrEmpty(interaction.PreviousPageOrFeature)) continue;

                var userId = interaction.UserId;
                var sourcePage = interaction.PreviousPageOrFeature;
                var destinationPage = interaction.CurrentPageOrFeature;

                var userTrans = _userTransitions.GetOrAdd(userId, new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>());
                var sourcePageTrans = userTrans.GetOrAdd(sourcePage, new ConcurrentDictionary<string, int>());

                sourcePageTrans.AddOrUpdate(destinationPage, 1, (key, oldValue) => oldValue + 1);
            }
            Console.WriteLine($"[MarkovChainAlgorithm] Trained with {historicalData.Count()} events.");
            progressReporter.ReportProgress(algoName, "Transitions calculées", 100, true, $"Traités {historicalData.Count()} événements."); // Report completion
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IEnumerable<SuggestedItem>> PredictAsync(
            string userId,
            string? currentContext, // This is the KEY CONTEXT for this algorithm
            int numberOfSuggestions,
            IDictionary<string, string>? contextData = null)
        {
            // This algorithm strictly requires a current context for sequential predictions.
            if (string.IsNullOrEmpty(currentContext) || !_userTransitions.TryGetValue(userId, out var userTrans))
            {
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            // Attempt to retrieve transitions starting from the user's current page
            if (!userTrans.TryGetValue(currentContext, out var transitionsFromContext) || transitionsFromContext.Count == 0)
            {
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            // Calculate the total number of transitions from the current page to normalize into probabilities (scores)
            double totalTransitions = transitionsFromContext.Values.Sum();
            if (totalTransitions == 0)
            {
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            var suggestions = transitionsFromContext
                .OrderByDescending(kvp => kvp.Value) // Sort by the number of times the transition occurred
                .Select(kvp => new SuggestedItem
                {
                    Name = kvp.Key,
                    // The score is the transition probability multiplied by the algorithm's weight
                    Score = (kvp.Value / totalTransitions) * Weight,
                    Reason = Name
                })
                .Take(numberOfSuggestions)
                .ToList();

            Console.WriteLine($"[MarkovChainAlgorithm] Predicted {suggestions.Count} items for user '{userId}' from context '{currentContext}'.");
            return Task.FromResult<IEnumerable<SuggestedItem>>(suggestions.AsEnumerable());
        }
    }
}