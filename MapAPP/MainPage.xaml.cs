using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

namespace MapAPP

{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Bussi bussi;
        public MainPage()
        {
            this.InitializeComponent();  
                     
            bussi = new Bussi
            {
                LocationX = BusCanvas.Width / 2,
                LocationY = BusCanvas.Height / 2
            };
            // Ajetaan seuraavat functiot heti ohjelman käynnistyttyä
            //--------------------------------------------------------
            GenerateStopsData();
            ReadRoutes();
            ReadStopTimesInfo();
            ReadTrips();  
            ReadFakeGpsData();
            ReadStopTimes();
            WaitDraw();

            //-------------------------------------------------------

        }
        // Olio-kokoelmat
        List<Trips>     trips = new     List<Trips>();
        List<Routes>    routes = new    List<Routes>();
        List<BussStops> stops = new     List<MapAPP.BussStops>();
        List<StopTimes> stoptimes = new List<StopTimes>();
        List<Homes>     homes = new     List<Homes>();
        List<FakeData>  fakedata = new  List<FakeData>();
        List<string>    routeto = new   List<string>();
        

        /// <summary>
        /// 2 GPS-pisteen välille piirrettävä lyhin reitti
        /// </summary>
        /// <param name="lista"></param>
        private async void ShowRouteOnMap(List<double> lista)
        {

            BasicGeoposition startPoint = new BasicGeoposition() { Latitude = lista[0], Longitude = lista[1] };
            BasicGeoposition endPoint = new BasicGeoposition() { Latitude = lista[2], Longitude = lista[3] };


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
                routeto.Clear();
            }
        }
        
        private void chooseBus_Click(object sender, RoutedEventArgs e)
        {
            if (!popupWindow.IsOpen) { popupWindow.IsOpen = true; }
            if (destinationWindow.IsOpen) { destinationWindow.IsOpen = false; }
            if (popstops.IsOpen) { popstops.IsOpen = false; }

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
        SolidColorBrush onbusclick = new SolidColorBrush(Color.FromArgb(1, 179, 255, 153));
        SolidColorBrush origcolor;
        void bus_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton btn = sender as AppBarButton;

            if ((SolidColorBrush)btn.Background == onbusclick)
            {
                btn.Background = origcolor;
            }
            else
            {
                origcolor = (SolidColorBrush)btn.Background;
                btn.Background = onbusclick;

            }
            string buttonlabel = btn.Label.ToString();
            if (buttonlabel =="Linja 20") WaitDraw();
            ReadLineData(buttonlabel);

        }
        // BussStops luokan tyyppinen oliolista
        public object ContactSampleDataSource { get; private set; }
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

                foreach (string splitti in pys)
                {
                    string s = splitti.Replace('"', ' ').Trim();
                    string[] parts = s.Split(',');
                    string stopname = parts[2];
                    int stopid = int.Parse(parts[0]);
                    double lon = double.Parse(parts[4]);
                    double lat;
                    // Tiedosto ottaa vain 1200 riviä ja heittää sitten exceptionia
                    if (double.TryParse(parts[3], out lat))
                        stops.Add(new BussStops { StopName = stopname, StopID = stopid, Latitude = lat, LonTitude = lon });
                }
                SaveStopsInfo();
            }
            catch (Exception e)
            {
                Debug.Write("Virhe:", e.Message);
            }
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
            foreach (BussStops stop in stops)
            {
                BasicGeoposition snPosition = new BasicGeoposition() { Latitude = stop.Latitude, Longitude = stop.LonTitude };
                Geopoint snPoint = new Geopoint(snPosition);
                MapIcon stopoint = new MapIcon();
                stopoint.Location = snPoint;
                stopoint.NormalizedAnchorPoint = new Point(0.5, 1.0);
                stopoint.Title = stop.StopName;
                if (stop.StopName == " Pupuhuhta ") stopoint.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/home_stop_icon.png"));
                else stopoint.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/bus_stop_icon.png"));
                JKLmap.MapElements.Add(stopoint);
            }
        }
        //paska
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
            JKLmap.MapElements.Clear();
            JKLmap.Routes.Clear();
        }
        private void fromTextBlock_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void destination_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!destinationWindow.IsOpen) { destinationWindow.IsOpen = true; }
            if (popstops.IsOpen) { popstops.IsOpen = false; }
            if (popupWindow.IsOpen) { popupWindow.IsOpen = false; }
        }
        private void closedestination_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (destinationWindow.IsOpen) { destinationWindow.IsOpen = false; }
        }
        private void popstopsbutton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!popstops.IsOpen) { popstops.IsOpen = true; }
            if (popupWindow.IsOpen) { popupWindow.IsOpen = false; }
            if (destinationWindow.IsOpen) { destinationWindow.IsOpen = false; }

        }
        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
        }
        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                Searchbox.Text = args.ChosenSuggestion.ToString();
                ShowPoint(args.ChosenSuggestion.ToString());
                routeto.Add(args.ChosenSuggestion.ToString());
                
            }
            else
            {
                Searchbox.Text = sender.Text;
            }
        }
        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            List<string> names = new List<string>();
            List<string> suggestion = new List<string>();
            foreach (BussStops name in stops) { names.Add(name.StopName); }
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (sender.Text.Length > 1)
                {
                    suggestion = names.Where(x => x.Contains(sender.Text)).ToList();
                    sender.ItemsSource = suggestion;
                }
                else sender.ItemsSource = new string[] { "Ei löydy" };
            }
        }
        private void DestinationSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {

        }
        private void DestinationSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion != null)
            {
                DestinationSuggestBox.Text = args.ChosenSuggestion.ToString();
                ShowPoint(args.ChosenSuggestion.ToString());
                routeto.Add(args.ChosenSuggestion.ToString());   
            }
            else
            {
                DestinationSuggestBox.Text = sender.Text;
            }
        }
        private void DestinationSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            List<string> names = new List<string>();
            List<string> suggestion = new List<string>();
            foreach (BussStops name in stops) { names.Add(name.StopName); }
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                if (sender.Text.Length > 1)
                {
                    suggestion = names.Where(x => x.Contains(sender.Text)).ToList();
                    sender.ItemsSource = suggestion;
                }
                else
                {
                    sender.ItemsSource = new string[] { "Ei löydy" };
                }
            }
        }
        private void showButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            List<double> route = new List<double>();
            foreach (BussStops stop in stops)
            {
                foreach (string n in routeto)
                {
                    if (n == stop.StopName)
                    {
                        route.Add(stop.Latitude);
                        route.Add(stop.LonTitude);
                    }
                }
            }
            ShowRouteOnMap(route);
            route.Clear();
        }
        public void ShowPoint(string name)
        {
            foreach (BussStops stop in stops)
            {
                if (name == stop.StopName)
                {
                    BasicGeoposition snPosition = new BasicGeoposition() { Latitude = stop.Latitude, Longitude = stop.LonTitude };
                    Geopoint snPoint = new Geopoint(snPosition);
                    // Luodaan uusi stop 
                    MapIcon stopoint = new MapIcon();
                    stopoint.Location = snPoint;
                    stopoint.NormalizedAnchorPoint = new Point(0.5, 1.0);
                    stopoint.Title = stop.StopName;      
                    stopoint.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/bus_stop_icon.png"));
                    JKLmap.MapElements.Add(stopoint);
                    display3DLocation(stop.Latitude, stop.LonTitude, 2);
                }
            }
        }
        List<double> showroutebyname = new List<double>();
        private async void ReadLineData(string label)
        {
            showroutebyname.Clear();
            StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            Debug.Write("Bussi " + label.ToString());
            string PathToGPSFile = @"Linkit\" + label + ".txt";
            StorageFile linkkitieto = await storageFolder.GetFileAsync(PathToGPSFile);
            IList<string> linjadata = await FileIO.ReadLinesAsync(linkkitieto);
            foreach (string n in linjadata)
            {
                foreach (BussStops stop in stops)
                {
                    if (n == stop.StopName)
                    {

                        BasicGeoposition snPosition = new BasicGeoposition() { Latitude = stop.Latitude, Longitude = stop.LonTitude };
                        // Näytä linjan perusteella reitit 
                        Geopoint snPoint = new Geopoint(snPosition);
                        // Luodaan uusi stop 
                        MapIcon stopoint = new MapIcon();
                        stopoint.Location = snPoint;
                        stopoint.NormalizedAnchorPoint = new Point(0.5, 1.0);
                        stopoint.Title = stop.StopName;
                        stopoint.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/home_stop_icon.png"));
                        JKLmap.MapElements.Add(stopoint);
                        showroutebyname.Add(stop.Latitude);
                        showroutebyname.Add(stop.LonTitude);
                    }
                }
            }
      
        }
        private async void ShowRoutesLines(List<double> lista)
        {
            Debug.Write(lista[0] + " " + lista[1] + " " + lista[2] + " " + lista[3]);
            BasicGeoposition startPoint = new BasicGeoposition() { Latitude = lista[0], Longitude = lista[1] };
            BasicGeoposition endPoint = new BasicGeoposition() { Latitude = lista[2], Longitude = lista[3] };
            MapRouteFinderResult routeResult =
                      await MapRouteFinder.GetDrivingRouteAsync(
                      new Geopoint(startPoint),
                      new Geopoint(endPoint),
                      MapRouteOptimization.Time,
                      MapRouteRestrictions.None);

            if (routeResult.Status == MapRouteFinderStatus.Success)
            {
                Debug.Write("Added");
                MapRouteView viewOfRoute = new MapRouteView(routeResult.Route);
                viewOfRoute.RouteColor = Colors.ForestGreen;
                viewOfRoute.OutlineColor = Colors.Black;
                JKLmap.Routes.Add(viewOfRoute);
            }

        }
        private async void ReadStopTimes()
        {
            try
            {
                // Avaa paikallinen kansio missä on asennettu tämä softa
                StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                string PathToGPSFile = @"Linkkidata\stop_times.txt";
                StorageFile linkkitieto = await storageFolder.GetFileAsync(PathToGPSFile);
                // Luetaan tiedostosta kaikki rivit yksitellen
                IList<string> pys = await FileIO.ReadLinesAsync(linkkitieto);
                // Lista mihin lisätään parsittu tieto
                List<string[]> InfoList = new List<string[]>();
                // Parsitaan tieto ensin splittaamalla , kohdalta ja sitten korvataan "" tyhjällä.
                // "2827af93-c63b-4433-90c8-9b5893001884","06:05:00","06:05:00","207773","26",""
                foreach (string splitti in pys)
                {
                    string s = splitti.Replace('"', ' ').Trim();
                    string[] parts = s.Split(',');
                    string arrivetime = parts[1];
                    string tripid = parts[0];
                    int stopid = int.Parse(parts[3]);
                    int sequence = int.Parse(parts[4]);
                    stoptimes.Add(new StopTimes { StopTime = arrivetime, StopID = stopid, Sequence = sequence, TripID = tripid });
                    // Debug.Write(stopid);
                }
                Debug.Write("All parsed");
                Debug.Write(stoptimes.Count);
                SaveStopTimesInfo();
            }
            catch (Exception e)
            {
                Debug.Write("Virhe:", e.Message);
            }
            
        }
        private async void SaveStopTimesInfo()
        {
            try
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile stopsfile = await storageFolder.CreateFileAsync("stoptimes.dat", CreationCollisionOption.ReplaceExisting);
                // save employees to disk
                Stream stream = await stopsfile.OpenStreamForWriteAsync();
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<StopTimes>));
                serializer.WriteObject(stream, stoptimes);
                await stream.FlushAsync();
                stream.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Following exception has happend (writing): " + ex.ToString());
            }
        }
        private async void ReadStopTimesInfo()
        {
            try
            {
                // find a file
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                Stream stream = await storageFolder.OpenStreamForReadAsync("stoptimes.dat");

                // is it empty
                if (stream == null) stops = new List<BussStops>();

                // read data
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<StopTimes>));
                stoptimes = (List<StopTimes>)serializer.ReadObject(stream);            
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Following exception has happend (reading): " + ex.ToString());
            }

        }      
        private async void ReadTrips()
        {
            try
            {
                // Avaa paikallinen kansio missä on asennettu tämä softa
                StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                string PathToGPSFile = @"Linkkidata\trips.txt";
                StorageFile linkkitieto = await storageFolder.GetFileAsync(PathToGPSFile);
                IList<string> pys = await FileIO.ReadLinesAsync(linkkitieto);
                List<string[]> InfoList = new List<string[]>();
                
                foreach (string splitti in pys)
                {
                    string s = splitti.Replace('"', ' ').Trim();
                    string[] parts = s.Split(',');
                    string tripid= parts[2];
                    int routeid = int.Parse(parts[0]);
                    string serviceid = parts[1];
                    trips.Add(new Trips { TripID = tripid, RouteID = routeid, ServiceID = serviceid});   

                }
                Debug.Write("All parsed");
                Debug.Write(trips.Count);
                SaveTripsInfo();
            }
            catch (Exception e)
            {
                Debug.Write("Virhe:", e.Message);
            }

        }
        private async void SaveTripsInfo()
        {
            try
            {


                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile stopsfile = await storageFolder.CreateFileAsync("trips.dat", CreationCollisionOption.ReplaceExisting);

                // save employees to disk
                Stream stream = await stopsfile.OpenStreamForWriteAsync();
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<Trips>));
                serializer.WriteObject(stream, trips);
                await stream.FlushAsync();
                stream.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Following exception has happend (writing): " + ex.ToString());
            }
        }
        private async void ReadTripsInfo()
        {
            try
            {
                // find a file
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                Stream stream = await storageFolder.OpenStreamForReadAsync("trips.dat");

                // is it empty
                if (stream == null) trips = new List<Trips>();

                // read data
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<Trips>));
                trips = (List<Trips>)serializer.ReadObject(stream);

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Following exception has happend (reading): " + ex.ToString());
            }

        }
        private async void ReadRoutes()
        {
            try
            {
                // Avaa paikallinen kansio missä on asennettu tämä softa
                StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                string PathToGPSFile = @"Linkkidata\routes.txt";
                StorageFile linkkitieto = await storageFolder.GetFileAsync(PathToGPSFile);
                IList<string> pys = await FileIO.ReadLinesAsync(linkkitieto);
                List<string[]> InfoList = new List<string[]>();

                foreach (string splitti in pys)
                {
                    string s = splitti.Replace('"', ' ').Trim();
                    string[] parts = s.Split(',');
                    int routeid = int.Parse(parts[0]);
                    int agencyid = int.Parse(parts[1]);
                    string routeshortname = parts[2];
                    string routelongname = parts[3];    
                    routes.Add(new Routes { RouteID = routeid, AgencyID = agencyid, RouteShortName = routeshortname, RouteLongName = routelongname });
                   
                }
                Debug.Write("All parsed");
                Debug.Write(routes.Count);
                SaveRoutesInfo();
            }
            catch (Exception e)
            {
                Debug.Write("Virhe:", e.Message);
            }

        }
        private async void SaveRoutesInfo()
        {
            try
            {


                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile stopsfile = await storageFolder.CreateFileAsync("routes.dat", CreationCollisionOption.ReplaceExisting);

                // save employees to disk
                Stream stream = await stopsfile.OpenStreamForWriteAsync();
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<Routes>));
                serializer.WriteObject(stream, routes);
                await stream.FlushAsync();
                stream.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Following exception has happend (writing): " + ex.ToString());
            }
        }
        private async void ReadRoutesInfo()
        {
            try
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                Stream stream = await storageFolder.OpenStreamForReadAsync("routes.dat");
                if (stream == null) routes = new List<Routes>();
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<Trips>));
                routes = (List<Routes>)serializer.ReadObject(stream);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Following exception has happend (reading): " + ex.ToString());
            }

        }
        public async void ReadFakeGpsData()
        {
            try
            {
                StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                string PathToGPSFile = @"FakeData\linja20.txt";
                StorageFile linkkitieto = await storageFolder.GetFileAsync(PathToGPSFile);
                IList<string> data = await FileIO.ReadLinesAsync(linkkitieto);
               
                foreach(string gps in data)
                {
                    string[] parts = gps.Split(',');
                    fakedata.Add(new FakeData { lat = double.Parse(parts[0]), lon = double.Parse(parts[1])});
                }

            }
            catch (Exception e)
            {
                Debug.Write("ReadFakeGpsdata error" + e.Message);
            }

        }
        string image = "bussi.png";
        IList<MapElement> fakedatalist = new List<MapElement>();
        int i = 0;
        public void DrawFakeGpsRoute()
        {
           try
            {      
               JKLmap.MapElements.RemoveAt(0);
               
            }
            catch (Exception e)
            {
                Debug.Write(e.Message);
            }
            finally
            {
                double latitude = fakedata[i].lat;
                double longtitude = fakedata[i].lon;
                BasicGeoposition snPosition = new BasicGeoposition() { Latitude = latitude, Longitude = longtitude };
                Geopoint snPoint = new Geopoint(snPosition);
                MapIcon stopoint = new MapIcon();
                stopoint.Location = snPoint;
                stopoint.NormalizedAnchorPoint = new Point(0.5, 1.0);
                stopoint.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/bussi.png"));
                stopoint.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/"+image));
               // JKLmap.MapElements.Add(stopoint);
                JKLmap.MapElements.Insert(0, stopoint);
                i++;
            }   
        }
        public async Task WaitDraw()
        {    
            while (true)
            {
                DrawFakeGpsRoute();
                await Task.Delay(5000);
            }
        }

        private async void display3DLocation(double latitude = 62.2417, double longtitude = 25.7473, int style = 1)
        {
            if (JKLmap.Is3DSupported)
            {
                switch (style)
                {
                    case 1:
                        JKLmap.Style = MapStyle.Terrain;
                        break;
                    case 2:
                        JKLmap.Style = MapStyle.Aerial3DWithRoads;
                        break;
                }
                BasicGeoposition hwGeoposition = new BasicGeoposition() { Latitude = latitude, Longitude = longtitude };
                Geopoint hwPoint = new Geopoint(hwGeoposition);
                MapScene hwScene = MapScene.CreateFromLocationAndRadius(hwPoint,80, /* Metrit */0, /* Pohjois */ 60 /* Asteluku */);
                await JKLmap.TrySetSceneAsync(hwScene, MapAnimationKind.Bow);
            }
            else
            {
                ContentDialog viewNotSupportedDialog = new ContentDialog()
                {
                    Title = "3D ei ole tuettu",
                    Content = "Ei tukea 3D näkymälle",
                    PrimaryButtonText = "OK"
                };
                await viewNotSupportedDialog.ShowAsync();
            }
        }

        private void Show3DRoute_Click(object sender, RoutedEventArgs e)
        {
            display3DLocation();
        }
    }
    }

