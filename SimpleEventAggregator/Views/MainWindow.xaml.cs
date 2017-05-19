using Kirides.Libs.Events;
using SimpleEventAggregator.ViewModels;
using System.Windows;

namespace SimpleEventAggregator.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            App.EventAggregator.Subscribe<string>(OnStringEvent, ThreadOption.Inherited);
        }

        private void OnStringEvent(string obj)
        {
            lbl.Content = obj;
        }
    }
}
