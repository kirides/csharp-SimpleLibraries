using System;

namespace SimpleEventAggregator.Events
{
    public class ApplicationStartedEvent
    {
        public DateTime StartedDate { get; private set; }
        public string WindowsUsername { get; private set; }

        public ApplicationStartedEvent()
        {
            this.StartedDate = DateTime.Now;
            var currentWindowsIdentity = System.Security.Principal.WindowsIdentity.GetCurrent();
            if (currentWindowsIdentity != null)
                this.WindowsUsername = currentWindowsIdentity.Name;
            else
                this.WindowsUsername = $@"{Environment.UserDomainName}\{Environment.UserName}";
        }
    }

}
