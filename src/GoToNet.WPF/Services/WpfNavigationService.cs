// GoToNet.WpfDemo/Services/WpfNavigationService.cs
using System;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace GoToNet.WpfDemo.Services
{
    public interface IWpfNavigationService
    {
        void Initialize(Frame navigationFrame);
        void NavigateTo(string pageName);
    }

    public class WpfNavigationService : IWpfNavigationService
    {
        private Frame? _navigationFrame;

        public void Initialize(Frame navigationFrame)
        {
            _navigationFrame = navigationFrame ?? throw new ArgumentNullException(nameof(navigationFrame));
            Console.WriteLine("[WpfNavigationService] Initialized navigation frame.");
        }

        public void NavigateTo(string pageName)
        {
            if (_navigationFrame == null)
            {
                Console.WriteLine("[WpfNavigationService] Navigation frame not initialized.");
                return;
            }

            // In a real app, you might map page names to actual Page instances or URIs
            // For this demo, we'll use a simple switch or direct URI based on convention
            Uri pageUri;
            switch (pageName)
            {
                case "Home":
                case "/home":
                    pageUri = new Uri("/Views/HomePage.xaml", UriKind.Relative);
                    break;
                case "Projects":
                case "/projects":
                    pageUri = new Uri("/Views/ProjectListPage.xaml", UriKind.Relative);
                    break;
                // Add cases for other pages as needed
                default:
                    Console.WriteLine($"[WpfNavigationService] Attempted to navigate to unknown page: {pageName}");
                    return;
            }

            _navigationFrame.Navigate(pageUri);
            Console.WriteLine($"[WpfNavigationService] Navigated to: {pageName}");
        }
    }
}