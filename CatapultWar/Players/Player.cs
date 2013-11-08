#region File Description
//-----------------------------------------------------------------------------
// Player.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region File Information
//-----------------------------------------------------------------------------
// Player.cs
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
    #region Player weapon enum definitions
    public enum WeaponType
    {
        Normal,
        Split
    }
    #endregion

    internal abstract class Player
    {
        #region Variables/Fields
        protected SpriteBatch spriteBatch;
        ContentManager contentManager;
        // Constants used for calculating shot strength
        public const float MinShotVelocity = 200f;
        public const float MaxShotVelocity = 665f;
        public const float MinShotAngle = 0; // 0 degrees
        public const float MaxShotAngle = 1.3962634f; // 80 degrees

        // Public variables used by Gameplay class
        public Catapult Catapult { get; set; }
        public int Score { get; set; }
        public string Name { get; set; }
        public int Health { get; set; }

        public Player Enemy
        {
            set
            {
                Catapult.Enemy = value;
                Catapult.Self = this;
            }
        }

        public bool IsActive { get; set; }

        public WeaponType Weapon { get; set; }
        #endregion

        #region Initialization
        public Player(ContentManager cm)
        {
            contentManager = cm;
        }

        public Player(ContentManager cm, SpriteBatch screenSpriteBatch)
            : this(cm)
        {
            spriteBatch = screenSpriteBatch;
            Score = 0;
            Health = 100;

            Weapon = WeaponType.Normal;
        }
        public abstract void Initialize();
        #endregion

        #region Update and Render
        public abstract void Update(GameTimerEventArgs gameTime);
        public abstract void Draw(GameTimerEventArgs gameTime);
      
        #endregion
    }
}
