//Server Controller
using System;
using System.Collections.Generic;
using NetworkUtil;
using System.Text.RegularExpressions;
using World;
using SnakeGame;
using Newtonsoft.Json;
using System.Timers;
using System.Xml;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Xml.Linq;
using System.IO;
using System.Text;

namespace Server;

/// <summary>
/// A server to handle clients for the snake game
/// </summary>
class SnakeServer
{
    // A map of clients that are connected, each with an ID
    private Dictionary<long, SocketState> clients;

    //store all the disconnected clients
    private HashSet<long> disconnectedClients = new HashSet<long>();

    //copy of the world
    private SnakeWorld world;

    //the number of milliseconds per frame
    private int msPerFrame;

    //stopwatch
    private Stopwatch watch;


    /// <summary>
    /// Starting the server
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
        SnakeServer server = new SnakeServer();
        server.StartServer();
        Console.Read();
    }

    /// <summary>
    /// Initialized the server's state
    /// </summary>
    public SnakeServer()
    {
        world = new SnakeWorld();
        clients = new Dictionary<long, SocketState>();
        watch = new Stopwatch();
    }

    /// <summary>
    /// Start accepting Tcp sockets connections from clients
    /// </summary>
    public void StartServer()
    {
        // This begins an "event loop"
        Networking.StartServer(NewClientConnected, 11000);

        //read the game settings file
        GameSettings gs = new();
        gs.fileread();

        //update the world according to game settings
        world = gs.sendWorld();

        //spawn powerups
        world.PowSpawn();
    }

    /// <summary>
    /// Method to be invoked by the networking library
    /// when a new client connects (see line 41)
    /// </summary>
    /// <param name="state">The SocketState representing the new client</param>
    private void NewClientConnected(SocketState state)
    {
        //check if there's an error
        if (state.ErrorOccurred)
            return;

        // Save the client state
        // Need to lock here because clients can disconnect at any time
        lock (clients)
        {
            clients[state.ID] = state;
        }

        // change the state's network action to the 
        // receive handler so we can process data when something
        // happens on the network
        state.OnNetworkAction = ProcessFirstMessage;
        Networking.GetData(state);
    }

    /// <summary>
    /// only being called when a client first connected, send the id, world size and walls
    /// </summary>
    /// <param name="state"></param>
    private void ProcessFirstMessage(SocketState state)
    {
        //check if there's any errors
        if (state.ErrorOccurred)
        {
            RemoveClient(state.ID);
            return;
        }

        //get data from socket state
        string totalData = state.GetData();
        string name = totalData.Remove(totalData.Length - 1);
        msPerFrame = world.msPerFrame;

        Snake s = new Snake(state.ID, name);
        //create a new snake

        //add the new snake to the model
        lock (world)
        {
            world.Snakes[s.snake] = s;
        }

        //send the id
        if (!Networking.Send(state.TheSocket!, s.snake + "\n" + world.universeSize + "\n"))
        {
            disconnectedClients.Add(s.snake);
        }

        //send the walls to the client
        foreach (Walls w in world.Walls.Values)
        {
            Networking.Send(state.TheSocket!, JsonConvert.SerializeObject(w) + "\n");
        }

        //update event loop for handling commands send from the client
        state.OnNetworkAction = ProcessMessage;

        //start updating frames
        Thread t = new(new ThreadStart(updatesFrame));
        t.Start();

        //clear the date
        state.RemoveData(0, totalData.Length);
        Networking.GetData(state);
    }


    /// <summary>
    /// Given the data that has arrived so far, 
    /// potentially from multiple receive operations, 
    /// determine if we have enough to make a complete message,
    /// and process it (print it and broadcast it to other clients).
    /// </summary>
    /// <param name="sender">The SocketState that represents the client</param>
    private void ProcessMessage(SocketState state)
    {
        //check if there's any error
        if (state.ErrorOccurred)
        {
            RemoveClient(state.ID);
            return;
        }

        //get all the data
        string totalData = state.GetData();
        string[] parts = Regex.Split(totalData, @"(?<=[\n])");

        //loop through all the messages
        foreach (string p in parts)
        {
            //if empty, ignore
            if (p.Length == 0)
                continue;

            // The regex splitter will include the last string even if it doesn't end with a '\n',
            // So we need to ignore it if this happens. 
            if (p[p.Length - 1] != '\n')
                break;

            //get the command
            string command = p.Remove(p.Length - 1);
            ControlCommands? c = JsonConvert.DeserializeObject<ControlCommands>(command);

            //update the snake movement according to the client's
            lock (world)
            {
                Snake s;
                if(world.Snakes.TryGetValue((int)state.ID, out s))
                {
                    //check if the direction is different than before
                    if (!c.GetDir().Equals(s.dir) && !c.GetDir().Equals(new Vector2D(0, 0)))
                    {
                        s.Turn(c.GetDir());//handle snake turn
                    }
                }
            }

            // Remove it from the SocketState's growable buffer
            state.RemoveData(0, p.Length);

            //remove disconnected clients
            foreach (long id in disconnectedClients)
            {
                RemoveClient(id);
            }

        }
        //state.RemoveData(0, totalData.Length);
        Networking.GetData(state);
    }

    /// <summary>
    /// Removes a client from the clients dictionary
    /// </summary>
    /// <param name="id">The ID of the client</param>
    private void RemoveClient(long id)
    {
        //make sure it's not modified the world, clients while the other is looping through it
        lock (clients)
        {
            world.Snakes.Remove((int)id);
            clients.Remove(id);
            disconnectedClients.Remove(id);
        }
    }

    /// <summary>
    /// infinate loop to update the frame by the frame rate 
    /// </summary>
    private void updatesFrame()
    {
        while (true)
        {
            watch.Start();
            //make sure it wait for the right time
            while (watch.ElapsedMilliseconds < msPerFrame)
            { Thread.Sleep(1); }

            watch.Restart();

            //actually update the frame
            update();
           
        }
    }

    /// <summary>
    /// update everything in the world
    /// </summary>
    private void update()
    {
        //store everything in the snakeworld
        StringBuilder sb = new StringBuilder();
        lock (world)
        {
            //get all the powerups
            foreach (Powerup p in world.Pow.Values)
            {
                string powerUp = JsonConvert.SerializeObject(p);
                sb.Append(powerUp + "\n");
            }

            //get all the snakes
            foreach (Snake s in world.Snakes.Values)
            {
                //remove all the disconnected snakes
                if (s.alive || (!s.alive && s.died && s.dc))
                {
                    string snake = JsonConvert.SerializeObject(s);
                    sb.Append(snake + "\n");
                }

            }
        }

        //send to each clients
        lock (clients)
        {

            foreach (SocketState client in clients.Values)
            {
                //update the snake and check for any collisions each frame
                Snake? s = new Snake();
                if (world.Snakes.TryGetValue((int)client.ID, out s))
                {
                    lock (world)
                    {
                        s.getWS(world.universeSize);
                        s.UpdateSnake();
                        world.Collisions();
                    }
  
                }

                //send the json string of the powerups and snakes to clients.
                string temp = sb.ToString();
                if (!Networking.Send(client.TheSocket!, temp))
                {
                    //detect if there's disconnected snakes
                    if (world.Snakes.TryGetValue((int)client.ID, out s))
                    {
                        s.dc = true;
                        disconnectedClients.Add(client.ID);
                    }
                }


            }
        }
    }
}

