using KioskLibrary.Actions;
using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace KioskClient.Pages.Actions
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ImagePage : Page
    {
        public ImagePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var action = e.Parameter as ImageAction;
            imgDisplay.Source = new BitmapImage(new Uri(action.Path));
            imgDisplay.Stretch = action.Stretch;
        }
    }
}
