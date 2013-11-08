using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

using com.shephertz.app42.gaming.multiplayer.client;
using CatapultWar.AppWarp;
using System.Windows.Threading;
using System.Windows.Navigation;

namespace CatapultWar
{
    public partial class JoinPage : PhoneApplicationPage
    {
        public JoinPage()
        {    
            InitializeComponent();

            // Initialize the SDK with your applications credentials that you received
            // after creating the app from http://apphq.shephertz.com
            WarpClient.initialize(GlobalContext.API_KEY, GlobalContext.SECRET_KEY);
            WarpClient.setRecoveryAllowance(60);
            // Keep a reference of the SDK singleton handy for later use.
            GlobalContext.warpClient = WarpClient.GetInstance();
        }
        /// <summary>
        /// Explicit saving of settings
        /// </summary>
        /// <param name="UserName"></param>
        /// <remarks>Settings are update when the user
        /// click Join.</remarks>
        private void joinButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUserName.Text))
                MessageBox.Show("Please Specifiy user name");
            else
            {
                messageTB.Text = "Please wait..";
                MessagePopup.Visibility = Visibility.Visible;
                // Initiate the connection
                // Create and add listener objects to receive callback events for the APIs used
                GlobalContext.conListenObj = new ConnectionListener(moveToMainScreen);
                GlobalContext.warpClient.AddConnectionRequestListener(GlobalContext.conListenObj);
                GlobalContext.localUsername = txtUserName.Text;
                WarpClient.GetInstance().Connect(GlobalContext.localUsername);
            }
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            MessagePopup.Visibility = Visibility.Collapsed;
        }
    
        internal void moveToMainScreen(string message)
        {
            Dispatcher.BeginInvoke(() =>
            NavigationService.Navigate(new Uri("/MainPage.xaml", UriKind.RelativeOrAbsolute)));
        }
        //Shows the message in the message grid
        void showMessage(string message)
        {
            messageTB.Text = message;
            MessagePopup.Visibility = Visibility.Visible;
            // messageGrid.Visibility = System.Windows.Visibility.Visible;

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 1);
            timer.Tick += new EventHandler(timer_Tick);
            timer.Start();
        }

        //Hides the message grid after 2 seconds
        void timer_Tick(object sender, EventArgs e)
        {
            //Hide message grid
            MessagePopup.Visibility = Visibility.Collapsed;
            //messageGrid.Visibility = System.Windows.Visibility.Collapsed;

            //stop the timer
            (sender as DispatcherTimer).Stop();
        }
    }
}