using com.shephertz.app42.gaming.multiplayer.client;
using com.shephertz.app42.gaming.multiplayer.client.command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CatapultWar.AppWarp
{
   public class ZoneRequestListener : com.shephertz.app42.gaming.multiplayer.client.listener.ZoneRequestListener
    {
        public void onCreateRoomDone(com.shephertz.app42.gaming.multiplayer.client.events.RoomEvent eventObj)
        {
            GlobalContext.GameRoomId = eventObj.getData().getId();
            WarpClient.GetInstance().JoinRoom(eventObj.getData().getId());
        }

        public void onDeleteRoomDone(com.shephertz.app42.gaming.multiplayer.client.events.RoomEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onGetAllRoomsDone(com.shephertz.app42.gaming.multiplayer.client.events.AllRoomsEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onGetLiveUserInfoDone(com.shephertz.app42.gaming.multiplayer.client.events.LiveUserInfoEvent eventObj)
        {
            if (eventObj.getResult() == WarpResponseResultCode.SUCCESS)
            {
                // Join the room where the friend is playing
                GlobalContext.GameRoomId = eventObj.getLocationId();
                WarpClient.GetInstance().JoinRoom(GlobalContext.GameRoomId);
            }
            else
            {
                // remote user is either off line or has not joined any room yet. Create one and wait for him.
                GlobalContext.tableProperties["IsPrivateRoom"]="true";
                WarpClient.GetInstance().CreateRoom(GlobalContext.localUsername, GlobalContext.localUsername, 2, GlobalContext.tableProperties);
            }
        }

        public void onGetMatchedRoomsDone(com.shephertz.app42.gaming.multiplayer.client.events.MatchedRoomsEvent matchedRoomsEvent)
        {
            throw new NotImplementedException();
        }

        public void onGetOnlineUsersDone(com.shephertz.app42.gaming.multiplayer.client.events.AllUsersEvent eventObj)
        {
            throw new NotImplementedException();
        }

        public void onSetCustomUserDataDone(com.shephertz.app42.gaming.multiplayer.client.events.LiveUserInfoEvent eventObj)
        {
            throw new NotImplementedException();
        }
    }
}
