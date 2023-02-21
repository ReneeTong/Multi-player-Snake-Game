//contain everything in the snake world
using SnakeGame;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace World;
/// <summary>
/// This is the model that stores everything
/// </summary>
public class SnakeWorld
{
    public int playerID; // the current player id given by the server
    public Dictionary<int, Snake> Snakes;//all the snakes in the world
    public Dictionary<int, Powerup> Pow;// all the powerup in the world
    public Dictionary<int, Walls> Walls; //all the walls in the world
   
    //the number of frames per shot given by the game settings
    public int framesPerShot;
    //the number of milliseconds between frames
    public int msPerFrame;
    //number of frames to wait to respawn
    public int respawnRate;
    //size of the world
    public int universeSize;



    private delegate bool handleCollide(Object o);

    /// <summary>
    /// initialized the world model
    /// </summary>
    public SnakeWorld()
    {
        playerID = 0;
        Snakes = new Dictionary<int, Snake>();
        Pow = new Dictionary<int, Powerup>();
        Walls = new Dictionary<int, Walls>();
        
    }

    /// <summary>
    /// change player ID to the one given by the server
    /// </summary>
    /// <param name="id"></param>
    public void upDatePlayerID(int id)
    {
        playerID = id;
    }

    /// <summary>
    /// return player id
    /// </summary>
    /// <returns></returns>
    public int getPlayerID()
    {
        return playerID;
    }

    /// <summary>
    /// change world size to the one given by the server
    /// </summary>
    /// <param name="size"></param>
    public void upDateWorldSize(int size)
    {
       universeSize = size; 
    }

    /// <summary>
    /// return the world size
    /// </summary>
    /// <returns></returns>
    public int getWorldSize()
    {
        return universeSize;
    }

    /// <summary>
    /// spawns 20 powerups
    /// </summary>
    public void PowSpawn()
    {
        for (int i = 0; i < 20; i++)
        {
            PowCreate(i);
        }
    }
    /// <summary>
    /// creates a new powerup
    /// </summary>
    /// <param name="id"></param>
    private void PowCreate(int id)
    {
        Powerup p = new Powerup();
        p.PowSpawn();
        //if spawned on a wall or out of bounds
        while (!powCollide(p))
        {
            p.PowSpawn();
        }
  
        lock (Pow)
        {
            p.power = id;
            Pow[id] = p;
        }

    }
    /// <summary>
    /// creates a new snake
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    private void SnakeCreate(int id, string name)
    {
        Snake newS = new Snake(id, name);
        newS.SnakeSpawn();
        newS.getWS(universeSize);
        //if spawned on a wall or out of bounds
        while (!snakeCollide(newS))
        {
            newS.SnakeSpawn();
        }

        lock (Snakes)
        {
            newS.snake = id;
            Snakes[id] = newS;
        }

    }
    /// <summary>
    /// This method checks whether or not a snake collides with all the other objects
    /// in the current world every frame. 
    /// </summary>
    public void Collisions()
    {
        //loop through all the snakes
        foreach (Snake s in Snakes.Values)
        {
            //if the snake is disconnected
            if (s.dc)
            {
                s.died = true;
                s.alive = false;
                continue;
            }
            //check if the snake hit a powerup
            PowCheck(s);
            //check if the snake hit a wall
            wallCollide(s.body[s.body.Count - 1].X + 5, s.body[s.body.Count - 1].Y + 5, SnakeDied, s);
            //check our snake with every other snake
            foreach (Snake thisSnake in Snakes.Values)
            {
                //ignoring if it is our own snake
                if (s.snake.Equals(thisSnake.snake))
                {
                    continue;
                }

                //loop through all the segments of every other snake and check if the current
                //snakes head has collided
                Vector2D head = s.body[s.body.Count - 1];
                int count = thisSnake.body.Count > 2 ? thisSnake.body.Count - 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    Vector2D p1 = new Vector2D(thisSnake.body[i].X, thisSnake.body[i].Y);
                    Vector2D p2 = new Vector2D(thisSnake.body[i + 1].X, thisSnake.body[i + 1].Y);
                    if (p2.X == p1.X) //vertical wall
                    {

                        checkY(p1, p2);
                        //acount for the width
                        if (p1.X + 5 >= head.X + 5 && p1.X - 5 <= head.X + 5
                            && p1.Y - 5 <= head.Y + 5 && head.Y + 5 <= p2.Y + 5)
                        {
                            SnakeDied(s);
                            //all conditions to do when snake dies/respawns
                        }
                    }
                    else
                    {
                        checkX(p1, p2);
                        //acount for the width
                        if (p1.X - 5 <= head.X + 5 && p2.X + 5 >= head.X + 5
                             && p1.Y + 5 >= head.Y + 5 && head.Y + 5 >= p2.Y - 5)
                        {
                            SnakeDied(s);
                            //all conditions to do when snake dies/respawns
                        }
                    }
                }
            }

            //self collision
            if (s.body.Count > 4)
            {
                Vector2D head = s.body[s.body.Count - 1];
                for (int i = 0; i < s.body.Count - 4; i++)
                {
                    Vector2D p1 = new Vector2D(s.body[i].X, s.body[i].Y);
                    Vector2D p2 = new Vector2D(s.body[i + 1].X, s.body[i + 1].Y);
                    if (p2.X == p1.X) //vertical 
                    {
                        checkY(p1, p2);
                        //acount for the width
                        if (p1.X + 5 >= head.X + 5 && p1.X - 5 <= head.X + 5
                            && p1.Y - 5 <= head.Y + 5 && head.Y + 5 <= p2.Y + 5)
                        {
                 
                            SnakeDied(s);
                        }
                    }
                    else
                    {
                        checkX(p1, p2);
                        //acount for the width
                        if (p1.X - 5 <= head.X + 5 && p2.X + 5 >= head.X + 5
                             && p1.Y + 5 >= head.Y + 5 && head.Y + 5 >= p2.Y - 5)
                        {
                            SnakeDied(s);
                        }
                    }
                }
            }
            
            //if the snake has died
            if (s.died)
            {
                //count how many frames ave passed
                int framepass = s.getFramePass();
                if (framepass > respawnRate)
                {
                    //create new snake
                    SnakeCreate(s.snake, s.name);
                    s.setFramePass(0);

                }
                else if (framepass > 5)
                {
                    s.alive = false;
                    s.passFrame();

                }
                else
                {
                    s.passFrame();//increment by 1
                }
            }
        }
    }
    /// <summary>
    /// check if a snake has spawned on top of a wall or out of bounds
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private bool snakeCollide(Snake s)
    {
        Vector2D location = s.body[s.body.Count-1];
        int bounds = universeSize / 2;
        //check if the snake has spawned out of bounds
        if (location.X > bounds || location.X < -bounds
        || location.Y > bounds || location.Y < -bounds)
        {
            return false;
        }
        //loop through all the walls to check if the snake has collided
        foreach (Walls w in Walls.Values)
        {

            if (w.p2.X == w.p1.X) //vertical wall
            {
                checkY(w.p1, w.p2);
                //acount for the width
                if (w.p1.X + 25 >= location.X + 5 && w.p1.X - 25 <= location.X + 5
                    && w.p1.Y - 25 <= location.Y + 5 && location.Y + 5 <= w.p2.Y + 25)
                {
                    return false;
                }
            }
            else
            {
                checkX(w.p1, w.p2);
                //acount for the width
                if (w.p1.X - 25 <= location.X + 5 && w.p2.X + 25 >= location.X + 5
                     && w.p1.Y + 25 >= location.Y + 5 && location.Y + 5 >= w.p2.Y - 25)
                {
                    return false;
                }
            }

        }
        return true;
    }

    /// <summary>
    /// Check is a powerup has spawned on top of a wall or out of bounds.
    /// Same logic as snakeCOllide
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    private bool powCollide(Powerup p)
    {
        Vector2D location = p.loc;
        int bounds = universeSize / 2;
        if(location.X > bounds || location.X < -bounds
        || location.Y > bounds || location.Y < -bounds)
        {
            return false;
        }
        
        foreach (Walls w in Walls.Values)
        {

            if (w.p2.X == w.p1.X) //vertical wall
            {
                checkY(w.p1, w.p2);
                //acount for the width
                if (w.p1.X + 25 >= location.X + 8 && w.p1.X - 25 <= location.X + 8
                    && w.p1.Y - 25 <= location.Y + 8 && location.Y + 8 <= w.p2.Y + 25)
                {
                    return false;
                }
            }
            else
            {
                checkX(w.p1, w.p2);
                //acount for the width
                if (w.p1.X - 25 <= location.X + 8 && w.p2.X + 25 >= location.X + 8
                     && w.p1.Y + 25 >= location.Y + 8 && location.Y + 8 >= w.p2.Y - 25)
                {
                    return false;
                }
            }

        }
        //wallCollide(location.X + 8, location.Y + 8 ,(p) => false, p);
        return true;
    }
    /// <summary>
    /// This method checks if a snake has gotten a powerup.
    /// If it has, the snake grows larger and the snake's score is incremented.
    /// </summary>
    /// <param name="s"></param>
    /// <returns></returns>
    private bool PowCheck(Snake s)
    {
  
        foreach(Powerup p in Pow.Values)
        {
            //if the snakes head is within the powerup
            if ((p.loc - s.body[s.body.Count-1]).Length() < 13)
            {
                //the powerup has been eaten
                p.died = true;
                s.setPow(true);

                //start counting frames
                int pframe = p.getFramePass();
                Random rng = new Random();
                int wait = rng.Next(1, 200);

                if (pframe < wait)
                {
                    p.passFrame();
                }
                else
                {
                    //create a new powerup
                    PowCreate(p.power);
                    s.setFramePass(0);
                }

       
                //count frames and grow the snake
                int frames = s.getFramePass();
                if(frames < 12)
                {
                    s.body[0] += s.dir * 0;
                    s.passFrame();
                }
                else
                {
                    s.body[0] += s.dir * 3;
                    s.setFramePass(0);
                }
                return true;
            }
        }
        //increment the snakes score if it has gotten a powerup
        if (s.powUp())
        {
            s.score++;
            s.setPow(false);
        }
        return false;
    }
    /// <summary>
    /// This method is used to check if an object has hit a wall. 
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="h"></param>
    /// <param name="o"></param>
    private void wallCollide(double x, double y, handleCollide h, Object o)
    {
        foreach (Walls w in Walls.Values)
        {

            if (w.p2.X == w.p1.X) //vertical wall
            {
                checkY(w.p1, w.p2);
                //acount for the width
                if (w.p1.X + 25 >= x && w.p1.X - 25 <= x
                    && w.p1.Y - 25 <= y + 5 && y <= w.p2.Y + 25)
                {
                    //all conditions to do when snake dies/respawns
                    h(o);
                }
            }
            else
            {
                checkX(w.p1, w.p2);
                //acount for the width
                if (w.p1.X - 25 <= x && w.p2.X + 25 >= x + 5
                     && w.p1.Y + 25 >= y + 5 && y >= w.p2.Y - 25)
                {
                    h(o);
                    //all conditions to do when snake dies/respawns
                }
            }

        }
    }
    /// <summary>
    /// Helper method for handling conditions when a snae dies
    /// </summary>
    /// <param name="o"></param>
    /// <returns></returns>
    private bool SnakeDied(Object o)
    {
        Snake s = (Snake)o;
        s.setSpeed(0);
        s.died = true;
        return true;
        //all conditions to do when snake dies/respawns
    }
    /// <summary>
    /// Helper method to check which way X points are oriented
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    private void checkX(Vector2D p1, Vector2D p2)
    {
        if (p2.X < p1.X)
        {
            double temp = p1.X;
            p1.X = p2.X;
            p2.X = temp;
        }
    }
    /// <summary>
    /// Helper method to check which way Y points are oriented
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    private void checkY(Vector2D p1, Vector2D p2)
    {
        if (p2.Y < p1.Y)
        {
            double temp = p1.Y;
            p1.Y = p2.Y;
            p2.Y = temp;
        }
    }

}