// /src/GoToNet.WpfDemo/Services/MyWpfNavigationNotifier.cs
using GoToNet.Core.Interfaces;
using GoToNet.Core.Models;
using System.Collections.Generic;
using System; // For Action delegate

namespace GoToNet.WpfDemo.Services
{
    public class MyWpfNavigationNotifier : INavigationNotifier
    {
        // Un délégué pour appeler une méthode sur le ViewModel pour la mise à jour.
        private Action<string, IEnumerable<SuggestedItem>>? _updateAction;

        // Constructeur qui ne prend PLUS la ObservableCollection.
        public MyWpfNavigationNotifier()
        {
            // Les dépendances sont injectées par le DI si besoin.
            // La méthode SetUpdateAction sera appelée par App.xaml.cs pour lier l'action.
        }

        /// <summary>
        /// Définit l'action (méthode du ViewModel) que le notifier appellera pour mettre à jour l'UI.
        /// </summary>
        /// <param name="updateAction">L'action à déclencher avec les suggestions.</param>
        public void SetUpdateAction(Action<string, IEnumerable<SuggestedItem>> updateAction)
        {
            _updateAction = updateAction;
            Console.WriteLine("[MyWpfNavigationNotifier] Action de mise à jour depuis le ViewModel définie.");
        }

        public void UpdateSuggestionsDisplay(string userId, IEnumerable<SuggestedItem> suggestedItems)
        {
            // Appelle l'action fournie par le ViewModel pour mettre à jour la collection.
            // La logique de Dispatcher.Invoke() est maintenant dans la méthode du ViewModel.
            _updateAction?.Invoke(userId, suggestedItems);
        }
    }
}