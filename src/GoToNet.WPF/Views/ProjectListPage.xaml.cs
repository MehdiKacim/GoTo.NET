// /src/GoToNet.WpfDemo/Views/ProjectListPage.xaml.cs
using GoToNet.WpfDemo.Models; // For Project model
using System.Windows.Controls;
// REMOVED: using System.Windows.Data;
// REMOVED: using System;
// REMOVED: using System.Globalization;
// REMOVED: using System.Windows;
// No converter definitions should be here now.

namespace GoToNet.WpfDemo.Views
{
    public partial class ProjectListPage : UserControl // Make sure it's UserControl, not Page
    {
        public ProjectListPage()
        {
            InitializeComponent();
        }

        // Event handler for ListBoxItem click to ensure selection is processed
        private void ListBoxItem_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var item = sender as ListBoxItem;
            if (item != null && item.DataContext is Project project)
            {
                if (DataContext is ViewModels.ProjectListPageViewModel viewModel)
                {
                    viewModel.SelectProjectCommand.Execute(project);
                }
            }
        }
    }
    // Converter definitions should NOT be here anymore. They are in WpfConverters.cs.
}