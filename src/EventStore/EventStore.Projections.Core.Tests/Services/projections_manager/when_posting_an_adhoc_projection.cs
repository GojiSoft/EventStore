// Copyright (c) 2012, Event Store LLP
// All rights reserved.
// 
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions are
// met:
// 
// Redistributions of source code must retain the above copyright notice,
// this list of conditions and the following disclaimer.
// Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
// Neither the name of the Event Store LLP nor the names of its
// contributors may be used to endorse or promote products derived from
// this software without specific prior written permission
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT
// HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
// 

using System.Linq;
using EventStore.Core.Messaging;
using EventStore.Core.Tests.Fakes;
using EventStore.Projections.Core.Messages;
using EventStore.Projections.Core.Services.Management;
using NUnit.Framework;

namespace EventStore.Projections.Core.Tests.Services.projections_manager
{
    [TestFixture]
    public class when_posting_an_adhoc_projection
    {
        private ProjectionManager _manager;
        private FakePublisher _publisher;

        [SetUp]
        public void setup()
        {
            _publisher = new FakePublisher();
            _manager = new ProjectionManager(_publisher, checkpointForStatistics: null);

            _manager.Handle(new ProjectionManagementMessage.Post(new PublishEnvelope(_publisher), @"log(1);", enabled: true));
        }

        [TearDown]
        public void TearDown()
        {
            _manager.Dispose();
        }

        [Test]
        public void projection_updated_is_published()
        {
            Assert.AreEqual(1, _publisher.Messages.OfType<ProjectionManagementMessage.Updated>().Count());
        }
    }
}