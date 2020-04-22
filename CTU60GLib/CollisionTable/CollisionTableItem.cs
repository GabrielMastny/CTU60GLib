using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib.CollisionTable
{
    public class CollisionTableItem
    {
        public int Id { get; }
        public bool Owned { get; }
        public string Name{get;}
        public bool Collision{get;}
        public string Type{get;}
        public string Link{get;}

        public CollisionTableItem(string id, bool owned, string name, bool collision, string type, string link)
        {
            Id = int.Parse(id.Replace("#", ""));
            Owned = bool.Parse(owned.ToString());
            Name = name;
            Collision = bool.Parse(collision.ToString());
            Type = type;
            Link = link;
        }
    }
}
