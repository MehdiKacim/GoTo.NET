// /src/GoToNet.Core/Models/TrainingMode.cs
namespace GoToNet.Core.Models
{
    /// <summary>
    /// Defines the training modes for GoTo.NET prediction algorithms.
    /// </summary>
    public enum TrainingMode
    {
        /// <summary>
        /// The model is trained once when the application starts.
        /// Suitable for static or pre-trained models.
        /// </summary>
        OnStartupOnce,

        /// <summary>
        /// The model is trained asynchronously after each recorded navigation event.
        /// Suitable for development environments or small datasets.
        /// NOT RECOMMENDED FOR PRODUCTION WITH HIGH VOLUME.
        /// </summary>
        ContinuousDevelopment,

        /// <summary>
        /// Training must be manually triggered by the consuming application.
        /// </summary>
        Manual,

        /// <summary>
        /// A custom mode where the training logic is provided by the user.
        /// </summary>
        Custom
    }
}