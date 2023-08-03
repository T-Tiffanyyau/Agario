/// <summary>
/// Author:    Man Wai Lam & Tiffany Yau
/// Date:      26 Apr 2023
/// Course:    CS 3500, University of Utah, School of Computing
/// Copyright: CS 3500 and Tiffany Yau - This work may not 
///            be copied for use in Academic Coursework.
///
/// We, Man Wai Lam & Tiffany Yau, certify that I wrote this code from scratch and
/// did not copy it in part or whole from another source.  All 
/// references used in the completion of the assignments are cited 
/// in my README file.
///
/// File Contents
///
///    This class creates the main page of the GUI.
///    Has been edited from asg8 to send data to sql.
/// 
/// </summary>
using Communications;
using Microsoft.Extensions.Logging;
using AgarioModels;
using Microsoft.UI.Xaml;
using Newtonsoft.Json;
using System.Numerics;
using System.Diagnostics;

namespace ClientGUI
{

    /// <summary>
    /// The main page of the GUI.
    /// </summary>
    public partial class MainPage : ContentPage
    {

        // Constants
        public const string CMD_Eaten_Food = "{Command Eaten Food}";
        public const string CMD_Dead_Players = "{Command Dead Players}";

        // Networking and game objects
        private Networking currentClient;
        private GameWorldDrawable gameWorldDrawable;
        private World world;

        // Timers
        private DispatcherTimer timer;
        private System.Threading.Timer movementUpdateTimer;
        private DispatcherTimer gameUpdateTimer;

        // Logging
        private ILogger<MainPage> myLogger;

        // UI-related
        private Point targetCoordinates;
        private Vector2? mousePosition;

        // Game stats
        private int fps;
        private int heartBeat;
        private int frameCounter;
        private int framesSinceLastUpdate;
        private readonly object worldLock = new object();
        private TimeSpan currentTime;
        private TimeSpan deadTime;
        private Dictionary<long, PlayerStats> playerSql;
        /// <summary>
        /// Constructor for the main page.
        /// </summary>
        /// <param name="logger"> the logger </param>
        public MainPage(ILogger<MainPage> logger)
        {
            InitializeComponent();
            InitializeUI();
            myLogger = logger;
            world = new World(logger);
            SetupTimers();
            playerSql = new Dictionary<long, PlayerStats>();

        }

        /// <summary>
        /// Initialize the GUI
        /// </summary>
        private void InitializeUI()
        {
            PointerGestureRecognizer recognizer = new PointerGestureRecognizer();
            var playSurface = new GraphicsView()
            {
                WidthRequest = 500,
                HeightRequest = 500
            };
            recognizer.PointerMoved += OnPointerMoved;
            PlaySurface.GestureRecognizers.Add(recognizer);
        }

        /*** Timer setup ***/
        /// <summary>
        /// Set up the timers.
        /// </summary>
        private void SetupTimers()
        {
            movementUpdateTimer = new System.Threading.Timer(OnMovementUpdateTimer, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(50));
            // Game update timer
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(1000 / 30);
            timer.Tick += (s, e) =>
            {
                GameUpdate();
                PlaySurface.Invalidate();
                framesSinceLastUpdate++;
                UpdateFrameCounter(++frameCounter);
            };
            timer.Start();

            // FPS timer
            SetupFPSTimer();

            // Heartbeat timer
            SetupHeartBeatTimer();
        }


        /*** Game update methods ***/
        /// <summary>
        /// Updates the game world.
        /// </summary>
        private void GameUpdate()
        {
            if (gameWorldDrawable == null || gameWorldDrawable.World == null)
            {
                return;
            }

            var currentPlayer = gameWorldDrawable.GetCurrentPlayer();
            if (currentPlayer == null)
            {
                return;
            }

            var foods = gameWorldDrawable.World.Food;

            foreach (var food in foods.Values.ToList())
            {
                if (gameWorldDrawable.World.CheckCollision(currentPlayer, food))
                {
                    gameWorldDrawable.World.PlayerEatFood(currentPlayer, food);
                }
            }
            UpdateCenteredOn(currentPlayer.Location);
            UpdateMass(currentPlayer.Mass);
            framesSinceLastUpdate++;
        }

        /*** Event handlers ***/
        /// <summary>
        /// Event handler when the connect button is clicked.
        /// </summary>
        /// <param name="sender"> teh sender </param>
        /// <param name="e"> the event </param>
        private async void OnConnectButtonClicked(object sender, EventArgs e)
        {
            string playerName = PlayerNameEntry.Text;
            string serverAddress = ServerAddressEntry.Text;

            currentClient = new Networking(
                            logger: myLogger,
                            OnConnectionEstablished,
                            OnDisconnect,
                            OnMessageReceived,
                            '\n');

            try
            {
                if (playerName is null)
                    throw new Exception("Player name cannot be empty.");
                currentClient.ID = playerName;
                currentClient.Connect(serverAddress, 11000);
                currentClient.AwaitMessagesAsync(infinite: true);
                WelcomeScreen.IsVisible = false;
                GameScreen.IsVisible = true;
                ErrorMessageLabel.Text = string.Empty;


            }
            catch (Exception ex)
            {
                if (ex.Message == "Player name cannot be empty.")
                {
                    ErrorMessageLabel.Text = "Player name cannot be empty.";
                }
                else
                {
                    ErrorMessageLabel.Text = "Connection failed. Please try again.";
                }
                myLogger.LogError(ex, $"Connection error: {ex.Message}");
            }
        }

        /// <summary>
        /// Display a dialog when the player is dead.
        /// </summary>
        /// <returns> The pop up dialog </returns>
        private async Task ShowGameOverDialog()
        {
            bool playAgain = await DisplayAlert("Game Over", "You have been eaten. Would you like to play again?", "Yes", "No");
            if (playAgain)
            {
                // Restart the game
                if (currentClient != null)
                {
                    currentClient.Send(String.Format(Protocols.CMD_Start_Game, currentClient.ID));
                }
            }
            else
            {
                // Disconnect and show the welcome screen
                currentClient?.Disconnect();
                GameScreen.IsVisible = false;
                WelcomeScreen.IsVisible = true;
            }
        }

        /// <summary>
        /// Event handler when message is received.
        /// </summary>
        /// <param name="channel"> the networking channel </param>
        /// <param name="message"> the message </param>
        private void OnMessageReceived(Networking channel, string message)
        {
            if (message.StartsWith("{Command Food}"))
            {
                try
                {
                    string JsonMessage = message.Substring("{Command Food}".Length);
                    List<Food> foods = JsonConvert.DeserializeObject<List<Food>>(JsonMessage);
                    foreach (var food in foods)
                    {
                        world.AddFood(food);
                    }
                    world.UpdateDictonary();
                }
                catch (Exception ex)
                {
                    // Handle any exceptions that may occur during deserialization
                    myLogger.LogError($"Failed to deserialize JSON: {ex.Message}");
                    Console.WriteLine("Failed to deserialize JSON: " + ex.Message);
                }
            }
            else if (message.StartsWith("{Command Players}"))
            {
                string JsonMessage = message.Substring("{Command Players}".Length);
                List<Player> playersList = JsonConvert.DeserializeObject<List<Player>>(JsonMessage);

                // Clear the existing players and add the new players to the dictionary.w
                world.Players.Clear();
                foreach (Player player in playersList)
                {
                    world.Players[player.Id] = player;
                }

                world.UpdateDictonary();
            }
            else if (message.StartsWith("{Command Player Object}"))
            {
                string CurrentPlayerString = message.Substring("{Command Player Object}".Length);
                long.TryParse(CurrentPlayerString, out var CurrentPlayerID);
                world.CurrentPlayerID = CurrentPlayerID;
                currentTime = DateTime.Now.TimeOfDay;
                PlayerStats playerStats = new PlayerStats
                {
                    PlayerId = world.CurrentPlayerID,
                    PlayerName = world.Players[world.CurrentPlayerID].Name,
                    Mass = 150,
                    StartTime = currentTime.ToString(@"hh\:mm\:ss"),
                    DeadTime = ""
                };
                playerSql[world.CurrentPlayerID] = playerStats;

                // Send the player's data to the server
                Task.Run(() => SendPlayerStatsToServerAsync(playerStats));
            }
            else if (message.StartsWith(CMD_Eaten_Food))
            {
                string jsonMessage = message.Substring(CMD_Eaten_Food.Length).Trim();
                List<long> foodIds = JsonConvert.DeserializeObject<List<long>>(jsonMessage);

                foreach (var foodId in foodIds)
                {
                    gameWorldDrawable.World.RemoveFood(foodId);
                }

                gameWorldDrawable.DrawTheWorldForMe();


            }
            else if (message.StartsWith(CMD_Dead_Players))
            {

                string jsonMessage = message.Substring(CMD_Dead_Players.Length).Trim();
                List<long> deadPlayerIds = JsonConvert.DeserializeObject<List<long>>(jsonMessage);
         
                foreach (var playerId in deadPlayerIds)
                {
                    gameWorldDrawable.World.RemovePlayer(playerId);
                    if (playerId == gameWorldDrawable.World.CurrentPlayerID)
                    {
                        deadTime = DateTime.Now.TimeOfDay;
                        if (deadPlayerIds.Count > 0)
                        {
                            PlayerStats playerStats = new PlayerStats
                            {
                                PlayerId = playerSql[world.CurrentPlayerID].PlayerId,
                                PlayerName = playerSql[world.CurrentPlayerID].PlayerName,
                                Mass = playerSql[world.CurrentPlayerID].Mass,
                                StartTime = currentTime.ToString(@"hh\:mm\:ss"),
                                DeadTime = deadTime.ToString(@"hh\:mm\:ss")
                            };
                            playerSql[world.CurrentPlayerID] = playerStats;
                            Task.Run(() => SendPlayerStatsToServerAsync(playerStats));
                        }


                        Device.BeginInvokeOnMainThread(async () =>
                        {
                            await ShowGameOverDialog();
                        });

                    }
                }
                gameWorldDrawable.DrawTheWorldForMe();
            }

            myLogger.LogInformation("Received message: " + message);
            UpdateHeartBeat(++heartBeat);
            gameWorldDrawable.UpdateWorld(world);
            PlaySurface.Invalidate();


        }

        /// <summary>
        /// Event handler when the connection is established.
        /// </summary>
        /// <param name="client"> the current networking client </param>
        private void OnConnectionEstablished(Networking client)
        {
            currentClient = client;
            string userId = PlayerNameEntry.Text;
            currentClient.ID = userId;
            currentClient.Send(String.Format(Protocols.CMD_Start_Game, userId));

            // Initialize the GameWorldDrawable object
            gameWorldDrawable = new GameWorldDrawable(world, currentClient, PlaySurface);
            PlaySurface.Drawable = gameWorldDrawable;

        }

        /// <summary>
        /// When the connection is disconnected.
        /// </summary>
        /// <param name="channel"></param>
        private void OnDisconnect(Networking channel)
        {
            myLogger.LogInformation("Disconnected from server");
        }

        /// <summary>
        /// Method to send the player's stats to the server.
        /// </summary>
        /// <param name="playerStats"></param>
        /// <returns></returns>
        public async Task SendPlayerStatsToServerAsync(PlayerStats playerStats)
        {
            // Replace with your server's address and desired endpoint
            string serverUrl = $"http://localhost:11001/data?playerName={playerSql[world.CurrentPlayerID].PlayerName}&playerId={playerSql[world.CurrentPlayerID].PlayerId}&Mass={playerSql[world.CurrentPlayerID].Mass}&StartTime={playerSql[world.CurrentPlayerID].StartTime}&DeadTime={playerSql[world.CurrentPlayerID].DeadTime}";

            using (HttpClient httpClient = new HttpClient())
            {
                // Send the player's name and id as part of the URL
                HttpResponseMessage response = await httpClient.GetAsync(serverUrl);

                if (response.IsSuccessStatusCode)
                {
                    Debug.WriteLine("Data sent successfully.");
                }
                else
                {
                    Debug.WriteLine("Error sending data: " + response.ReasonPhrase);
                }
            }
        }


        /// <summary>
        /// Event handler for when the pointer is moved.
        /// </summary>
        /// <param name="sender"> teh sender </param>
        /// <param name="e"> the event </param>
        private void OnPointerMoved(object sender, Microsoft.Maui.Controls.PointerEventArgs e)
        {
            Point? screenCoordinates = e.GetPosition(sender as View);
            if (screenCoordinates.HasValue)
            {
                Point targetCoordinates = ScreenToWorldCoordinates(screenCoordinates.Value);
                mousePosition = new Vector2((float)targetCoordinates.X, (float)targetCoordinates.Y);
            }
        }

        /// <summary>
        /// Event handler for when the split button is clicked.
        /// </summary>
        /// <param name="sender"> teh sender </param>
        /// <param name="e"> the event </param>
        private void OnSplitButtonClicked(object sender, EventArgs e)
        {
            SendSplitRequest();
        }

        /// <summary>
        /// Event handler for tapped given form the CS 3500 code
        /// </summary>
        /// <param name="sender"> the sender</param>
        /// <param name="e"> the event </param>
        private void OnTap(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// Event handler for pan given form the CS 3500 code
        /// </summary>
        /// <param name="sender"> the sender</param>
        /// <param name="e"> the event </param>
        private void PanUpdated(object sender, PanUpdatedEventArgs e)
        {
        }

        /*** Helper methods ***/
        /// <summary>
        /// Conver the coordinate of the pointer to the world coordinates.
        /// </summary>
        /// <param name="screenCoordinates"> the coordinates of the pointer </param>
        /// <returns></returns>
        private Point ScreenToWorldCoordinates(Point screenCoordinates)
        {
            Player currentPlayer = gameWorldDrawable.GetCurrentPlayer();
            if (currentPlayer == null) return new Point(0, 0);

            float centerX = currentPlayer.Location.X;
            float centerY = currentPlayer.Location.Y;

            double zoomFactor = 800.0 / (500 * 2);

            double worldX = screenCoordinates.X / zoomFactor + (currentPlayer.X - 500);
            double worldY = screenCoordinates.Y / zoomFactor + (currentPlayer.Y - 500);

            return new Point(worldX, worldY);
        }

        /// <summary>
        /// Send the split request to the server.
        /// </summary>
        private void SendSplitRequest()
        {
            if (currentClient == null || !mousePosition.HasValue) return;

            int worldX = (int)Math.Round(mousePosition.Value.X);
            int worldY = (int)Math.Round(mousePosition.Value.Y);
            string splitCommand = String.Format(Protocols.CMD_Split, worldX, worldY);
            currentClient.Send(splitCommand);
        }

        /// <summary>
        /// Set up the FPS timer
        /// </summary>
        private void SetupFPSTimer()
        {
            var fpsTimer = new DispatcherTimer();
            fpsTimer.Interval = TimeSpan.FromSeconds(1);
            fpsTimer.Tick += (s, e) => UpdateFPSAndResetCounter();
            fpsTimer.Start();
        }

        /// <summary>
        /// Updates the FPS and resets the counter.
        /// </summary>
        private void UpdateFPSAndResetCounter()
        {
            UpdateFPS(framesSinceLastUpdate);
            framesSinceLastUpdate = 0;
        }

        /// <summary>
        /// Sets up the heart beat timer.
        /// </summary>
        private void SetupHeartBeatTimer()
        {
            var heartBeatTimer = new DispatcherTimer();
            heartBeatTimer.Interval = TimeSpan.FromMilliseconds(1000); // Adjust the interval as needed
            heartBeatTimer.Tick += (s, e) => UpdateHeartBeat(++heartBeat);
            heartBeatTimer.Start();
        }



        /*** UI update methods ***/
        /// <summary>
        /// Updates the FPS.
        /// </summary>
        /// <param name="newFPS"> the new FPS </param>
        private void UpdateFPS(int newFPS)
        {
            fps = newFPS;
            fpsLabel.Text = $"FPS: {fps}";
        }

        /// <summary>
        /// Updates the heart beat.
        /// </summary>
        /// <param name="newHeartBeat"> the new heartbeat</param>
        private void UpdateHeartBeat(int newHeartBeat)
        {
            heartBeat = newHeartBeat;
            heartBeatLabel.Text = $"Heart Beat: {heartBeat}";
        }

        /// <summary>
        /// Updates the centered on label.
        /// </summary>
        /// <param name="position"> the current position of the player </param>
        private void UpdateCenteredOn(Vector2 position)
        {
            centeredOnLabel.Text = $"Centered On: ({position.X:0.00}, {position.Y:0.00})";
        }

        /// <summary>
        /// Updates the mass label.
        /// </summary>
        /// <param name="mass"> the current mass </param>
        private void UpdateMass(float mass)
        {
            massLabel.Text = $"Mass: {mass:0.00}";
            PlayerStats playerStats = new PlayerStats
            {
                PlayerId = world.CurrentPlayerID,
                PlayerName = playerSql[world.CurrentPlayerID].PlayerName,
                Mass = mass,
                StartTime = currentTime.ToString(@"hh\:mm\:ss"),
                DeadTime = ""
            };
            playerSql[world.CurrentPlayerID] = playerStats;
            Task.Run(() => SendPlayerStatsToServerAsync(playerStats));
        }

        /// <summary>
        /// Updates the frame counter.
        /// </summary>
        /// <param name="newFrameCounter"> the new frame counter </param>
        private void UpdateFrameCounter(int newFrameCounter)
        {
            frameCounter = newFrameCounter;
            frameCounterLabel.Text = $"Frame Counter: {frameCounter}";
        }

        /// <summary>
        /// Sends the player movement to the server.
        /// </summary>
        /// <param name="state"></param>
        private void OnMovementUpdateTimer(object state)
        {
            if (mousePosition.HasValue && currentClient != null)
            {
                int worldX = (int)Math.Round(mousePosition.Value.X);
                int worldY = (int)Math.Round(mousePosition.Value.Y);
                currentClient.Send(String.Format(Protocols.CMD_Move, worldX, worldY));
            }
        }
    }

    public class PlayerStats
    {
        public long PlayerId { get; set; }
        public string PlayerName { get; set; }
        public float Mass { get; set; }
        public string StartTime { get; set; }
        public string DeadTime { get; set; }
    }
}