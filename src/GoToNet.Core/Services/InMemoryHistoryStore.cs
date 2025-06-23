// /src/GoToNet.Core/Services/InMemoryHistoryStore.cs
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
    /// Implémentation en mémoire de INavigationHistoryStore pour le prototypage et les tests.
    /// </summary>
    public class InMemoryHistoryStore : INavigationHistoryStore
    {
        // Stocke les événements de navigation par utilisateur pour un accès rapide.
        // Utilise ConcurrentDictionary et ConcurrentBag pour la sécurité des threads.
        private readonly ConcurrentDictionary<string, ConcurrentBag<NavigationEvent>> _userEvents = new();

        /// <inheritdoc />
        public Task AddEventAsync(NavigationEvent navigationEvent)
        {
            if (string.IsNullOrEmpty(navigationEvent.UserId))
            {
                throw new ArgumentException("UserId cannot be null or empty.", nameof(navigationEvent.UserId));
            }

            var eventsForUser = _userEvents.GetOrAdd(navigationEvent.UserId, new ConcurrentBag<NavigationEvent>());
            eventsForUser.Add(navigationEvent);
            Console.WriteLine($"[InMemoryHistoryStore] Added event for user '{navigationEvent.UserId}': '{navigationEvent.CurrentPageOrFeature}'");
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IEnumerable<NavigationEvent>> GetUserHistoryAsync(string userId, DateTimeOffset? since = null)
        {
            if (_userEvents.TryGetValue(userId, out var eventsForUser))
            {
                // Convertir en liste pour permettre le filtrage LINQ et un snapshot thread-safe
                var history = eventsForUser.ToList();
                if (since.HasValue)
                {
                    history = history.Where(e => e.Timestamp >= since.Value).ToList();
                }
                Console.WriteLine($"[InMemoryHistoryStore] Retrieved {history.Count} events for user '{userId}'.");
                return Task.FromResult<IEnumerable<NavigationEvent>>(history.AsEnumerable());
            }
            Console.WriteLine($"[InMemoryHistoryStore] No history found for user '{userId}'.");
            return Task.FromResult<IEnumerable<NavigationEvent>>(Enumerable.Empty<NavigationEvent>());
        }

        /// <inheritdoc />
        public Task<IEnumerable<NavigationEvent>> GetAllHistoryAsync(DateTimeOffset? since = null)
        {
            // Rassemble tous les événements de tous les utilisateurs
            var allEvents = _userEvents.Values.SelectMany(bag => bag.ToList()).ToList();
            if (since.HasValue)
            {
                allEvents = allEvents.Where(e => e.Timestamp >= since.Value).ToList();
            }
            Console.WriteLine($"[InMemoryHistoryStore] Retrieved total of {allEvents.Count} events.");
            return Task.FromResult<IEnumerable<NavigationEvent>>(allEvents.AsEnumerable());
        }
    }
}