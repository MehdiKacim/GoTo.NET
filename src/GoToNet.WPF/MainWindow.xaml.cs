// /src/GoToNet.WpfDemo/MainWindow.xaml.cs
using System.Windows;
// REMOVED: using System.Windows.Controls; (not needed as Frame is removed)

namespace GoToNet.WpfDemo
{
    /// <summary>
    /// Interaction logic for MainWindow.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Le DataContext est défini dans App.xaml.cs via l'injection de dépendances.
        }

        // REMOVED: SetNavigationService method as Frame is removed.
    }
}