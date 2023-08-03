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
///    This class represents a player in the game. It inherits from GameObject.
/// 
/// </summary>
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AgarioModels
{
    /// <summary>
    /// This class represents a player in the game. It inherits from GameObject.
    /// </summary>
    public class Player : GameObject
    {
        /// <summary>
        /// The name of the player.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The constructor for the Player class.
        /// </summary>
        /// <param name="id">The unique identifier for the player.</param>
        /// <param name="loc">The location of the player.</param>
        /// <param name="Argbcolor">The ARGB color value of the player.</param>
        /// <param name="mass">The mass of the player.</param>
        /// <param name="name">The name of the player.</param>
        /// <param name="X">The X-coordinate of the player's position.</param>
        /// <param name="Y">The Y-coordinate of the player's position.</param>
        [JsonConstructor]
        public Player(long id, PointF loc, int Argbcolor, float mass, string name, float X, float Y)
            : base(id, loc, Argbcolor, mass,X,Y)
        {
            Name = name;
        }
    }
}
