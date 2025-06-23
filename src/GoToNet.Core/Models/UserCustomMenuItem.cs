// /src/GoToNet.Core/Models/UserCustomMenuItem.cs
namespace GoToNet.Core.Models
{
    /// <summary>
    /// Représente un élément de menu personnalisé ajouté par l'utilisateur.
    /// </summary>
    public class UserCustomMenuItem
    {
        /// <summary>
        /// Obtient ou définit l'identifiant unique de l'utilisateur.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Obtient ou définit le nom de l'élément personnalisé.
        /// </summary>
        public string ItemName { get; set; } = string.Empty;

        /// <summary>
        /// Obtient ou définit l'ordre de l'élément dans le menu personnalisé.
        /// Un nombre plus petit indique une priorité d'affichage plus élevée.
        /// </summary>
        public int Order { get; set; }
    }
}