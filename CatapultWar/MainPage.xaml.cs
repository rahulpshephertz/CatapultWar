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
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
        }
        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            MessagePopup.Visibility = Visibility.Collapsed;
        }
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                WarpClient.GetInstance().Disconnect();
            }
            catch (Exception e1)
            { 
            
            }

            base.OnBackKeyPress(e);
        }
        // Simple button Click event handler to take us to the second page
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            switch(Convert.ToInt32(btn.Tag.ToString()))
            {
                case 0:
                    PlayVsHumanPopup.Visibility = Visibility.Visible;
                    break;
                case 1:
                     messageTB.Text = "Please wait..";
                     MessagePopup.Visibility = Visibility.Visible;
                     App.g_isTwoHumanPlayers=false;
                    NavigationService.Navigate(new Uri("/GamePage.xaml", UriKind.RelativeOrAbsolute));
                    break;
                case 2: 
                    if(String.IsNullOrEmpty(userName.Text))
                    {
                    showMessage("please fill remote user name..");
                    return;
                    }
                     messageTB.Text = "Please wait..";
                     MessagePopup.Visibility = Visibility.Visible;
                    if (GlobalContext.IsConnectedToAppWarp)
                    {
                        JoinRoomWithRemoteUserID();
                    }
                    else
                    {
                        GlobalContext.warpClient.RemoveConnectionRequestListener(GlobalContext.conListenObj);
                        GlobalContext.conListenObj = new ConnectionListener(showResult, JoinRoomWithRemoteUserID);
                        GlobalContext.warpClient.AddConnectionRequestListener(GlobalContext.conListenObj);
                        WarpClient.GetInstance().Connect(GlobalContext.localUsername);
                    }
                      break;
                case 3:
                     messageTB.Text = "Please wait..";
                     MessagePopup.Visibility = Visibility.Visible;
                    if (GlobalContext.IsConnectedToAppWarp)
                    {
                        MakeJoinRoomInRangeRequest();
                    }
                    else
                    {
                        GlobalContext.warpClient.RemoveConnectionRequestListener(GlobalContext.conListenObj);
                        GlobalContext.conListenObj = new ConnectionListener(showResult, MakeJoinRoomInRangeRequest);
                        GlobalContext.warpClient.AddConnectionRequestListener(GlobalContext.conListenObj);
                        WarpClient.GetInstance().Connect(GlobalContext.localUsername);
                    }
                      break;
            }
        }
        internal void moveToPlayScreen()
        {
            GlobalContext.fireNumber = 1;
            App.g_isTwoHumanPlayers = true;
            Dispatcher.BeginInvoke(() =>
            NavigationService.Navigate(new Uri("/GamePage.xaml", UriKind.RelativeOrAbsolute)));
        }
        private void AddListeners()
        {
            if (GlobalContext.roomReqListenerObj == null)
            {
                GlobalContext.roomReqListenerObj = new RoomReqListener(showResult, moveToPlayScreen);
                GlobalContext.warpClient.AddRoomRequestListener(GlobalContext.roomReqListenerObj);
            }
            if (GlobalContext.notificationListenerObj == null)
            {
                GlobalContext.notificationListenerObj = new NotificationListener();
                WarpClient.GetInstance().AddNotificationListener(GlobalContext.notificationListenerObj);
            }
            if (GlobalContext.zoneRequestListenerobj == null)
            {
                GlobalContext.zoneRequestListenerobj = new ZoneRequestListener();
                WarpClient.GetInstance().AddZoneRequestListener(GlobalContext.zoneRequestListenerobj);
            }
        }
        private  void JoinRoomWithRemoteUserID()
        {
            AddListeners();
            //here we are looking for location-ID by his username
            WarpClient.GetInstance().GetLiveUserInfo(userName.Text);
        }
        private void MakeJoinRoomInRangeRequest()
        {
           AddListeners();
           Dictionary<string, object>  tableProperties = new Dictionary<string, object>();
           tableProperties.Add("IsPrivateRoom", "false");
           WarpClient.GetInstance().JoinRoomWithProperties(tableProperties);
        }
        public void showResult(String result)
        {
            Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                showMessage(result);
            });
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