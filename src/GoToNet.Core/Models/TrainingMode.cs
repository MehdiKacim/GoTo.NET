// /src/GoToNet.Core/Models/TrainingMode.cs
namespace GoToNet.Core.Models
{
    /// <summary>
    /// Définit les modes d'entraînement pour les algorithmes de prédiction GoTo.NET.
    /// </summary>
    public enum TrainingMode
    {
        /// <summary>
        /// Le modèle est entraîné une seule fois au démarrage de l'application.
        /// Convient pour des modèles statiques ou pré-entraînés.
        /// </summary>
        OnStartupOnce,

        /// <summary>
        /// Le modèle est entraîné de manière asynchrone après chaque événement de navigation enregistré.
        /// Convient pour les environnements de développement ou les petits ensembles de données.
        /// NE PAS UTILISER EN PRODUCTION AVEC UN VOLUME ÉLEVÉ.
        /// </summary>
        ContinuousDevelopment,

        /// <summary>
        /// Le modèle est entraîné périodiquement en arrière-plan ou après qu'un certain seuil d'événements soit atteint.
        /// Mode recommandé pour l'apprentissage continu en production.
        /// </summary>
        BatchScheduled, // NOUVEAU MODE

        /// <summary>
        /// L'entraînement doit être déclenché manuellement par l'application consommatrice.
        /// </summary>
        Manual,

        /// <summary>
        /// Un mode personnalisé où la logique d'entraînement est fournie par l'utilisateur.
        /// </summary>
        Custom
    }
}