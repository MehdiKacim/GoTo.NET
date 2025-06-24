// /src/GoToNet.WpfDemo/Services/MyWpfAppNavigationCatalog.cs
using GoToNet.Core.Interfaces;
using System.Collections.Generic;
using System.Linq;

namespace GoToNet.WpfDemo.Services
{
    public class MyWpfAppNavigationCatalog : IAppNavigationCatalog
    {
        // Cette liste doit contenir TOUTES les pages/fonctionnalités possibles de votre application.
        // Elle est cruciale pour ML.NET afin de prédire les probabilités pour toutes les destinations.
        private readonly List<string> _allAvailablePages = new List<string>
        {
            // Pages principales (noms "real-world app")
            "Dashboard", "ProjectManagement", "UserProfiles", "Settings",
            "Home", // La page d'accueil d'où l'app démarre
            
            // Actions CRUD spécifiques pour les projets
            "ProjectManagement/CreateNew", // Créer un nouveau projet
            "ProjectManagement/ViewAll",   // Voir tous les projets
            "ProjectManagement/Search",    // Rechercher un projet
            "ProjectManagement/EditDetails", // Editer les détails d'un projet
            "ProjectManagement/Delete",    // Supprimer un projet

            // Exemples de pages/fonctionnalités plus spécifiques qui pourraient exister
            "Reporting/Financial", "Analytics/UserBehavior", "Support/Tickets",

            // Pages internes ou actions spécifiques qui sont suivies par GoTo.NET
            "Metrics", "Logs", "Alerts", "Deployments", "Pipelines", "Environments",
            "Source Code", "Builds", "Profile", "Invoices", "Payments", "Pay Invoice",

            // Pages qui pourraient être ajoutées comme raccourcis personnalisés par l'utilisateur
            "Mon Tableau de Bord Perso", "Dernier Rapport d'Erreurs", "Contacts Clés", "Prochain Sprint",
            
            // Pages/actions liées aux flux de conception (pour ML.NET et DesignFlow)
            "Vue d'ensemble rapide", "Alertes importantes", "Paramètres du tableau de bord",
            "Créer un nouveau projet", "Voir tous les projets", "Gérer les équipes", // Redondant avec PM, mais explicite pour DesignFlow
            "Ajouter une nouvelle tâche", "Voir mes tâches", "Filtres de tâches avancés",
            "Payer une facture", "Télécharger la facture PDF", "Historique des paiements",
            "Profil utilisateur", "Changer le mot de passe", "Paramètres de notification",
            "Getting Started"
        };

        public IEnumerable<string> GetAllAvailableNavigationItems()
        {
            return _allAvailablePages;
        }
    }
}