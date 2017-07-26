using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kirides.Libs.Mvvm
{
    public class ViewModelBase : ObservableObject
    {

        private string title;
        public virtual string Title
        {
            get => this.title;
            set => SetProperty(ref this.title, value);
        }

        private bool isBusy;
        public virtual bool IsBusy
        {
            get => this.isBusy;
            set => SetProperty(ref this.isBusy, value, val => RaisePropertyChanged(nameof(IsNotBusy)));
        }
        public virtual bool IsNotBusy
        {
            get => !IsBusy;
            set => SetProperty(ref this.isBusy, !value, val => RaisePropertyChanged(nameof(IsBusy)));
        }
    }
}