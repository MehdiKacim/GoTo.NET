// /src/GoToNet.Core/Models/Configuration/GoToNetConfig.cs
using System.Collections.Generic;

namespace GoToNet.Core.Models.Configuration
{
    public class GoToNetConfig
    {
        // Liste explicite de toutes les pages/fonctionnalités de l'application (catalogue global pour ML.NET)
        public List<string> AppPages { get; set; } = new List<string>();

        // Les flux de design (règles de transition pour DesignFlowAlgorithm)
        public List<DesignFlowConfigEntry>? DesignFlows { get; set; }

        // Liste des pages à afficher spécifiquement dans le menu de navigation principal de l'UI
        public List<string> MainNavigationItems { get; set; } = new List<string>();
    }

    public class DesignFlowConfigEntry
    {
        public string SourcePage { get; set; } = string.Empty;
        public List<string> TargetPages { get; set; } = new List<string>();
    }
}