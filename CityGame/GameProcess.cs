using System;
using System.Collections.Generic;
using System.Threading;

namespace CityGame
{
    public class GameProcess
    {
        public string GameState { get; set; } = string.Empty;
        public Guid CurrentLeaderId { get; set; } = Guid.Empty;
        public string LastCity { get; set; } = string.Empty;
        public char CurrentLetter { get; set; }
        public Timer Timer { get; set; } = null;
        public List<string> playedCities { get; set; } = new List<string>();

    }
}
