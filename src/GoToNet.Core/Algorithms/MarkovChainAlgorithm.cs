// /src/GoToNet.Core/Algorithms/MarkovChainAlgorithm.cs
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System; // Required for Console.WriteLine

namespace GoToNet.Core.Algorithms
{
    /// <summary>
    /// Implémente un algorithme de prédiction basé sur les chaînes de Markov du premier ordre.
    /// Il prédit la prochaine page/fonctionnalité la plus probable en fonction de la page actuelle.
    /// </summary>
    public class MarkovChainAlgorithm : IPredictionAlgorithm
    {
        public string Name => "MarkovChain";
        public double Weight { get; set; } = 1.5; // Poids par défaut, généralement plus élevé que Fréquence car plus contextuel

        // Stocke les transitions observées pour chaque utilisateur:
        // UserId -> PageSource -> PageDestination -> Count
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ConcurrentDictionary<string, int>>> _userTransitions = new();

        /// <inheritdoc />
        public Task TrainAsync(IEnumerable<NavigationEvent> historicalData, IAlgorithmProgressReporter progressReporter) // MODIFIED HERE
        {
            string algoName = Name;
            progressReporter.ReportProgress(algoName, "Démarrage du calcul des transitions", 0); // Signale le début

            _userTransitions.Clear(); // Réinitialise pour un ré-entraînement complet

            // L'ordonnancement par timestamp est crucial pour reconstruire correctement les séquences
            foreach (var interaction in historicalData.OrderBy(e => e.Timestamp))
            {
                // Nous avons besoin d'une page précédente pour établir une transition
                if (string.IsNullOrEmpty(interaction.PreviousPageOrFeature)) continue;

                var userId = interaction.UserId;
                var sourcePage = interaction.PreviousPageOrFeature;
                var destinationPage = interaction.CurrentPageOrFeature;

                var userTrans = _userTransitions.GetOrAdd(userId, new ConcurrentDictionary<string, ConcurrentDictionary<string, int>>());
                var sourcePageTrans = userTrans.GetOrAdd(sourcePage, new ConcurrentDictionary<string, int>());

                sourcePageTrans.AddOrUpdate(destinationPage, 1, (key, oldValue) => oldValue + 1);
            }
            Console.WriteLine($"[MarkovChainAlgorithm] Entraîné avec {historicalData.Count()} événements.");
            progressReporter.ReportProgress(algoName, "Transitions calculées", 100, true, $"Traités {historicalData.Count()} événements."); // Signale la fin
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IEnumerable<SuggestedItem>> PredictAsync(
            string userId,
            string? currentContext, // C'est le CONTEXTE CLÉ pour cet algorithme
            int numberOfSuggestions,
            IDictionary<string, string>? contextData = null)
        {
            // Cet algorithme a absolument besoin d'un contexte actuel pour faire des prédictions séquentielles.
            if (string.IsNullOrEmpty(currentContext) || !_userTransitions.TryGetValue(userId, out var userTrans))
            {
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            // Tente d'obtenir les transitions à partir de la page actuelle de l'utilisateur
            if (!userTrans.TryGetValue(currentContext, out var transitionsFromContext) || transitionsFromContext.Count == 0)
            {
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            // Calcule le nombre total de transitions à partir de la page actuelle pour normaliser en probabilités (scores)
            double totalTransitions = transitionsFromContext.Values.Sum();
            if (totalTransitions == 0)
            {
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            var suggestions = transitionsFromContext
                .OrderByDescending(kvp => kvp.Value) // Trie par le nombre de fois que la transition s'est produite
                .Select(kvp => new SuggestedItem
                {
                    Name = kvp.Key,
                    // Le score est la probabilité de transition multipliée par le poids de l'algorithme
                    Score = (kvp.Value / totalTransitions) * Weight,
                    Reason = Name
                })
                .Take(numberOfSuggestions)
                .ToList();

            Console.WriteLine($"[MarkovChainAlgorithm] A prédit {suggestions.Count} éléments pour l'utilisateur '{userId}' depuis le contexte '{currentContext}'.");
            return Task.FromResult<IEnumerable<SuggestedItem>>(suggestions.AsEnumerable());
        }
    }
}