// /src/GoToNet.Core/Models/MlNet/NavigationInput.cs
using Microsoft.ML.Data;

namespace GoToNet.Core.Models.MlNet
{
    /// <summary>
    /// Represents the input data (features) for the ML.NET prediction model.
    /// </summary>
    public class NavigationInput
    {
        [LoadColumn(0)] // Column index in the training data
        public string UserId { get; set; } = string.Empty;

        [LoadColumn(1)]
        public string CurrentPageOrFeature { get; set; } = string.Empty;

        [LoadColumn(2)]
        public string PreviousPageOrFeature { get; set; } = "NONE"; // Use a specific string for null/empty

        // Temporal features can be very powerful for predicting user behavior
        [LoadColumn(3)]
        public float HourOfDay { get; set; } // 0-23
        [LoadColumn(4)]
        public float DayOfWeek { get; set; } // 0 (Sunday) - 6 (Saturday)

        // This is the 'Label' column that ML.NET will try to predict during training.
        // When making a prediction, you'd iterate through all possible 'NextPageOrFeature' values
        // to get a score for each.
        [LoadColumn(5)]
        public string NextPageOrFeature { get; set; } = string.Empty; // The target page/feature
    }
}