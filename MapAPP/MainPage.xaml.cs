using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace MapAPP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void showButton_Click(object sender, RoutedEventArgs e)
        {
            string PopupName = "popUp";
            string PopupSettings = "height=900,width=1000,top=100,left=100,scrollbars=no,resizable=no,toolbar=no,menubar=no,location=no,status=yes";

        }

        private void chooseBus_Click(object sender, RoutedEventArgs e)
        {


        }
        private void AddMapIcon()
        {
            MapIcon forum = new MapIcon();
            forum.Location = new Geopoint(new BasicGeoposition()
            {

                Latitude = 62.2416403,
                Longitude = 25.7474285
            });
            forum.NormalizedAnchorPoint = new Point(0.5, 1.0);
            forum.Title = "Forum";
            JKLmap.MapElements.Add(forum);
        }


        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
        JKLmap.Center =
            new Geopoint(new BasicGeoposition()
            {
                Latitude = 62.2417,
                Longitude = 25.7473
            });
        JKLmap.ZoomLevel = 13;
        JKLmap.LandmarksVisible = true;
        }
}
}
