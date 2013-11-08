#region File Description
//-----------------------------------------------------------------------------
// Projectile.cs
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
    public enum ProjectileState
    {
        InFlight,
        HitGround,
        Destroyed
    }

    class Projectile
    {
        #region Fields/Properties
        protected SpriteBatch spriteBatch;
        protected ContentManager contentManager;

        // List of currently active projectiles. This allows projectiles
        // to spawn other projectiles.
        protected List<Projectile> activeProjectiles;

        // Texture name for projectile
        string textureName;

        // Movement related fields
        protected Vector2 projectileInitialVelocity = Vector2.Zero;
        protected Vector2 projectileRotationPosition = Vector2.Zero;

        protected float gravity;
        public virtual float Wind { get; set; }

        protected float flightTime;

        protected float projectileRotation;

        // State related fields
        protected bool isRightPlayer;


        protected float hitOffset;

        Vector2 projectileStartPosition;
        public Vector2 ProjectileStartPosition
        {
            get
            {
                return projectileStartPosition;
            }
            set
            {
                projectileStartPosition = value;
            }
        }

        Vector2 currentVelocity = Vector2.Zero;
        public Vector2 CurrentVelocity
        {
            get
            {
                return currentVelocity;
            }
        }

        Vector2 projectilePosition = Vector2.Zero;
        public Vector2 ProjectilePosition
        {
            get
            {
                return projectilePosition;
            }
            set
            {
                projectilePosition = value;
            }
        }

        /// <summary>
        /// Gets the position where the projectile hit the ground.
        /// Only valid after a hit occurs.
        /// </summary>
        public Vector2 ProjectileHitPosition { get; private set; }

        public ProjectileState State { get; private set; }

        Texture2D projectileTexture;
        public Texture2D ProjectileTexture
        {
            get
            {
                return projectileTexture;
            }
            set
            {
                projectileTexture = value;
            }
        }

        /// <summary>
        /// This property can be used to set a hit animation for the projectile.
        /// Must be set and manually initialized before the projectile attempts to
        /// draw frames from the hit animation (after its state changes to "HitGround").
        /// </summary>
        public Animation HitAnimation { get; set; }

        /// <summary>
        /// Used to mark whether or not the projectile's hit was handled.
        /// </summary>
        public bool HitHandled { get; set; }
        #endregion

        #region Initialization
        public Projectile(ContentManager cm)
        {
            contentManager = cm;
        }

        public Projectile(ContentManager cm, SpriteBatch screenSpriteBatch,
            List<Projectile> activeProjectiles, 
            string textureName,
            Vector2 startPosition, float groundHitOffset, bool isRightPlayer,
            float gravity)
        {
            contentManager = cm;
            spriteBatch = screenSpriteBatch;
            this.activeProjectiles = activeProjectiles; 
            projectileStartPosition = startPosition;
            this.textureName = textureName;
            this.isRightPlayer = isRightPlayer;
            hitOffset = groundHitOffset;
            this.gravity = gravity;
        }

        public  void Initialize()
        {
            // Load a projectile texture
            projectileTexture = contentManager.Load<Texture2D>(textureName);
        }
        #endregion

        #region Render/Update
        public void Update(GameTimerEventArgs gameTime)
        {
            switch (State)
            {
                case ProjectileState.InFlight:
                    UpdateProjectileFlight(gameTime);
                    break;
                case ProjectileState.HitGround:
                    UpdateProjectileHit(gameTime);
                    break;
                default:
                    // Nothing to update in other states
                    break;
            }
        }

        /// <summary>
        /// This method is used to update the projectile after it has hit the ground.
        /// This allows derived projectile types to alter the projectile's hit
        /// phase more easily.
        /// </summary>
        protected void UpdateProjectileHit(GameTimerEventArgs gameTime)
        {
            if (HitAnimation.IsActive == false)
            {
                State = ProjectileState.Destroyed;
                return;
            }

            HitAnimation.Update();
        }

        /// <summary>
        /// This method is used to update the projectile while it is in flight.
        /// This allows derived projectile types to alter the projectile's flight
        /// phase more easily.
        /// </summary>
        protected virtual void UpdateProjectileFlight(GameTimerEventArgs gameTime)
        {
            UpdateProjectileFlightData(gameTime, Wind, gravity);
        }

        public void Draw(GameTimerEventArgs gameTime)
        {
            switch (State)
            {
                case ProjectileState.InFlight:
                    spriteBatch.Draw(projectileTexture, projectilePosition, null,
                    Color.White, projectileRotation,
                    new Vector2(projectileTexture.Width / 2,
                                projectileTexture.Height / 2),
                    1.0f, SpriteEffects.None, 0);
                    break;
                case ProjectileState.HitGround:
                    HitAnimation.Draw(spriteBatch, ProjectileHitPosition,
                        SpriteEffects.None);
                    break;
                default:
                    // Nothing to draw in this case
                    break;
            }
        }
        #endregion

        #region Public functionality
        /// <summary>
        /// Helper function - calculates the projectile position and velocity based on time.
        /// </summary>
        /// <param name="gameTime">game time information.</param>
        /// <param name="wind">The current wind.</param>
        /// <param name="gravity">The current gravity.</param>
        private void UpdateProjectileFlightData(GameTimerEventArgs gameTime, float wind, float gravity)
        {
            UpdateProjectileFlightData((float)gameTime.ElapsedTime.TotalSeconds,
                wind, gravity);
        }

        /// <summary>
        /// Helper function - calculates the projectile position and velocity based on time.
        /// </summary>
        public void UpdateProjectileFlightData(float elapsedSeconds, float wind, float gravity)
        {
            flightTime += elapsedSeconds;

            // Calculate new projectile position using standard
            // formulas, taking the wind as a force.
            int direction = isRightPlayer ? -1 : 1;

            float previousXPosition = projectilePosition.X;
            float previousYPosition = projectilePosition.Y;

            projectilePosition.X = projectileStartPosition.X +
                (direction * projectileInitialVelocity.X * flightTime) +
                0.5f * (8 * wind * (float)Math.Pow(flightTime, 2));

            currentVelocity.X = projectileInitialVelocity.X + 8 * wind * flightTime;

            projectilePosition.Y = projectileStartPosition.Y -
                (projectileInitialVelocity.Y * flightTime) +
                0.5f * (gravity * (float)Math.Pow(flightTime, 2));

            currentVelocity.Y = projectileInitialVelocity.Y - gravity * flightTime;

            // Calculate the projectile rotation
            projectileRotation += MathHelper.ToRadians(projectileInitialVelocity.X * 0.5f);

            // Check if projectile hit the ground or even passed it 
            // (could happen during normal calculation)
            if (projectilePosition.Y >= 332 + hitOffset)
            {
                projectilePosition.X = previousXPosition;
                projectilePosition.Y = previousYPosition;

                ProjectileHitPosition = new Vector2(previousXPosition, 332);

                State = ProjectileState.HitGround;  
            }           
        }

        public void Fire(float velocityX, float velocityY)
        {
            // Set initial projectile velocity
            projectilePosition = projectileStartPosition;
            projectileInitialVelocity.X = velocityX;
            projectileInitialVelocity.Y = velocityY;
            currentVelocity.X = velocityX;
            currentVelocity.Y = velocityY;
            // Reset calculation variables
            flightTime = 0;
            State = ProjectileState.InFlight;
            HitHandled = false;
        }
        #endregion
    }
}
