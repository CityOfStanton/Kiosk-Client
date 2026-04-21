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
    public class ViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Event that's raised when a property's value has changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Constructor
        /// </summary>
        public ViewModel() { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="lists"></param>
        protected ViewModel(List<string> lists)
        {
            Validators = lists;
        }

        /// <summary>
        /// Method that generally handles firing the <see cref="PropertyChanged" />
        /// </summary>
        /// <param name="propertyName"></param>
        public virtual void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (propertyName != null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

                foreach (var v in Validators)
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(v));
            }
        }

        public List<string> Validators { get; }
    }
}
