using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Linq;
using com.shephertz.app42.gaming.multiplayer.client.events;
using com.shephertz.app42.gaming.multiplayer.client.command;
using com.shephertz.app42.gaming.multiplayer.client;
using System.Collections.Generic;
using System.Windows.Threading;

namespace CatapultWar
{
    public class NotificationListener : com.shephertz.app42.gaming.multiplayer.client.listener.NotifyListener
    {
        public delegate void UserLeftRoom();
        public delegate void UserPaused();
        public delegate void UserResumed();
        UserLeftRoom OpponentLeftRoom=null;
        UserPaused RemoteUserPaused = null;
        UserResumed RemoteUserResumed = null;
        public NotificationListener()
        {
        }
        public void AddCallBacks(UserLeftRoom uCallback,UserPaused RPaused,UserResumed RResumed)
        {
            OpponentLeftRoom = uCallback;
            RemoteUserPaused = RPaused;
            RemoteUserResumed = RResumed;
        }
        public void RemoveCallBacks()
        {
            OpponentLeftRoom = null;
            RemoteUserPaused = null;
            RemoteUserResumed = null;
        }
        public void onRoomCreated(RoomData eventObj)
        {
           
        }

        public void onRoomDestroyed(RoomData eventObj)
        {
            
        }

        public void onUserLeftRoom(RoomData eventObj, String username)
        {
            if (eventObj.getId().Equals(GlobalContext.GameRoomId))
            {
                if (!GlobalContext.localUsername.Equals(username))
                {
                    if(OpponentLeftRoom!=null)
                    Deployment.Current.Dispatcher.BeginInvoke(new UserLeftRoom(OpponentLeftRoom));
                   // WarpClient.GetInstance().UpdateRoomProperties(GlobalContext.GameRoomId, GlobalContext.tableProperties, null);               
                }
            }
        }

        public void onUserJoinedRoom(RoomData eventObj, String username)
        {
            if (!GlobalContext.localUsername.Equals(username))
            {  
                GlobalContext.opponentName = username;
                GlobalContext.joinedUsers = new[] {GlobalContext.localUsername, GlobalContext.opponentName};
            }
        }

        public void onUserLeftLobby(LobbyData eventObj, String username)
        {
            
        }

        public void onUserJoinedLobby(LobbyData eventObj, String username)
        {
            
        }

        public void onChatReceived(ChatEvent eventObj)
        {
            
        }

        public void onUpdatePeersReceived(UpdateEvent eventObj)
        {
            MoveMessage.buildMessage(eventObj.getUpdate());           
        }

        public void onUserChangeRoomProperty(RoomData roomData, string sender, Dictionary<string, object> properties)
        {
            GlobalContext.tableProperties = properties;
        }
        public void onMoveCompleted(MoveEvent moveEvent)
        {
            throw new NotImplementedException();
        }

        public void onPrivateChatReceived(string sender, string message)
        {
            throw new NotImplementedException();
        }

        public void onUserChangeRoomProperty(RoomData roomData, string sender, Dictionary<string, object> properties, Dictionary<string, string> lockedPropertiesTable)
        {
            GlobalContext.tableProperties = properties;
        }


        public void onUserPaused(string locid, bool isLobby, string username)
        {
             if (!GlobalContext.localUsername.Equals(username) && !isLobby)
              {
                  if (RemoteUserPaused != null)
                      Deployment.Current.Dispatcher.BeginInvoke(new UserLeftRoom(RemoteUserPaused));
                    // WarpClient.GetInstance().UpdateRoomProperties(GlobalContext.GameRoomId, GlobalContext.tableProperties, null);               
             }
        }

        public void onUserResumed(string locid, bool isLobby, string username)
        {
              if (!GlobalContext.localUsername.Equals(username) && !isLobby)
                {
                    if (RemoteUserResumed != null)
                        Deployment.Current.Dispatcher.BeginInvoke(new UserLeftRoom(RemoteUserResumed));
                    // WarpClient.GetInstance().UpdateRoomProperties(GlobalContext.GameRoomId, GlobalContext.tableProperties, null);               
                }
        }


        public void onGameStarted(string sender, string roomId, string nextTurn)
        {
            //throw new NotImplementedException();
        }

        public void onGameStopped(string sender, string roomId)
        {
            //throw new NotImplementedException();
        }
    }
}
