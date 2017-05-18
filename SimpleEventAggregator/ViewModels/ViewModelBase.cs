using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SimpleEventAggregator.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged([CallerMemberName]string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public bool SetProperty<T>(ref T property, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(property, value))
                return false;
            property = value;
            RaisePropertyChanged(propertyName);
            return true;
        }
        public bool SetProperty<T>(ref T property, T value, Action callback, [CallerMemberName]string propertyName = null)
        {
            if (SetProperty(ref property, value, propertyName))
            {
                callback?.Invoke();
                return true;
            }
            return false;
        }
    }
}
