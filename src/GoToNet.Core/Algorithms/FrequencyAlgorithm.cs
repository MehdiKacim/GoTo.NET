// /src/GoToNet.Core/Algorithms/FrequencyAlgorithm.cs
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
    /// Implémente un algorithme de prédiction basé uniquement sur la fréquence des visites de pages/fonctionnalités.
    /// Il suggère les éléments qu'un utilisateur individuel a visités le plus souvent.
    /// </summary>
    public class FrequencyAlgorithm : IPredictionAlgorithm
    {
        /// <summary>
        /// Obtient le nom unique de cet algorithme.
        /// </summary>
        public string Name => "Frequency";

        /// <summary>
        /// Obtient ou définit la pondération appliquée aux scores générés par cet algorithme
        /// lors de la combinaison avec d'autres algorithmes dans le GoToPredictionEngine.
        /// </summary>
        public double Weight { get; set; } = 1.0; // Poids par défaut

        // Stocke la fréquence de chaque page/fonctionnalité par utilisateur.
        // Clé: UserId -> Valeur: (Dictionnaire de Nom de Page/Fonctionnalité -> Compte)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, int>> _userPageFrequencies = new();

        /// <inheritdoc />
        public Task TrainAsync(IEnumerable<NavigationEvent> historicalData, IAlgorithmProgressReporter progressReporter)
        {
            string algoName = Name;
            progressReporter.ReportProgress(algoName, "Démarrage du calcul des fréquences", 0); // Signale le début

            _userPageFrequencies.Clear(); // Réinitialise pour un ré-entraînement complet.

            foreach (var interaction in historicalData)
            {
                if (string.IsNullOrEmpty(interaction.UserId)) continue;
                var userFreq = _userPageFrequencies.GetOrAdd(interaction.UserId, new ConcurrentDictionary<string, int>());
                userFreq.AddOrUpdate(interaction.CurrentPageOrFeature, 1, (key, oldValue) => oldValue + 1);
            }

            Console.WriteLine($"[FrequencyAlgorithm] Entraîné avec {historicalData.Count()} événements.");
            progressReporter.ReportProgress(algoName, "Fréquences calculées", 100, true, $"Traités {historicalData.Count()} événements."); // Signale la fin
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IEnumerable<SuggestedItem>> PredictAsync(
            string userId,
            string? currentContext, // Non utilisé par FrequencyAlgorithm
            int numberOfSuggestions,
            IDictionary<string, string>? contextData = null) // Non utilisé par FrequencyAlgorithm
        {
            // Tente d'obtenir les données de fréquence pour l'utilisateur spécifique
            if (!_userPageFrequencies.TryGetValue(userId, out var userFreq) || userFreq.Count == 0)
            {
                // Si aucun historique n'existe pour l'utilisateur, retourne une liste vide
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            // Trie les pages/fonctionnalités visitées par l'utilisateur par fréquence décroissante,
            // sélectionne les N premiers éléments et les convertit en objets SuggestedItem.
            var suggestions = userFreq
                .OrderByDescending(kvp => kvp.Value) // Trie par le nombre de visites (Value)
                .Select(kvp => new SuggestedItem
                {
                    Name = kvp.Key,              // Le nom de la page/fonctionnalité
                    Score = kvp.Value * Weight,  // Calcule le score en appliquant la pondération de l'algorithme
                    Reason = Name                // Indique la source de la suggestion
                })
                .Take(numberOfSuggestions)       // Prend seulement le nombre de suggestions demandé
                .ToList();                       // Convertit en liste

            Console.WriteLine($"[FrequencyAlgorithm] A prédit {suggestions.Count} éléments pour l'utilisateur '{userId}'.");
            return Task.FromResult<IEnumerable<SuggestedItem>>(suggestions.AsEnumerable());
        }
    }
}