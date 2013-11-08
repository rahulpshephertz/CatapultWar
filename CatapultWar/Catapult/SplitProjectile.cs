#region File Description
//-----------------------------------------------------------------------------
// SplitProjectile.cs
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
    class SplitProjectile : Projectile
    {
        #region Fields
        Projectile subProjectile1;
        Projectile subProjectile2;
        Projectile subProjectile3;

        public override float Wind
        {
            get
            {
                return base.Wind;
            }
            set
            {
                subProjectile1.Wind = value;
                subProjectile2.Wind = value;
                subProjectile3.Wind = value;
                base.Wind = value;
            }
        }
        #endregion

        #region Initialization
        public SplitProjectile(ContentManager cm) : base(cm)
        {
        }

        public SplitProjectile(ContentManager cm, SpriteBatch screenSpriteBatch, 
            List<Projectile> activeProjectiles, string textureName, 
            Vector2 startPosition, float groundHitOffset, bool isAI, 
            float gravity) : 
            base(cm, screenSpriteBatch, activeProjectiles, textureName, startPosition,
            groundHitOffset, isAI, gravity)
        {
            Initialize();
        }

        public void Initialize()
        {
            subProjectile1 = new Projectile(contentManager, spriteBatch, activeProjectiles,
                "Textures/Ammo/rock_ammo", ProjectileStartPosition, hitOffset, 
                isRightPlayer, gravity);
            subProjectile1.Initialize();

            subProjectile2 = new Projectile(contentManager, spriteBatch, activeProjectiles,
                "Textures/Ammo/rock_ammo", ProjectileStartPosition, hitOffset,
                isRightPlayer, gravity);
            subProjectile2.Initialize();

            subProjectile3 = new Projectile(contentManager, spriteBatch, activeProjectiles,
                "Textures/Ammo/rock_ammo", ProjectileStartPosition, hitOffset,
                isRightPlayer, gravity);
            subProjectile3.Initialize();
        }

        #endregion

        /// <summary>
        /// Overrides the default flight update method to have the projectile split
        /// into 3 mid-flight.
        /// </summary>
        /// <param name="gameTime"></param>
        protected  void UpdateProjectileFlight(GameTimerEventArgs gameTime)
        {
            base.UpdateProjectileFlight(gameTime);

            float projectileYCurrentSpeed = projectileInitialVelocity.Y - gravity * flightTime;

            // If the projectile starts falling down, split it into three projectiles
            if (projectileYCurrentSpeed <= 0)
            {
                activeProjectiles.Clear();

                if (isRightPlayer)
                {
                    subProjectile1.ProjectileStartPosition = ProjectilePosition +
                        new Vector2(-30, 8);
                    subProjectile1.Fire(CurrentVelocity.X + 40, 
                        CurrentVelocity.Y);
                    subProjectile2.ProjectileStartPosition = ProjectilePosition +
                        new Vector2(-15, 4);
                    subProjectile2.Fire(CurrentVelocity.X, CurrentVelocity.Y);
                    subProjectile3.ProjectileStartPosition = ProjectilePosition +
                        new Vector2(-0, 0);
                    subProjectile3.Fire(CurrentVelocity.X - 20,
                        CurrentVelocity.Y);
                }
                else
                {
                    subProjectile1.ProjectileStartPosition = ProjectilePosition +
                        new Vector2(30, 8);
                    subProjectile1.Fire(CurrentVelocity.X + 40,
                        CurrentVelocity.Y);
                    subProjectile2.ProjectileStartPosition = ProjectilePosition +
                        new Vector2(15, 4);
                    subProjectile2.Fire(CurrentVelocity.X, CurrentVelocity.Y);
                    subProjectile3.ProjectileStartPosition = ProjectilePosition +
                        new Vector2(0, 0);
                    subProjectile3.Fire(CurrentVelocity.X - 20,
                        CurrentVelocity.Y);
                }

                activeProjectiles.Add(subProjectile1);
                activeProjectiles.Add(subProjectile2);
                activeProjectiles.Add(subProjectile3);
            }
        }
    }
}
