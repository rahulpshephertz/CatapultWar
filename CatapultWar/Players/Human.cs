#region File Description
//-----------------------------------------------------------------------------
// Human.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region File Information
//-----------------------------------------------------------------------------
// Human.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using Microsoft.Xna.Framework.Content;
using com.shephertz.app42.gaming.multiplayer.client;
#endregion

namespace CatapultWar
{
    enum PlayerSide
    {
        Left,
        Right
    }

    class Human : Player
    {
        #region Fields/Constants
        //Drag variables to hold first and last gesture samples
        ContentManager contentManager;
        GestureSample? prevSample,tempSample;
        GestureSample? firstSample;
        Vector2 deltaSum = Vector2.Zero;
        public bool isDragging { get; set; }
        // Constant for longest distance possible between drag points
        readonly float maxDragDelta = (new Vector2(480, 800)).Length();
        // Textures & position & spriteEffects used for Catapult
        Texture2D arrow;
        Texture2D guideDot;
        MoveMessage msg=new MoveMessage();
        float arrowScale;
        Vector2 catapultPosition, remoteFirstSample = Vector2.Zero, remotePrevSample = Vector2.Zero, remoteCurrentSample = Vector2.Zero;
        PlayerSide playerSide;
        SpriteEffects spriteEffect = SpriteEffects.None;
        int skipCounter = 5;
        // A projectile which we will use to draw guide lines
        Projectile guideProjectile;
        #endregion

        #region Initialization
        public Human(ContentManager cm)
            : base(cm)
        {
            contentManager = cm;
        }

        public Human(ContentManager cm, SpriteBatch screenSpriteBatch, PlayerSide playerSide)
            : base(cm, screenSpriteBatch)
        {
            contentManager = cm;
            string idleTextureName = "";
            this.playerSide = playerSide;

            if (playerSide == PlayerSide.Left)
            {
                catapultPosition = new Vector2(140, 332);
                idleTextureName = "Textures/Catapults/Blue/blueIdle/blueIdle";
            }
            else
            {
                catapultPosition = new Vector2(600, 332);
                spriteEffect = SpriteEffects.FlipHorizontally;
                idleTextureName = "Textures/Catapults/Red/redIdle/redIdle";
            }

            Catapult = new Catapult(cm, screenSpriteBatch,
                                    idleTextureName, catapultPosition, spriteEffect, 
                                    playerSide == PlayerSide.Left ? false : true, true);
        }

        public override void Initialize()
        {
            arrow = contentManager.Load<Texture2D>("Textures/HUD/arrow");
            guideDot = contentManager.Load<Texture2D>("Textures/HUD/guideDot");

            Catapult.Initialize();

            guideProjectile =
                new Projectile(contentManager, spriteBatch, null,
                "Textures/Ammo/rock_ammo", Catapult.ProjectileStartPosition,
                Catapult.GroundHitOffset, playerSide == PlayerSide.Right, 
                Catapult.Gravity);    
        }
        #endregion

        #region Handle Input
        /// <summary>
        /// Function processes the user input
        /// </summary>
        /// <param name="gestureSample"></param>
        public void HandleInput(GestureSample gestureSample)
        {            
            // Process input only if in Human's turn
            if (IsActive)
            {
                // Process any Drag gesture
                if (gestureSample.GestureType == GestureType.FreeDrag)
                {
                    // If drag just began save the sample for future 
                    // calculations and start Aim "animation"
            
                    if (null == firstSample)
                    {
                        firstSample = gestureSample;
                        if (App.g_isTwoHumanPlayers && GlobalContext.IsUDPEnableOnNetwork)
                        {
                            GlobalContext.currentUDPPacketNumber = 1;
                            if(GlobalContext.IsUDPEnableOnNetwork)
                            WarpClient.GetInstance().SendUDPUpdatePeers(MoveMessage.buildDragingMessageBytes(GlobalContext.currentUDPPacketNumber, gestureSample.Position.X.ToString(), gestureSample.Position.Y.ToString()));
                        }
                        Catapult.CurrentState = CatapultState.Aiming;
                    }
                    if (App.g_isTwoHumanPlayers&&GlobalContext.IsUDPEnableOnNetwork)
                    {
                        tempSample = gestureSample;
                        if (prevSample != null)
                        {
                            Vector2 tempDelta = tempSample.Value.Position - prevSample.Value.Position;
                            if (tempDelta.Length() != 0)
                            {
                                if (skipCounter == 5)
                                {
                                    skipCounter = 0;
                                    GlobalContext.currentUDPPacketNumber++;
                                    WarpClient.GetInstance().SendUDPUpdatePeers(MoveMessage.buildDragingMessageBytes(GlobalContext.currentUDPPacketNumber, gestureSample.Position.X.ToString(), gestureSample.Position.Y.ToString()));
                                }
                                else
                                {
                                    skipCounter++;
                                }
                            }
                        }
                    }
                    // save the current gesture sample 
                    prevSample = gestureSample;

                    // calculate the delta between first sample and current
                    // sample to present visual sound on screen
                    Vector2 delta = prevSample.Value.Position-
                        firstSample.Value.Position;
                    Catapult.ShotStrength = delta.Length() / maxDragDelta;

                    Catapult.ShotVelocity = MinShotVelocity +
                        Catapult.ShotStrength * (MaxShotVelocity - MinShotVelocity);

                    if (delta.Length() > 0)
                        Catapult.ShotAngle =
                                MathHelper.Clamp((float)Math.Asin(-delta.Y / delta.Length()),
                                MinShotAngle, MaxShotAngle);
                    else
                        Catapult.ShotAngle = MinShotAngle;

                    float baseScale = 0.001f;
                    arrowScale = baseScale * delta.Length();

                    isDragging = true;

                }
                else if (gestureSample.GestureType == GestureType.DragComplete)
                {
                    // calc velocity based on delta between first and last
                    // gesture samples
                    if (null != firstSample)
                    {
                        Vector2 delta = prevSample.Value.Position - firstSample.Value.Position;
                        Catapult.ShotVelocity = MinShotVelocity +
                                                Catapult.ShotStrength * (MaxShotVelocity - MinShotVelocity);
                        if (delta.Length() > 0)
                            Catapult.ShotAngle =
                                    MathHelper.Clamp((float)Math.Asin(-delta.Y / delta.Length()),
                                    MinShotAngle, MaxShotAngle);
                        else
                            Catapult.ShotAngle = MinShotAngle;
                        if (App.g_isTwoHumanPlayers)
                        {
                            GlobalContext.fireNumber = Convert.ToInt32(GlobalContext.tableProperties["fireNumber"]);
                            GlobalContext.fireNumber++;
                            Dictionary<string, object> fireProperties = new Dictionary<string, object>();
                            fireProperties.Add("fireNumber", GlobalContext.fireNumber);
                            WarpClient.GetInstance().UpdateRoomProperties(GlobalContext.GameRoomId, fireProperties, null);
                            WarpClient.GetInstance().SendUpdatePeers(MoveMessage.buildSHOTMessageBytes(Catapult.ShotVelocity.ToString(), Catapult.ShotAngle.ToString()));
                            //Here i am resetting this counter because host user has sent his final shot via TCP so in next 
                            //step remote user will send his data,before receive remote user UDP packets we need to reset this counter
                            GlobalContext.prevUDPPacketNumber = 0;
                        }
                            Catapult.CurrentState = CatapultState.Firing;
                    }
                    // turn off dragging state
                    ResetDragState();
                }
            }
        }
        #endregion
        public override void Update(GameTimerEventArgs gameTime)
        {
            // Check if it is time to take a shot  
            if (!IsActive&&App.g_isTwoHumanPlayers)
            {
                msg = MoveMessage.GetCurrentInstance();
                if (msg.Type.Equals("SHOT"))
                {
                    remoteFirstSample =Vector2.Zero;
                    Catapult.ShotVelocity = (float)Convert.ToDouble(msg.ShotVelocity);
                    Catapult.ShotAngle = (float)Convert.ToDouble(msg.ShotAngle);
                    Catapult.CurrentState = CatapultState.Firing;
                    ResetDragState();
                }
                else if (msg.Type.Equals("DRAGGING"))
                {
                    // If drag just began save the sample for future 
                    // calculations and start Aim "animation"
                    remoteCurrentSample = new Vector2();
                    remoteCurrentSample.X = (float)Convert.ToDouble(msg.X);
                    remoteCurrentSample.Y = (float)Convert.ToDouble(msg.Y);
                    if (Vector2.Zero == remoteFirstSample)
                    {
                        remoteFirstSample = remoteCurrentSample;
                        Catapult.CurrentState = CatapultState.Aiming;
                    }

                    // save the current gesture sample 
                    remotePrevSample = remoteCurrentSample;

                    // calculate the delta between first sample and current
                    // sample to present visual sound on screen
                    Vector2 delta = remotePrevSample -
                        remoteFirstSample;
                    Catapult.ShotStrength = delta.Length() / maxDragDelta;

                    Catapult.ShotVelocity = MinShotVelocity +
                        Catapult.ShotStrength * (MaxShotVelocity - MinShotVelocity);

                    if (delta.Length() > 0)
                        Catapult.ShotAngle =
                                MathHelper.Clamp((float)Math.Asin(-delta.Y / delta.Length()),
                                MinShotAngle, MaxShotAngle);
                    else
                        Catapult.ShotAngle = MinShotAngle;

                    float baseScale = 0.001f;
                    arrowScale = baseScale * delta.Length();
                    isDragging = true;
                }
            }
           
            Catapult.Update(gameTime);
        }
        #region Draw
        public override void Draw(GameTimerEventArgs gameTime)
        {
            if (isDragging)
            {
                DrawGuide();
                DrawDragArrow(arrowScale);
            }
            Catapult.Draw(gameTime);
        }

        public void DrawDragArrow(float arrowScale)
        {
            spriteBatch.Draw(arrow, catapultPosition + new Vector2(0, -40),
                null, playerSide == PlayerSide.Left ? Color.Blue : Color.Red , 
                playerSide == PlayerSide.Left ? -Catapult.ShotAngle : 
                Catapult.ShotAngle,
                playerSide == PlayerSide.Left ? new Vector2(34, arrow.Height / 2) :
                new Vector2(725, arrow.Height / 2),
                new Vector2(arrowScale, 0.1f),
                playerSide == PlayerSide.Left ? SpriteEffects.None : SpriteEffects.FlipHorizontally
                , 0);
        }

        /// <summary>
        /// Draws a guide line which shows the course of the shot
        /// </summary>
        public void DrawGuide()
        {
            guideProjectile.ProjectilePosition = Catapult.ProjectileStartPosition;

            Single direction = playerSide == PlayerSide.Left ? 1 : -1;

            guideProjectile.Fire(Catapult.ShotVelocity * (float)Math.Cos(Catapult.ShotAngle),
                                 Catapult.ShotVelocity * (float)Math.Sin(Catapult.ShotAngle));           

            while (guideProjectile.State == ProjectileState.InFlight)
            {
                guideProjectile.UpdateProjectileFlightData(0.1f, 
                    Catapult.Wind,
                    Catapult.Gravity);

                spriteBatch.Draw(guideDot, guideProjectile.ProjectilePosition, null,
                    playerSide == PlayerSide.Left ? Color.Blue : Color.Red, 0f, 
                    Vector2.Zero, 1f, spriteEffect, 0);
            }
        }
        #endregion

        /// <summary>
        /// Turn off dragging state and reset drag related variables
        /// </summary>
        public void ResetDragState()
        {
            firstSample = null;
            prevSample = null;
            isDragging = false;
            arrowScale = 0;
            Catapult.ShotStrength = 0;
            deltaSum = Vector2.Zero;
        }
    }
}
