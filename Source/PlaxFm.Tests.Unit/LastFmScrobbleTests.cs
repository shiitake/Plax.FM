using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Ninject.MockingKernel;
using Ninject.MockingKernel.Moq;
using NUnit.Framework;
using PlaxFm.Configuration;
using PlaxFm.Models;

namespace PlaxFm.Tests.Unit
{
    [TestFixture]
    public class LastFmScrobbleTests
    {
        private readonly MoqMockingKernel _kernel;
        public LastFmScrobbleTests()
        {
            _kernel = new Ninject.MockingKernel.Moq.MoqMockingKernel();
            _kernel.Bind<IAppSettings>().ToMock();
        }
        [Test]
        public void TestLastFmSignature()
        {
            var scrobbler = new LastFmScrobbler(null, null, null);
            var log = new PlexMediaServerLog
            {
                DateAdded = "Sep 16, 2015 10:08:04:991",
                LogEntry = "5672] DEBUG - Library item 107202 'When We Die' got played by account 1!"
            };
            var song = FakeSongFactory.CreateResponse(new SongEntry(log));
            var request = FakeLastFmFactory.CreateScrobbleRequest(song);
            var sig = scrobbler.GenerateLastFmSignature(request);
            var hash = "9B6065D4E85C7E1F726C102CC49120D9";
            sig.Should().Be(hash);

        }
    }
}
