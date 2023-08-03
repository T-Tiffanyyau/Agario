/// <summary>
/// Author:    Man Wai Lam & Tiffany Yau
/// Date:      3 Mar 2023
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
    /// Represents the food class.
    /// </summary>
    public class Food : GameObject
    {
        /// <summary>
        /// Constructor of food.
        /// </summary>
        /// <param name="id">The unique identifier of the food object.</param>
        /// <param name="loc">The location of the food object.</param>
        /// <param name="Argbcolor">The color of the food object in ARGB format.</param>
        /// <param name="mass">The mass of the food object.</param>
        /// <param name="X">The X coordinate of the food object.</param>
        /// <param name="Y">The Y coordinate of the food object.</param>
        public Food(long id, PointF loc, int Argbcolor, float mass, float X, float Y)
            : base(id, loc, Argbcolor, mass,X,Y)
        {
        }
    }
}
