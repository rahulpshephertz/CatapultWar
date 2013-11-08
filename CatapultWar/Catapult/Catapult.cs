#region File Description
//-----------------------------------------------------------------------------
// Catapult.cs
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
using Microsoft.Devices;
using System.Xml.Linq;
using Microsoft.Xna.Framework.Content;
using com.shephertz.app42.gaming.multiplayer.client;
#endregion

namespace CatapultWar
{
    #region Catapult states definition enum
    public enum CatapultState
    {
        Idle,
        Aiming,
        Firing,
        ProjectileFlying,
        ProjectileHit,
        HitKill,
        HitDamage,
        Reset,
        Stalling,
        ProjectilesFalling
    }

    enum HitCheckResult
    {
        Nothing,
        SelfCatapult,
        EnemyCatapult,
        SelfCrate,
        EnemyCrate
    }
    #endregion

    public class Catapult 
    {
        #region Variables/Fields and Properties
        // Hold what the game to which the catapult belongs
        ContentManager contentManager;

        SpriteBatch spriteBatch;
        Random random;

        public bool AnimationRunning { get; set; }
        public string Name { get; set; }
        public bool IsActive { get; set; }

        // In some cases the game need to start second animation while first animation is still running;
        // this variable define at which frame the second animation should start
        Dictionary<string, int> splitFrames;

        Texture2D idleTexture;
        Dictionary<string, Animation> animations;

        SpriteEffects spriteEffects;

        // Projectiles
        const int MaxActiveProjectiles = 3;

        Projectile normalProjectile;
        Projectile splitProjectile;

        List<Projectile> activeProjectiles; // Projectiles which are active
        List<Projectile> activeProjectilesCopy; // Copy of the above list
        List<Projectile> destroyedProjectiles; // Projectiles which are active

        // Supply crate
        SupplyCrate crate;

        string idleTextureName;
        bool isLeftSide;
        bool isHuman;

        // Game constants
        public const float Gravity = 500f;

        // State of the catapult during its last update
        CatapultState lastUpdateState = CatapultState.Idle;

        // Used to stall animations
        int stallUpdateCycles;

        // Current state of the Catapult
        CatapultState currentState;
        public CatapultState CurrentState
        {
            get { return currentState; }
            set { currentState = value; }
        }

        float wind;
        public float Wind
        {
            set
            {
                wind = value;
            }
            get
            {
                return wind;
            }
        }

        Player enemy;
        internal Player Enemy
        {
            set
            {
                enemy = value;
            }
        }

        Player self;
        internal Player Self
        {
            set
            {
                self = value;
            }
        }

        Vector2 catapultPosition;
        public Vector2 Position
        {
            get
            {
                return catapultPosition;
            }
        }

        /// <summary>
        /// Describes how powerful the current shot being fired is. The more powerful
        /// the shot, the further it goes. 0 is the weakest, 1 is the strongest.
        /// </summary>
        public float ShotStrength { get; set; }

        public float ShotVelocity { get; set; }

        // <summary>
        /// The angle at which projectiles are fired, in radians.
        /// </summary>
        public float ShotAngle { get; set; }

        public Vector2 ProjectileStartPosition { get; private set; }

        public int GroundHitOffset
        {
            get
            {
                return animations["Fire"].FrameSize.Y;
            }
        }

        /// <summary>
        /// Used to determine whether or not the game is over
        /// </summary>
        public bool GameOver { get; set; }

        const int winScore = 5;
        #endregion

        #region Initialization
        public Catapult(ContentManager cm)
        {
            contentManager = cm;
        }

        public Catapult(ContentManager cm, SpriteBatch screenSpriteBatch,
          string IdleTexture,
          Vector2 CatapultPosition, SpriteEffects SpriteEffect, bool isLeftSide, bool isHuman)
            : this(cm)
        {
            contentManager = cm;
            idleTextureName = IdleTexture;
            catapultPosition = CatapultPosition;
            spriteEffects = SpriteEffect;
            spriteBatch = screenSpriteBatch;
            this.isLeftSide = isLeftSide;
            this.isHuman = isHuman;

            if (isLeftSide)
                ProjectileStartPosition = new Vector2(630, 340);
            else
                ProjectileStartPosition = new Vector2(175, 340);

            splitFrames = new Dictionary<string, int>();
            animations = new Dictionary<string, Animation>();
        }

        /// <summary>
        /// Function initializes the catapult instance and loads the animations from XML definition sheet
        /// </summary>
        public void Initialize()
        {
            // Define initial state of the catapult
            IsActive = true;
            AnimationRunning = false;
            currentState = CatapultState.Idle;
            stallUpdateCycles = 0;

            // Load multiple animations form XML definition
            XDocument doc = XDocument.Load("Content/Textures/Catapults/AnimationsDef.xml");
            XName name = XName.Get("Definition");
            var definitions = doc.Document.Descendants(name);

            // Loop over all definitions in XML
            foreach (var animationDefinition in definitions)
            {
                bool? toLoad = null;
                bool val;
                if (bool.TryParse(animationDefinition.Attribute("IsAI").Value, out val))
                    toLoad = val;

                // Check if the animation definition need to be loaded for current catapult
                if (toLoad == isLeftSide || null == toLoad)
                {
                    // Get a name of the animation
                    string animatonAlias = animationDefinition.Attribute("Alias").Value;
                    Texture2D texture =
                        contentManager.Load<Texture2D>(animationDefinition.Attribute("SheetName").Value);

                    // Get the frame size (width & height)
                    Point frameSize = new Point();
                    frameSize.X = int.Parse(animationDefinition.Attribute("FrameWidth").Value);
                    frameSize.Y = int.Parse(animationDefinition.Attribute("FrameHeight").Value);

                    // Get the frames sheet dimensions
                    Point sheetSize = new Point();
                    sheetSize.X = int.Parse(animationDefinition.Attribute("SheetColumns").Value);
                    sheetSize.Y = int.Parse(animationDefinition.Attribute("SheetRows").Value);

                    // If definition has a "SplitFrame" - means that other animation should start here - load it
                    if (null != animationDefinition.Attribute("SplitFrame"))
                        splitFrames.Add(animatonAlias,
                            int.Parse(animationDefinition.Attribute("SplitFrame").Value));

                    // Defing animation speed
                    TimeSpan frameInterval = TimeSpan.FromSeconds((float)1 /
                        int.Parse(animationDefinition.Attribute("Speed").Value));

                    Animation animation = new Animation(texture, frameSize, sheetSize);

                    // If definition has an offset defined - means that it should be rendered relatively
                    // to some element/other animation - load it
                    if (null != animationDefinition.Attribute("OffsetX") &&
                      null != animationDefinition.Attribute("OffsetY"))
                    {
                        animation.Offset = new Vector2(int.Parse(animationDefinition.Attribute("OffsetX").Value),
                            int.Parse(animationDefinition.Attribute("OffsetY").Value));
                    }

                    animations.Add(animatonAlias, animation);
                }
            }

            // Load the textures
            idleTexture = contentManager.Load<Texture2D>(idleTextureName);

            activeProjectiles = new List<Projectile>(MaxActiveProjectiles);
            activeProjectilesCopy = new List<Projectile>(MaxActiveProjectiles);
            destroyedProjectiles = new List<Projectile>(MaxActiveProjectiles);

            normalProjectile = new Projectile(contentManager, spriteBatch, activeProjectiles,
                "Textures/Ammo/rock_ammo", ProjectileStartPosition,
                animations["Fire"].FrameSize.Y, isLeftSide, Gravity);
            normalProjectile.Initialize();

            splitProjectile = new SplitProjectile(contentManager, spriteBatch, activeProjectiles,
                "Textures/Ammo/split_ammo", ProjectileStartPosition,
                animations["Fire"].FrameSize.Y, isLeftSide, Gravity);
            splitProjectile.Initialize();

            crate = new SupplyCrate(contentManager, spriteBatch, "Textures/Crate/box",
                Position + new Vector2(animations["Fire"].FrameSize.X / 2, 0), isLeftSide);
            crate.Initialize();

            // Initialize randomizer
            random = new Random();
        }
        #endregion

        #region Update and Render
        public void Update(GameTimerEventArgs gameTime)
        {
            bool startStall;
            CatapultState postUpdateStateChange = 0;

            if (gameTime == null)
                throw new ArgumentNullException("gameTime");

            // The catapult is inactive, so there is nothing to update
            if (!IsActive)
            {
               // base.Update(gameTime);
                return;
            }

            switch (currentState)
            {
                case CatapultState.Idle:
                    // Nothing to do
                    break;
                case CatapultState.Aiming:
                    if (lastUpdateState != CatapultState.Aiming)
                    {
                        AudioManager.PlaySound("ropeStretch", true);

                        AnimationRunning = true;
                        if (isLeftSide == true && !isHuman)
                        {
                            animations["Aim"].PlayFromFrameIndex(0);
                            stallUpdateCycles = 20;
                            startStall = false;
                        }
                    }

                    // Progress Aiming "animation"
                    if (isHuman)
                    {
                        UpdateAimAccordingToShotStrength();
                    }
                    else if (isLeftSide && !isHuman)
                    {
                        animations["Aim"].Update();
                        startStall = AimReachedShotStrength();
                        currentState = (startStall) ?
                            CatapultState.Stalling : CatapultState.Aiming;
                    }
                    break;
                case CatapultState.Stalling:
                    if (stallUpdateCycles-- <= 0)
                    {
                        // We've finished stalling, fire the projectile
                        postUpdateStateChange = CatapultState.Firing;
                    }
                    break;
                case CatapultState.Firing:
                    // Progress Fire animation
                    if (lastUpdateState != CatapultState.Firing)
                    {
                        AudioManager.StopSound("ropeStretch");
                        AudioManager.PlaySound("catapultFire");
                        StartFiringFromLastAimPosition();
                    }

                    animations["Fire"].Update();

                    // If in the "split" point of the animation start 
                    // projectile fire sequence
                    if (animations["Fire"].FrameIndex == splitFrames["Fire"])
                    {
                        Fire(ShotVelocity, ShotAngle);
                    }
                    if (animations["Fire"].IsActive == false)
                    {
                        postUpdateStateChange = CatapultState.ProjectilesFalling;
                    }
                    break;
                case CatapultState.ProjectilesFalling:
                    // End turn if all projectiles have been destroyed
                    if (activeProjectiles.Count == 0)
                    {
                        postUpdateStateChange = CatapultState.Reset;
                    }
                    break;
                case CatapultState.HitDamage:
                    if (animations["hitSmoke"].IsActive == false)
                        postUpdateStateChange = CatapultState.Reset;

                    animations["hitSmoke"].Update();

                    break;
                case CatapultState.HitKill:
                    // Progress hit animation
                    if ((animations["Destroyed"].IsActive == false) &&
                        (animations["hitSmoke"].IsActive == false))
                    {
                        if (enemy.Score >= winScore)
                        {
                            GameOver = true;
                            break;
                        }
                        self.Health = 100;
                        postUpdateStateChange = CatapultState.Reset;
                    }

                    animations["Destroyed"].Update();
                    animations["hitSmoke"].Update();

                    break;
                case CatapultState.Reset:
                    AnimationRunning = false;
                    break;
                default:
                    break;
            }

            lastUpdateState = currentState;
            if (postUpdateStateChange != 0)
            {
                currentState = postUpdateStateChange;
            }

            // Update active projectiles
            destroyedProjectiles.Clear(); // Clean swap list
            activeProjectilesCopy.Clear();

            // Copy the projectile list so that it may be modified while updating
            activeProjectilesCopy.AddRange(activeProjectiles);

            foreach (var projectile in activeProjectilesCopy)
            {
                projectile.Update(gameTime);

                // If the projectile hit the ground
                if ((projectile.State == ProjectileState.HitGround) &&
                    (projectile.HitHandled == false))
                {
                    HandleProjectileHit(projectile);
                }
                if (projectile.State == ProjectileState.Destroyed)
                {
                    destroyedProjectiles.Add(projectile);
                }
            }

            // Filter out destroyed projectiles
            foreach (var projectile in destroyedProjectiles)
            {
                activeProjectiles.Remove(projectile);
            }

            // Update crate
            crate.Update(gameTime);
        }

        /// <summary>
        /// Used to check if the current aim animation frame represents the shot
        /// strength set for the catapult.
        /// </summary>
        /// <returns>True if the current frame represents the shot strength,
        /// false otherwise.</returns>
        private bool AimReachedShotStrength()
        {
            return (animations["Aim"].FrameIndex ==
                (Convert.ToInt32(animations["Aim"].FrameCount * ShotStrength) - 1));
        }

        private void UpdateAimAccordingToShotStrength()
        {
            var aimAnimation = animations["Aim"];
            int frameToDisplay =
                Convert.ToInt32(aimAnimation.FrameCount * ShotStrength);
            aimAnimation.FrameIndex = frameToDisplay;
        }

        /// <summary>
        /// Calculates the frame from which to start the firing animation, 
        /// and activates it.
        /// </summary>
        private void StartFiringFromLastAimPosition()
        {
            int startFrame = animations["Aim"].FrameCount -
                animations["Aim"].FrameIndex;
            animations["Fire"].PlayFromFrameIndex(startFrame);
        }

        public void Draw(GameTimerEventArgs gameTime)
        {
            if (gameTime == null)
                throw new ArgumentNullException("gameTime");

            // Using the last update state makes sure we do not draw
            // before updating animations properly
            switch (lastUpdateState)
            {
                case CatapultState.ProjectilesFalling:
                case CatapultState.Idle:
                case CatapultState.Reset:
                    DrawIdleCatapult();
                    break;
                case CatapultState.Aiming:
                case CatapultState.Stalling:
                    animations["Aim"].Draw(spriteBatch, catapultPosition,
                        spriteEffects);
                    break;
                case CatapultState.Firing:
                    animations["Fire"].Draw(spriteBatch, catapultPosition,
                        spriteEffects);
                    break;
                case CatapultState.HitDamage:
                    // Draw the catapult
                    DrawIdleCatapult();
                    break;
                case CatapultState.HitKill:
                    // Catapult hit animation
                    animations["Destroyed"].Draw(spriteBatch, catapultPosition,
                        spriteEffects);
                    break;
                default:
                    break;
            }

            // Draw projectiles
            foreach (var projectile in activeProjectiles)
            {
                projectile.Draw(gameTime);
            }

            // Draw crate
            crate.Draw(gameTime);
        }
        #endregion

        #region Hit
        /// <summary>
        /// Performs all logic necessary when a projectile hits the ground.
        /// </summary>
        /// <param name="projectile"></param>
        private void HandleProjectileHit(Projectile projectile)
        {
            projectile.HitHandled = true;

            switch (CheckHit(projectile))
            {
                case HitCheckResult.SelfCrate:
                // Ignore self crate hits
                case HitCheckResult.Nothing:
                    PerformNothingHit(projectile);
                    break;
                case HitCheckResult.SelfCatapult:
                    if ((CurrentState == CatapultState.HitKill) ||
                        (CurrentState == CatapultState.HitDamage))
                    {
                        projectile.HitAnimation = animations["hitSmoke"];
                    }
                    break;
                case HitCheckResult.EnemyCatapult:
                    if ((enemy.Catapult.CurrentState == CatapultState.HitKill) ||
                        (enemy.Catapult.CurrentState == CatapultState.HitDamage))
                    {
                        projectile.HitAnimation = animations["hitSmoke"];
                    }
                    else
                    {
                        PerformNothingHit(projectile);
                    }
                    break;
                case HitCheckResult.EnemyCrate:
                    if (enemy.Catapult.crate.CurrentState == CrateState.Idle)
                    {
                        AudioManager.PlaySound("catapultExplosion");
                        projectile.HitAnimation = animations["hitSmoke"];
                        enemy.Catapult.crate.Hit();
                        self.Weapon = WeaponType.Split;
                    }
                    else
                    {
                        PerformNothingHit(projectile);
                    }
                    break;
                default:
                    throw new InvalidOperationException("Hit invalid entity");
            }

            projectile.HitAnimation.PlayFromFrameIndex(0);
        }

        private void PerformNothingHit(Projectile projectile)
        {
            VibrateController.Default.Start(TimeSpan.FromMilliseconds(100));
            // Play hit sound only on a missed hit,
            // a direct hit will trigger the explosion sound
            AudioManager.PlaySound("boulderHit");
            projectile.HitAnimation = animations["fireMiss"];
        }

        /// <summary>
        /// Start Hit sequence on catapult - could be executed on self or from enemy in case of hit
        /// </summary>
        public void Hit(bool isKilled)
        {
            AnimationRunning = true;
            if (isKilled)
                animations["Destroyed"].PlayFromFrameIndex(0);

            animations["hitSmoke"].PlayFromFrameIndex(0);

            if (isKilled)
                currentState = CatapultState.HitKill;
            else
                currentState = CatapultState.HitDamage;

            self.Weapon = WeaponType.Normal;
        }
        #endregion

        public void Fire(float velocity, float angle)
        {
            Projectile firedProjectile = null;

            switch (self.Weapon)
            {
                case WeaponType.Normal:
                    firedProjectile = normalProjectile;
                    break;
                case WeaponType.Split:
                    firedProjectile = splitProjectile;
                    break;
                default:
                    throw new InvalidOperationException("Firing invalud ammunition");
            }

            // Fire the projectile
            firedProjectile.ProjectilePosition = firedProjectile.ProjectileStartPosition;
            firedProjectile.Fire(
                velocity * (float)Math.Cos(angle),
                velocity * (float)Math.Sin(angle));
            firedProjectile.Wind = wind;
            activeProjectiles.Add(firedProjectile);
        }

        #region Helper Functions
        /// <summary>
        /// Check what a projectile hit. The possibilities are:
        /// Nothing hit, Hit enemy, Hit self, hit own/enemy's crate.
        /// </summary>
        /// <param name="projectile">The projectile for which to 
        /// perform the check.</param>
        /// <returns>A result inidicating what, if anything, was hit</returns>
        private HitCheckResult CheckHit(Projectile projectile)
        {
            HitCheckResult hitRes = HitCheckResult.Nothing;

            // Build a sphere around a projectile
            Vector3 center = new Vector3(projectile.ProjectilePosition, 0);
            BoundingSphere sphere = new BoundingSphere(center,
                Math.Max(projectile.ProjectileTexture.Width / 2,
                projectile.ProjectileTexture.Height / 2));

            // Check Self-Hit - create a bounding box around self
            Vector3 min = new Vector3(catapultPosition, 0);
            Vector3 max = new Vector3(catapultPosition +
                new Vector2(animations["Fire"].FrameSize.X,
                    animations["Fire"].FrameSize.Y), 0);
            BoundingBox selfBox = new BoundingBox(min, max);

            // Check enemy - create a bounding box around the enemy
            min = new Vector3(enemy.Catapult.Position, 0);
            max = new Vector3(enemy.Catapult.Position +
                new Vector2(animations["Fire"].FrameSize.X,
                    animations["Fire"].FrameSize.Y), 0);
            BoundingBox enemyBox = new BoundingBox(min, max);

            // Check self-crate - Create bounding box around own crate
            min = new Vector3(crate.Position, 0);
            max = new Vector3(crate.Position + new Vector2(crate.Width, crate.Height), 0);
            BoundingBox selfCrateBox = new BoundingBox(min, max);

            // Check enemy-crate - Create bounding box around enemy crate
            min = new Vector3(enemy.Catapult.crate.Position, 0);
            max = new Vector3(enemy.Catapult.crate.Position +
                new Vector2(enemy.Catapult.crate.Width, enemy.Catapult.crate.Height), 0);
            BoundingBox enemyCrateBox = new BoundingBox(min, max);

            // Check self hit
            if (sphere.Intersects(selfBox) && currentState != CatapultState.HitKill)
            {
                AudioManager.PlaySound("catapultExplosion");
                // Launch hit animation sequence on self
                UpdateHealth(self, sphere, selfBox);
                if (self.Health <= 0)
                {
                    Hit(true);
                    enemy.Score++;
                }

                hitRes = HitCheckResult.SelfCatapult;
            }
            // Check if enemy was hit
            else if (sphere.Intersects(enemyBox)
                && enemy.Catapult.CurrentState != CatapultState.HitKill
                && enemy.Catapult.CurrentState != CatapultState.Reset)
            {
                AudioManager.PlaySound("catapultExplosion");
                // Launch enemy hit animaton
                UpdateHealth(enemy, sphere, enemyBox);
                if (enemy.Health <= 0)
                {
                    enemy.Catapult.Hit(true);
                    if (self.IsActive)
                    {
                        self.Score++;
                        if (App.g_isTwoHumanPlayers)
                        {
                            Dictionary<string, object> scoreProperties = new Dictionary<string, object>();
                            if (GlobalContext.PlayerIsFirstOnAppWarp)
                            {
                                GlobalContext.tableProperties["Player1Score"] = self.Score;
                                scoreProperties.Add("Player1Score", self.Score);
                            }
                            else
                            {
                                scoreProperties.Add("Player2Score", self.Score);
                                GlobalContext.tableProperties["Player2Score"] = self.Score;
                            }
                            WarpClient.GetInstance().UpdateRoomProperties(GlobalContext.GameRoomId, scoreProperties, null);
                        }
                    }
                }

                hitRes = HitCheckResult.EnemyCatapult;
                currentState = CatapultState.Reset;
            }
            // Check if own crate was hit
            else if (sphere.Intersects(selfCrateBox))
            {
                hitRes = HitCheckResult.SelfCrate;
            }
            // Check if enemy crate was hit
            else if (sphere.Intersects(enemyCrateBox))
            {
                hitRes = HitCheckResult.EnemyCrate;
            }

            return hitRes;
        }

        /// <summary>
        /// Updates the health status of the player based on hit area
        /// </summary>
        /// <param name="enemy"></param>
        private void UpdateHealth(Player player, BoundingSphere projectile, BoundingBox catapult)
        {
            bool isHit = false;

            float midPoint = (catapult.Max.X - catapult.Min.X) / 2;
            BoundingBox catapultCenter = new BoundingBox(
                new Vector3(catapult.Min.X + midPoint - projectile.Radius, projectile.Center.Y - projectile.Radius, 0),
                new Vector3(catapult.Min.X + midPoint + projectile.Radius, projectile.Center.Y + projectile.Radius, 0));

            BoundingBox catapultLeft = new BoundingBox(
                new Vector3(catapult.Min.X, projectile.Center.Y - projectile.Radius, 0),
                new Vector3(catapult.Min.X + midPoint - projectile.Radius, projectile.Center.Y + projectile.Radius, 0));

            BoundingBox catapultRight = new BoundingBox(
                new Vector3(catapult.Min.X + midPoint + projectile.Radius, projectile.Center.Y - projectile.Radius, 0),
                new Vector3(catapult.Max.X, projectile.Center.Y + projectile.Radius, 0));

            if (projectile.Intersects(catapultCenter))
            {
                player.Health -= 75;
                isHit = true;
            }
            else if (projectile.Intersects(catapultLeft))
            {
                player.Health -= isLeftSide ? 50 : 25;
                isHit = true;
            }
            else if (projectile.Intersects(catapultRight))
            {
                player.Health -= isLeftSide ? 25 : 50;
                isHit = true;
            }

            if (isHit)
            {
                player.Catapult.Hit(false);

                // Catapult hit - start longer vibration on any catapult hit 
                VibrateController.Default.Start(
                    TimeSpan.FromMilliseconds(250));
            }
        }

        /// <summary>
        /// Draw catapult in Idle state
        /// </summary>
        private void DrawIdleCatapult()
        {
            spriteBatch.Draw(idleTexture, catapultPosition, null, Color.White,
              0.0f, Vector2.Zero, 1.0f,
              spriteEffects, 0);
        }

        public void ForcedTosetGameOver()
        {
            GameOver = true;
        }
        #endregion

    }
}
