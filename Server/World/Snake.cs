//snake object
using Newtonsoft.Json;
using SnakeGame;
using System;
using System.Drawing;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;

namespace World
{
    /// <summary>
    /// Create a snake object
    /// </summary>
    public class Snake
    {
        public int snake;//the id of the snake assigned by the server

        public string name; //name that the player typed

        public List<Vector2D> body { get; private set; }//vectors of the snanke body, last one is the head

        public Vector2D dir;//direction of the snake

        public int score;

        public bool died;

        public bool alive;

        public bool dc;// see if disconnected

        private List<Vector2D> direction;//store the direction of each body vector accordingly

        private Vector2D oldDirection;

        //settings from setting files
        private double speed;
        private int framepassed;
        private Vector2D oldTail;
        private bool scoreUp;
        private int ws;

        
        /// <summary>
        /// initialized the snake, this constructor is mainly for the client side, it is used for json to be parse to object
        /// </summary>
        public Snake()
        {
            body = new List<Vector2D>();
            dir = new Vector2D();
            score = 0;
            alive = true;
            died = false;
            dc = false;
            snake = 0;
            name = "n";
            speed = 3;
            direction = new List<Vector2D>();
            oldDirection = dir;
            framepassed = 0;
            scoreUp = false;

        }

        /// <summary>
        /// This constructor is used for server, when it create a new snake, it pass in the assigned id
        /// and the name given by the client
        /// </summary>
        /// <param name="id"></param>
        /// <param name="snakeName"></param>
        public Snake(long id, string snakeName)
        {
            snake = (int)id;
            name = snakeName;
            alive = true;
            speed = 3;

            // pick a direction ramdomly
            Random rng = new Random();
            int x = rng.Next(4);
            List<Vector2D> fourDirection = new List<Vector2D>() { new Vector2D(1, 0), new Vector2D(0, 1), new Vector2D(-1, 0), new Vector2D(0, -1) };
            dir = fourDirection[x];

            //set up the snake using the random direction
            Vector2D tail = new Vector2D(fourDirection[x].X, fourDirection[x].Y);
            body = new List<Vector2D>();
            body.Add(tail);
            Vector2D head = fourDirection[x] * 120;
            body.Add(head);
            direction = new List<Vector2D>();
            oldDirection = dir;
            direction.Add(dir);
            scoreUp = false;

            //pick a random location for the snake
            SnakeSpawn();

        }

        /// <summary>
        /// pick a random location for the snake to be spawned
        /// </summary>
        public void SnakeSpawn()
        {
            Random rand = new Random();
            int x = rand.Next(-1000, 1000);
            int y = rand.Next(-1000, 1000);

            body[body.Count - 1].X += x;
            body[body.Count - 1].Y += y;

            body[0].X += x;
            body[0].Y += y;
        }

        /// <summary>
        /// update the snake location, will be called each frame
        /// </summary>
        public void UpdateSnake()
        {
            
            lock (direction)
            {
                //update the head
                body[body.Count-1] += dir * speed;

                //update the body, and check if there's any turn
                SnakeCheck();

                //check the wrap around
                wrapAroundCheck();
                
            } 
        }

        /// <summary>
        //When a snake changes direction, a new vertex is added going in that direction, and the head is updated to be that new vertex.
        //The rest of the snake's body follows along the path that the segments in front of it previously followed.
        //When the tail "catches up" to the next vertex, a vertex is removed (one of the turns in the body is removed).
        //get the specific snake who's direction changed
        /// </summary>
        /// <param name="newDirection"></param>
        public void Turn(Vector2D newDirection)
        {
            //disallowd turning 180 degree
            if(-newDirection.X == dir.X || -newDirection.Y == dir.Y)
            {
                return;
            }

            //add the new vertex, and add the trunning direction
            lock (direction)
            {
                body.Add(body[body.Count - 1]);
                direction.Add(newDirection);
                oldDirection = dir;
                dir = newDirection;
            }
        }

        /// <summary>
        /// update the old direction, used for snake turn
        /// </summary>
        /// <param name="dir"></param>
        public void updateOldDirection(Vector2D dir)
        {
            oldDirection = dir;
        }

        /// <summary>
        /// add new direction to the direction list
        /// </summary>
        /// <param name="dir"></param>
        public void addDirection(Vector2D dir)
        {
            direction.Add(dir);
        }

        /// <summary>
        /// check the snake turnning for every vetex in the body
        /// </summary>
        private void SnakeCheck()
        {
           //get the tail direction
            Vector2D tailDir = direction[0];

            //check direction
            string dir = GetDirString(tailDir);

            //if going right
            if (dir.Equals("right"))
            {
                if (body[0].X < body[1].X)//this is when the tail haven't "catches up" the tunning point
                {
                    body[0] += direction[0] * speed;
                }
                else if (body[0].X >= body[1].X)// remove the turnning point if it catches up
                {
                    direction.RemoveAt(0);
                    body.RemoveAt(0);
                }
                else // it's a stright line
                {
                    body[0] += direction[direction.Count - 1] * speed;
                }
            }
            else if (dir.Equals("left"))
            {
                if (body[0].X > body[1].X)//this is when the tail haven't "catches up" the tunning point
                {
                    body[0] += direction[0] * speed;
                }
                else if (body[0].X <= body[1].X)// remove the turnning point if it catches up
                {
                    direction.RemoveAt(0);
                    body.RemoveAt(0);
                }
                else // it's a stright line
                {
                    body[0] += direction[direction.Count - 1] * speed;
                }
            }
            else if (dir.Equals("up"))
            {
                if (body[0].Y > body[1].Y)//this is when the tail haven't "catches up" the tunning point
                {
                    body[0] += direction[0] * speed;
                }
                else if (body[0].Y <= body[1].Y)// remove the turnning point if it catches up
                {
                    direction.RemoveAt(0);
                    body.RemoveAt(0);
                }
                else // it's a stright line
                {
                    body[0] += direction[direction.Count - 1] * speed;
                }
            }
            else
            {
                if (body[0].Y < body[1].Y)//this is when the tail haven't "catches up" the tunning point
                {
                    body[0] += direction[0] * speed;
                }
                else if (body[0].Y >= body[1].Y)// remove the turnning point if it catches up
                {
                    direction.RemoveAt(0);
                    body.RemoveAt(0);
                }
                else // it's a stright line
                {
                    body[0] += direction[direction.Count - 1] * speed;
                }
            }

        }

        /// <summary>
        /// get the direction string accroding to the vector
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        private string GetDirString(Vector2D dir)
        {
            if (dir.Equals(new Vector2D(0, -1)))
            {
                return ("up");
            }
            else if (dir.Equals(new Vector2D(0, 1)))
            {
                return ("down");
            }
            else if (dir.Equals(new Vector2D(-1, 0)))
            {
                return ("left");
            }
            else
            {
                return ("right");
            }
        }

        /// <summary>
        /// used for respawned delay
        /// </summary>
        public void passFrame()
        {
            framepassed++;
        }

        /// <summary>
        /// used for getting the amount of frames that have passed
        /// </summary>
        /// <returns></returns>
        public int getFramePass()
        {
            return framepassed;
        }
        /// <summary>
        /// 
        /// used to set the amount of frames that have passed
        /// </summary>
        /// <param name="s"></param>
        public void setFramePass(int s)
        {
            framepassed = s;
        }
        /// <summary>
        /// used to set the speed of the snake
        /// </summary>
        /// <param name="s"></param>
        public void setSpeed(int s)
        {
            speed = s;
        }
        /// <summary>
        /// used to return the tail of the current snake
        /// </summary>
        /// <returns></returns>
        public Vector2D getTail()
        {
            return oldTail;
        }
        /// <summary>
        /// used to get the world size
        /// </summary>
        /// <param name="x"></param>
        public void getWS(int x)
        {
            ws = x;
        }
        /// <summary>
        /// used to set the snakes tail
        /// </summary>
        /// <param name="v"></param>
        public void setTail(Vector2D v)
        {
            oldTail = v;
        }
        /// <summary>
        /// used to get the boolean value indicating whether or not
        /// the snake has gotten a power up
        /// </summary>
        /// <returns></returns>
        public bool powUp()
        {
            return scoreUp;
        }
        /// <summary>
        /// resets the powerup boolean
        /// </summary>
        /// <param name="x"></param>
        public void setPow(bool x)
        {
            scoreUp = x; 
        }
        /// <summary>
        /// checks for wrap around 
        /// </summary>
        private void wrapAroundCheck()
        {
            Vector2D head = body[body.Count - 1];
            if (head.X > ws/2)
            {
                for (int i = 0; i < body.Count; i++)
                {
                    body[i].X -= ws;
                }
            }
            else if (head.X < -ws/2)
            {
                for (int i = 0; i < body.Count; i++)
                {
                    body[i].X += ws;
                }
            }
            else if (head.Y > ws/2)
            {
                for (int i = 0; i < body.Count; i++)
                {
                    body[i].Y -= ws;
                }
            }
            else if (head.Y < -ws/2)
            {
                for (int i = 0; i < body.Count; i++)
                {
                    body[i].Y += ws;
                }
            }

        }
    }
}