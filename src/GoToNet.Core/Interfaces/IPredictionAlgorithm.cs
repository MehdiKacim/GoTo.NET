// /src/GoToNet.Core/Interfaces/IPredictionAlgorithm.cs
using GoToNet.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoToNet.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a prediction algorithm.
    /// </summary>
    public interface IPredictionAlgorithm
    {
        string Name { get; }
        double Weight { get; set; }

        /// <summary>
        /// Trains the algorithm using historical navigation data.
        /// The progress reporter allows the algorithm to signal its advancement.
        /// </summary>
        /// <param name="historicalData">A collection of past navigation events.</param>
        /// <param name="progressReporter">A progress reporter for signaling training advancement.</param>
        /// <returns>A task representing the asynchronous training operation.</returns>
        Task TrainAsync(IEnumerable<NavigationEvent> historicalData, IAlgorithmProgressReporter progressReporter); // SIGNAURE INCHANGÉE


        Task<IEnumerable<SuggestedItem>> PredictAsync(
            string userId,
            string? currentContext,
            int numberOfSuggestions,
            IDictionary<string, string>? contextData = null);
    }
    
    /// <summary>
    /// Définit le contrat pour un algorithme de prédiction de flux de conception.
    /// Ces algorithmes sont spécialisés dans l'utilisation d'un fournisseur de règles de flux de conception.
    /// </summary>
    public interface IDesignFlowAlgorithm : IPredictionAlgorithm // Hérite de IPredictionAlgorithm
    {
        // Pas de membres supplémentaires ici si l'interface sert juste à typer.
        // La dépendance IDesignFlowRulesProvider sera gérée par le constructeur
        // de l'implémentation concrète (DesignFlowAlgorithm).
    }
}