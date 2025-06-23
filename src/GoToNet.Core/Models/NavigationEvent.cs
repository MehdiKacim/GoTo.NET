// /src/GoToNet.Core/Models/NavigationEvent.cs
using System;
using System.Collections.Generic;

namespace GoToNet.Core.Models
{
    /// <summary>
    /// Représente un événement de navigation ou une interaction utilisateur au sein de l'application.
    /// </summary>
    public class NavigationEvent
    {
        /// <summary>
        /// Obtient ou définit l'identifiant unique de l'utilisateur.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Obtient ou définit le nom de la page ou de la fonctionnalité actuellement visitée/utilisée.
        /// </summary>
        public string CurrentPageOrFeature { get; set; } = string.Empty;

        /// <summary>
        /// Obtient ou définit le nom de la page ou de la fonctionnalité précédemment visitée/utilisée, si applicable.
        /// Peut être null si c'est la première interaction dans une session.
        /// </summary>
        public string? PreviousPageOrFeature { get; set; }

        /// <summary>
        /// Obtient ou définit l'horodatage de l'événement de navigation.
        /// Défaut à l'heure UTC actuelle.
        /// </summary>
        public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;

        /// <summary>
        /// Obtient ou définit l'identifiant de la session à laquelle appartient cet événement.
        /// Peut être null si la notion de session n'est pas pertinente ou disponible.
        /// </summary>
        public string? SessionId { get; set; }

        /// <summary>
        /// Obtient ou définit des données contextuelles additionnelles associées à l'événement.
        /// Par exemple : type d'appareil, rôle de l'utilisateur, projet en cours.
        /// </summary>
        public Dictionary<string, string>? ContextData { get; set; }
    }
}