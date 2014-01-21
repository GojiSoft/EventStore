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
using System;
using EventStore.Core.Data;
using EventStore.Core.Services;
using EventStore.Projections.Core.Messages;
using EventStore.Projections.Core.Services;
using EventStore.Projections.Core.Services.Processing;
using ResolvedEvent = EventStore.Projections.Core.Services.Processing.ResolvedEvent;

namespace EventStore.Projections.Core.Standard
{
    public class CategorizeEventsByStreamPath : IProjectionStateHandler
    {
        private readonly string _categoryStreamPrefix;
        private readonly StreamCategoryExtractor _streamCategoryExtractor;

        public CategorizeEventsByStreamPath(string source, Action<string> logger)
        {
            var extractor = StreamCategoryExtractor.GetExtractor(source, logger);
            // we will need to declare event types we are interested in
            _categoryStreamPrefix = "$ce-";
            _streamCategoryExtractor = extractor;
        }

        public void ConfigureSourceProcessingStrategy(SourceDefinitionBuilder builder)
        {
            builder.FromAll();
            builder.AllEvents();
            builder.SetIncludeLinks();
        }

        public void Load(string state)
        {
        }

        public void LoadShared(string state)
        {
            throw new NotImplementedException();
        }

        public void Initialize()
        {
        }

        public void InitializeShared()
        {
        }

        public string GetStatePartition(CheckpointTag eventPosition, string category, ResolvedEvent data)
        {
            throw new NotImplementedException();
        }

        public string TransformCatalogEvent(CheckpointTag eventPosition, ResolvedEvent data)
        {
            throw new NotImplementedException();
        }

        public bool ProcessEvent(
            string partition, CheckpointTag eventPosition, string category1, ResolvedEvent data,
            out string newState, out string newSharedState, out EmittedEventEnvelope[] emittedEvents)
        {
            newSharedState = null;
            emittedEvents = null;
            newState = null;
            string positionStreamId;
            bool isMetaStream;
            if (SystemStreams.IsMetastream(data.PositionStreamId))
            {
                isMetaStream = true;
                positionStreamId = data.PositionStreamId.Substring("$$".Length);
            }
            else
            {
                isMetaStream = false;
                positionStreamId = data.PositionStreamId;
            }
            var category = _streamCategoryExtractor.GetCategoryByStreamId(positionStreamId);
            if (category == null)
                return true; // handled but not interesting

            var isStreamDeletedEvent = false;
            if (isMetaStream)
            {
                if (data.EventType != SystemEventTypes.StreamMetadata)
                    return true; // handled but not interesting

                var metadata = StreamMetadata.FromJson(data.Data);
                //NOTE: we do not ignore JSON deserialization exceptions here assuming that metadata stream events must be deserializable

                if (metadata.TruncateBefore != EventNumber.DeletedStream)
                    return true; // handled but not interesting

                isStreamDeletedEvent = true;
            }

            if (data.EventType == SystemEventTypes.StreamDeleted)
                isStreamDeletedEvent = true;

            string linkTarget;
            if (data.EventType == SystemEventTypes.LinkTo) 
                linkTarget = data.Data;
            else 
                linkTarget = data.EventSequenceNumber + "@" + data.EventStreamId;

            emittedEvents = new[]
            {
                new EmittedEventEnvelope(
                    new EmittedLinkToWithRecategorization(
                        _categoryStreamPrefix + category, Guid.NewGuid(), linkTarget, eventPosition, expectedTag: null,
                        originalStreamId: positionStreamId, isStreamDeletedEvent: isStreamDeletedEvent))
            };

            return true;
        }

        public string TransformStateToResult()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }

        public IQuerySources GetSourceDefinition()
        {
            return SourceDefinitionBuilder.From(ConfigureSourceProcessingStrategy);
        }
    }
}
