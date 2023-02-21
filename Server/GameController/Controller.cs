using NetworkUtil;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using World;

namespace GameController;

/// <summary>
/// This should contain logic for parsing the data received by the server,
/// updating the model accordingly, and anything else you think belongs here.
/// Key press handlers in your View should be "landing points" only,
/// and should invoke controller methods that contain the heavy logic.
/// </summary>
public class Controller
{
    // Controller events that the view can subscribe to

    //called everytime when receiving messages from the server
    public delegate void WorldChangedHandler();
    public event WorldChangedHandler? WorldChanged;

    //called when the connection to server is successfully
    public delegate void ConnectedHandler();
    public event ConnectedHandler? Connected;

    //called when the connection is unsuccssfully
    public delegate void ErrorHandler(string err);
    public event ErrorHandler? Error;

    //store the the setting
    private int id;
    private int worldSize;
    //store the incomplete message
    private string incomplete;


    /// <summary>
    /// State representing the connection with the server
    /// </summary>
    SocketState? theServer = null;
    SnakeWorld world;
    public Controller()
    {
        world = new SnakeWorld();
        incomplete = "";
    }

    /// <summary>
    /// This method is called when the client is trying to connect to the server
    /// </summary>
    /// <param name="name"></param>
    public void Connect(string name)
    {
        Networking.ConnectToServer(OnConnect, name, 11000);
    }

    /// <summary>
    /// this method is called when it's connecting, if successful, would infom the view
    /// </summary>
    /// <param name="state"></param>
    private void OnConnect(SocketState state)
    {
        //if error occor during connection stage
        if (state.ErrorOccurred)
        {
            // inform the view
            Error?.Invoke("Error connecting to server");
            return;
        }

        theServer = state;

        // inform the view
        Connected?.Invoke();

        // Start an event loop to receive messages from the server
        state.OnNetworkAction = ReceiveMessage;
        Networking.GetData(state);
    }

    /// <summary>
    /// Method to be invoked by the networking library when 
    /// data is available
    /// </summary>
    /// <param name="state"></param>
    private void ReceiveMessage(SocketState state)
    {
        if (state.ErrorOccurred)
        {
            // inform the view
            Error?.Invoke("Lost connection to server");
            return;
        }
        ProcessMessages(state);

        // Continue the event loop
        // state.OnNetworkAction has not been changed, 
        // so this same method (ReceiveMessage) 
        // will be invoked when more data arrives
        Networking.GetData(state);
    }

    /// <summary>
    /// Process any buffered messages separated by '\n'
    /// Then inform the view
    /// </summary>
    /// <param name="state"></param>
    private void ProcessMessages(SocketState state)
    {
        //lock to prevent racing condition when the controller is trying to add
        //objects to dictionary while the worldpanel is drawing from dictionary
        lock (world)
        {
            //parse the data line by line
            string totalData = state.GetData();
            string[] parts = Regex.Split(totalData, @"(?<=[\n])");
            string newPart = "";

            //loop through each line
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == "")
                {
                    break;
                }
                //only for the world size and id
                if (int.TryParse(parts[i].Remove(parts[i].Length - 1), out int x))
                {
                    for (int j = 0; j < 2; j++)
                    {

                        newPart = parts[j].Remove(parts[j].Length - 1);
                        if (j == 0)//set player's id
                        {
                            int.TryParse(newPart, out int s);
                            id = s;
                            world.upDatePlayerID(id);
                        }
                        if (j == 1)//set the world size
                        {
                            int.TryParse(newPart, out int s);
                            worldSize = s;
                            world.upDateWorldSize(worldSize);
                        }

                    }
                }
                else // for the json objects
                {

                    string temp = parts[i];

                    //if it's an empty line
                    if (temp.Length == 0)
                    {
                        break;
                    }
                    //converting char to string 
                    char c = temp[temp.Length - 1];
                    string last = c.ToString();
                    //if there's incomplete data at the end

                    if (!last.Equals("\n"))
                    {
                        incomplete += temp;
                        break;
                    }
                    //converting char to string
                    string edge = "X";
                  
                    //edge case, when the incomplete data is starting with {"X
                    if (!temp[0].ToString().Equals("{") || (temp[0].ToString().Equals("{") && temp[2].ToString().Equals(edge)))
                    {
                        incomplete += temp;
                        temp = incomplete;
                        if (!last.Equals("\n"))
                        {
                            break;
                        }
                     
                    }
                    incomplete = "";
                    //query the string
                    JObject obj = JObject.Parse(temp);
                    JToken? wtoken = obj["wall"];
                    JToken? stoken = obj["snake"];
                    JToken? ptoken = obj["power"];

                    newPart = temp.Remove(temp.Length - 1);//remove "\n" at the end

                    if (wtoken != null)//if the object is a wall
                    {
                        Walls? w = JsonConvert.DeserializeObject<Walls>(newPart);
                        if (w is null)
                        {

                        }
                        else
                        {
                            //check if it's already exist in the dictionary, if yes, update, if not, add it
                            if (world.Walls.ContainsKey(w.wall))
                            {
                                world.Walls.Remove(w.wall);
                            }
                            world.Walls.Add(w.wall, w);
                        }
                      

                    }
                    if (stoken != null)//if it's a snake
                    {
                        
                        Snake? s = JsonConvert.DeserializeObject<Snake>(newPart);
                        if(s is null)
                        {

                        }
                        else
                        {
                            //check if it's already exist in the dictionary, if yes, update, if not, add it
                            if (world.Snakes.ContainsKey(s.snake))
                            {
                                world.Snakes.Remove(s.snake);
                            }
                            world.Snakes.Add(s.snake, s);
                        }
                        
                    }
                    if (ptoken != null)//if it's a powerup
                    {
                        Powerup? p = JsonConvert.DeserializeObject<Powerup>(newPart);
                        if(p is null)
                        {

                        }
                        else
                        {
                            //check if it's already exist in the dictionary, if yes, update, if not, add it
                            if (world.Pow.ContainsKey(p.power))
                            {
                                world.Pow.Remove(p.power);
                            }
                            world.Pow.Add(p.power, p);
                        }
                       
                    }

                }
            }
            state.RemoveData(0, totalData.Length);// remove data from the socket state after process everything
        }

        WorldChanged?.Invoke();//after process the message, tell the view to draw it

    }
    /// <summary>
    /// Closes the connection with the server
    /// </summary>
    public void Close()
    {
        theServer?.TheSocket.Close();
    }

    /// <summary>
    /// Send a message to the server
    /// </summary>
    /// <param name="message"></param>
    public void Send(string message)
    {
        if (theServer is not null)
            Networking.Send(theServer.TheSocket, message + "\n");
    }

    /// <summary>
    /// return the world model
    /// </summary>
    /// <returns></returns>
    public SnakeWorld GetWorld()
    {
        return world;
    }

    /// <summary>
    /// return the current player ID
    /// </summary>
    /// <returns></returns>
    public int PlayID()
    {
        return id;
    }

    /// <summary>
    /// retrun the passed in worldSize
    /// </summary>
    /// <returns></returns>
    public int WorldSize()
    {
        return worldSize;
    }



}