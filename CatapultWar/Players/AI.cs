#region File Description
//-----------------------------------------------------------------------------
// AI.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region File Information
//-----------------------------------------------------------------------------
// AI.cs
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
using Microsoft.Xna.Framework.Content;
#endregion

namespace CatapultWar
{
    class AI : Player
    {
        #region Fields
        Random random;
        #endregion

        #region Initialization
        public AI(ContentManager cm)
            : base(cm)
        {
        }

        public AI(ContentManager cm, SpriteBatch screenSpriteBatch)
            : base(cm, screenSpriteBatch)
        {
            Catapult = new Catapult(cm, screenSpriteBatch,
                            "Textures/Catapults/Red/redIdle/redIdle",
                            new Vector2(600, 332), SpriteEffects.FlipHorizontally, true, false);
        }

        public override void Initialize()
        {
            //Initialize randomizer
            random = new Random();
            Catapult.Initialize();

        }
        #endregion

        #region Update
        public override void Update(GameTimerEventArgs gameTime)
        {
            // Check if it is time to take a shot
           
            if (Catapult.CurrentState == CatapultState.Aiming &&
                !Catapult.AnimationRunning)
            {
                // Fire at a random strength and angle
                float shotVelocity =
                    random.Next((int)MinShotVelocity, (int)MaxShotVelocity);
                float shotAngle = MinShotAngle +
                    (float)random.NextDouble() * (MaxShotAngle - MinShotAngle);

                Catapult.ShotStrength = (shotVelocity / MaxShotVelocity);
                Catapult.ShotVelocity = shotVelocity;
                Catapult.ShotAngle = shotAngle;
            }
            Catapult.Update(gameTime);
        }
        public override void Draw(GameTimerEventArgs gameTime)
        {
            // Draw related catapults
            Catapult.Draw(gameTime);

        }
        #endregion
    }
}
