using System;
using System.Collections.Generic;
using System.Text;

namespace CTU60GLib.CollisionTable
{
    public class CollisionTableItem
    {
        public string Id { get; }
        public string Owned { get; }
        public string Name{get;}
        public string Collision{get;}
        public string Type{get;}
        public string Link{get;}

        public CollisionTableItem(string id, bool owned, string name, bool collision, string type, string link)
        {
            Id = id.Replace("#", "");
            Owned = owned.ToString();
            Name = name;
            Collision = collision.ToString();
            Type = type;
            Link = link;
        }
    }
}
