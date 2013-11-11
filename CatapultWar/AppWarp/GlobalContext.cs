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
        public static String API_KEY = "268031d927c5b6fe6919b5c6d42846e483ec44797ce74c05c81ce3faa9e797fe";
        public static String SECRET_KEY = "6cfd1beef6304efd8aea318f78181d4ed719e8a8884e25d8bfc877eb36908893";
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
