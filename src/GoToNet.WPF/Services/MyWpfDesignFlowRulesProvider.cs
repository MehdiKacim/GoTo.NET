// /src/GoToNet.WpfDemo/Services/MyWpfDesignFlowRulesProvider.cs
using GoToNet.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GoToNet.WpfDemo.Services
{
    /// <summary>
    /// Implémentation spécifique à l'application WPF de démonstration pour IDesignFlowRulesProvider.
    /// Fournit des règles de flux de conception en dur pour l'exemple WPF.
    /// Dans une application réelle, ces règles seraient chargées depuis une configuration, une base de données, etc.
    /// </summary>
    public class MyWpfDesignFlowRulesProvider : IDesignFlowRulesProvider
    {
        private readonly IReadOnlyDictionary<string, List<string>> _wpfDesignFlows;

        public MyWpfDesignFlowRulesProvider()
        {
            // Ces règles sont spécifiques à la logique de navigation de l'application WPF de démonstration.
            _wpfDesignFlows = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                // NOUVEAU : Règles basées sur les noms de pages "real-world"
                { "Dashboard", new List<string> { "Vue d'ensemble rapide", "Alertes importantes", "Gestion de Projets" } },
                { "ProjectManagement", new List<string> { "Créer un nouveau projet", "Voir tous les projets", "Rechercher un projet" } },
                { "UserProfiles", new List<string> { "Ajouter un utilisateur", "Gérer les rôles", "Rapports utilisateurs" } },
                { "Settings", new List<string> { "Paramètres de l'application", "Mon profil", "Notifications" } },
                
                // Anciennes règles (si elles sont toujours pertinentes et que les pages existent)
                { "Home", new List<string> { "Dashboard", "ProjectManagement" } }, // Ex: depuis la page d'accueil
                { "Projects", new List<string> { "Add New Project", "View All Projects", "Search Projects" } },
                { "Project/Add", new List<string> { "Save Project", "Cancel Add" } },
                { "Project/Edit", new List<string> { "Save Project Changes", "Discard Changes" } },
                { "Getting Started", new List<string> { "Créer un compte", "Voir le tutoriel", "Commencer un essai gratuit" } }
            };
        }

        /// <inheritdoc />
        public Task<IReadOnlyDictionary<string, List<string>>> GetDesignFlowRulesAsync()
        {
            Console.WriteLine("[MyWpfDesignFlowRulesProvider] Fourniture des règles de flux de conception spécifiques à WPF (en dur).");
            return Task.FromResult(_wpfDesignFlows);
        }
    }
}