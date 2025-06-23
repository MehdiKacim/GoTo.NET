// /src/GoToNet.Core/Interfaces/IDesignFlowRulesProvider.cs
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoToNet.Core.Interfaces
{
    /// <summary>
    /// Définit le contrat pour un fournisseur de règles de flux de conception d'application.
    /// </summary>
    public interface IDesignFlowRulesProvider
    {
        /// <summary>
        /// Obtient les règles de flux de conception, où la clé est la page/fonctionnalité source
        /// et la valeur est une liste des pages/fonctionnalités logiquement suivantes.
        /// </summary>
        /// <returns>Une tâche contenant un dictionnaire des règles de flux de conception.</returns>
        Task<IReadOnlyDictionary<string, List<string>>> GetDesignFlowRulesAsync();
    }
}