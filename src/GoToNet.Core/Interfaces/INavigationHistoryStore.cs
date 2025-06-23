// /src/GoToNet.Core/Interfaces/INavigationHistoryStore.cs
using GoToNet.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoToNet.Core.Interfaces
{
    /// <summary>
    /// Définit le contrat pour le stockage et la récupération de l'historique de navigation de l'utilisateur.
    /// </summary>
    public interface INavigationHistoryStore
    {
        /// <summary>
        /// Ajoute un événement de navigation à l'historique.
        /// </summary>
        /// <param name="navigationEvent">L'événement de navigation à ajouter.</param>
        /// <returns>Une tâche représentant l'opération asynchrone.</returns>
        Task AddEventAsync(NavigationEvent navigationEvent);

        /// <summary>
        /// Récupère l'historique de navigation pour un utilisateur spécifique.
        /// </summary>
        /// <param name="userId">L'identifiant de l'utilisateur.</param>
        /// <param name="since">Date et heure à partir de laquelle récupérer les événements (optionnel).</param>
        /// <returns>Une tâche contenant une collection d'événements de navigation.</returns>
        Task<IEnumerable<NavigationEvent>> GetUserHistoryAsync(string userId, DateTimeOffset? since = null);

        /// <summary>
        /// Récupère l'historique de navigation de tous les utilisateurs.
        /// </summary>
        /// <param name="since">Date et heure à partir de laquelle récupérer les événements (optionnel).</param>
        /// <returns>Une tâche contenant une collection d'événements de navigation pour tous les utilisateurs.</returns>
        Task<IEnumerable<NavigationEvent>> GetAllHistoryAsync(DateTimeOffset? since = null);
    }
}