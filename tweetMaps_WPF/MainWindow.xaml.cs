using GoogleMaps.LocationServices;
using Hammock.Authentication.OAuth;
using MahApps.Metro.Controls;
using Microsoft.Maps.MapControl.WPF;
using Microsoft.Maps.MapControl.WPF.Design;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
using tweetMaps_WPF.Models;
using TweetSharp;
using System.Drawing;
using LinqToTwitter;


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

        public MainWindow()
        {
            string AccessToken = Properties.Settings.Default.AccessToken;
            string AccessTokenSecret = Properties.Settings.Default.AccessTokenSecret;
            var getPinWindow = new GetPinWindow(); 

            InitializeComponent();

            //Set focus on the map
            myMap.Focus();

            // Displays the current latitude and longitude as the map animates.
            myMap.ViewChangeOnFrame += new EventHandler<MapEventArgs>(viewMap_ViewChangeOnFrame);

            myMap.ViewChangeEnd += new EventHandler<MapEventArgs>(viewMap_ViewChangeEnd);

            TwitterContext twitterContext;

            if (string.IsNullOrEmpty(AccessToken) || string.IsNullOrEmpty(AccessTokenSecret))
            {
                var authorizer = new PinAuthorizer
                {
                    Credentials = new InMemoryCredentials
                    {
                        ConsumerKey = Properties.Settings.Default.ConsumerKey,
                        ConsumerSecret = Properties.Settings.Default.ConsumerSecret
                    },

                    AuthAccessType = AuthAccessType.NoChange,
                    UseCompression = true,
                    GoToTwitterAuthorization = pageLink => Process.Start(pageLink),
                    GetPin = () =>
                    {
                        getPinWindow.ShowDialog();

                        return App.pin.ToString();
                    }


                };

                authorizer.Authorize();

                twitterContext = new TwitterContext(authorizer);
                
            }
            else
            {
                var authorizer = new SingleUserAuthorizer
                {
                    Credentials = new SingleUserInMemoryCredentials
                    {
                        ConsumerKey = Properties.Settings.Default.ConsumerKey,
                        ConsumerSecret = Properties.Settings.Default.ConsumerSecret,
                        TwitterAccessToken = Properties.Settings.Default.AccessToken,
                        TwitterAccessTokenSecret = Properties.Settings.Default.AccessTokenSecret
                    },

                    AuthAccessType = AuthAccessType.NoChange,
                    UseCompression = true


                };

                twitterContext = new TwitterContext(authorizer);

                var users =
                    from tweet in twitterContext.User
                    where tweet.Type == UserType.Show &&
                          tweet.ScreenName == "griffinfujioka"
                    select tweet;

                //var user = users.SingleOrDefault();

            }

            //Account account = twitterContext.Account.Single(acct => acct.Type == AccountType.VerifyCredentials && acct.SkipStatus == true);

            Account account = twitterContext.Account.SingleOrDefault(acct => acct.Type == AccountType.VerifyCredentials && acct.SkipStatus == true);

            User user = account.User;

            profilePicture.Source = new BitmapImage(new Uri(user.ProfileImageUrl));
            usernameTxtBlock.Text = user.Name;

            tweetsTxtBlock.Text = user.StatusesCount.ToString();
            followersTxtBlock.Text = user.FollowersCount.ToString();
            followingTxtBlock.Text = user.FriendsCount.ToString();
            




            Loaded += OnLoaded;


        }


        private async void viewMap_ViewChangeEnd(object sender, MapEventArgs e)
        {

            await DetermineLocation();

            var map = sender as Map; 

            // Determine if we have a valid map object.
            if (map != null)
            {

                // Gets the center of the current map view for this particular frame
                Microsoft.Maps.MapControl.WPF.Location mapCenter = map.Center;
                CurrentLocation.latitude = mapCenter.Latitude;
                CurrentLocation.longitude = mapCenter.Longitude;

                var location = await ReverseGeocode(CurrentLocation.latitude, CurrentLocation.longitude);
            }


        }

        private void viewMap_ViewChangeOnFrame(object sender, MapEventArgs e)
        {
            // Gets the map object that raised this event.
            Map map = sender as Map;

            // Determine if we have a valid map object.
            if (map != null)
            {

                // Gets the center of the current map view for this particular frame.
                Microsoft.Maps.MapControl.WPF.Location mapCenter = map.Center;

                // Updates the latitude and longitude values, in real time,
                // as the map animates to the new location.
                txtLatitude.Text = string.Format(CultureInfo.InvariantCulture,
                  "{0:F5}", mapCenter.Latitude);
                txtLongitude.Text = string.Format(CultureInfo.InvariantCulture,
                    "{0:F5}", mapCenter.Longitude);




            }
        }

        private async void ChangeMapView_Click(object sender, RoutedEventArgs e)
        {
            // Parse the information of the button's Tag property
            string[] tagInfo = ((Button)sender).Tag.ToString().Split(' ');
            if (((Button)sender).Name == "btnCurrentLocation")
            {
                await GetMyLocation();
                return;
            }
            else if (((Button)sender).Name == "btnSearchForLocation")
            {
                SearchForLocation();
                return;
            }

            Microsoft.Maps.MapControl.WPF.Location center = (Microsoft.Maps.MapControl.WPF.Location)locConverter.ConvertFrom(tagInfo[0]);
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

            Microsoft.Maps.MapControl.WPF.Location center = new Microsoft.Maps.MapControl.WPF.Location(latitude, longitude);
            double zoom = 10;

            myMap.SetView(center, zoom);
        }

        private async Task DetermineLocation()
        {
            try
            {
                //create a request to geoiptool.com
                var request = WebRequest.Create(new Uri("http://geoiptool.com/data.php")) as HttpWebRequest;


                if (request != null)
                {
                    //set the request user agent
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0; SLCC1; .NET CLR 2.0.50727)";

                    var webResponse = await request.GetResponseAsync() as HttpWebResponse;

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
                                var city = marker.GetAttribute("city").ToString();

                                CurrentLocation.latitude = latitude;
                                CurrentLocation.longitude = longitude;
                                CurrentLocation.city = city;

                                //cityTxtBlock.Text = city;
                            }
                        }
                }



            }
            catch (Exception ex)
            {
                throw;
            }
        }


        private async Task GetMyLocation()
        {
            try
            {
                //create a request to geoiptool.com
                var request = WebRequest.Create(new Uri("http://geoiptool.com/data.php")) as HttpWebRequest;


                if (request != null)
                {
                    //set the request user agent
                    request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0; SLCC1; .NET CLR 2.0.50727)";

                    var webResponse = await request.GetResponseAsync() as HttpWebResponse;

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
                                var city = marker.GetAttribute("city").ToString();

                                CurrentLocation.latitude = latitude;
                                CurrentLocation.longitude = longitude;
                                CurrentLocation.city = city;

                                Microsoft.Maps.MapControl.WPF.Location center = new Microsoft.Maps.MapControl.WPF.Location(latitude, longitude);
                                double zoom = 10;

                                myMap.SetView(center, zoom);
                            }
                        }
                }
                //get the response
                //using (var webResponse = (request.GetResponseAsync() as HttpWebResponse))
                //    if (webResponse != null)
                //        using (var reader = new StreamReader(webResponse.GetResponseStream()))
                //        {

                //            //get the XML document
                //            var doc = new XmlDocument();
                //            doc.Load(reader);

                //            //now we parse the XML document
                //            var nodes = doc.GetElementsByTagName("marker");

                //            //Guard.AssertCondition(nodes.Count > 0, "nodes", new object());
                //            //make sure we have nodes before looping
                //            if (nodes.Count > 0)
                //            {
                //                //grab the first response
                //                var marker = nodes[0] as XmlElement;


                //                var latitude = Convert.ToDouble(marker.GetAttribute("lat"));

                //                var longitude = Convert.ToDouble(marker.GetAttribute("lng"));
                //                var city = marker.GetAttribute("city").ToString();

                //                CurrentLocation.latitude = latitude;
                //                CurrentLocation.longitude = longitude;
                //                CurrentLocation.city = city;

                //                Location center = new Location(latitude, longitude);
                //                double zoom = 10;

                //                myMap.SetView(center, zoom);

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

        private async void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            WindowState = WindowState.Maximized;
            ResizeMode = ResizeMode.CanResizeWithGrip;

            await GetMyLocation();

            DownloadHomeTimeline();

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
        }

        private void signOutBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private async Task<ReverseGeocodedLocation> ReverseGeocode(double latitude, double longitude)
        {
            var location = new ReverseGeocodedLocation(); 

            //try
            //{
            //    var url = "http://maps.google.com/maps/api/geocode/json?latlng=" + latitude + "," + longitude + "&sensor=false";
            //    //create a request
            //    var request = WebRequest.Create(new Uri(url)) as HttpWebRequest;


            //    if (request != null)
            //    {
            //        //set the request user agent
            //        request.UserAgent = "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.0; SLCC1; .NET CLR 2.0.50727)";

            //        var webResponse = await request.GetResponseAsync() as HttpWebResponse;

            //        if (webResponse != null)
            //            using (var reader = new StreamReader(webResponse.GetResponseStream()))
            //            {
            //                var text = reader.ReadToEnd();
            //                var des = JsonConvert.DeserializeObject(text); 
            //                //get the XML document
            //                var doc = new XmlDocument();
            //                doc.Load(reader);

            //                //now we parse the XML document
            //                var nodes = doc.GetElementsByTagName("GeocodeResponse");

            //                //Guard.AssertCondition(nodes.Count > 0, "nodes", new object());
            //                //make sure we have nodes before looping
            //                if (nodes.Count > 0)
            //                {
            //                    //grab the first response
            //                    var marker = nodes[0] as XmlElement;

            //                    var name = marker.GetAttribute("long_name");

            //                    var newLatitude = Convert.ToDouble(marker.GetAttribute("lat"));

            //                    var newLongitude = Convert.ToDouble(marker.GetAttribute("lng"));
            //                    var city = marker.GetAttribute("city").ToString();

            //                    CurrentLocation.latitude = latitude;
            //                    CurrentLocation.longitude = longitude;
            //                    CurrentLocation.city = city;

            //                    Location center = new Location(latitude, longitude);
            //                    double zoom = 10;

            //                    myMap.SetView(center, zoom);
            //                }
            //            }
            //    }
         


            //}
            //catch (Exception ex)
            //{
            //    throw;
            //}

            return location;
        }

        private void submitNewTweetButton_Click(object sender, RoutedEventArgs e)
        {
            var tweetMsg = composeNewTweetTxtBox.Text;
            var getPinWindow = new GetPinWindow();


            var authorizer = new PinAuthorizer
            {
                Credentials = new InMemoryCredentials
                {
                    ConsumerKey = Properties.Settings.Default.ConsumerKey,
                    ConsumerSecret = Properties.Settings.Default.ConsumerSecret
                },

                AuthAccessType = AuthAccessType.NoChange,
                UseCompression = true,
                GoToTwitterAuthorization = pageLink => Process.Start(pageLink),
                GetPin = () =>
                {
                    getPinWindow.ShowDialog();

                    return App.pin.ToString();
                }


            };

            authorizer.Authorize();

            if (authorizer.IsAuthorized)
            {
                using (var twitterCtx = new TwitterContext(authorizer))
                {
                    twitterCtx.Log = Console.Out;

                    var tweet = twitterCtx.UpdateStatus(composeNewTweetTxtBox.Text);
                }
            }
            else
            {
                // authorization failed
            }
        }

        private void composeNewTweetTxtBox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return && composeNewTweetTxtBox.Text.Length < 140)
            {
                submitNewTweetButton_Click(sender, (RoutedEventArgs)e);
            }
            else if (composeNewTweetTxtBox.Text.Length > 140)
            {
                characterCountTxtBlock.Text = (140 - composeNewTweetTxtBox.Text.Length).ToString();
                submitNewTweetButton.IsEnabled = false;
            }
            else
            {
                characterCountTxtBlock.Text = composeNewTweetTxtBox.Text.Length + "/140";
                submitNewTweetButton.IsEnabled = true;
                
            }

        }

        private void DownloadHomeTimeline()
        {
            var authorizer = new SingleUserAuthorizer
            {
                Credentials = new SingleUserInMemoryCredentials
                {
                    ConsumerKey = Properties.Settings.Default.ConsumerKey,
                    ConsumerSecret = Properties.Settings.Default.ConsumerSecret,
                    TwitterAccessToken = Properties.Settings.Default.AccessToken,
                    TwitterAccessTokenSecret = Properties.Settings.Default.AccessTokenSecret
                },

                AuthAccessType = AuthAccessType.NoChange,
                UseCompression = true


            };

            var twitterContext = new TwitterContext(authorizer);

            var tweets =
                
                (from tweet in twitterContext.Status
                    where tweet.Type == StatusType.Home
                    select tweet)
                .ToList();
        }


    }
}
