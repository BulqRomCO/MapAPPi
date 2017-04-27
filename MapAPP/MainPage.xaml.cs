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
            GenerateStopsData();
            ReadFakeGpsData();
            
        }
        // Olio-kokoelmat
        List<BussStops> stops = new     List<MapAPP.BussStops>();
        List<StopTimes> stoptimes = new List<StopTimes>();
        List<FakeData>  fakedata = new  List<FakeData>();
        List<string>    routeto = new   List<string>();
        /// <summary>
        /// 2 GPS-pisteen välille piirrettävä lyhin reitti
        /// Ottaa double tyyppisen lista joka sisältää 2 GPS tietoa, lon ja lat tiedot.
        /// </summary>
        /// <param name="lista"></param>
        private async void ShowRouteOnMap(List<double> lista)
        {
            
            try
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
                arrivaltime.Text = "";
            }
            catch (Exception e)
            {

                arrivaltime.Text = "Add another point to show route!!";
            }
        
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

        /// <summary>
        /// Funktio ottaa parametreina objektin joka on näppäin. Funktiossa toteutetaan väliaikainen muuttuja nimeltä btn.
        /// Näppäimen sisällä oleva 'label' tuodaan buttonlabel nimiseen merkkinoon.
        /// Buttonlabel muuttujan sisältö parsitaan numeroista ja korvataan '' tyhjällä s merkkijono muuttujaan.
        /// s muuttujaa käytetään if lausekkeissa. Linja näppäimessä käytetään s ja button label muuttujia, tämä ei olisi tarpeellista jos meillä olisi oikeata gps dataa.
        /// Funktio kaikikille näppäimille mitä ohjelmassa käytetään:
        /// CHOOSE BUS näppäin avaa popup näkymän mistä voi valita bussin tai busseja
        /// Linja 'numero' näppäin tuo kartalle näkyviin bussin, sekä linjan kyseiset pysäkit punaisella. Ainoastaan linja 20 on feikki gps dataa demoamisen vuoksi
        /// DESTINATION näppäin avaa popup näkymän mistä voi valita reitin kahden pysäkin väliltä
        /// SHOW STOPS näppäin tuo kartalle näkyviin kaikki Jyväskylän liikenteen bussipysäkit
        /// D View näppäim vaihtaa kartan näkymän 3D:hen karttaa, jossa voidaan tarkastella näkymää x,y,z akseleilla. Näppäintä painaessa uudestaan näkymä vaihtuu takaisin 2D näkymään
        /// CLEAR MAP näppäin poistaa kartalla olevat elementit(pysäkit tai reitit)
        /// Show näppäin on DESTINATION näppäimen popup ikkunan takana, jota painaessa voidaan piirtää kartalle pysäkkien välinen reitti
        /// OK tai Cancel näppäin sulkee CHOOSE BUS ikkunan, tämä voidaan myös toteuttaa painamalla SHOW STOPS tai DESTINATIOM tai CLEAR MAP näppäintä
        /// Close näppäin tekee saman asian kuin OK tai Cancel, mutta DESTINATION ikkunalle.
        /// EXIT näppäin sulkee sovelluksen kokonaan.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void bus_Click(object sender, RoutedEventArgs e)
        {
            AppBarButton btn = sender as AppBarButton;
            string buttonlabel = btn.Label.ToString();
            string s = Regex.Replace(buttonlabel, "[0-9]", "").Trim();
            Debug.WriteLine(s.ToString());
            if( s== "CHOOSE BUS") {
                if (!popupWindow.IsOpen) { popupWindow.IsOpen = true; }
                if (destinationWindow.IsOpen) { destinationWindow.IsOpen = false; }
            }
            else if (s == "Linja")
            {
                if (buttonlabel == "Linja 20")
                {
                    WaitDraw(buttonlabel);
                    ReadLineData(buttonlabel);
                }
                else ReadLineData(buttonlabel);
            }
            else if (s == "DESTINATION")
            {
                if (!destinationWindow.IsOpen) { destinationWindow.IsOpen = true; }
                if (popupWindow.IsOpen) { popupWindow.IsOpen = false; }
            }
            else if (s == "SHOW STOPS")
            {
                if (popupWindow.IsOpen) { popupWindow.IsOpen = false; }
                if (destinationWindow.IsOpen) { destinationWindow.IsOpen = false; }
                ReadStops();
                ShowStops();
            }
            else if (s == "D View") display3DLocation();
            else if (s == "CLEAR MAP")
            {
                JKLmap.MapElements.Clear();
                JKLmap.Routes.Clear();
            }
            else if (s == "Show")
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
            else if (s == "OK" || s== "Cancel" ) { if (popupWindow.IsOpen) { popupWindow.IsOpen = false; } }
            else if (s == "Close") { if (destinationWindow.IsOpen) { destinationWindow.IsOpen = false; } }
            else if (s == "EXIT") Application.Current.Exit();
            
        }
        /// <summary>
        /// Funktion joka avaa linkkidatan stops tiedoston ja parsii siitä osan tiedoista ja lisää olioon.
        /// </summary>
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
                    if (double.TryParse(parts[3], out lat))
                        stops.Add(new BussStops { StopName = stopname, StopID = stopid, Latitude = lat, LonTitude = lon });
                }
                SaveStopsInfo();
            }
            catch (Exception e)
            {
                arrivaltime.Text = e.Message.ToString();
            }
        }
        // Tallenetaan oliot-tiedostoon
        /// <summary>
        /// SaveStopsInfo tallentaa kaikki olion "instanssit" .dat tiedostoon josta niitä on helppo myöhemmin lukea.
        /// </summary>
        private async void SaveStopsInfo()
        {
            try
            {
                StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
                StorageFile stopsfile = await storageFolder.CreateFileAsync("stops.dat", CreationCollisionOption.OpenIfExists);  
                Stream stream = await stopsfile.OpenStreamForWriteAsync();
                DataContractSerializer serializer = new DataContractSerializer(typeof(List<BussStops>));
                serializer.WriteObject(stream, stops);
                await stream.FlushAsync();
                stream.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        /// <summary>
        /// ReadStops funktio lukee stops.dat tiedostosta tiedot takaisin "olioon"
        /// </summary>
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
            catch (Exception e)
            {
                arrivaltime.Text = e.ToString();
            }

        }
        /// <summary>
        /// ShowStops funktio hakee BussStops tyyppisestä listasta kaikki pysäkit ja tulostaa ne kartalle.
        /// </summary>
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
                // Jos pysäkin nimi on pupuhuhta niin POIn logo on eri värinen. tämä kuvastaa mikaelin koti pysäkkiä
                if (stop.StopName == " Pupuhuhta ") stopoint.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/home.png"));
                else stopoint.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/bus_stop_icon.png"));
                JKLmap.MapElements.Add(stopoint);
            }
        }
        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
        }
        /// <summary>
        /// AutoSuggestBox_querySubmitted funktio kutsuu 2 funktiota, ShowPoint ja routeto sekä antaa niille parametrina valitun string tyypisen pysäkin nime
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
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
                    
                }
            }
        }
        List<double> showroutebyname = new List<double>();
        /// <summary>
        /// ReadLineData funktio ottaa XAML painikkeen nimen parametrina ja sen perusteella antaa tiedoston etuliitteen.
        /// Sitten BussStops listaa käydään läpi ja tarkistetaan onko tiedostosta otettu tieto sama, jos on niin piiretään kartalle sen
        /// nimen perusteella pysäkki
        /* tiedosto on muotoa: 
          Pupuhuhta 
          Pupuhuhdan koulu 1 
          Pieles 1 
         */
        /// </summary>
        /// <param name="label"></param>
        /// 
        private async void ReadLineData(string label)
        {
            showroutebyname.Clear();
            StorageFolder storageFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
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
        /// <summary>
        /// ReadFakeGpsData funktio lisää fakedata listaan gps tiedot ja muiden funktioiden avulla piirtää kartallla
        /// </summary>
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
        int i = 0;
        /// <summary>
        /// DrawFakeGpsRoute funktio piirtää kartalle GPS pisteen 5 sekunnin välein
        /// </summary>
        /// <param name="label"></param>
        public void DrawFakeGpsRoute(string label)
        {

           try
            {  // Poistetaan kartalta edellinen GPS paikka
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
                stopoint.Title = label; 
                stopoint.Image = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/bussi.png"));
                // Lisätään karttaelementti paikalle "0" jotta se voidaan poistaa
                JKLmap.MapElements.Insert(0, stopoint);
                i++;
            }   
        }
        /// <summary>
        /// WaitDraw funktiossa kutsutaan DrawFakeGpsRoute funktiota 5 sekunnin välein
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public async Task WaitDraw(string label)
        {
            int time = 5000;
            
            while (true)
            {
                DrawFakeGpsRoute(label);
                await Task.Delay(time);
            }
        }
        /// <summary>
        /// display3DLocation funktio ottaa oletusparemtreina 2 double arvoa, lon ja lat jotka ohjaavat jyävskylän keskustaan.
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longtitude"></param>
        /// <param name="style"></param>
        private async void display3DLocation(double latitude = 62.2417, double longtitude = 25.7473, int style = 2)
        {
            if (JKLmap.Is3DSupported)
            {
                BasicGeoposition hwGeoposition = new BasicGeoposition() { Latitude = latitude, Longitude = longtitude };
                Geopoint hwPoint = new Geopoint(hwGeoposition);
                MapScene hwScene = MapScene.CreateFromLocationAndRadius(hwPoint,80,0,60);
                if(JKLmap.Style == MapStyle.Aerial3D)
                {
                    JKLmap.Style = MapStyle.Road;
                    hwScene = MapScene.CreateFromLocationAndRadius(hwPoint, 2500,0,0);
                    await JKLmap.TrySetSceneAsync(hwScene, MapAnimationKind.Bow);
                }
                else if (JKLmap.Style == MapStyle.Road)
                {
                    JKLmap.Style = MapStyle.Aerial3D;
                    await JKLmap.TrySetSceneAsync(hwScene, MapAnimationKind.Bow);
                }
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
    }
}

