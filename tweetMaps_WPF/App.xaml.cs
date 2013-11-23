using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using tweetMaps_WPF.Models;

namespace tweetMaps_WPF
{
     
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static int pin = 0;
        private readonly System.Configuration.ApplicationSettingsGroup settings;
        public static bool IsAuthenticated = false;

        protected override void OnLoadCompleted(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnLoadCompleted(e);
        }

        protected override void OnStartup(StartupEventArgs e)
        {

            base.OnStartup(e);
        }
    }
}
