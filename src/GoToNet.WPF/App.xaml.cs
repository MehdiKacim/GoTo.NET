// /src/GoToNet.WpfDemo/App.xaml.cs
using GoToNet.Core.Algorithms;
using GoToNet.Core.Extensions;
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using GoToNet.Core.Services; // Ensure this is present for GoTo.NET core services
using GoToNet.WpfDemo.Services; // Your WPF-specific service implementations
using GoToNet.WpfDemo.ViewModels; // Your ViewModels
using Microsoft.Extensions.DependencyInjection; // Necessary for IServiceCollection
using Microsoft.Extensions.Hosting; // Necessary for Host.CreateDefaultBuilder
using System.Collections.ObjectModel; // For ObservableCollection
using System.Windows; // For Application, EventArgs
using System; // For Task.Run
using System.Threading.Tasks;
using GoToNet.Core.Services.ConfigurationProviders; // For Task

namespace GoToNet.WpfDemo
{
    public partial class App : Application
    {
        // Publicly expose the GoTo.NET engine and UserMenuBuilder (if needed for static access by other parts of the app).
        public static GoToPredictionEngine PredictionEngine { get; private set; } = default!;
        public static UserMenuBuilder MenuBuilder { get; private set; } = default!;

        private IHost? _host; // The generic host instance manages DI and app lifecycle.

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _host = Host.CreateDefaultBuilder(e.Args)
                .ConfigureServices((context, services) =>
                {
                    // --- 1. Enregistrement des dépendances CORE de GoTo.NET ---
                    services.AddSingleton<INavigationHistoryStore, InMemoryHistoryStore>();
                    services.AddSingleton<IUserMenuPreferencesStore, InMemoryUserMenuPreferencesStore>();
                    services.AddSingleton<UserMenuBuilder>();

                    // --- 2. Enregistrement des implémentations spécifiques au client WPF pour GoTo.NET ---

                    services.AddSingleton<MyWpfNavigationNotifier>();
                    services.AddSingleton<INavigationNotifier>(sp => sp.GetRequiredService<MyWpfNavigationNotifier>());

                    services.AddSingleton<MyWpfNavigationActionHandler>();
                    services.AddSingleton<INavigationActionHandler>(sp => sp.GetRequiredService<MyWpfNavigationActionHandler>());
                    
                    // CORRECTION CRITIQUE ICI : Fournir le chemin du fichier YAML au constructeur de YamlConfigProvider.
                    // C'est la ligne qui cause l'erreur CS1061.
                    // Au lieu de services.AddSingleton<YamlConfigProvider>();
                    services.AddSingleton<YamlConfigProvider>(sp => new YamlConfigProvider("app_config.yaml"));
                    
                    services.AddSingleton<IAppNavigationCatalog>(sp => sp.GetRequiredService<YamlConfigProvider>());
                    services.AddSingleton<IDesignFlowRulesProvider>(sp => sp.GetRequiredService<YamlConfigProvider>());
                    
                    services.AddSingleton<MyWpfDesignFlowRulesProvider>(); 
                    services.AddSingleton<IDesignFlowRulesProvider>(sp => sp.GetRequiredService<MyWpfDesignFlowRulesProvider>());

                    // --- 3. Enregistrement des algorithmes de prédiction de GoTo.NET ---
                    services.AddSingleton<FrequencyAlgorithm>();
                    services.AddSingleton<IPredictionAlgorithm>(sp => sp.GetRequiredService<FrequencyAlgorithm>());

                    services.AddSingleton<MarkovChainAlgorithm>();
                    services.AddSingleton<IPredictionAlgorithm>(sp => sp.GetRequiredService<MarkovChainAlgorithm>());

                    services.AddSingleton<DesignFlowAlgorithm>();
                    services.AddSingleton<IDesignFlowAlgorithm>(sp => sp.GetRequiredService<DesignFlowAlgorithm>());

                    services.AddSingleton<MLNetAlgorithm>();
                    services.AddSingleton<IPredictionAlgorithm>(sp => sp.GetRequiredService<MLNetAlgorithm>());


                    // --- 4. Appel de la méthode d'extension AddGoToNet pour configurer le moteur de prédiction ---
                    services.AddGoToNet(options =>
                    {
                        options.AddAlgorithm<FrequencyAlgorithm>();
                        options.AddAlgorithm<MarkovChainAlgorithm>();
                        options.AddDesignFlowAlgorithm<DesignFlowAlgorithm>();
                        options.AddAlgorithm<MLNetAlgorithm>();

                        options.SetNavigationNotifier<MyWpfNavigationNotifier>();
                        options.SetNavigationActionHandler<MyWpfNavigationActionHandler>();
                        options.SetAppNavigationCatalog<YamlConfigProvider>();
                        options.SetDesignFlowRulesProvider<YamlConfigProvider>(); 
                        
                        options.TrainingMode = TrainingMode.BatchScheduled; 
                    });

                    // --- 5. Enregistrement des Vues et ViewModels WPF ---
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainWindowViewModel>(); 
                    services.AddSingleton<ProjectListPageViewModel>(); 
                })
                .Build(); // Construit l'IHost

            // Démarre l'hôte. Ceci initialise tous les services.
            await _host.StartAsync();

            // --- Charger la configuration YAML APRÈS le démarrage de l'hôte ---
            // Le YamlConfigProvider est résolu ici et sa méthode LoadConfigAsync() est appelée.
            var yamlConfigProvider = _host.Services.GetRequiredService<YamlConfigProvider>();
            await yamlConfigProvider.LoadConfigAsync();


            // Résout les services GoTo.NET pour un accès statique (si besoin)
            PredictionEngine = _host.Services.GetRequiredService<GoToPredictionEngine>();
            MenuBuilder = _host.Services.GetRequiredService<UserMenuBuilder>();

            // Résout et définit le DataContext pour MainWindow.
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _host.Services.GetRequiredService<MainWindowViewModel>();

            // Initialisation du Notifier APRÈS que le ViewModel soit prêt.
            var wpfNotifier = _host.Services.GetRequiredService<MyWpfNavigationNotifier>();
            var mainWindowViewModel = mainWindow.DataContext as MainWindowViewModel;
            if (mainWindowViewModel != null)
            {
                wpfNotifier.SetUpdateAction(mainWindowViewModel.UpdateSuggestedShortcuts);
            }
            else
            {
                Console.WriteLine("[App.xaml.cs] Erreur: MainWindowViewModel n'est pas du bon type ou n'est pas initialisé comme DataContext.");
            }

            // Déclenche l'entraînement initial ici.
            _ = Task.Run(async () =>
            {
                await PredictionEngine.TrainAlgorithmsAsync();
            });

            // Enfin, affiche la fenêtre principale de l'application.
            mainWindow.Show();
        }
        
    

        protected override async void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            if (_host != null)
            {
                await _host.StopAsync();
                _host.Dispose();
            }
        }
    }
}