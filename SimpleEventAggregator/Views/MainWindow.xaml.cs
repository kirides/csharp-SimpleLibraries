using Kirides.Libs.Events;
using SimpleEventAggregator.ViewModels;
using System.Threading.Tasks;
using System.Windows;

namespace SimpleEventAggregator.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;
            var ctxt = System.Threading.SynchronizationContext.Current;
            Task.Run(() => App.EventAggregator.Subscribe<string>(OnStringEvent, ctxt));
        }

        private void OnStringEvent(string obj)
        {
            lbl.Content = obj;
        }
    }
}
