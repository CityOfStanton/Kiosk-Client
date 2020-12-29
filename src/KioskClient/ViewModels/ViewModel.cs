using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KioskClient.ViewModels
{
    public abstract class ViewModel : INotifyPropertyChanged
    {
        public ViewModel() { }

        protected ViewModel(List<string> lists)
        {
            Validators = lists;
        }

        private List<string> Validators { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            foreach(var v in Validators)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(v));
        }
    }
}
