// /src/GoToNet.Core/Models/TrainingProgressEventArgs.cs
using System;

namespace GoToNet.Core.Models
{
    /// <summary>
    /// Provides data for the algorithm training progress event.
    /// </summary>
    public class TrainingProgressEventArgs : EventArgs
    {
        /// <summary>
        /// The name of the algorithm currently being trained.
        /// </summary>
        public string AlgorithmName { get; }

        /// <summary>
        /// The current step of the progress (e.g., "Preparing Data", "Model Training").
        /// </summary>
        public string CurrentStep { get; }

        /// <summary>
        /// The progress percentage (0 to 100).
        /// </summary>
        public int ProgressPercentage { get; }

        /// <summary>
        /// Indicates if this step marks the completion of the algorithm's training.
        /// </summary>
        public bool IsCompleted { get; }

        /// <summary>
        /// Gets an optional additional message about the status or results of the step.
        /// </summary>
        public string? Message { get; }

        public TrainingProgressEventArgs(string algorithmName, string currentStep, int progressPercentage, bool isCompleted = false, string? message = null)
        {
            AlgorithmName = algorithmName;
            CurrentStep = currentStep;
            ProgressPercentage = progressPercentage;
            IsCompleted = isCompleted;
            Message = message;
        }
    }
}