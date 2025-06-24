// GoToNet.WpfDemo/ViewModels/ProjectListPageViewModel.cs
using GoToNet.Core.Models; // For NavigationEvent
using GoToNet.Core.Services; // For GoToPredictionEngine, UserMenuBuilder
using GoToNet.WpfDemo.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input; // For ICommand

namespace GoToNet.WpfDemo.ViewModels
{
    public class ProjectListPageViewModel : ViewModelBase
    {
        private readonly GoToPredictionEngine _predictionEngine;
        private readonly UserMenuBuilder _menuBuilder;

        public ObservableCollection<Project> Projects { get; } = new ObservableCollection<Project>();

        private Project? _selectedProject;
        public Project? SelectedProject
        {
            get => _selectedProject;
            set
            {
                SetProperty(ref _selectedProject, value);
                // Refresh commands' canExecute state when selection changes
                (SaveProjectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (DeleteProjectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                (AddCustomShortcutCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private string _newProjectName = string.Empty;
        public string NewProjectName
        {
            get => _newProjectName;
            set => SetProperty(ref _newProjectName, value);
        }

        private string _newProjectDescription = string.Empty;
        public string NewProjectDescription
        {
            get => _newProjectDescription;
            set => SetProperty(ref _newProjectDescription, value);
        }

        private string _customShortcutName = string.Empty;
        public string CustomShortcutName
        {
            get => _customShortcutName;
            set => SetProperty(ref _customShortcutName, value);
        }

        // Commands
        public ICommand AddProjectCommand { get; }
        public ICommand SaveProjectCommand { get; }
        public ICommand DeleteProjectCommand { get; }
        public ICommand SelectProjectCommand { get; } // Used for ListBox selection
        public ICommand AddCustomShortcutCommand { get; }

        public ProjectListPageViewModel(GoToPredictionEngine predictionEngine, UserMenuBuilder menuBuilder)
        {
            _predictionEngine = predictionEngine ?? throw new ArgumentNullException(nameof(predictionEngine));
            _menuBuilder = menuBuilder ?? throw new ArgumentNullException(nameof(menuBuilder));

            // Initialize Commands
            AddProjectCommand = new RelayCommand(async () => await OnAddProject(), () => !string.IsNullOrWhiteSpace(NewProjectName));
            SaveProjectCommand = new RelayCommand(async () => await OnSaveProject(), () => SelectedProject != null);
            DeleteProjectCommand = new RelayCommand(async () => await OnDeleteProject(), () => SelectedProject != null);
            SelectProjectCommand = new RelayCommand<Project>(p => SelectedProject = p); // Simple selection command
            AddCustomShortcutCommand = new RelayCommand(async () => await OnAddCustomShortcut(), () => !string.IsNullOrWhiteSpace(CustomShortcutName) && SelectedProject != null);

            LoadProjects(); // Load initial data
        }

        private void LoadProjects()
        {
            // Simulate loading existing projects
            Projects.Add(new Project { Id = 1, Name = "GoTo.NET Project", Description = "Develop the core library.", CreationDate = DateTime.Parse("2025-01-15") });
            Projects.Add(new Project { Id = 2, Name = "WPF UI Client", Description = "Build the WPF demo app.", CreationDate = DateTime.Parse("2025-02-01") });
            Projects.Add(new Project { Id = 3, Name = "API Backend", Description = "Create the REST API for GoTo.NET.", CreationDate = DateTime.Parse("2025-02-10") });
            // Select the first one by default if any exist
            SelectedProject = Projects.FirstOrDefault();
        }

        private async Task OnAddProject()
        {
            int newId = Projects.Any() ? Projects.Max(p => p.Id) + 1 : 1;
            var newProject = new Project { Id = newId, Name = NewProjectName, Description = NewProjectDescription, CreationDate = DateTime.Now };
            Projects.Add(newProject);
            SelectedProject = newProject;

            // Record this action for GoTo.NET
            await _predictionEngine.RecordNavigationAsync(new NavigationEvent
            {
                UserId = (App.Current.MainWindow.DataContext as MainWindowViewModel)?.CurrentUserId ?? "unknown_user",
                CurrentPageOrFeature = $"Project/{newProject.Name}/Add", // Specific event for adding a project
                PreviousPageOrFeature = "/projects", // Context
                Timestamp = DateTimeOffset.UtcNow
            });

            // Clear inputs
            NewProjectName = string.Empty;
            NewProjectDescription = string.Empty;
        }

        private async Task OnSaveProject()
        {
            if (SelectedProject == null) return;
            // Simulate saving (no actual persistence in this demo)
            Console.WriteLine($"Project saved: {SelectedProject.Name}");

            // Record this action for GoTo.NET
            await _predictionEngine.RecordNavigationAsync(new NavigationEvent
            {
                UserId = (App.Current.MainWindow.DataContext as MainWindowViewModel)?.CurrentUserId ?? "unknown_user",
                CurrentPageOrFeature = $"Project/{SelectedProject.Name}/Edit", // Specific event for editing a project
                PreviousPageOrFeature = "/projects", // Context
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        private async Task OnDeleteProject()
        {
            if (SelectedProject == null) return;
            string projectName = SelectedProject.Name;
            Projects.Remove(SelectedProject);
            SelectedProject = Projects.FirstOrDefault();

            // Record this action for GoTo.NET
            await _predictionEngine.RecordNavigationAsync(new NavigationEvent
            {
                UserId = (App.Current.MainWindow.DataContext as MainWindowViewModel)?.CurrentUserId ?? "unknown_user",
                CurrentPageOrFeature = $"Project/{projectName}/Delete", // Specific event for deleting a project
                PreviousPageOrFeature = "/projects", // Context
                Timestamp = DateTimeOffset.UtcNow
            });
        }

        private async Task OnAddCustomShortcut()
        {
            if (SelectedProject == null || string.IsNullOrWhiteSpace(CustomShortcutName)) return;

            string shortcutTarget = $"Project/{SelectedProject.Name}/{CustomShortcutName}";
            await _menuBuilder.AddItemToUserMenuAsync(
                (App.Current.MainWindow.DataContext as MainWindowViewModel)?.CurrentUserId ?? "unknown_user",
                shortcutTarget,
                0 // High priority
            );
            Console.WriteLine($"Custom shortcut added: {shortcutTarget}");
            CustomShortcutName = string.Empty; // Clear input
            
            // Trigger a refresh of main window's suggestions
            (App.Current.MainWindow.DataContext as MainWindowViewModel)?.RefreshSuggestionsCommand.Execute(null);
        }
    }
}