using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TweetSharp;

namespace tweetMaps_WPF
{
    /// <summary>
    /// Interaction logic for GetPinWindow.xaml
    /// </summary>
    public partial class GetPinWindow : MetroWindow
    {
        public GetPinWindow()
        {
            InitializeComponent();
        }

        protected override void OnClosed(EventArgs e)
        {
            App.pin = Convert.ToInt32(pinTxtBox.Text);
            
           
            
            base.OnClosed(e);

            

            
        }

        

        private void submitPinBtn_Click(object sender, RoutedEventArgs e)
        {
            

            this.Close(); 



        }
    }
}
