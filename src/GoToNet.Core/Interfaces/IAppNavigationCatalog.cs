// /src/GoToNet.Core/Interfaces/IAppNavigationCatalog.cs
using System.Collections.Generic;

namespace GoToNet.Core.Interfaces
{
    /// <summary>
    /// Définit le contrat pour la fourniture d'une liste exhaustive de toutes les pages ou fonctionnalités
    /// de navigation disponibles dans l'application cliente.
    /// </summary>
    public interface IAppNavigationCatalog
    {
        /// <summary>
        /// Fournit une liste de tous les éléments de navigation disponibles dans l'application.
        /// Utilisé par les algorithmes de GoTo.NET (comme ML.NET) qui ont besoin de connaître
        /// l'ensemble complet des destinations possibles.
        /// </summary>
        /// <returns>Une collection de noms d'éléments de navigation disponibles.</returns>
        IEnumerable<string> GetAllAvailableNavigationItems();
    }
}