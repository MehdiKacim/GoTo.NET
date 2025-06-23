// /src/GoToNet.Core/Interfaces/INavigationActionHandler.cs
namespace GoToNet.Core.Interfaces
{
    /// <summary>
    /// Définit le contrat pour la gestion des actions de navigation réelles au sein de l'application cliente.
    /// </summary>
    public interface INavigationActionHandler
    {
        /// <summary>
        /// Appelée par GoTo.NET pour demander à l'application cliente de naviguer vers un élément spécifié.
        /// L'implémentation doit gérer la logique de navigation réelle dans l'interface utilisateur de l'application.
        /// </summary>
        /// <param name="userId">L'identifiant de l'utilisateur demandant la navigation.</param>
        /// <param name="itemName">Le nom de l'élément vers lequel naviguer.</param>
        /// <returns>True si la navigation a été prise en charge et réussie, False sinon.</returns>
        bool PerformNavigation(string userId, string itemName);
    }
}