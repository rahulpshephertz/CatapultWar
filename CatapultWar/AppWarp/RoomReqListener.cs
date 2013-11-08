using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using com.shephertz.app42.gaming.multiplayer.client.events;
using com.shephertz.app42.gaming.multiplayer.client.command;
using com.shephertz.app42.gaming.multiplayer.client;
using System.Text;
using System.Collections.Generic;

namespace CatapultWar
{
    public class RoomReqListener : com.shephertz.app42.gaming.multiplayer.client.listener.RoomRequestListener
    { 
        // private JoinPage _page;
        public delegate void ShowResultCallback(String message);
        ShowResultCallback mShowResultCallback;
        public delegate void MoveToPlayCallback();
        MoveToPlayCallback mMoveToPlay;
        public RoomReqListener(ShowResultCallback showResult, MoveToPlayCallback MoveToPlay)
        {
            mShowResultCallback = showResult;
            mMoveToPlay = MoveToPlay;  
        }

        public void onSubscribeRoomDone(RoomEvent eventObj)
        {
            if (eventObj.getResult() == WarpResponseResultCode.SUCCESS)
            {
                //WarpClient.GetInstance().GetLiveRoomInfo(GlobalContext.GameRoomId);
            }
        }
        public void onUnSubscribeRoomDone(RoomEvent eventObj)
        {
            if (eventObj.getResult() == WarpResponseResultCode.SUCCESS)
            {
                //Deployment.Current.Dispatcher.BeginInvoke(new ShowResultCallback(mShowResultCallback), "Yay! UnSubscribe room");
            }
        }

        public void onJoinRoomDone(RoomEvent eventObj)
        {
           
            if (eventObj.getResult() == WarpResponseResultCode.SUCCESS)
            {
                GlobalContext.tableProperties["Player1Score"]=0;
                GlobalContext.tableProperties["Player2Score"]=0;
                GlobalContext.tableProperties["WindX"]=0;
                GlobalContext.tableProperties["WindY"]=0;
                GlobalContext.tableProperties["fireNumber"]=1;
                GlobalContext.GameRoomId = eventObj.getData().getId();
                WarpClient.GetInstance().SubscribeRoom(GlobalContext.GameRoomId);
                if (GlobalContext.localUsername.Equals(eventObj.getData().getName()))
                {
                    GlobalContext.PlayerIsFirstOnAppWarp = true;
                }
                else
                {
                    GlobalContext.PlayerIsFirstOnAppWarp = false;
                }
                WarpClient.GetInstance().GetLiveRoomInfo(GlobalContext.GameRoomId);
            }
            else
            {
                try
                {   
                    if (GlobalContext.tableProperties["IsPrivateRoom"].ToString().Equals("true",StringComparison.InvariantCultureIgnoreCase))
                    {
                        Deployment.Current.Dispatcher.BeginInvoke(new ShowResultCallback(mShowResultCallback), "Sorry,Remote has already got the partner!!");
                    }
                    else
                    {
                        WarpClient.GetInstance().CreateRoom(GlobalContext.localUsername, GlobalContext.localUsername, 2, GlobalContext.tableProperties);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);

                }
            }
        }

        public void onLeaveRoomDone(RoomEvent eventObj)
        {
            if (eventObj.getResult() == WarpResponseResultCode.SUCCESS)
            {
                //Deployment.Current.Dispatcher.BeginInvoke(new ShowResultCallback(mShowResultCallback), "Yay! Leave room :");
                GlobalContext.tableProperties["Player1Score"] = 0;
                GlobalContext.tableProperties["Player2Score"] = 0;
                GlobalContext.tableProperties["WindX"] = 0;
                GlobalContext.tableProperties["WindY"] = 0;
                GlobalContext.tableProperties["fireNumber"]=1;
                GlobalContext.opponentName = "No Opponent";
                GlobalContext.messageFromOpponent = "Please wait for Opponent to join!";
                GlobalContext.joinedUsers = null;
            }
        }

        public void onGetLiveRoomInfoDone(LiveRoomInfoEvent eventObj)
        {
            if (eventObj.getResult() == WarpResponseResultCode.SUCCESS && (eventObj.getJoinedUsers() != null))
            {
                GlobalContext.tableProperties = eventObj.getProperties();
                for (int i = 0; i < eventObj.getJoinedUsers().Length; i++)
                {
                    if (!GlobalContext.localUsername.Equals(eventObj.getJoinedUsers()[i]))
                    {
                        GlobalContext.opponentName = eventObj.getJoinedUsers()[i];
                        break;
                    }
                }
                GlobalContext.joinedUsers = eventObj.getJoinedUsers();
                Deployment.Current.Dispatcher.BeginInvoke(new MoveToPlayCallback(mMoveToPlay));
            }            
        }

        public void onSetCustomRoomDataDone(LiveRoomInfoEvent eventObj)
        {

        }

        public void onUpdatePropertyDone(LiveRoomInfoEvent lifeLiveRoomInfoEvent)
        {
            if (lifeLiveRoomInfoEvent.getResult() == WarpResponseResultCode.SUCCESS)
            {
                GlobalContext.tableProperties = lifeLiveRoomInfoEvent.getProperties();
            }
        }
        public void onLockPropertiesDone(byte result)
        {
          //  throw new NotImplementedException();
        }

        public void onUnlockPropertiesDone(byte result)
        {
        }
    }
}
