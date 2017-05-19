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
        private ISubscriptionToken timeSub;
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

        int count;
        private ISubscriptionToken timeSub2;

        private void OnTimeChanged(string obj)
        {
            count++;
            Console.WriteLine(obj);
            if (count > 5)
            {
                eventAggregator.Unsubscribe<string>(o.String);
                timeSub.Dispose();
            }
        }

        private void OnApplicationStarted(ApplicationStartedEvent payload)
        {
            this.Username = payload.WindowsUsername;
        }
    }
}
