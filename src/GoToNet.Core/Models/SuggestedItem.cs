// /src/GoToNet.Core/Models/SuggestedItem.cs
namespace GoToNet.Core.Models
{
    /// <summary>
    /// Représente un élément suggéré par le moteur de prédiction GoTo.NET.
    /// </summary>
    public class SuggestedItem
    {
        /// <summary>
        /// Obtient ou définit le nom de l'élément suggéré (ex: "Tableau de bord", "Mes Documents").
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Obtient ou définit le score de pertinence de l'élément suggéré.
        /// Un score plus élevé indique une plus grande pertinence.
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// Obtient ou définit la raison pour laquelle l'élément a été suggéré (ex: "Frequent", "Sequence", "UserCustom", "AI_Model").
        /// Utile pour le débogage et la compréhension.
        /// </summary>
        public string Reason { get; set; } = string.Empty;
    }
}