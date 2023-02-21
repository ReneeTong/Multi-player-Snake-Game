//store wall object
using Newtonsoft.Json;
using SnakeGame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace World
{
    [DataContract(Name = "Wall", Namespace = "")]
    /// <summary>
    /// Create a wall object
    /// </summary>
    public class Walls
    {
        [DataMember(Name = "ID")]
        [JsonProperty(PropertyName = "wall")] 
        public int wall;//the wall id
        [DataMember(Name = "p1")]
        public Vector2D p1 { get; private set; }
        [DataMember(Name = "p2")]
        public Vector2D p2 { get; private set; }

        /// <summary>
        /// initialized the wall
        /// </summary>
        public Walls()
        {
   
            p1 = new Vector2D();
            p2 = new Vector2D();

        }
      /// <summary>
      /// List class for XML deserialization purposes
      /// </summary>
        public class WallList : List<Walls> 
        {
            
        }

    }
}