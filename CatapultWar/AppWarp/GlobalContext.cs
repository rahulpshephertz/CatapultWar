// Commenting
using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using com.shephertz.app42.gaming.multiplayer.client;
using CatapultWar.AppWarp;
using System.Collections.Generic;

namespace CatapultWar
{
    public class GlobalContext
    {
        public static String localUsername="";
        public static String opponentName="No Opponent";
        //create your game at apphq and find the api key and secret key
        public static String API_KEY = "Your Api Key";
        public static String SECRET_KEY = "Your Secret Key";
        public static String GameRoomId = "";
        internal static bool PlayerIsFirstOnAppWarp = true;
        public static Dictionary<string, object> tableProperties=null;
        public static string[] joinedUsers=new []{""};
        public static int fireNumber = 1;
        public static int currentUDPPacketNumber = 0;
        public static int prevUDPPacketNumber = 0;
        public static string messageFromOpponent ="Please wait for Opponent to join!";
        public static WarpClient warpClient;
        public static bool IsConnectedToAppWarp = true;
        public static bool IsUDPEnableOnNetwork = false;
        public static ConnectionListener conListenObj;
        public static RoomReqListener roomReqListenerObj;
        public static NotificationListener notificationListenerObj;
        public static ZoneRequestListener zoneRequestListenerobj;
    }
}
