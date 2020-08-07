using System;
using System.Collections.Generic;
using System.Text;

namespace Cauldron
{
    /// <summary>
    /// Serializable class representing a single JSON object in a blaseball socket.io update
    /// Currently the only thing this parser cares about is the Schedule - the game updates themselves
    /// </summary>
    class Update
    {
        public List<Game> Schedule { get; set; }
    }
}
