// /src/GoToNet.Core/Interfaces/IUserMenuPreferencesStore.cs
using GoToNet.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoToNet.Core.Interfaces
{
    /// <summary>
    /// Définit le contrat pour le stockage et la gestion des préférences de menu personnalisées par l'utilisateur.
    /// </summary>
    public interface IUserMenuPreferencesStore
    {
        /// <summary>
        /// Ajoute ou met à jour un élément de menu personnalisé pour un utilisateur.
        /// </summary>
        /// <param name="item">L'élément de menu personnalisé à ajouter ou mettre à jour.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        Task AddOrUpdateCustomMenuItemAsync(UserCustomMenuItem item);

        /// <summary>
        /// Supprime un élément de menu personnalisé pour un utilisateur.
        /// </summary>
        /// <param name="userId">L'identifiant de l'utilisateur.</param>
        /// <param name="itemName">Le nom de l'élément à supprimer.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        Task RemoveCustomMenuItemAsync(string userId, string itemName);

        /// <summary>
        /// Récupère tous les éléments de menu personnalisés pour un utilisateur.
        /// </summary>
        /// <param name="userId">L'identifiant de l'utilisateur.</param>
        /// <returns>Une tâche contenant une collection d'éléments de menu personnalisés.</returns>
        Task<IEnumerable<UserCustomMenuItem>> GetCustomMenuItemsAsync(string userId);
    }
}