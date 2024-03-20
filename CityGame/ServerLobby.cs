using System;
using System.Collections.Generic;

namespace CityGame
{
    public class ServerLobby
    {
        public Guid Id { get; set; }
        public List<ServerUser> Players { get; set; }

        public GameProcess gameProcess { get; set; }
    }
}
