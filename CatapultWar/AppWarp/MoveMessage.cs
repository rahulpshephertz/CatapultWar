using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework;
using Newtonsoft.Json.Linq;

namespace CatapultWar
{
    /// <summary>
    /// CatapultWar gameplay message class. Objects of this class represent actions of the user
    /// and are used to serialize/deserialize JSON exchanged between users in the room
    /// </summary>
    public class MoveMessage
    {
        public String sender;
        public String Type="NONE";
        public String ShotVelocity;
        public String ShotAngle;
        public String X;
        public String Y;
        private static JObject dragJsonObj=null;
        public static MoveMessage GetCurrentInstance()
        {
           
            MoveMessage msg = new MoveMessage();
            if (dragJsonObj != null)
            {
                msg.sender = dragJsonObj["sender"].ToString();
                if (!msg.sender.Equals(GlobalContext.localUsername))
                {
                    msg.Type = dragJsonObj["Type"].ToString();
                    if (msg.Type.Equals("SHOT"))
                    { 
                        msg.ShotVelocity = dragJsonObj["ShotVelocity"].ToString();
                        msg.ShotAngle = dragJsonObj["ShotAngle"].ToString();
                    }
                    else if (msg.Type.Equals("DRAGGING"))
                    {
                        GlobalContext.fireNumber = Convert.ToInt32(GlobalContext.tableProperties["fireNumber"]);
                        int fireNumber = Convert.ToInt32(dragJsonObj["fireNumber"].ToString());
                        int udpPacketNumber = Convert.ToInt32(dragJsonObj["packetNumber"].ToString());
                        if ((fireNumber == GlobalContext.fireNumber) && (udpPacketNumber > GlobalContext.prevUDPPacketNumber))
                        {
                            GlobalContext.prevUDPPacketNumber = udpPacketNumber;
                            msg.X = dragJsonObj["X"].ToString();
                            msg.Y = dragJsonObj["Y"].ToString();
                        }
                        else
                        {//igonre the udp packet
                            msg.Type = "NONE";
                        }
                    }
                }
                dragJsonObj = null;
            }
            return msg;
             // Vector2 vect = new Vector2((float)Convert.ToDouble(msg.X), (float)Convert.ToDouble(msg.Y));
        }
        public static void buildMessage(byte[] update)
        {
            try
            {
                dragJsonObj = JObject.Parse(System.Text.Encoding.UTF8.GetString(update, 0, update.Length));
            }
            catch (Exception e)
            { 
            
            }
        }

        public static byte[] buildSHOTMessageBytes(String ShotVelocity, String ShotAngle)
        {
            JObject moveObj = new JObject();
            moveObj.Add("sender", GlobalContext.localUsername);
            moveObj.Add("Type", "SHOT");
            moveObj.Add("ShotVelocity", ShotVelocity);
            moveObj.Add("ShotAngle", ShotAngle);
            return System.Text.Encoding.UTF8.GetBytes(moveObj.ToString());
        }
        public static byte[] buildDragingMessageBytes(int pnumber,String X, String Y)
        {
            GlobalContext.fireNumber = Convert.ToInt32(GlobalContext.tableProperties["fireNumber"]);
            JObject moveObj = new JObject();
            moveObj.Add("sender", GlobalContext.localUsername);
            moveObj.Add("fireNumber",GlobalContext.fireNumber.ToString());
            moveObj.Add("packetNumber", pnumber.ToString());
            moveObj.Add("Type", "DRAGGING");
            moveObj.Add("X", X);
            moveObj.Add("Y", Y);
            return System.Text.Encoding.UTF8.GetBytes(moveObj.ToString());
        }
    }
}
