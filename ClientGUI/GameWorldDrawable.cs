/// <summary>
/// Author:    Man Wai Lam & Tiffany Yau
/// Date:      14 Apr 2023
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
///    This class defines how the Agario game is drew for users to play.
/// 
/// </summary>
using AgarioModels;
using System.Numerics;
using Communications;

namespace ClientGUI
{
    /// <summary>
    /// This class defines how the Agario game is drew for users to play.
    /// </summary>
    public class GameWorldDrawable : IDrawable
    {
        public World World
        {
            get => world;
            set => world = value;
        }
        private World world = null;
        private Networking currentClient;
        private readonly Player player;
        public GraphicsView PlaySurface { get; set; }
        private bool havePlayer = false;
        public float ScreenWidth { get; private set; }
        public float ScreenHeight { get; private set; }
        private Dictionary<int, Color> colorCache = new Dictionary<int, Color>();
        private const float ZoomX = 500;
        private const float ZoomY = 500;
        private Thread updateThread;
        private readonly object worldLock = new object();

        /// <summary>
        /// Starts the update thread
        /// </summary>
        public void StartUpdateThread()
        {
            updateThread = new Thread(UpdateThreadLoop)
            {
                IsBackground = true
            };
            updateThread.Start();
        }

        /// <summary>
        /// Updates the thread.
        /// </summary>
        private void UpdateThreadLoop()
        {
            while (true)
            {
                // Get the updated world from the server
                World newWorld = this.world;

                // Update the world
                UpdateWorld(newWorld);

                // Redraw the world
                DrawTheWorldForMe();

                // Add some delay if needed
                Thread.Sleep(16); // Approximately 60 FPS
            }
        }

        /// <summary>
        /// Constructor for the GameWorldDrawable class.
        /// </summary>
        /// <param name="world">The game world to be drawn.</param>
        /// <param name="currentClient">The current client connected to the game.</param>
        /// <param name="playSurface">The surface on which the game is being played.</param>
        /// <exception cref="ArgumentNullException"></exception>
        public GameWorldDrawable(World world, Networking currentClient, GraphicsView playSurface)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.currentClient = currentClient;
            this.PlaySurface = playSurface;

            ScreenWidth = (float)playSurface.WidthRequest;
            ScreenHeight = (float)playSurface.HeightRequest;

            // Start the update thread
            StartUpdateThread();
        }

        /// <summary>
        /// Updates the game screen.
        /// </summary>
        /// <param name="newWorld"> The updated world </param>
        public void UpdateWorld(World newWorld)
        {
            lock (worldLock)
            {
                if (this.world.CurrentPlayerID != null)
                {
                    newWorld.CurrentPlayerID = this.world.CurrentPlayerID;
                }
                this.world = newWorld;
            }
        }

        /// <summary>
        /// Gets the current player
        /// </summary>
        /// <returns> The current player </returns>
        public Player GetCurrentPlayer()
        {
            return world.CurrentPlayer;
        }

        /// <summary>
        /// Draws the game world.
        /// </summary>
        public void DrawTheWorldForMe()
        {
            PlaySurface.Invalidate();
        }

        /// <summary>
        /// Gets the color
        /// </summary>
        /// <param name="argbColor"> The color </param>
        /// <returns></returns>
        private Color GetCachedColor(int argbColor)
        {
            if (colorCache.TryGetValue(argbColor, out var color))
            {
                return color;
            }

            float a = ((argbColor >> 24) & 0xFF) / 255.0f;
            float r = ((argbColor >> 16) & 0xFF) / 255.0f;
            float g = ((argbColor >> 8) & 0xFF) / 255.0f;
            float b = (argbColor & 0xFF) / 255.0f;

            color = new Color(r, g, b, a);
            colorCache[argbColor] = color;

            return color;
        }

        /// <summary>
        /// Draws the game world.
        /// </summary>
        /// <param name="canvas"> The game screen </param>
        /// <param name="dirtyRect"> The screen on the GUI where the drawable and draw </param>
        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            lock (worldLock)
            {
                canvas.FillColor = Colors.Gray;
                canvas.FillRectangle(0, 0, 800, 800);
                Player currentPlayer = GetCurrentPlayer();


                foreach (var gameObject in world.GameObjects.Values)
                {
                    canvas.FillColor = GetCachedColor(gameObject.ARGBColor);
                    canvas.StrokeColor = Colors.Black;

                    Vector2 FullScreenPosition = Zoom(gameObject.Location);

                    canvas.DrawCircle(FullScreenPosition, gameObject.Radius);
                    canvas.FillCircle(FullScreenPosition, gameObject.Radius);
                    if (gameObject is Player player && player != currentPlayer)
                    {
                        DrawPlayerName(canvas, player, FullScreenPosition);
                    }
                }
                if (currentPlayer != null)
                {
                    Vector2 currentFullScreenPosition = Zoom(currentPlayer.Location);
                    DrawPlayerName(canvas, currentPlayer, currentFullScreenPosition);
                }
            }
        }

        /// <summary>
        /// Draws the player name
        /// </summary>
        /// <param name="canvas">The canvas on which the player name is to be drawn.</param>
        /// <param name="player">The player whose name is to be drawn.</param>
        /// <param name="position">The position of the player on the canvas.</param>
        private void DrawPlayerName(ICanvas canvas, Player player, Vector2 position)
        {
            // Set the font size and color
            float fontSize = 14; // Adjust the font size as needed
            Color fontColor = Colors.White; // Choose a suitable text color
            canvas.FontSize = fontSize;
            canvas.FontColor = fontColor;

            // Estimate the text width
            float textWidth = player.Name.Length * fontSize;

            // Calculate the text position
            float textX = position.X - textWidth / 2;
            float textY = position.Y - player.Radius - fontSize; // Adjust the vertical position to be on top of the circle

            // Draw the player's name
            canvas.DrawString(player.Name, textX, textY, textWidth, fontSize, Microsoft.Maui.Graphics.HorizontalAlignment.Center, Microsoft.Maui.Graphics.VerticalAlignment.Center, TextFlow.ClipBounds, 0.0f);
        }

        /// <summary>
        /// Draws the object on the screen while the screen is zoomed in.
        /// </summary>
        /// <param name="objectLocation"> the game object location </param>
        /// <returns></returns>
        private Vector2 Zoom(Vector2 objectLocation)
        {
            Player currentPlayer = GetCurrentPlayer();
            if (currentPlayer == null)
            {
                return objectLocation; 
            }

            float zoomFactorX = ScreenWidth / (ZoomX * 2);
            float zoomFactorY = ScreenHeight / (ZoomY * 2);

            float x = (objectLocation.X - (currentPlayer.X - ZoomX)) * zoomFactorX;
            float y = (objectLocation.Y - (currentPlayer.Y - ZoomY)) * zoomFactorY;

            return new Vector2(x, y);
        }
    }
}
