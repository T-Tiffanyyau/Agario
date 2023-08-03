/// <summary>
/// Author:    Man Wai Lam & Tiffany Yau
/// Date:      15 Apr 2023
/// Course:    CS 3500, University of Utah, School of Computing
/// Copyright: CS 3500 and Man Wai Lam & Tiffany Yau - This work may not 
///            be copied for use in Academic Coursework.
///
/// We, Man Wai Lam & Tiffany Yau, certify that I wrote this code from scratch and
/// did not copy it in part or whole from another source.  All 
/// references used in the completion of the assignments are cited 
/// in my README file.
///
/// File Contents
///
///    This class defines the world of Agario. It contains all the game data and also functions that is use for the game objects.
/// 
/// </summary>
using Microsoft.Extensions.Logging;
using System.Numerics;
namespace AgarioModels
{
    /// <summary>
    /// The world class. This class represents the world of Agario.
    /// </summary>
    public class World
    {
        public Dictionary<long, GameObject> GameObjects { get; } = new Dictionary<long, GameObject>();
        public const int Width = 5000;
        public const int Height = 5000;
        public ILogger Logger { get; }
        public World(ILogger logger)
        {
            Logger = logger;
        }
        public Dictionary<long, Player> Players = new();
        public Dictionary<long, Food> Food = new();
        public long CurrentPlayerID { get; set; }
        public Player? CurrentPlayer
        {
            get
            {
                if (Players.TryGetValue(CurrentPlayerID, out Player? player))
                {
                    return player;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Updates the dictionary of game objects - player and food.
        /// </summary>
        public void UpdateDictonary()
        {
            foreach (var player in Players.Values)
            {
                GameObjects[player.Id] = player;
            }
            foreach (var food in Food.Values)
            {
                GameObjects[food.Id] = food;
            }
        }

        /// <summary>
        /// Remove the food form the food list.
        /// </summary>
        /// <param name="id"> the food id </param>
        public void RemoveFood(long id)
        {
            Food.Remove(id);
            GameObjects.Remove(id);
        }

        /// <summary>
        /// Removes the food and also add the mass to the player.
        /// </summary>
        /// <param name="player"> the player </param>
        /// <param name="food"> the food </param>
        public void PlayerEatFood(Player player, Food food)
        {
            player.Mass += food.Mass;
            RemoveFood(food.Id);
        }

        /// <summary>
        /// Add the food to the game.
        /// </summary>
        /// <param name="food"> teh food </param>
        public void AddFood(Food food)
        {
            Food[food.Id] = food;
            GameObjects[food.Id] = food;
        }

        /// <summary>
        /// Add the player to the game.
        /// </summary>
        /// <param name="player"> the player </param>
        public void AddPlayer(Player player)
        {
            Players[player.Id] = player;
            GameObjects[player.Id] = player;

        }

        /// <summary>
        /// Removes the player from the game.
        /// </summary>
        /// <param name="playerId"></param>
        public void RemovePlayer(long playerId)
        {
            Players.Remove(playerId);
            GameObjects.Remove(playerId);
        }

        /// <summary>
        /// Use when a player is eaten by another player.
        /// </summary>
        /// <param name="predator"> the bigger player </param>
        /// <param name="prey"> the smaller player </param>
        public void PlayerEatPlayer(Player predator, Player prey)
        {
            if (predator.Mass > prey.Mass)
            {
                predator.Mass += prey.Mass;
                RemovePlayer(prey.Id);
            }
        }

        /// <summary>
        /// Defines if the game objects collies.
        /// </summary>
        /// <param name="circle1"> a game object </param>
        /// <param name="circle2"> another game object </param>
        /// <returns></returns>
        public bool CheckCollision(GameObject circle1, GameObject circle2)
        {
            float distance = Vector2.Distance(circle1.Location, circle2.Location);
            return distance <= (circle1.Radius + circle2.Radius);
        }

        /// <summary>
        /// Check if the player collides with another player.
        /// </summary>
        public void CheckPlayerCollisions()
        {

            foreach (var player1 in Players.Values)
            {
                foreach (var player2 in Players.Values)
                {
                    if (player1.Id != player2.Id && CheckCollision(player1, player2))
                    {
                        PlayerEatPlayer(player1, player2);
                    }
                }
            }
        }
    }
}
