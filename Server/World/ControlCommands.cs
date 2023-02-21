//command from the client
using System;
using SnakeGame;

namespace World
{
    /// <summary>
    /// store the command send from the client
    /// </summary>
    public class ControlCommands
    {
        //store the command
        public string moving;

        /// <summary>
        /// default constructor
        /// </summary>
        public ControlCommands()
        {
            moving = "up";
        }

        /// <summary>
        /// convert all the command from string to vectors
        /// </summary>
        /// <returns></returns>
        public Vector2D GetDir()
        {
            if (moving.Equals("up"))//if move up
            {
                return new Vector2D(0, -1);
            }else if (moving.Equals("down"))//if move down
            {
                return new Vector2D(0, 1);
            }
            else if (moving.Equals("left"))//if move left
            {
                return new Vector2D(-1, 0);
            }
            else if (moving.Equals("right"))//if move right
            {
                return new Vector2D(1, 0);
            }
            else // if it's 'none'
            {
                return new Vector2D(0,0);
            }
        }
       

    }
}

