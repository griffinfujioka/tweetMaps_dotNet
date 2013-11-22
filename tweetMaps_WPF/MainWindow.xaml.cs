using GoogleMaps.LocationServices;
using MahApps.Metro.Controls;
using Microsoft.Maps.MapControl.WPF;
using Microsoft.Maps.MapControl.WPF.Design;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace tweetMaps_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
         LocationConverter locConverter = new LocationConverter();
         //Geolocator geolocator;
         Pushpin GpsPushpin;

        public MainWindow()
        {
            InitializeComponent();
            //Set focus on the map
            myMap.Focus();
            // Displays the current latitude and longitude as the map animates.
            myMap.ViewChangeOnFrame += new EventHandler<MapEventArgs>(viewMap_ViewChangeOnFrame);
            // The default animation level: navigate between different map locations.
            //viewMap.AnimationLevel = AnimationLevel.Full;

            Loaded += OnLoaded;
        }

        private void viewMap_ViewChangeOnFrame(object sender, MapEventArgs e)
        {
            // Gets the map object that raised this event.
            Map map = sender as Map;
            // Determine if we have a valid map object.
            if (map != null)
            {
                // Gets the center of the current map view for this particular frame.
                Location mapCenter = map.Center;

                // Updates the latitude and longitude values, in real time,
                // as the map animates to the new location.
                txtLatitude.Text = string.Format(CultureInfo.InvariantCulture,
                  "{0:F5}", mapCenter.Latitude);
                txtLongitude.Text = string.Format(CultureInfo.InvariantCulture,
                    "{0:F5}", mapCenter.Longitude);
            }
        }

        private void ChangeMapView_Click(object sender, RoutedEventArgs e)
        {
            // Parse the information of the button's Tag property
            string[] tagInfo = ((Button)sender).Tag.ToString().Split(' ');
            if (((Button)sender).Name == "btnCurrentLocation")
            {
                GetMyLocation();
                return; 
            }
            else if (((Button)sender).Name == "btnSearchForLocation")
            {
                SearchForLocation(sender);
                return; 
            }

            Location center = (Location)locConverter.ConvertFrom(tagInfo[0]);
            double zoom = System.Convert.ToDouble(tagInfo[1]);

            // Set the map view
            myMap.SetView(center, zoom);

        }

        private void SearchForLocation(object sender)
        {
            var location = searchForLocationTxtBox.Text;

            var locationService = new GoogleLocationService();
            var point = locationService.GetLatLongFromAddress(location);

            var latitude = point.Latitude;
            var longitude = point.Longitude;

            Location center = new Location(latitude, longitude);
            double zoom = 4;

            myMap.SetView(center, zoom); 
        }

        private void GetMyLocation()
        {
            

            myMap.CredentialsProvider.GetCredentials((c) =>
            {
                string sessionKey = c.ApplicationId;

                //Generate a request URL for the Bing Maps REST services.
                //Use the session key in the request as the Bing Maps key
            });
        }


        private void AnimationLevel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBoxItem cbi = (ComboBoxItem)(((ComboBox)sender).SelectedItem);
            string v = cbi.Content as string;
            if (!string.IsNullOrEmpty(v) && myMap != null)
            {
                AnimationLevel newLevel = (AnimationLevel)Enum.Parse(typeof(AnimationLevel), v, true);
                myMap.AnimationLevel = newLevel;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.CanResizeWithGrip;
            //ShowMaxRestoreButton = false;
            //ShowMinButton = false;
            Loaded -= OnLoaded;
        }
    }
}
