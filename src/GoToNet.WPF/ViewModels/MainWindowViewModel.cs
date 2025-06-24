// GoToNet.WpfDemo/ViewModels/MainWindowViewModel.cs
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using GoToNet.Core.Services;
using GoToNet.WpfDemo.Services; // For IWpfNavigationService
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using GoToNet.Core.Services.ConfigurationProviders; // For ICommand

namespace GoToNet.WpfDemo.ViewModels
{
  public class MainWindowViewModel : ViewModelBase
    {
        private readonly GoToPredictionEngine _predictionEngine;
        private readonly UserMenuBuilder _menuBuilder;
        private readonly IAppNavigationCatalog _appNavigationCatalog;
        private readonly IDesignFlowRulesProvider _designFlowRulesProvider;
        private readonly YamlConfigProvider _yamlConfigProvider; 

        // La collection des suggestions est une propriété du ViewModel.
        public ObservableCollection<SuggestedItem> SuggestedShortcuts { get; } = new ObservableCollection<SuggestedItem>();

        // Collection pour les messages de statut de l'entraînement
        public ObservableCollection<string> TrainingLogMessages { get; } = new ObservableCollection<string>();

        // Propriétés pour l'UI de simulation et le statut d'entraînement
        private string _currentUserId = "user_demo_1"; // ID utilisateur par défaut
        public string CurrentUserId
        {
            get => _currentUserId;
            set => SetProperty(ref _currentUserId, value);
        }

        private string _currentPageContext = "Dashboard"; // Page actuellement affichée par l'utilisateur
        public string CurrentPageContext
        {
            get => _currentPageContext;
            set
            {
                SetProperty(ref _currentPageContext, value);
                UpdateDynamicContent(); // Met à jour le contenu principal
                RefreshSuggestions(); // Rafraîchit les suggestions pour le nouveau contexte
            }
        }

        private string _trainingStatus = "Initialisation du moteur GoTo.NET...";
        public string TrainingStatus
        {
            get => _trainingStatus;
            set => SetProperty(ref _trainingStatus, value);
        }
        private int _trainingProgress = 0; // Garde la progress bar pour des futures utilisations
        public int TrainingProgress
        {
            get => _trainingProgress;
            set => SetProperty(ref _trainingProgress, value);
        }
        private bool _isTrainingComplete = false;
        public bool IsTrainingComplete
        {
            get => _isTrainingComplete;
            set
            {
                SetProperty(ref _isTrainingComplete, value);
                (TriggerManualTrainingCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Met à jour l'état du bouton
            }
        }

        private bool _isTrainingRunning = false; // Pour désactiver le bouton pendant l'entraînement
        public bool IsTrainingRunning
        {
            get => _isTrainingRunning;
            set
            {
                SetProperty(ref _isTrainingRunning, value);
                (TriggerManualTrainingCommand as RelayCommand)?.RaiseCanExecuteChanged(); // Met à jour l'état du bouton
            }
        }
        
        // Collection pour les boutons de navigation principaux (basés sur YamlConfigProvider)
        public ObservableCollection<string> MainNavigationItems { get; } = new ObservableCollection<string>();

        // Propriété pour le titre de la page actuelle dans le contenu principal
        private string _mainContentTitle = "Bienvenue !";
        public string MainContentTitle
        {
            get => _mainContentTitle;
            set => SetProperty(ref _mainContentTitle, value);
        }

        // Collection pour les "sous-menus" dynamiques de la page actuelle
        public ObservableCollection<string> CurrentPageSubMenus { get; } = new ObservableCollection<string>();

        // ViewModel pour la gestion des projets (sera affiché directement si ProjectManagement est sélectionné)
        private ProjectListPageViewModel _projectManagementViewModel;
        public ProjectListPageViewModel ProjectManagementViewModel
        {
            get => _projectManagementViewModel;
            private set => SetProperty(ref _projectManagementViewModel, value);
        }

        // Commandes
        public ICommand GoToSuggestedItemCommand { get; }
        public ICommand RefreshSuggestionsCommand { get; }
        public ICommand TriggerManualTrainingCommand { get; }

        // Commande générique pour la navigation principale et les sous-menus
        public ICommand NavigateToMainPageCommand { get; } 
        public ICommand NavigateToSubMenuItemCommand { get; } 


        public MainWindowViewModel(
            GoToPredictionEngine predictionEngine,
            UserMenuBuilder menuBuilder,
            IAppNavigationCatalog appNavigationCatalog, // IAppNavigationCatalog (sera le YamlConfigProvider)
            IDesignFlowRulesProvider designFlowRulesProvider, // IDesignFlowRulesProvider (sera le YamlConfigProvider)
            YamlConfigProvider yamlConfigProvider, // Injecte le provider YAML directement
            ProjectListPageViewModel projectManagementViewModel) // Injected ProjectListPageViewModel
        {
            _predictionEngine = predictionEngine ?? throw new ArgumentNullException(nameof(predictionEngine));
            _menuBuilder = menuBuilder ?? throw new ArgumentNullException(nameof(menuBuilder));
            _appNavigationCatalog = appNavigationCatalog ?? throw new ArgumentNullException(nameof(appNavigationCatalog));
            _designFlowRulesProvider = designFlowRulesProvider ?? throw new ArgumentNullException(nameof(designFlowRulesProvider));
            _yamlConfigProvider = yamlConfigProvider ?? throw new ArgumentNullException(nameof(yamlConfigProvider)); // Initialisation
            ProjectManagementViewModel = projectManagementViewModel ?? throw new ArgumentNullException(nameof(projectManagementViewModel));

            SuggestedShortcuts = new ObservableCollection<SuggestedItem>();
            TrainingLogMessages = new ObservableCollection<string>();
            TrainingLogMessages.Add("GoTo.NET: Prêt à apprendre.");

            // Initialiser les commandes
            GoToSuggestedItemCommand = new RelayCommand<SuggestedItem>(async (item) => await OnGoToSuggestedItem(item));
            RefreshSuggestionsCommand = new RelayCommand(async () => await RefreshSuggestions());
            TriggerManualTrainingCommand = new RelayCommand(
                async () => await OnTriggerManualTraining(),
                () => _predictionEngine.TrainingMode == TrainingMode.Manual && !IsTrainingRunning
            );

            // Commandes génériques pour la navigation
            NavigateToMainPageCommand = new RelayCommand<string>(async (pageName) => await OnNavigateToPage(pageName));
            NavigateToSubMenuItemCommand = new RelayCommand<string>(async (subMenuItemName) => await OnNavigateToPage(subMenuItemName));

            // S'abonner aux événements d'entraînement
            _predictionEngine.AlgorithmsTrained += OnAlgorithmsTrained;
            _predictionEngine.TrainingProgressUpdated += OnTrainingProgressUpdated;

            // Initialisation des menus principaux (directement depuis YamlConfigProvider)
            LoadMainNavigationItems();

            // Définit la page par défaut au démarrage, ce qui déclenche UpdateDynamicContent() et RefreshSuggestions().
            CurrentPageContext = "Dashboard"; 
        }

        // Ajout de la méthode UpdateSuggestedShortcuts
        /// <summary>
        /// Met à jour la collection ObservableCollection des suggestions de raccourcis.
        /// Cette méthode est appelée par le INavigationNotifier.
        /// </summary>
        /// <param name="userId">L'ID de l'utilisateur concerné.</param>
        /// <param name="newSuggestions">La nouvelle liste de suggestions.</param>
        public void UpdateSuggestedShortcuts(string userId, IEnumerable<SuggestedItem> newSuggestions)
        {
            // IMPORTANT: S'assurer que la mise à jour se fait sur le thread UI
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                SuggestedShortcuts.Clear();
                foreach (var item in newSuggestions)
                {
                    SuggestedShortcuts.Add(item);
                }
                Console.WriteLine($"[MainWindowViewModel] Suggestions UI mises à jour pour {userId}. Total: {SuggestedShortcuts.Count}.");
            });
        }

        // Charge les noms des menus principaux directement depuis YamlConfigProvider
        private void LoadMainNavigationItems()
        {
            MainNavigationItems.Clear();
            foreach(var page in _yamlConfigProvider.GetMainNavigationItems())
            {
                MainNavigationItems.Add(page);
            }
        }
        
        // Met à jour le contenu de la zone principale (le titre et le sous-menu)
        private async void UpdateDynamicContent()
        {
            MainContentTitle = $"Page Actuelle : {_currentPageContext}";
            CurrentPageSubMenus.Clear();

            // Si c'est la page ProjectManagement, nous n'affichons pas de sous-menus ici
            // car le contrôle ProjectListPage prendra toute la place.
            if (_currentPageContext == "ProjectManagement")
            {
                // Pas de sous-menus dynamiques ici, le contenu CRUD est le focus.
                return;
            }

            // Simuler un chargement de sous-menus basé sur le DesignFlow
            var designRules = await _designFlowRulesProvider.GetDesignFlowRulesAsync();
            if (designRules.TryGetValue(_currentPageContext, out var subMenusForPage))
            {
                foreach(var item in subMenusForPage)
                {
                    CurrentPageSubMenus.Add(item);
                }
            }
            else
            {
                CurrentPageSubMenus.Add($"Aucun sous-menu prédéfini pour '{_currentPageContext}'.");
                CurrentPageSubMenus.Add("Ceci est une page de contenu générique.");
            }
        }

        // Gère la navigation dans l'application WPF et enregistre l'événement
        private async Task OnNavigateToPage(string? pageName)
        {
            if (string.IsNullOrEmpty(pageName)) return;

            // Enregistrer l'événement de navigation
            await _predictionEngine.RecordNavigationAsync(new NavigationEvent
            {
                UserId = CurrentUserId,
                CurrentPageOrFeature = pageName, // La page vers laquelle on va
                PreviousPageOrFeature = CurrentPageContext, // Page d'où l'utilisateur vient
                Timestamp = DateTimeOffset.UtcNow
            });

            CurrentPageContext = pageName; // Met à jour le contexte, ce qui déclenche UpdateDynamicContent() et RefreshSuggestions()
        }

        private void OnAlgorithmsTrained(object? sender, EventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsTrainingRunning = false;
                IsTrainingComplete = true;
                TrainingStatus = "Entraînement terminé (Modèle prêt !)";
                TrainingProgress = 100;
            });
        }

        private void OnTrainingProgressUpdated(object? sender, TrainingProgressEventArgs e)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsTrainingRunning = true;
                TrainingStatus = $"GoTo.NET: {e.AlgorithmName} - {e.CurrentStep}";
                TrainingProgress = e.ProgressPercentage;
                TrainingLogMessages.Insert(0, $"{DateTime.Now.ToShortTimeString()} | {e.AlgorithmName}: {e.CurrentStep} ({e.ProgressPercentage}%) - {e.Message}");
                if (TrainingLogMessages.Count > 10) TrainingLogMessages.RemoveAt(TrainingLogMessages.Count - 1);
            });
        }

        private async Task OnGoToSuggestedItem(SuggestedItem? item)
        {
            if (item == null) return;

            // GoTo.NET déclenche une action, qui est ici de changer le contexte de la page.
            _predictionEngine.PerformSuggestedNavigation(CurrentUserId, item.Name); // Appelle l'action du Notifier (simulée)

            // Mettre à jour le contexte du ViewModel (simule la navigation réelle)
            CurrentPageContext = item.Name; // Ce setter va déclencher l'enregistrement et le rafraîchissement des suggestions
        }

        private async Task RefreshSuggestions()
        {
            Console.WriteLine($"[MainWindowViewModel] Demande de suggestions pour l'utilisateur '{CurrentUserId}' dans le contexte '{CurrentPageContext}'...");
            await _predictionEngine.GetSuggestionsAsync(CurrentUserId, CurrentPageContext, 7);
        }

        private async Task OnTriggerManualTraining()
        {
            if (_predictionEngine.TrainingMode == TrainingMode.Manual && !IsTrainingRunning)
            {
                IsTrainingRunning = true;
                IsTrainingComplete = false;
                TrainingStatus = "Déclenchement manuel de l'entraînement...";
                TrainingProgress = 0;
                TrainingLogMessages.Insert(0, $"{DateTime.Now.ToShortTimeString()} | Déclenchement manuel de l'entraînement...");

                await _predictionEngine.TrainAlgorithmsAsync();
            }
        }
    }
}