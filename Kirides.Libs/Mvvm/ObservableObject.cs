using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace Kirides.Libs.Mvvm
{
    public class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public virtual void RaisePropertyChanged([CallerMemberName]string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public virtual bool SetProperty<T>(ref T storage, T value, Action<T> onChange = null, [CallerMemberName]string propertyName ="")
        {
            if (EqualityComparer<T>.Default.Equals(storage, value))
                return false;

            storage = value;
            onChange?.Invoke(value);
            RaisePropertyChanged(propertyName);
            return true;
        }
    }
}
