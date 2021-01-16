/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KioskLibrary.ViewModels
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
