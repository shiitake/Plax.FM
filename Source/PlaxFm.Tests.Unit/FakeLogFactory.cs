using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileHelpers;
using PlaxFm.Models;

namespace PlaxFm.Tests.Unit
{
    public static class FakeLogFactory
    {
        public static PlexMediaServerLog[] CreatePlexMediaServerLogs()
        {
            var engine = new FileHelperEngine<PlexMediaServerLog>();
            return engine.ReadFile("logs/ExamplePlexMediaServer.log");
        }
    }
}
