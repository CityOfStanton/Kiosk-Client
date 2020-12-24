using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace KioskClient.Actions
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SlideshowPage : Page
    {
        public SlideshowPage()
        {
            this.InitializeComponent();
        }

        private void Page_KeyUp(object sender, KeyRoutedEventArgs e) => Common.CommonKeyUp(sender, e);
    }
}
