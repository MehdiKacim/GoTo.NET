// /src/GoToNet.Core/Interfaces/IAlgorithmProgressReporter.cs
using GoToNet.Core.Models;
using System;

namespace GoToNet.Core.Interfaces
{
    /// <summary>
    /// Defines the contract for a component capable of reporting algorithm training progress.
    /// </summary>
    public interface IAlgorithmProgressReporter
    {
        // Supprimons l'événement de cette interface.
        // Les algorithmes appelleront une méthode pour rapporter la progression.

        /// <summary>
        /// Reports the progress of an algorithm's training.
        /// </summary>
        /// <param name="algorithmName">The name of the algorithm reporting progress.</param>
        /// <param name="currentStep">The current step in the training process.</param>
        /// <param name="progressPercentage">The progress as a percentage (0-100).</param>
        /// <param name="isCompleted">True if this step marks the completion of the algorithm's training.</param>
        /// <param name="message">An optional message about the status or results.</param>
        void ReportProgress(string algorithmName, string currentStep, int progressPercentage, bool isCompleted = false, string? message = null);
    }
}