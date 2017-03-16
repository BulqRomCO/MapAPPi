﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
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

        public object BackColor { get; private set; }

        public MainPage()
        {
            this.InitializeComponent();
        }


        private void chooseBus_Click(object sender, RoutedEventArgs e)
        {
            if (!popupWindow.IsOpen) { popupWindow.IsOpen = true; }

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

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (popupWindow.IsOpen) { popupWindow.IsOpen = false; }
            // show elements on map here
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            if (popupWindow.IsOpen) { popupWindow.IsOpen = false; }
            // do nothing in this function
        }




        void bus_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            btn.Background = new SolidColorBrush(Color.FromArgb(1,179, 255, 153));

            }


        
        // LUE PYSÄKKIEN TIEDOT LINKKIDATASTA
        
        // LUO TIETOJEN POHJALTA OLIOT
        private async void SaveStopsInfo()
        {
            try
            {
                // open/create a file
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile employeesFile = await storageFolder.CreateFileAsync("stops.dat", CreationCollisionOption.OpenIfExists);

                // save employees to disk
                Stream stream = await employeesFile.OpenStreamForWriteAsync();
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<BussStops>));
                serializer.WriteObject(stream, stops);
                await stream.FlushAsync();
                stream.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Following exception has happend (writing): " + ex.ToString());
            }
        }
        // TALLENNA OLIODATA TIEDOSTOON

    }
}
