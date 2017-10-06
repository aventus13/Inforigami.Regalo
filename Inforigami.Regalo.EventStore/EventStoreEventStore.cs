﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;
using Inforigami.Regalo.Core;
using Inforigami.Regalo.EventSourcing;
using Inforigami.Regalo.Interfaces;
using Newtonsoft.Json;
using ILogger = Inforigami.Regalo.Core.ILogger;

namespace Inforigami.Regalo.EventStore
{
    public class EventStoreEventStore : IEventStore, IDisposable
    {
        private readonly IEventStoreSession _eventStoreConnection;
        private readonly ILogger _logger;

        public EventStoreEventStore(IEventStoreSession eventStoreConnection, ILogger logger)
        {
            if (eventStoreConnection == null) throw new ArgumentNullException("eventStoreConnection");
            if (logger == null) throw new ArgumentNullException("logger");

            _eventStoreConnection = eventStoreConnection;
            _logger = logger;
        }

        public void Save<T>(string aggregateId, int expectedVersion, IEnumerable<IEvent> newEvents)
        {
            var eventStoreExpectedVersion = expectedVersion == -1 ? ExpectedVersion.NoStream : expectedVersion;

            try
            {
                _logger.Debug(this, "Saving " + typeof(T) + " to stream " + aggregateId);

                _eventStoreConnection.AppendToStreamAsync(aggregateId, eventStoreExpectedVersion, GetEventData(newEvents)).Wait();
            }
            catch (WrongExpectedVersionException ex)
            {
                // Wrap in a Regalo-defined exception so that callers don't have to worry what impl is in place
                var concurrencyException = new EventStoreConcurrencyException("Unable to save to EventStore", ex);
                throw concurrencyException;
            }
        }

        public EventStream<T> Load<T>(string aggregateId)
        {
            return Load<T>(aggregateId, EventStreamVersion.Max);
        }

        public EventStream<T> Load<T>(string aggregateId, int version)
        {
            if (string.IsNullOrWhiteSpace(aggregateId)) throw new ArgumentException("An aggregate ID is required", "aggregateId");

            if (version == EventStreamVersion.NoStream)
            {
                throw new ArgumentOutOfRangeException("version", "By definition you cannot load a stream when specifying the EventStreamVersion.NoStream (-1) value.");
            }

            _logger.Debug(this, "Loading " + typeof(T) + " version " + version + " from stream " + aggregateId);

            var streamEvents = new List<ResolvedEvent>();

            StreamEventsSlice currentSlice;
            var nextSliceStart = StreamPosition.Start;
            do
            {
                currentSlice = _eventStoreConnection.ReadStreamEventsForwardAsync(aggregateId, nextSliceStart, 200, false).Result;

                nextSliceStart = currentSlice.NextEventNumber;

                foreach (var evt in currentSlice.Events)
                {
                    streamEvents.Add(evt);
                    if (evt.OriginalEventNumber == version)
                    {
                        break;
                    }
                }

            } while (!currentSlice.IsEndOfStream);

            if (streamEvents.Count == 0)
            {
                return null;
            }

            var domainEvents = streamEvents.Select(x => BuildDomainEvent(x.OriginalEvent.Data, x.OriginalEvent.Metadata)).ToList();
            var result = new EventStream<T>(aggregateId);
            result.Append(domainEvents);

            if (version != EventStreamVersion.Max && result.GetVersion() != version)
            {
                var exception = new ArgumentOutOfRangeException("version", version, string.Format("Event for version {0} could not be found for stream {1}", version, aggregateId));
                exception.Data.Add("Existing stream", domainEvents);
                throw exception;
            }

            return result;
        }

        public void Delete(string aggregateId, int version)
        {
            _eventStoreConnection.Delete(aggregateId, version);
        }

        private static IEvent BuildDomainEvent(byte[] data, byte[] metadata)
        {
            var dataJson = Encoding.UTF8.GetString(data);

            var evt = (IEvent)JsonConvert.DeserializeObject(dataJson, GetDefaultJsonSerializerSettings());

            return evt;
        }

        public void Dispose()
        {
        }

        private static IEnumerable<EventData> GetEventData(IEnumerable<IEvent> newEvents)
        {
            return newEvents.Select(
                x => new EventData(
                    x.MessageId,
                    GetEventTypeFriendlyName(x),
                    true,
                    GetEventBytes(x),
                    null)); }

        private static byte[] GetEventBytes(IEvent evt)
        {
            if (evt == null) throw new ArgumentNullException("evt");

            return GetBytes(evt);
        }

        private static byte[] GetBytes(object item)
        {
            var json = JsonConvert.SerializeObject(item, Formatting.None, GetDefaultJsonSerializerSettings());
            var bytes = Encoding.UTF8.GetBytes(json);
            return bytes;
        }

        private static JsonSerializerSettings GetDefaultJsonSerializerSettings()
        {
            var settings = new JsonSerializerSettings();

            settings.TypeNameHandling = TypeNameHandling.All;

            settings.Converters.Add(new DomainValueConverter<Guid>(Guid.Parse));
            settings.Converters.Add(new DomainValueConverter<int>(int.Parse));
            settings.Converters.Add(new DomainValueConverter<string>(s => s));

            return settings;
        }

        private static string GetEventTypeFriendlyName(IEvent evt)
        {
            if (evt == null) throw new ArgumentNullException("evt");

            return evt.GetType().Name.ToCamelCase();
        }
    }
}