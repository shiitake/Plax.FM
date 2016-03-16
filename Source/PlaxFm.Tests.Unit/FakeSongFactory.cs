using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using PlaxFm.Models;

namespace PlaxFm.Tests.Unit
{
    public class FakeSongFactory
    {
        public static SongEntry CreateResponse(SongEntry song)
        {
            var doc = XDocument.Load("xml/ExampleSongData.xml");
            return song.PopulateSongData(song, doc);
        }

        public static SongEntry CreateResponseUmlaut(SongEntry song)
        {
            var doc = XDocument.Load("xml/ExampleSongData.xml");
            return song.PopulateSongData(song, doc);
        }
    }
}
