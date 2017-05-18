using Kirides.Events;
using SimpleEventAggregator.Events;
using SimpleEventAggregator.ViewModels;
using SimpleEventAggregator.Views;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleEventAggregator
{
    /// <summary>
    /// Interaktionslogik für "App.xaml"
    /// </summary>
    public partial class App : Application
    {
        //private EventAggregator eventAggregator;
        public static IEventAggregator EventAggregator;
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            //eventAggregator = new EventAggregator();
            EventAggregator = new EventAggregator();
            // Worlds best DependencyInjection.
            MainWindow = new MainWindow(
                new MainWindowViewModel(
                    EventAggregator));
            MainWindow.Show();

            // Publishing self-Instantiating Event-Class.
            EventAggregator.Publish<ApplicationStartedEvent>();

            while (true)
            {
                await Task.Delay(2000);
                await Task.Run(() =>
                EventAggregator.Publish<string>(DateTime.Now.ToString()));
            }
        }
    }
}
