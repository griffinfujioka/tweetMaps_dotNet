﻿using GoogleMaps.LocationServices;
using Hammock.Authentication.OAuth;
using MahApps.Metro.Controls;
using Microsoft.Maps.MapControl.WPF;
using Microsoft.Maps.MapControl.WPF.Design;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Xml;
using TweetSharp;

namespace tweetMaps_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        LocationConverter locConverter = new LocationConverter();
        OAuthRequestToken requestToken;
        string ConsumerKey = Properties.Settings.Default.ConsumerKey;
        string ConsumerSecret = Properties.Settings.Default.ConsumerSecret;
        TwitterService twitterService;

        Pushpin GpsPushpin;

        public MainWindow()
        {
            InitializeComponent();

            //Set focus on the map
            myMap.Focus();

            // Displays the current latitude and longitude as the map animates.
            myMap.ViewChangeOnFrame += new EventHandler<MapEventArgs>(viewMap_ViewChangeOnFrame);

            TwitterClientInfo twitterClientInfo = new TwitterClientInfo();
            twitterClientInfo.ConsumerKey = ConsumerKey;            //Read ConsumerKey from User Settings
            twitterClientInfo.ConsumerSecret = ConsumerSecret;         //Read ConsumerSecret from User Settings

            string AccessToken = Properties.Settings.Default.AccessToken;
            string AccessTokenSecret = Properties.Settings.Default.AccessTokenSecret;


            twitterService = new TwitterService(twitterClientInfo);

            if (string.IsNullOrEmpty(AccessToken) || string.IsNullOrEmpty(AccessTokenSecret))
            {
                // We need to get an AccessToken and Secret
                requestToken = twitterService.GetRequestToken();
                string authUrl = "https://api.twitter.com/oauth/authorize" + "?oauth_token=" + requestToken.Token;

                /****************************************/ 
                /* Idea: Use a webview here instead     */ 
                /****************************************/
                Process.Start(authUrl); //Launches a browser that'll go to the AuthUrl.

                var getPinWindow = new GetPinWindow();
                getPinWindow.ShowDialog();

                //getPinMenu.Visibility = Visibility.Visible;
                var pin = App.pin;
                OAuthAccessToken accessToken = twitterService.GetAccessToken(requestToken, App.pin.ToString());

                // Save the AccessToken and AccessTokenSecret in User Settings
                Properties.Settings.Default.AccessToken = accessToken.Token;
                Properties.Settings.Default.AccessTokenSecret = accessToken.TokenSecret;
                AccessToken = Properties.Settings.Default.AccessToken;
                AccessTokenSecret = Properties.Settings.Default.AccessTokenSecret;

            }

            twitterService.AuthenticateWith(AccessToken, AccessTokenSecret);

            GetUserProfileOptions options = new GetUserProfileOptions();
            var profile = twitterService.GetUserProfile(options);



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
                SearchForLocation();
                return;
            }

            Location center = (Location)locConverter.ConvertFrom(tagInfo[0]);
            double zoom = System.Convert.ToDouble(tagInfo[1]);

            // Set the map view
            myMap.SetView(center, zoom);

        }

        private void SearchForLocation()
        {
            var location = searchForLocationTxtBox.Text;

            var locationService = new GoogleLocationService();
            var point = locationService.GetLatLongFromAddress(location);

            var latitude = point.Latitude;
            var longitude = point.Longitude;

            Location center = new Location(latitude, longitude);
            double zoom = 10;

            myMap.SetView(center, zoom);
        }

        private void GetMyLocation()
        {
            try
            {
                //create a request to geoiptool.com
                var request = WebRequest.Create(new Uri("http://geoiptool.com/data.php")) as HttpWebRequest;


                if (request != null)
                {
                    //set the request user agent
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0; SLCC1; .NET CLR 2.0.50727)";

                    //get the response
                    using (var webResponse = (request.GetResponse() as HttpWebResponse))
                        if (webResponse != null)
                            using (var reader = new StreamReader(webResponse.GetResponseStream()))
                            {

                                //get the XML document
                                var doc = new XmlDocument();
                                doc.Load(reader);

                                //now we parse the XML document
                                var nodes = doc.GetElementsByTagName("marker");

                                //Guard.AssertCondition(nodes.Count > 0, "nodes", new object());
                                //make sure we have nodes before looping
                                if (nodes.Count > 0)
                                {
                                    //grab the first response
                                    var marker = nodes[0] as XmlElement;


                                    var latitude = Convert.ToDouble(marker.GetAttribute("lat"));

                                    var longitude = Convert.ToDouble(marker.GetAttribute("lng"));

                                    Location center = new Location(latitude, longitude);
                                    double zoom = 10;

                                    myMap.SetView(center, zoom);

                                    //var _geoLoc = new GeoLoc();
                                    //get the data and return it
                                    //_geoLoc.City = marker.GetAttribute("city");
                                    //_geoLoc.Country = marker.GetAttribute("country");
                                    //_geoLoc.Code = marker.GetAttribute("code");
                                    //_geoLoc.Host = marker.GetAttribute("host");
                                    //_geoLoc.Ip = marker.GetAttribute("ip");
                                    //_geoLoc.Latitude = marker.GetAttribute("lat");
                                    //_geoLoc.Lognitude = marker.GetAttribute("lng");
                                    //_geoLoc.State = GetMyState(_geoLoc.Latitude, _geoLoc.Lognitude);

                                    //return _geoLoc;
                                }
                            }
                }


            }
            catch (Exception ex)
            {
                throw;
            }
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

            GetMyLocation();

            Loaded -= OnLoaded;
        }

        private void searchForLocationTxtBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                SearchForLocation();
            }
        }

        private void signInBtn_Click(object sender, RoutedEventArgs e)
        {


        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            var accessToken = Properties.Settings.Default.AccessToken;
            var accessTokenSecret = Properties.Settings.Default.AccessTokenSecret;

            if (accessToken != "" && accessTokenSecret != "" && !App.IsAuthenticated)
            {
                twitterService.AuthenticateWith(accessToken, accessTokenSecret);
                App.IsAuthenticated = true;
            }
        }

        private void signOutBtn_Click(object sender, RoutedEventArgs e)
        {

        }


    }
}
