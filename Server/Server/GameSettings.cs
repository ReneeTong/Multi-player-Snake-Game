//Game settings class
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using World;

namespace Server
{/// <summary>
/// Class for getting the settings of the game
/// </summary>
    [DataContract(Name = "GameSettings", Namespace = "")]
    public class GameSettings
    {
        [DataMember(Name = "FramesPerShot")]
        public int fps  { get; set; }

        [DataMember(Name = "MSPerFrame")]
        public int mps { get; set; }

        [DataMember(Name = "RespawnRate")]
        public int rr { get; set; }

        [DataMember(Name = "UniverseSize")]
        public int us { get; set; }

        [DataMember(Name = "Walls")]
        public Walls.WallList Walls { get; set; }

        private SnakeServer server;
        private SnakeWorld w;
        /// <summary>
        /// GameSettings constructor
        /// </summary>
        public GameSettings()
        {
            w = new SnakeWorld();
            server = new();
            Walls = new Walls.WallList();   
        }
        /// <summary>
        /// This methods reads the given settings file
        /// </summary>
        public void fileread()      
        {
            //creat an xml reader
            using (XmlReader reader = XmlReader.Create("settings.xml"))
            {
                //creat a data contract serializer
                DataContractSerializer serializer = new DataContractSerializer(typeof(GameSettings));
                //read the new game settings object
                GameSettings? gs = (GameSettings?)serializer.ReadObject(reader);

                //add the game settings to the world
                foreach (Walls wall in gs.Walls)
                {
                    w.Walls[wall.wall] = wall;
                }
                w.respawnRate = gs.rr;
                w.msPerFrame = gs.mps;
                w.framesPerShot = gs.fps;
                w.universeSize = gs.us;
            }
        }
        /// <summary>
        /// Send the new world to the controller
        /// </summary>
        /// <returns></returns>
        public SnakeWorld sendWorld()
        {
            return w;
        }
    }
    
}
