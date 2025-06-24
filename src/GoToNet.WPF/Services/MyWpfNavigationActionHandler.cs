// /src/GoToNet.WpfDemo/Services/MyWpfNavigationActionHandler.cs
using GoToNet.Core.Interfaces;
using System.Windows; // Pour MessageBox (pour simuler la navigation)
using System;

namespace GoToNet.WpfDemo.Services
{
    public class MyWpfNavigationActionHandler : INavigationActionHandler
    {
        // REMOVED: private readonly IWpfNavigationService _wpfNavigationService;
        // REMOVED: Constructor dependency on IWpfNavigationService

        public MyWpfNavigationActionHandler() // NOUVEAU : Constructeur sans paramètres
        {
            // Le service ne gère plus la navigation Frame, juste les rapports.
        }

        public bool PerformNavigation(string userId, string itemName)
        {
            Console.WriteLine($"[WPF Action Handler] Utilisateur '{userId}' demande la navigation vers : '{itemName}'");
            MessageBox.Show($"Simulation de navigation vers : {itemName}", "Navigation GoTo.NET", MessageBoxButton.OK, MessageBoxImage.Information);
            // La navigation réelle dans l'UI est maintenant gérée par MainWindowViewModel.CurrentPageContext
            return true;
        }
    }
}