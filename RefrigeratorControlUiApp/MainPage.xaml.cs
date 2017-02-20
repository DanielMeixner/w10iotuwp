using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RefrigeratorControlUiApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("MainPage.Constructor");
            this.InitializeComponent();
        }

        private void btnTemp_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Debug.WriteLine("Tapped");
            tbTemp.Text = "Temp: -16";
            Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("btn.TempClicked");
        }

        private void btnVacation_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Debug.WriteLine("Tapped");
            Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("btn.VacationClicked");
        }

        private void btnFreeze_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("btn.FreezeClicked");
            Debug.WriteLine("Tapped");
        }

        private void btnLock_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("btn.LockClicked");
            Debug.WriteLine("Tapped");
        }

        private void tbVersion_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Microsoft.HockeyApp.HockeyClient.Current.TrackEvent("Version Tabbed");
            Debug.WriteLine("Tapped");
        }
    }
}
