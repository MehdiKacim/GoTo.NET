// /src/GoToNet.Core/Interfaces/INavigationNotifier.cs
using GoToNet.Core.Models;
using System.Collections.Generic;

namespace GoToNet.Core.Interfaces
{
    /// <summary>
    /// Définit le contrat pour la notification et la mise à jour de l'interface utilisateur
    /// avec les suggestions de navigation générées par GoTo.NET.
    /// </summary>
    public interface INavigationNotifier
    {
        /// <summary>
        /// Appelée par GoTo.NET pour notifier l'application cliente des suggestions de navigation mises à jour.
        /// L'implémentation doit mettre à jour l'interface utilisateur en conséquence.
        /// </summary>
        /// <param name="userId">L'identifiant de l'utilisateur concerné.</param>
        /// <param name="suggestedItems">La liste des éléments de navigation suggérés par GoTo.NET.</param>
        void UpdateSuggestionsDisplay(string userId, IEnumerable<SuggestedItem> suggestedItems);
    }
}