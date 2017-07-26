using Kirides.Libs.Events;
using SimpleEventAggregator.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleEventAggregator.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly IEventAggregator eventAggregator;
        private string username;
        int count;
        private IDisposable timeSub2;
        private IDisposable timeSub;
        LightObj o;

        public string Username
        {
            get => username;
            set => SetProperty(ref username, value);
        }
        public MainWindowViewModel(IEventAggregator eventAggregator)
        {
            this.eventAggregator = eventAggregator;

            Initialize();
        }

        private void Initialize()
        {
            o = new LightObj();

            eventAggregator.Subscribe<ApplicationStartedEvent>(OnApplicationStarted);
            timeSub = eventAggregator.Subscribe<string>(OnTimeChanged);
            timeSub2 = eventAggregator.Subscribe<string>(o.String);
        }


        private void OnTimeChanged(string obj)
        {
            count++;
            Console.WriteLine(obj);
            if (count > 2)
            {
                if (o != null)
                {
                    eventAggregator.Unsubscribe<string>(o.String);
                    o = null;
                }
                timeSub2.Dispose();
                GC.Collect(2);
                GC.WaitForPendingFinalizers();
                GC.Collect(2);
            }
        }

        private void OnApplicationStarted(ApplicationStartedEvent payload)
        {
            this.Username = payload.WindowsUsername;
        }
    }
}
