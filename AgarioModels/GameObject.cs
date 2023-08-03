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
///    This class defines a game object in the game, which is the parent class of Player and Food.
/// 
/// </summary>
using System.Numerics;
using Newtonsoft.Json;
using System.Drawing;

namespace AgarioModels
{
    /// <summary>
    /// The game object class. This class represents a game object in the game.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class GameObject
    {
        public PointF Position { get; set; }
        public long Id { get; }
        public Vector2 Location { get;  set; }
        private int _argbColor; // Add a private backing field for ARGBColor
        public int ARGBColor => _argbColor; // Use the backing field in the getter
        public float Mass { get; set; }
        public float X { get; }
        public float Y { get; }

        /// <summary>
        /// Represents a game object in the game.
        /// </summary>
        /// <param name="id">The unique identifier of the game object.</param>
        /// <param name="loc">The location of the game object.</param>
        /// <param name="ARGBColor">The color of the game object in ARGB format.</param>
        /// <param name="mass">The mass of the game object.</param>
        /// <param name="X">The X coordinate of the game object.</param>
        /// <param name="Y">The Y coordinate of the game object.</param>
        public GameObject(long id, PointF loc, int ARGBColor, float mass,float X,float Y)
        {
            Id = id;
            Position = loc;
            _argbColor = ARGBColor;
            Mass = mass;
            Location= new Vector2(X, Y);
            this .X = X;
            this .Y = Y;
        }

        /// <summary>
        /// The radius of the game object.
        /// </summary>
        public float Radius => (float)Math.Sqrt(Mass / Math.PI);

    }
}
