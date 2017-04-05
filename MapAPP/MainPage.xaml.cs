﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Services.Maps;
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
        private double sLatitude = 62.2416403;
        private double sLongtitude = 25.7474285;
        private double eLatitude = 62.236496;
        private double eLongtitude = 25.723306;
        // public object BackColor { get; set; }
        ObservableCollection<BussStops> listItems = new ObservableCollection<BussStops>();
        public MainPage()
        {
            this.InitializeComponent();
            GenerateStopsData();
            // Listaan itemit
            listItems.Add(new BussStops { StopName = "Forum", StopID = 6000, Latitude = 62.2416403, LonTitude = 25.7474285 });
            ListView itemListView = new ListView();
            StopsList.ItemsSource = listItems;
        }
        // Drawing route on map
        private async void ShowRouteOnMap()
        {
            // Start point
            BasicGeoposition startPoint = new BasicGeoposition() { Latitude = sLatitude, Longitude = sLongtitude };
        
            // End point
            BasicGeoposition endPoint = new BasicGeoposition() { Latitude = eLatitude, Longitude = eLongtitude };


            // Get the route between the points
            MapRouteFinderResult routeResult =
                  await MapRouteFinder.GetDrivingRouteAsync(
                  new Geopoint(startPoint),
                  new Geopoint(endPoint),
                  MapRouteOptimization.Time,
                  MapRouteRestrictions.None);

            if (routeResult.Status == MapRouteFinderStatus.Success)
            {
                // Initialize the route on map
                MapRouteView viewOfRoute = new MapRouteView(routeResult.Route);
                viewOfRoute.RouteColor = Colors.ForestGreen;
                viewOfRoute.OutlineColor = Colors.Black;
                JKLmap.Routes.Add(viewOfRoute);
            }
        }

        private void chooseBus_Click(object sender, RoutedEventArgs e)
        {
            if (!popupWindow.IsOpen) { popupWindow.IsOpen = true; }

        }

        // Kun kartta ladataan oletus GPS paikka on JKL koordinaatit !
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
        // BussStops luokan tyyppinen oliolista
        private List<MapAPP.BussStops> stops = new List<MapAPP.BussStops>();
        private async void GenerateStopsData()
        {
            try
            {
                // Avaa paikallinen kansio missä on asennettu tämä softa
                StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                // Linkkidatan sijainti 
                string PathToGPSFile = @"Linkkidata\stops.txt";
                StorageFile linkkitieto = await storageFolder.GetFileAsync(PathToGPSFile);
                // Luetaan tiedostosta kaikki rivit yksitellen
                IList<string> pys = await FileIO.ReadLinesAsync(linkkitieto);
                // Lista mihin lisätään parsittu tieto
                List<string[]> InfoList = new List<string[]>();
                // Parsitaan tieto ensin splittaamalla , kohdalta ja sitten korvataan "" tyhjällä.
                
                foreach(string splitti in pys) {
                    string s = splitti.Replace('"', ' ').Trim();
                    string[] parts = s.Split(',');
                    string stopname = parts[2];
                    int stopid = int.Parse(parts[0]);
                    double lon = double.Parse(parts[4]);
                    double lat;
                    // Tiedosto ottaa vain 1200 riviä ja heittää sitten exceptionia
                    if (double.TryParse(parts[3], out lat))
                    stops.Add(new BussStops { StopName = stopname, StopID = stopid, Latitude = lat, LonTitude = lon });
                    //loadingdata.Value += 1;
                    

                }
                SaveStopsInfo();
            }

            catch (Exception e)
            {
                Debug.Write("Virhe:", e.Message);
            }
        //stops.Add(new BussStops { StopName = "Forum", StopID = 6000, Latitude = 62.2416403, LonTitude = 25.7474285 });
        //stops.Add(new BussStops { StopName = "Jupari", StopID = 6000, Latitude = 62.236496, LonTitude = 25.723306 });


        }
        // Tallenetaan oliot-tiedostoon
        private async void SaveStopsInfo()
        {
            try
            {
                // open/create a file

                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile stopsfile = await storageFolder.CreateFileAsync("stops.dat", CreationCollisionOption.OpenIfExists);

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
                Stream stream = await storageFolder.OpenStreamForReadAsync("stops.dat");

                // is it empty
                if (stream == null) stops = new List<BussStops>();

                // read data
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<BussStops>));
                stops = (List<BussStops>)serializer.ReadObject(stream);             
                ShowStops();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Following exception has happend (reading): " + ex.ToString());
            }

        }
        private void ShowStops()
        {
            //stoptextblock.Text = "Stops:" + Environment.NewLine;
            foreach (BussStops stop in stops)
            {
                Debug.Write(stop.ToString());
              //  stoptextblock.Text += stop.StopID + " " + stop.StopName + + stop.LonTitude + stop.LonTitude + Environment.NewLine;
                Debug.Write(stop.Latitude + stop.LonTitude);

                BasicGeoposition snPosition = new BasicGeoposition() { Latitude = stop.Latitude, Longitude = stop.LonTitude };
                Geopoint snPoint = new Geopoint(snPosition);
                // Luodaan uusi stop 
                MapIcon stopoint = new MapIcon();
                stopoint.Location = snPoint;
                stopoint.NormalizedAnchorPoint = new Point(0.5, 1.0);
                stopoint.Title = stop.StopName;
                // ALLA VOIT VAIHTAA BUSSIN KUVAN
                stopoint.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/bus_stop_icon.png"));
                JKLmap.MapElements.Add(stopoint);

            }
        }

        private void stopsonmap_Click(object sender, RoutedEventArgs e)
        {
            ReadStops();
            ShowStops();
            
        } 

        private void EXIT_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Application.Current.Exit();
        }

        private void clearmap_Tapped(object sender, TappedRoutedEventArgs e)
        {
            //stops.Clear();
            JKLmap.MapElements.Clear();
            
            
         }
        

        private void fromTextBlock_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void destination_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!destinationWindow.IsOpen) { destinationWindow.IsOpen = true; }
        }

        private void closedestination_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (destinationWindow.IsOpen) { destinationWindow.IsOpen = false; }
        }

    }
    }

