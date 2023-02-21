//powerup object
using SnakeGame;
using System;

namespace World
{
    /// <summary>
    /// This create a powerup object
    /// </summary>
    public class Powerup
    {
        public int power; // the id of the powerup
        public Vector2D loc { get; private set; }//the location of the power up
        public bool died;//the condition of the powerup
        private int framepassed = 0; // frames to wait before respawning

        /// <summary>
        /// initialize the power up
        /// </summary>
        public Powerup()   
        {
            loc = new Vector2D();
        }

        /// <summary>
        /// pick a random place between the world size for powerup to be spawn
        /// </summary>
        public void PowSpawn()
        {
            Random rand = new Random();
            int x = rand.Next(-1000, 1000);
            int y = rand.Next(-1000, 1000);
            loc = new Vector2D(x, y);
        }

        /// <summary>
        /// used for powerup respawned delay
        /// </summary>
        public void passFrame()
        {
            framepassed++;
        }

        /// <summary>
        /// used for powerup respawned delay
        /// </summary>
        /// <returns></returns>
        public int getFramePass()
        {
            return framepassed;
        }

        /// <summary>
        /// used for powerup respawned delay
        /// </summary>
        /// <param name="s"></param>
        public void setFramePass(int s)
        {
            framepassed = s;
        }
    }
}