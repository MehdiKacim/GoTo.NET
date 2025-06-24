// /src/GoToNet.Core/Algorithms/DesignFlowAlgorithm.cs
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using System; // For StringComparer
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoToNet.Core.Algorithms
{
    /// <summary>
    /// Implémente un algorithme de prédiction basé sur la logique de conception ou les flux métier de l'application.
    /// Il suggère des éléments prédéfinis qui sont logiquement pertinents pour un contexte donné,
    /// en s'appuyant sur un fournisseur de règles configurable.
    /// </summary>
    public class DesignFlowAlgorithm : IDesignFlowAlgorithm // Implémente la nouvelle interface
    {
        public string Name => "DesignFlow";
        public double Weight { get; set; } = 0.7; // Poids par défaut

        private readonly IDesignFlowRulesProvider _rulesProvider; // Dépendance au fournisseur de règles
        private IReadOnlyDictionary<string, List<string>>? _applicationDesignFlows; // Les règles chargées

        /// <summary>
        /// Initialise une nouvelle instance du <see cref="DesignFlowAlgorithm"/>.
        /// </summary>
        /// <param name="rulesProvider">Le fournisseur de règles de flux de conception.</param>
        public DesignFlowAlgorithm(IDesignFlowRulesProvider rulesProvider) // Constructeur avec dépendance
        {
            _rulesProvider = rulesProvider;
        }

        /// <inheritdoc />
        public async Task TrainAsync(IEnumerable<NavigationEvent> historicalData, IAlgorithmProgressReporter progressReporter) // MODIFIED HERE
        {
            string algoName = Name;
            progressReporter.ReportProgress(algoName, "Démarrage du chargement des règles de conception", 0); // Signale le début

            // Charger les règles via le fournisseur
            _applicationDesignFlows = await _rulesProvider.GetDesignFlowRulesAsync();

            Console.WriteLine($"[DesignFlowAlgorithm] Entraînement terminé (chargé {_applicationDesignFlows.Count} règles de flux de conception).");
            progressReporter.ReportProgress(algoName, "Règles de conception chargées", 100, true, $"Chargé {_applicationDesignFlows.Count} règles."); // Signale la fin
        }

        /// <inheritdoc />
        public Task<IEnumerable<SuggestedItem>> PredictAsync(
            string userId,
            string? currentContext,
            int numberOfSuggestions,
            IDictionary<string, string>? contextData = null)
        {
            // S'assurer que les règles ont été chargées via TrainAsync
            if (_applicationDesignFlows == null || string.IsNullOrEmpty(currentContext))
            {
                Console.WriteLine("[DesignFlowAlgorithm] Règles de conception non chargées ou contexte manquant.");
                return Task.FromResult<IEnumerable<SuggestedItem>>(Enumerable.Empty<SuggestedItem>());
            }

            var suggestions = new List<SuggestedItem>();

            // Vérifie si des flux de conception sont définis pour le contexte actuel
            if (_applicationDesignFlows.TryGetValue(currentContext, out var designRelatedItems))
            {
                foreach (var item in designRelatedItems)
                {
                    suggestions.Add(new SuggestedItem
                    {
                        Name = item,
                        Score = 1.0 * Weight, // Score fixe, peut être ajusté ou rendre configurable par item
                        Reason = Name
                    });
                }
            }

            Console.WriteLine($"[DesignFlowAlgorithm] A prédit {suggestions.Count} éléments pour le contexte '{currentContext}'.");
            return Task.FromResult<IEnumerable<SuggestedItem>>(suggestions.AsEnumerable());
        }
    }
}