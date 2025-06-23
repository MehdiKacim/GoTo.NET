// /src/GoToNet.Core/Services/InMemoryUserMenuPreferencesStore.cs
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GoToNet.Core.Services
{
    /// <summary>
    /// Implémentation en mémoire de IUserMenuPreferencesStore pour le prototypage et les tests.
    /// </summary>
    public class InMemoryUserMenuPreferencesStore : IUserMenuPreferencesStore
    {
        // Stocke les éléments de menu personnalisés par utilisateur.
        // Key: UserId -> Value: (Dictionary of ItemName -> UserCustomMenuItem)
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, UserCustomMenuItem>> _userCustomMenus = new();

        /// <inheritdoc />
        public Task AddOrUpdateCustomMenuItemAsync(UserCustomMenuItem item)
        {
            if (string.IsNullOrEmpty(item.UserId) || string.IsNullOrEmpty(item.ItemName))
            {
                throw new ArgumentException("UserId and ItemName cannot be null or empty.");
            }

            var userMenu = _userCustomMenus.GetOrAdd(item.UserId, new ConcurrentDictionary<string, UserCustomMenuItem>());
            userMenu.AddOrUpdate(item.ItemName, item, (key, oldValue) => item); // Ajoute ou met à jour
            Console.WriteLine($"[InMemoryUserMenuPreferencesStore] User '{item.UserId}' added/updated custom item: '{item.ItemName}'");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task RemoveCustomMenuItemAsync(string userId, string itemName)
        {
            if (_userCustomMenus.TryGetValue(userId, out var userMenu))
            {
                if (userMenu.TryRemove(itemName, out _))
                {
                    Console.WriteLine($"[InMemoryUserMenuPreferencesStore] User '{userId}' removed custom item: '{itemName}'");
                }
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IEnumerable<UserCustomMenuItem>> GetCustomMenuItemsAsync(string userId)
        {
            if (_userCustomMenus.TryGetValue(userId, out var userMenu))
            {
                // Tri par ordre pour l'affichage, si l'ordre est significatif
                var items = userMenu.Values.OrderBy(i => i.Order).ToList();
                Console.WriteLine($"[InMemoryUserMenuPreferencesStore] Retrieved {items.Count} custom items for user '{userId}'.");
                return Task.FromResult<IEnumerable<UserCustomMenuItem>>(items.AsEnumerable());
            }
            return Task.FromResult<IEnumerable<UserCustomMenuItem>>(Enumerable.Empty<UserCustomMenuItem>());
        }
    }
}