/*
 * Copyright 2021
 * City of Stanton
 * Stanton, Kentucky
 * www.stantonky.gov
 * github.com/CityOfStanton
 */

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KioskClient.Dialogs
{
    public sealed partial class RunTutorial : ContentDialog
    {
        public bool RunTutorialOnClose { get; set; }
        public bool DoNotShowThisAgain { get; set; } = true;

        public RunTutorial()
        {
            this.InitializeComponent();
        }

        private void ContentDialog_RunTutorial_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Escape)
                Hide();
        }

        private void Button_Yes_Click(object sender, RoutedEventArgs e)
        {
            RunTutorialOnClose = true;
            Hide();
        }

        private void Button_Close_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
