using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using PlaxFm.Models;
using Ninject.MockingKernel;
using Moq;
using Ninject.Extensions.Logging;
using Ninject.MockingKernel.Moq;
using PlaxFm.Configuration;

namespace PlaxFm.Tests.Unit
{
    [TestFixture]
    public class PlexSongEntryTests
    {
        private readonly MoqMockingKernel _kernel;

        public PlexSongEntryTests()
        {
            _kernel = new Ninject.MockingKernel.Moq.MoqMockingKernel();
            _kernel.Bind<IAppSettings>().ToMock();
        }

        [Test]
        public void TestNewSongEntry()
        {
            var entries = FakeLogFactory.CreatePlexMediaServerLogs();
            var songList = entries.Select(log => new SongEntry(log)).Where(song => song.MediaId > 0 && song.UserId > 0).ToList();
            songList.Count.Should().Be(2);
        }

        [Test]
        public void TestNewSongEntryMetadata()
        {
            //Todo: create mock for this
            var log = new PlexMediaServerLog
            {
                DateAdded = "Sep 16, 2015 10:08:04:991",
                LogEntry = "5672] DEBUG - Library item 107202 'When We Die' got played by account 1!"
            };
            var song = FakeSongFactory.CreateResponse(new SongEntry(log));
            song.MediaId.Should().Be(107202);
            song.UserId.Should().Be(1);
            song.Artist.Should().Be("Jack Peñate");
            song.Title.Should().Be("When We Die");
            song.Album.Should().Be("Matinée");
            var date = log.DateAdded.Trim();
            var format = "MMM dd, yyyy HH:mm:ss:fff";
            var timePlayed = DateTime.ParseExact(date, format, CultureInfo.InvariantCulture).ToUniversalTime();
            song.TimePlayed.Should().Be(timePlayed);

        }
    }
}
