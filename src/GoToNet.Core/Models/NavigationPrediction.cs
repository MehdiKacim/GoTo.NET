// /src/GoToNet.Core/Models/MlNet/NavigationPrediction.cs
using Microsoft.ML.Data;

namespace GoToNet.Core.Models.MlNet
{
    /// <summary>
    /// Represents the output of the ML.NET prediction.
    /// </summary>
    public class NavigationPrediction
    {
        /// <summary>
        /// The predicted next page/feature (the one with the highest score).
        /// </summary>
        [ColumnName("PredictedLabel")]
        public string PredictedNextPageOrFeature { get; set; } = string.Empty;

        /// <summary>
        /// An array of prediction scores (probabilities) for all possible classes (pages/features).
        /// The index in this array corresponds to ML.NET's internal mapping of labels.
        /// </summary>
        [ColumnName("Score")]
        public float[] Scores { get; set; } = new float[0];
    }
}