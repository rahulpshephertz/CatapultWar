using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input.Touch;
using GameStateManagement;
using com.shephertz.app42.gaming.multiplayer.client;

namespace CatapultWar
{
    public partial class GamePage : PhoneApplicationPage
    {
        #region Properties


        /// <summary>
        /// Normally when one screen is brought up over the top of another,
        /// the first screen will transition off to make room for the new
        /// one. This property indicates whether the screen is only a small
        /// popup, in which case screens underneath it do not need to bother
        /// transitioning off.
        /// </summary>
        public bool IsPopup
        {
            get { return isPopup; }
            protected set { isPopup = value; }
        }

        bool isPopup = false;


        /// <summary>
        /// Indicates how long the screen takes to
        /// transition on when it is activated.
        /// </summary>
        public TimeSpan TransitionOnTime
        {
            get { return transitionOnTime; }
            protected set { transitionOnTime = value; }
        }

        TimeSpan transitionOnTime = TimeSpan.Zero;


        /// <summary>
        /// Indicates how long the screen takes to
        /// transition off when it is deactivated.
        /// </summary>
        public TimeSpan TransitionOffTime
        {
            get { return transitionOffTime; }
            protected set { transitionOffTime = value; }
        }

        TimeSpan transitionOffTime = TimeSpan.Zero;


        /// <summary>
        /// Gets the current position of the screen transition, ranging
        /// from zero (fully active, no transition) to one (transitioned
        /// fully off to nothing).
        /// </summary>
        public float TransitionPosition
        {
            get { return transitionPosition; }
            protected set { transitionPosition = value; }
        }

        float transitionPosition = 1;


        /// <summary>
        /// There are two possible reasons why a screen might be transitioning
        /// off. It could be temporarily going away to make room for another
        /// screen that is on top of it, or it could be going away for good.
        /// This property indicates whether the screen is exiting for real:
        /// if set, the screen will automatically remove itself as soon as the
        /// transition finishes.
        /// </summary>
        public bool IsExiting
        {
            get { return isExiting; }
            protected internal set { isExiting = value; }
        }

        bool isExiting = false;

        bool otherScreenHasFocus;

        /// <summary>
        /// Gets the index of the player who is currently controlling this screen,
        /// or null if it is accepting input from any player. This is used to lock
        /// the game to a specific player profile. The main menu responds to input
        /// from any connected gamepad, but whichever player makes a selection from
        /// this menu is given control over all subsequent screens, so other gamepads
        /// are inactive until the controlling player returns to the main menu.
        /// </summary>
        public PlayerIndex? ControllingPlayer
        {
            get { return controllingPlayer; }
            internal set { controllingPlayer = value; }
        }

        PlayerIndex? controllingPlayer;

        /// <summary>
        /// Gets the gestures the screen is interested in. Screens should be as specific
        /// as possible with gestures to increase the accuracy of the gesture engine.
        /// For example, most menus only need Tap or perhaps Tap and VerticalDrag to operate.
        /// These gestures are handled by the ScreenManager when screens change and
        /// all gestures are placed in the InputState passed to the HandleInput method.
        /// </summary>
        public GestureType EnabledGestures
        {
            get { return enabledGestures; }
            protected set
            {
                enabledGestures = value;
                TouchPanel.EnabledGestures = value;
            }
        }

        GestureType enabledGestures = GestureType.None;

        /// <summary>
        /// Gets whether or not this screen is serializable. If this is true,
        /// the screen will be recorded into the screen manager's state and
        /// its Serialize and Deserialize methods will be called as appropriate.
        /// If this is false, the screen will be ignored during serialization.
        /// By default, all screens are assumed to be serializable.
        /// </summary>
        public bool IsSerializable
        {
            get { return isSerializable; }
            protected set { isSerializable = value; }
        }

        bool isSerializable = true;
        #endregion

        ContentManager contentManager;
        GameTimer timer;
        SpriteBatch mSpriteBatch;
        InputState mInputState;
        #region Fields
        // Texture Members
        Texture2D foregroundTexture;
        Texture2D cloud1Texture;
        Texture2D cloud2Texture;
        Texture2D mountainTexture;
        Texture2D skyTexture;
        Texture2D hudBackgroundTexture;
        Texture2D ammoTypeNormalTexture;
        Texture2D ammoTypeSplitTexture;
        Texture2D windArrowTexture;
        Texture2D defeatTexture;
        Texture2D victoryTexture;
        Texture2D blankTexture;
        SpriteFont hudFont;

        // Rendering members
        Vector2 cloud1Position;
        Vector2 cloud2Position;

        Vector2 playerOneHUDPosition;
        Vector2 playerTwoHUDPosition;
        Vector2 windArrowPosition;
        Vector2 playerOneHealthBarPosition;
        Vector2 playerTwoHealthBarPosition;
        Vector2 healthBarFullSize;
        NavigationMode mNavigationMode;
        // Gameplay members
        Human playerOne;
        Player playerTwo;
        Vector2 wind;
        bool changeTurn;
        bool isFirstPlayerTurn;
        bool isTwoHumanPlayers;
        bool gameOver;
        bool isPaused = false;
        Random random;
        const int minWind = 0;
        const int maxWind = 10;
        // Helper members
        bool isDragging;
        #endregion
        public GamePage()
        {
            InitializeComponent();

            // Get the content manager from the application
            contentManager = (Application.Current as App).Content;
             EnabledGestures = GestureType.FreeDrag |
                GestureType.DragComplete |
                GestureType.Tap;

            random = new Random();
            mInputState = new InputState();
            isTwoHumanPlayers = App.g_isTwoHumanPlayers;     
            // Create a timer for this page
            timer = new GameTimer();
            timer.UpdateInterval = TimeSpan.FromTicks(333333);
            timer.Update += OnUpdate;
            timer.Draw += OnDraw;
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SharedGraphicsDeviceManager.Current.GraphicsDevice.SetSharingMode(true);
            // Set the sharing mode of the graphics device to turn on XNA rendering          
            if (e.NavigationMode == NavigationMode.New)
            {
                // Create a new SpriteBatch, which can be used to draw textures.
                mSpriteBatch = new SpriteBatch(SharedGraphicsDeviceManager.Current.GraphicsDevice);
                LoadAssets();
                // TODO: use this.content to load your game content here
                if (isTwoHumanPlayers)
                {
                    GlobalContext.notificationListenerObj.AddCallBacks(OpponentLeftTheRoom, OpponentPaused, OpponentResumed);
                    GlobalContext.conListenObj.AddConnectionRecoverableCallbacks(ConnectionRecoverableError, ConnectionRecoverd);
                }
               
            }
            // Start the timer
            timer.Start();
            Start();
            mNavigationMode = e.NavigationMode;

           
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            // Stop the timer
            timer.Stop();
            // Set the sharing mode of the graphics device to turn off XNA rendering
            SharedGraphicsDeviceManager.Current.GraphicsDevice.SetSharingMode(false);
            if (isTwoHumanPlayers)
            {
                GlobalContext.tableProperties["Player1Score"] = 0;
                GlobalContext.tableProperties["Player2Score"] = 0;
                if (GlobalContext.joinedUsers != null)
                {
                    //Dictionary<string, object> scoreProperties = new Dictionary<string, object>();
                    //scoreProperties.Add("Player1Score", 0);
                    //scoreProperties.Add("Player2Score", 0);
                    //WarpClient.GetInstance().UpdateRoomProperties(GlobalContext.GameRoomId,scoreProperties,null);
                    WarpClient.GetInstance().LeaveRoom(GlobalContext.GameRoomId);
                    WarpClient.GetInstance().DeleteRoom(GlobalContext.GameRoomId);
                }
                playerOne.Score = playerTwo.Score = 0;
                GlobalContext.notificationListenerObj.RemoveCallBacks();
                GlobalContext.conListenObj.RemoveConnectionRecoverableCallbacks();
            }
            base.OnNavigatedFrom(e);
        }

        /// <summary>
        /// Allows the page to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        private void OnUpdate(object sender, GameTimerEventArgs e)
        {
            // TODO: Add your update logic here
            // Check it one of the players reached 5 and stop the game
            if (mNavigationMode == NavigationMode.Back)
            {
                mNavigationMode = NavigationMode.Refresh;
                NavigationService.GoBack();
                return;
            }
            float elapsed = (float)e.ElapsedTime.TotalSeconds;
            mInputState.Update();
            HandleInput(mInputState);
            if ((playerOne.Catapult.GameOver || playerTwo.Catapult.GameOver) &&
                (gameOver == false))
            {
                gameOver = true;
                if (isTwoHumanPlayers)
                {
                    if ((playerOne.Score > playerTwo.Score) && GlobalContext.PlayerIsFirstOnAppWarp)
                    {
                        AudioManager.PlaySound("gameOver_Win");
                    }
                    else
                    {
                        AudioManager.PlaySound("gameOver_Lose");
                    }
                }
                else
                {
                    if (playerOne.Score > playerTwo.Score)
                    {
                        AudioManager.PlaySound("gameOver_Win");
                    }
                    else
                    {
                        AudioManager.PlaySound("gameOver_Lose");
                    }
                }

                return;
            }

            // If Reset flag raised and both catapults are not animating - 
            // active catapult finished the cycle, new turn!
            if ((playerOne.Catapult.CurrentState == CatapultState.Reset ||
                playerTwo.Catapult.CurrentState == CatapultState.Reset) &&
                !(playerOne.Catapult.AnimationRunning ||
                playerTwo.Catapult.AnimationRunning))
            {
                if(!isTwoHumanPlayers)
                {
                    if (playerOne.IsActive == true) //Last turn was a left player turn?
                    {
                        playerOne.IsActive = false;
                        playerTwo.IsActive = true;
                        playerOne.Catapult.CurrentState = CatapultState.Idle;
                        if (!isTwoHumanPlayers)
                            playerTwo.Catapult.CurrentState = CatapultState.Aiming;
                        else
                            playerTwo.Catapult.CurrentState = CatapultState.Idle;
                    }
                    else //It was an right player turn
                    {
                        playerOne.IsActive = true;
                        playerTwo.IsActive = false;
                        playerTwo.Catapult.CurrentState = CatapultState.Idle;
                        playerOne.Catapult.CurrentState = CatapultState.Idle;
                    }
                }
                else
                {
                    playerTwo.Catapult.CurrentState = CatapultState.Idle;
                    playerOne.Catapult.CurrentState = CatapultState.Idle;
                }
                changeTurn = true;
                isFirstPlayerTurn = !isFirstPlayerTurn;
            }

            if (!isTwoHumanPlayers)
            {
                if (changeTurn)
                {
                    // Update wind
                    wind = new Vector2(random.Next(-1, 2),
                        random.Next(minWind, maxWind + 1));

                    // Set new wind value to the players and 
                    playerOne.Catapult.Wind = playerTwo.Catapult.Wind =
                        wind.X > 0 ? wind.Y : -wind.Y;
                    changeTurn = false;
                }
            }
            else if (changeTurn && ((isFirstPlayerTurn && GlobalContext.PlayerIsFirstOnAppWarp) || (!isFirstPlayerTurn && !GlobalContext.PlayerIsFirstOnAppWarp)))
            {
                // Update wind
                wind = new Vector2(random.Next(-1, 2),
                    random.Next(minWind, maxWind + 1));

                // Set new wind value to the players and 
                playerOne.Catapult.Wind = playerTwo.Catapult.Wind =
                    wind.X > 0 ? wind.Y : -wind.Y;

                Dictionary<string, object> windProperties = new Dictionary<string, object>();
                GlobalContext.tableProperties["WindX"] = wind.X;
                GlobalContext.tableProperties["WindY"] = wind.Y;
                windProperties.Add("WindX",wind.X);
                windProperties.Add("WindY", wind.Y);
                WarpClient.GetInstance().UpdateRoomProperties(GlobalContext.GameRoomId,windProperties, null);
                changeTurn = false;
            }
            else
            {
                wind.X = (float)Convert.ToDouble(GlobalContext.tableProperties["WindX"]);
                wind.Y = (float)Convert.ToDouble(GlobalContext.tableProperties["WindY"]);
                playerOne.Catapult.Wind = playerTwo.Catapult.Wind =
                         wind.X > 0 ? wind.Y : -wind.Y;
            }
            if (isTwoHumanPlayers)
            {
                if (GlobalContext.joinedUsers.Length == 2)
                {   
                    //both the players are on game so we need to update opponent's score from Appwarp server 
                    if (GlobalContext.PlayerIsFirstOnAppWarp)
                    {  
                        playerTwo.Score = Convert.ToInt32(GlobalContext.tableProperties["Player2Score"]);
                    }
                    else
                    {
                        playerOne.Score = Convert.ToInt32(GlobalContext.tableProperties["Player1Score"]);
                    }
                }
                else
                {
                   /*Game is not being played now,so update both players scores from AppWarp server
                     It is needed because for e.g. if oppnent left the room when host player score!=0
                    then for new opponent we need to reset the score of host player
                    */
                    playerOne.Health = 100;
                    playerTwo.Health = 100;
                    playerOne.Score = Convert.ToInt32(GlobalContext.tableProperties["Player1Score"]);
                    playerTwo.Score = Convert.ToInt32(GlobalContext.tableProperties["Player2Score"]);
                }
            
            }
            // Update the players
            playerOne.Update(e);
            playerTwo.Update(e);
            // Updates the clouds position
            UpdateClouds(elapsed);
        }

        #region Input
        /// <summary>
        /// Input helper method provided by GameScreen.  Packages up the various input
        /// values for ease of use.
        /// </summary>
        /// <param name="input">The state of the gamepads</param>
        public void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            if (gameOver)
            {
                if (input.IsPauseGame(null))
                {
                    FinishCurrentGame();
                }

                foreach (GestureSample gestureSample in input.Gestures)
                {
                    if (gestureSample.GestureType == GestureType.Tap)
                    {
                        FinishCurrentGame();
                    }
                }

                return;
            }

            if (input.IsPauseGame(null))
            {
                PauseCurrentGame();
            }
            else if (!isTwoHumanPlayers && isFirstPlayerTurn &&
                (playerOne.Catapult.CurrentState == CatapultState.Idle ||
                    playerOne.Catapult.CurrentState == CatapultState.Aiming))
            {
                // Read all available gestures
                foreach (GestureSample gestureSample in input.Gestures)
                {
                    if (gestureSample.GestureType == GestureType.FreeDrag)
                        isDragging = true;
                    else if (gestureSample.GestureType == GestureType.DragComplete)
                        isDragging = false;
                    playerOne.HandleInput(gestureSample);
                }
            }
            else if (!isPaused&&isTwoHumanPlayers && isFirstPlayerTurn && GlobalContext.PlayerIsFirstOnAppWarp &&(GlobalContext.joinedUsers.Length == 2)&&
               (playerOne.Catapult.CurrentState == CatapultState.Idle ||
                   playerOne.Catapult.CurrentState == CatapultState.Aiming))
            {
                // Read all available gestures
                foreach (GestureSample gestureSample in input.Gestures)
                {
                    if (gestureSample.GestureType == GestureType.FreeDrag)
                        isDragging = true;
                    else if (gestureSample.GestureType == GestureType.DragComplete)
                        isDragging = false;
                    playerOne.HandleInput(gestureSample);
                }
            }
            else if (!isPaused&&isTwoHumanPlayers && !isFirstPlayerTurn && !GlobalContext.PlayerIsFirstOnAppWarp && (GlobalContext.joinedUsers.Length == 2) &&
                (playerTwo.Catapult.CurrentState == CatapultState.Idle ||
                    playerTwo.Catapult.CurrentState == CatapultState.Aiming))
            {
                // Read all available gestures
                foreach (GestureSample gestureSample in input.Gestures)
                {
                    if (gestureSample.GestureType == GestureType.FreeDrag)
                        isDragging = true;
                    else if (gestureSample.GestureType == GestureType.DragComplete)
                        isDragging = false;
                    (playerTwo as Human).HandleInput(gestureSample);
                }
            }
        }
        #endregion
        /// <summary>
        /// Allows the page to draw itself.
        /// </summary>
        private void OnDraw(object sender, GameTimerEventArgs e)
        {
            try
            {
                if (mNavigationMode == NavigationMode.Back)
                {
                    mNavigationMode = NavigationMode.Refresh;
                    NavigationService.GoBack();
                    return;
                }
                else if (mNavigationMode == NavigationMode.Refresh)
                {
                    return;
                }
                SharedGraphicsDeviceManager.Current.GraphicsDevice.Clear(Color.CornflowerBlue);
                mSpriteBatch.Begin();
                // Render all parts of the screen
                DrawBackground();
                DrawPlayerTwo(e);
                DrawPlayerOne(e);
                DrawHud();
                mSpriteBatch.End();
            }
            catch (Exception e1)
            { 
            
            }
            // TODO: Add your drawing code here
        }
        #region Content Loading/Unloading
        /// <summary>
        /// Loads the game assets and initializes "players"
        /// </summary>
        public void LoadAssets()
        {
            // Load textures
            foregroundTexture = contentManager.Load<Texture2D>("Textures/Backgrounds/gameplay_screen");
            cloud1Texture = contentManager.Load<Texture2D>("Textures/Backgrounds/cloud1");
            cloud2Texture = contentManager.Load<Texture2D>("Textures/Backgrounds/cloud2");
            mountainTexture = contentManager.Load<Texture2D>("Textures/Backgrounds/mountain");
            skyTexture = contentManager.Load<Texture2D>("Textures/Backgrounds/sky");
            defeatTexture = contentManager.Load<Texture2D>("Textures/Backgrounds/defeat");
            victoryTexture = contentManager.Load<Texture2D>("Textures/Backgrounds/victory");
            hudBackgroundTexture = contentManager.Load<Texture2D>("Textures/HUD/hudBackground");
            windArrowTexture = contentManager.Load<Texture2D>("Textures/HUD/windArrow");
            ammoTypeNormalTexture = contentManager.Load<Texture2D>("Textures/HUD/ammoTypeNormal");
            ammoTypeSplitTexture = contentManager.Load<Texture2D>("Textures/HUD/ammoTypeSplit");
            blankTexture = contentManager.Load<Texture2D>("Textures/Backgrounds/blank");
            // Load font
            hudFont = contentManager.Load<SpriteFont>("Fonts/HUDFont");

            // Define initial cloud position
            cloud1Position = new Vector2(224 - cloud1Texture.Width, 32);
            cloud2Position = new Vector2(64, 90);

            // Define initial HUD positions
            playerOneHUDPosition = new Vector2(7, 7);
            playerTwoHUDPosition = new Vector2(613, 7);
            windArrowPosition = new Vector2(345, 46);
            Vector2 healthBarOffset = new Vector2(25, 82);
            playerOneHealthBarPosition = playerOneHUDPosition + healthBarOffset;
            playerTwoHealthBarPosition = playerTwoHUDPosition + healthBarOffset;
            healthBarFullSize = new Vector2(130, 20);

            // Initialize human & AI players
            playerOne = new Human(contentManager, mSpriteBatch, PlayerSide.Left);
            playerOne.Initialize();

            if (isTwoHumanPlayers)
            {
                playerOne.Name = (GlobalContext.PlayerIsFirstOnAppWarp ? GlobalContext.localUsername : GlobalContext.opponentName);

                playerTwo = new Human(contentManager, mSpriteBatch, PlayerSide.Right);
                playerTwo.Initialize();
                playerTwo.Name = (GlobalContext.PlayerIsFirstOnAppWarp ? GlobalContext.opponentName : GlobalContext.localUsername);
            }
            else
            {
               
                playerOne.Name = "Player" + (isTwoHumanPlayers ? GlobalContext.localUsername : "");
                playerTwo = new AI(contentManager, mSpriteBatch);
                playerTwo.Initialize();
                playerTwo.Name = "Phone";
            }

            // Identify enemies
            playerOne.Enemy = playerTwo;
            playerTwo.Enemy = playerOne;
            isFirstPlayerTurn = false;
            if (isTwoHumanPlayers)
            {
                if (GlobalContext.PlayerIsFirstOnAppWarp)
                {
                    playerOne.IsActive = true;
                    playerTwo.IsActive = false;

                }
                else
                {
                    playerOne.IsActive = false;
                    playerTwo.IsActive = true;
                }
            }
            else
            {
                playerOne.IsActive = false;
                playerTwo.IsActive = true;
            }

        }
        #endregion

        

        #region Update Helpers
        private void UpdateClouds(float elapsedTime)
        {
            // Move the clouds according to the wind
            int windDirection = wind.X > 0 ? 1 : -1;

            cloud1Position += new Vector2(24.0f, 0.0f) * elapsedTime *
                windDirection * wind.Y;
            if (cloud1Position.X > App.WIDTH)
                cloud1Position.X = -cloud1Texture.Width * 2.0f;
            else if (cloud1Position.X < -cloud1Texture.Width * 2.0f)
                cloud1Position.X = App.WIDTH;

            cloud2Position += new Vector2(16.0f, 0.0f) * elapsedTime *
                windDirection * wind.Y;
            if (cloud2Position.X > App.WIDTH)
                cloud2Position.X = -cloud2Texture.Width * 2.0f;
            else if (cloud2Position.X < -cloud2Texture.Width * 2.0f)
                cloud2Position.X = App.WIDTH;
        }
        #endregion

        #region Draw Helpers
        /// <summary>
        /// Draws the player's catapult
        /// </summary>
        void DrawPlayerOne(GameTimerEventArgs gameTime)
        {
            if (!gameOver)
                playerOne.Draw(gameTime);
        }

        /// <summary>
        /// Draws the AI's catapult
        /// </summary>
        void DrawPlayerTwo(GameTimerEventArgs gameTime)
        {
            if (!gameOver)
                playerTwo.Draw(gameTime);
        }

        /// <summary>
        /// Draw the sky, clouds, mountains, etc. 
        /// </summary>
        private void DrawBackground()
        {
            // Clear the background
            SharedGraphicsDeviceManager.Current.GraphicsDevice.Clear(Color.White);

            // Draw the Sky
            mSpriteBatch.Draw(skyTexture, Vector2.Zero, Color.White);

            // Draw Cloud #1
            mSpriteBatch.Draw(cloud1Texture,
                cloud1Position, Color.White);

            // Draw the Mountain
            mSpriteBatch.Draw(mountainTexture,
                Vector2.Zero, Color.White);

            // Draw Cloud #2
            mSpriteBatch.Draw(cloud2Texture,
                cloud2Position, Color.White);

            // Draw the Castle, trees, and foreground 
            mSpriteBatch.Draw(foregroundTexture,
                Vector2.Zero, Color.White);
        }

        /// <summary>
        /// Draw the HUD, which consists of the score elements and the GAME OVER tag.
        /// </summary>
        void DrawHud()
        {
            if (gameOver)
            {
                Texture2D texture = victoryTexture;
                string winMessage = "";
                if (isTwoHumanPlayers)
                {
                    if (GlobalContext.joinedUsers.Length == 2)
                    {
                        winMessage = GlobalContext.localUsername + " Won!";
                        if (((playerTwo.Score > playerOne.Score) && GlobalContext.PlayerIsFirstOnAppWarp) || ((playerOne.Score > playerTwo.Score) && !GlobalContext.PlayerIsFirstOnAppWarp))
                        {
                            texture = defeatTexture;
                            winMessage = GlobalContext.opponentName + " Won!";
                        }
                    }
                    else
                    { 
                       //when opponent has left the room then declare host player as winner
                        winMessage = GlobalContext.messageFromOpponent;
                    }
                }
                else
                {
                    if (playerOne.Score > playerTwo.Score)
                    {
                        winMessage = "Player 1 Won!";
                    }
                    else
                    {
                        texture = defeatTexture;
                        winMessage = "Player 2 Won!";
                    }
                
                }   
                mSpriteBatch.Draw(
                    texture,
                    new Vector2(App.WIDTH/ 2 - texture.Width / 2,
                                App.HEIGHT / 2 - texture.Height / 2),
                    Color.White);

                if (isTwoHumanPlayers)
                {
                    Vector2 size = hudFont.MeasureString(winMessage);
                    DrawString(hudFont, winMessage,
                        new Vector2(App.WIDTH / 2 - size.X / 2,
                            App.HEIGHT / 2 - texture.Height / 2 + 100),
                        Color.Red);

                    size = hudFont.MeasureString("press back to start new game");
                    DrawString(hudFont, "press back to start new game",
                        new Vector2(App.WIDTH / 2 - size.X / 2,
                            App.HEIGHT / 2 - texture.Height / 2 + 150),
                        Color.Red);
                }
            }
            else
            {
                // Draw Player Hud
                mSpriteBatch.Draw(hudBackgroundTexture,
                    playerOneHUDPosition, Color.White);
                mSpriteBatch.Draw(GetWeaponTexture(playerOne),
                    playerOneHUDPosition + new Vector2(33, 35), Color.White);
                DrawString(hudFont, playerOne.Score.ToString(),
                    playerOneHUDPosition + new Vector2(123, 35), Color.White);
                if (isTwoHumanPlayers)
                DrawString(hudFont, (GlobalContext.PlayerIsFirstOnAppWarp ? GlobalContext.localUsername : GlobalContext.opponentName),
                    playerOneHUDPosition + new Vector2(40, 1), Color.Blue);
                else
                  DrawString(hudFont,playerOne.Name,
                 playerOneHUDPosition + new Vector2(40, 1), Color.Blue);

                Rectangle rect = new Rectangle((int)playerOneHealthBarPosition.X, (int)playerOneHealthBarPosition.Y,
                    (int)healthBarFullSize.X * playerOne.Health / 100, (int)healthBarFullSize.Y);
                Rectangle underRect = new Rectangle(rect.X, rect.Y, rect.Width + 1, rect.Height + 1);
                mSpriteBatch.Draw(blankTexture, underRect, Color.Black);
                mSpriteBatch.Draw(blankTexture, rect, Color.Blue);

                // Draw Computer Hud
                mSpriteBatch.Draw(hudBackgroundTexture,
                    playerTwoHUDPosition, Color.White);
                mSpriteBatch.Draw(GetWeaponTexture(playerTwo),
                    playerTwoHUDPosition + new Vector2(33, 35), Color.White);
                DrawString(hudFont, playerTwo.Score.ToString(),
                    playerTwoHUDPosition + new Vector2(123, 35), Color.White);
                if(isTwoHumanPlayers)
                DrawString(hudFont, (GlobalContext.PlayerIsFirstOnAppWarp ? GlobalContext.opponentName : GlobalContext.localUsername),
                    playerTwoHUDPosition + new Vector2(30, 1), Color.Red);
                else
                 DrawString(hudFont,playerTwo.Name,
                   playerTwoHUDPosition + new Vector2(30, 1), Color.Red);

                rect = new Rectangle((int)playerTwoHealthBarPosition.X, (int)playerTwoHealthBarPosition.Y,
                    (int)healthBarFullSize.X * playerTwo.Health / 100, (int)healthBarFullSize.Y);
                underRect = new Rectangle(rect.X, rect.Y, rect.Width + 1, rect.Height + 1);
                mSpriteBatch.Draw(blankTexture, underRect, Color.Black);
                mSpriteBatch.Draw(blankTexture, rect, Color.Red);

                // Draw Wind direction
                string text = "WIND";
                Vector2 size = hudFont.MeasureString(text);
                Vector2 windarrowScale = new Vector2(wind.Y / 10, 1);
                mSpriteBatch.Draw(windArrowTexture,
                    windArrowPosition, null, Color.White, 0, Vector2.Zero,
                    windarrowScale, wind.X > 0
                    ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);

                DrawString(hudFont, text,
                    windArrowPosition - new Vector2(0, size.Y), Color.Black);
                if (wind.Y == 0)
                {
                text = "NONE";
                    DrawString(hudFont, text, windArrowPosition, Color.Black);
                }
                if (isTwoHumanPlayers)
                {
                    if ((GlobalContext.joinedUsers.Length == 2)&&!isPaused)
                    {
                        // Prepare prompt message
                        if ((GlobalContext.PlayerIsFirstOnAppWarp && isFirstPlayerTurn) || (!GlobalContext.PlayerIsFirstOnAppWarp && !isFirstPlayerTurn))
                            text = !isDragging ?
                                (isTwoHumanPlayers ? GlobalContext.localUsername + ", " : "") + "Drag Anywhere to Fire" : "Release to Fire!";
                        else
                            text = GlobalContext.localUsername + ", Please wait Its Opponent's turn!";
                    }
                    else
                    {
                        text = GlobalContext.messageFromOpponent;
                    }
                        size = hudFont.MeasureString(text);
                   
                }
                else
                {

                    if (isFirstPlayerTurn)
                    {
                        // Prepare first player prompt message
                        text = !isDragging ?
                            (isTwoHumanPlayers ? "Player 1, " : "") + "Drag Anywhere to Fire" : "Release to Fire!";
                        size = hudFont.MeasureString(text);
                    }
                    else
                    {
                        // Prepare second player prompt message
                        text = "I'll get you yet!";

                        size = hudFont.MeasureString(text);
                    }
                }

                DrawString(hudFont, text,
                    new Vector2(
                        App.WIDTH / 2 - size.X / 2,
                        App.HEIGHT - size.Y),
                        Color.Green);
            }
        }

        /// <summary>
        /// Returns the texture appropriate for the player's current weapon
        /// </summary>
        /// <param name="player">The player for which to get the texture</param>
        /// <returns>Ammo texture to draw in the HUD</returns>
        private Texture2D GetWeaponTexture(Player player)
        {
            switch (player.Weapon)
            {
                case WeaponType.Normal:
                    return ammoTypeNormalTexture;
                case WeaponType.Split:
                    return ammoTypeSplitTexture;
                default:
                    throw new ArgumentException("Player has invalid weapon type",
                        "player");
            }
        }

        /// <summary>
        /// A simple helper to draw shadowed text.
        /// </summary>
        void DrawString(SpriteFont font, string text, Vector2 position, Color color)
        {
            try
            {
                mSpriteBatch.DrawString(font, text,
                    new Vector2(position.X + 1, position.Y + 1), Color.Black);
                mSpriteBatch.DrawString(font, text, position, color);
            }
            catch (Exception e)
            {
                //Its hack code,Sometime Drawsrting throws exception while drawing oppentent's name
                //(because opponent is in different region and user's phone does not support his language)
                GlobalContext.opponentName = "random";
            }
        }

        /// <summary>
        /// A simple helper to draw shadowed text.
        /// </summary>
        void DrawString(SpriteFont font, string text, Vector2 position, Color color, float fontScale)
        {
            mSpriteBatch.DrawString(font, text, new Vector2(position.X + 1,
                position.Y + 1), Color.Black, 0, new Vector2(0, font.LineSpacing / 2),
                fontScale, SpriteEffects.None, 0);
            mSpriteBatch.DrawString(font, text, position, color, 0,
                new Vector2(0, font.LineSpacing / 2), fontScale, SpriteEffects.None, 0);
        }
        #endregion

        #region Input Helpers
        /// <summary>
        /// Finish the current game
        /// </summary>
        private void FinishCurrentGame()
        {
          //  ExitScreen();
        }

        /// <summary>
        /// Pause the current game
        /// </summary>
        private void PauseCurrentGame()
        {
           // var pauseMenuBackground = new BackgroundScreen();

            if (isDragging)
            {
                isDragging = false;
                playerOne.Catapult.CurrentState = CatapultState.Idle;

                if (isTwoHumanPlayers)
                    playerTwo.Catapult.CurrentState = CatapultState.Idle;
            }

            //ScreenManager.AddScreen(pauseMenuBackground, null);
            //ScreenManager.AddScreen(new PauseScreen(pauseMenuBackground, 
            //    playerOne, playerTwo), null);
        }
        //private void GetRoomPropertiesDoneCallback()
        //{
        //    if (((isFirstPlayerTurn && GlobalContext.PlayerIsFirstOnAppWarp) || (!isFirstPlayerTurn && !GlobalContext.PlayerIsFirstOnAppWarp)))
        //    {
              
        //    }          
        //}
        //private void UpdatePropertiesDoneCallback()
        //{
        //    playerOne.Catapult.Wind =(float)Convert.ToDouble(GlobalContext.tablePrperties["wind"]);
        //    playerOne.Score = Convert.ToInt32(GlobalContext.tablePrperties["Player1Score"]);
        //    playerTwo.Score = Convert.ToInt32(GlobalContext.tablePrperties["Player2Score"]);
        //}
        #endregion

        #region Gameplay Helpers
        /// <summary>
        /// Starts a new game session, setting all game states to initial values.
        /// </summary>
        void Start()
        {
            // Set initial wind direction
            wind = Vector2.Zero;
            isFirstPlayerTurn = false;
            changeTurn = true;
            playerTwo.Catapult.CurrentState = CatapultState.Reset;
        }

        private void OpponentLeftTheRoom()
        {  
            /*since opponents has left the room so set the high score to finish the game 
              and declare host user as winner 
             */
            if (!gameOver)
            {
                if (GlobalContext.PlayerIsFirstOnAppWarp)
                {
                    playerOne.Score = 100;
                    playerOne.Catapult.ForcedTosetGameOver();
                }
                else
                {
                    playerTwo.Score = 100;
                    playerTwo.Catapult.ForcedTosetGameOver();
                }
                GlobalContext.opponentName = "No Opponent";
                GlobalContext.messageFromOpponent = "Opponent has left the room";
                GlobalContext.PlayerIsFirstOnAppWarp = true;
                GlobalContext.tableProperties["Player1Score"] = 0;
                GlobalContext.tableProperties["Player2Score"] = 0;
                GlobalContext.tableProperties["WindX"] = 0;
                GlobalContext.tableProperties["WindY"] = 0;
                GlobalContext.tableProperties["fireNumber"] = 1;
                GlobalContext.joinedUsers = new[] { GlobalContext.localUsername };
            }
        }
        private void OpponentPaused()
        {
            isPaused = true;
            GlobalContext.messageFromOpponent = "Opponent is paused Please wait for some time";
        }
        private void OpponentResumed()
        {
            isPaused = false;
            GlobalContext.messageFromOpponent = "Opponent came back!!!";
        }
        private void ConnectionRecoverableError()
        {
            isPaused = true;
            GlobalContext.messageFromOpponent = "Connection is Lost,Please wait,trying to reconnect";
        }
        private void ConnectionRecoverd()
        {
            isPaused = false;
            GlobalContext.messageFromOpponent = "Connection is recoverd";
        }
        #endregion
    }
}