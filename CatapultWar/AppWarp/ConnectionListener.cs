using com.shephertz.app42.gaming.multiplayer.client;
using com.shephertz.app42.gaming.multiplayer.client.command;
using com.shephertz.app42.gaming.multiplayer.client.events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace CatapultWar.AppWarp
{
    public class ConnectionListener : com.shephertz.app42.gaming.multiplayer.client.listener.ConnectionRequestListener
    {
        public delegate void ShowResultCallback(String message);
        ShowResultCallback mShowResultCallback=null;

        public delegate void ConnectionCallback();
        ConnectionCallback mOnConnectDoneCallback = null,mConnectionRecoverableError=null,mConnectionRecoverd=null;

        public ConnectionListener(ShowResultCallback showResult)
        {
            mShowResultCallback = showResult;
        }
        public ConnectionListener(ShowResultCallback showResult, ConnectionCallback onConnectDoneCallback)
        {
            mShowResultCallback = showResult;
            mOnConnectDoneCallback = onConnectDoneCallback;
        }

        public void onConnectDone(ConnectEvent eventObj)
        {
            switch (eventObj.getResult())
            {
                case WarpResponseResultCode.SUCCESS: 
                     GlobalContext.IsConnectedToAppWarp = true;
                     // Successfully connected to the server. Lets go ahead and init the udp.
                     //Init udp is essentional if we are using UDP communication in our Game
                     WarpClient.GetInstance().initUDP();
                     if (mOnConnectDoneCallback != null)
                     {   //Request Was From Main Screen
                         Deployment.Current.Dispatcher.BeginInvoke(new ConnectionCallback(mOnConnectDoneCallback));
                     }
                     else if ((mOnConnectDoneCallback == null) && mShowResultCallback != null)
                     { 
                      //Request Was From Join Screen
                      Deployment.Current.Dispatcher.BeginInvoke(new ShowResultCallback(mShowResultCallback), "connected");
                     }
                      break;
                case WarpResponseResultCode.CONNECTION_ERROR_RECOVERABLE:
                      WarpClient.GetInstance().RecoverConnection();
                      if (mConnectionRecoverableError != null)
                          Deployment.Current.Dispatcher.BeginInvoke(new ConnectionCallback(mConnectionRecoverableError));
                      break;
                case WarpResponseResultCode.SUCCESS_RECOVERED:
                    if(mConnectionRecoverd!=null)
                      Deployment.Current.Dispatcher.BeginInvoke(new ConnectionCallback(mConnectionRecoverd)); 
                     break;
                default: 
                      GlobalContext.IsConnectedToAppWarp = false;
                      if (mShowResultCallback != null) 
                      Deployment.Current.Dispatcher.BeginInvoke(new ShowResultCallback(mShowResultCallback), "connection failed");
                      break;
            
            }

        }

        public void onDisconnectDone(ConnectEvent eventObj)
        {
           
        }


        public void onInitUDPDone(byte ResultCode)
        {
            if (ResultCode == WarpResponseResultCode.SUCCESS)
            {
                GlobalContext.IsUDPEnableOnNetwork = true;
            }
            else
            {
                GlobalContext.IsUDPEnableOnNetwork = false;
            }
        }
        public void AddConnectionRecoverableCallbacks(ConnectionCallback mRecoverable, ConnectionCallback mRecovered)
        {
            mConnectionRecoverableError = mRecoverable;
            mConnectionRecoverd = mRecovered;
        }
        public void RemoveConnectionRecoverableCallbacks()
        {
            mConnectionRecoverableError = null;
            mConnectionRecoverd = null;
        }
    }
}
