using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoToNet.Core.Services
{
    /// <summary>
    /// Fournit des fonctionnalités pour permettre aux utilisateurs de gérer leurs propres éléments de menu personnalisés.
    /// </summary>
    public class UserMenuBuilder
    {
        private readonly IUserMenuPreferencesStore _preferencesStore;

        /// <summary>
        /// Initialise une nouvelle instance du <see cref="UserMenuBuilder"/>.
        /// </summary>
        /// <param name="preferencesStore">Le store pour persister les préférences de menu de l'utilisateur.</param>
        public UserMenuBuilder(IUserMenuPreferencesStore preferencesStore)
        {
            _preferencesStore = preferencesStore;
        }

        /// <summary>
        /// Ajoute ou met à jour un élément dans le menu personnalisé d'un utilisateur.
        /// </summary>
        /// <param name="userId">L'identifiant de l'utilisateur.</param>
        /// <param name="itemName">Le nom de l'élément à ajouter ou mettre à jour.</param>
        /// <param name="order">L'ordre d'affichage de l'élément dans le menu (les plus petits nombres viennent en premier).</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public async Task AddItemToUserMenuAsync(string userId, string itemName, int order = 0)
        {
            await _preferencesStore.AddOrUpdateCustomMenuItemAsync(new UserCustomMenuItem
            {
                UserId = userId,
                ItemName = itemName,
                Order = order
            });
        }

        /// <summary>
        /// Supprime un élément du menu personnalisé d'un utilisateur.
        /// </summary>
        /// <param name="userId">L'identifiant de l'utilisateur.</param>
        /// <param name="itemName">Le nom de l'élément à supprimer.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public async Task RemoveItemFromUserMenuAsync(string userId, string itemName)
        {
            await _preferencesStore.RemoveCustomMenuItemAsync(userId, itemName);
        }

        /// <summary>
        /// Récupère tous les éléments du menu personnalisé d'un utilisateur.
        /// </summary>
        /// <param name="userId">L'identifiant de l'utilisateur.</param>
        /// <returns>Une tâche contenant une collection d'éléments de menu personnalisés.</returns>
        public async Task<IEnumerable<UserCustomMenuItem>> GetUserCustomMenuAsync(string userId)
        {
            return await _preferencesStore.GetCustomMenuItemsAsync(userId);
        }

        /// <summary>
        /// Efface tous les éléments du menu personnalisé d'un utilisateur.
        /// </summary>
        /// <param name="userId">L'identifiant de l'utilisateur.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        public async Task ClearUserCustomMenuAsync(string userId)
        {
            var currentItems = await _preferencesStore.GetCustomMenuItemsAsync(userId);
            foreach (var item in currentItems)
            {
                await _preferencesStore.RemoveCustomMenuItemAsync(userId, item.ItemName);
            }
            Console.WriteLine($"[UserMenuBuilder] Cleared custom menu for user '{userId}'.");
        }
    }
}