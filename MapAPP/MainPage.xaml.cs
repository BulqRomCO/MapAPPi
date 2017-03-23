﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Streams;
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
        private Windows.Storage.StorageFile sampleFile;
        // public object BackColor { get; set; }

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

        // OLETUS GPS PAIKKA KUN KARTTA LADATAAN
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

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            
            if (popupWindow.IsOpen) { popupWindow.IsOpen = false; }
            // do nothing in this function
        }

        SolidColorBrush onbusclick = new SolidColorBrush(Color.FromArgb(1,179, 255, 153));
        SolidColorBrush origcolor;

        void bus_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
               
            if ((SolidColorBrush)btn.Background == onbusclick)
            {
                btn.Background = origcolor;
            }
            else
            {
                origcolor = (SolidColorBrush)btn.Background;
                btn.Background = onbusclick;
            }

        }

        // TÄHÄN VILLEN OSUUS TIEDOSTOJEN LUKEMINEN KIRJOITTAMINEN
        private List<MapAPP.BussStops> stops = new List<MapAPP.BussStops>();

        private async void GenerateStopsData()
        {
            // KOKEILLAAN TIEDOSTON LUKUA ETTÄ SAADAAN PYSÄKKIEN GPS-TIEDOT

            try
            {
                //StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                string PathToGPSFile = @"Linkkidata\stops.txt";
                StorageFile sampleFile = await storageFolder.GetFileAsync(PathToGPSFile);
                // string text = await FileIO.ReadTextAsync(sampleFile);
                // Luetaan tiedostosta kaikki rivit yksitellen
                IList<string> pys = await FileIO.ReadLinesAsync(sampleFile);
                List<string> Testi = new List<string>();
                foreach(string splitti in pys) {
                    string[] doit = splitti.Split(',');
                    Testi.Add(doit[2]);
                }
                foreach(string s in Testi) {
                    Debug.Write(s);
                }
                
            }

            catch (Exception e)
            {
                Debug.Write("Virhe:", e.Message);
            }
         
            
        stops.Add(new BussStops { StopName = "Pupari", StopID = 6000, Latitude = 62.2416403, LonTitude = 25.7474285 });
        stops.Add(new BussStops { StopName = "Jupari", StopID = 6000, Latitude = 62.236496, LonTitude = 25.723306 });


        }
        // Tallenetaan oliot-tiedostoon
        private async void SaveStopsInfo()
        {
            try
            {
                // open/create a file

                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile stopsfile = await storageFolder.CreateFileAsync("stops.txt", CreationCollisionOption.OpenIfExists);

                // save employees to disk
                Stream stream = await stopsfile.OpenStreamForWriteAsync();
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
        // Save stops data napin funktio joka kutsuu SaveStopsInfo funktiota ja kirjoittaa pysäkkien tiedot tiedostoon stops.dat
     
        private async void ReadStops()
        {
            try
            {
                // find a file
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                Stream stream = await storageFolder.OpenStreamForReadAsync("stops.txt");

                // is it empty
                if (stream == null) stops = new List<BussStops>();

                // read data
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<BussStops>));
                stops = (List<BussStops>)serializer.ReadObject(stream);
                // älä näytä vielä pysäkkejä koska prossu 100%
                ShowStops();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Following exception has happend (reading): " + ex.ToString());
            }

        }
        private void ShowStops()
        {
            stoptextblock.Text = "Stops:" + Environment.NewLine;
            foreach (BussStops stop in stops)
            {
                Debug.Write(stop.ToString());
                stoptextblock.Text += stop.StopID + " " + stop.StopName + Environment.NewLine;
                Debug.Write(stop.Latitude + stop.LonTitude);
                BasicGeoposition snPosition = new BasicGeoposition() { Latitude = stop.Latitude, Longitude = stop.LonTitude };
                Geopoint snPoint = new Geopoint(snPosition);
                // Luodaan uusi stop 
                MapIcon stopoint = new MapIcon();
                stopoint.Location = snPoint;
                stopoint.NormalizedAnchorPoint = new Point(0.5, 1.0);
                stopoint.Title = stop.StopName;
                // ALLA VOIT VAIHTAA BUSSIN KUVAN
                // stopoint.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/StoreLogo.png"));
                JKLmap.MapElements.Add(stopoint);

            }
        }
        private void savestopdata_Click(object sender, RoutedEventArgs e)
        {
            GenerateStopsData();
            //ShowStops();
            
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void stopsonmap_Click(object sender, RoutedEventArgs e)
        {
            ShowStops();
        }
    }

}
