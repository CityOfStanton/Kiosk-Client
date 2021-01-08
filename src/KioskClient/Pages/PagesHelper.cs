using KioskClient.PageArguments;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace KioskClient.Pages
{
    public class PagesHelper
    {
        public static void CommonKeyUp(object sender, Windows.UI.Core.KeyEventArgs args)
        {
            if (args.VirtualKey == Windows.System.VirtualKey.Home || args.VirtualKey == Windows.System.VirtualKey.Escape)
            {
                var rootFrame = Window.Current.Content as Frame;
                rootFrame.Navigate(typeof(MainPage), new MainPageArguments(showSetupInformation: true));
            }
        }

    }
}
