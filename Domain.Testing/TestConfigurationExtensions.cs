// Copyright (c) Microsoft. All rights reserved. 
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using Microsoft.Its.Domain.Serialization;
using Microsoft.Its.Domain.Sql;
using Microsoft.Its.Domain.Sql.CommandScheduler;
using Newtonsoft.Json;
using Pocket;

namespace Microsoft.Its.Domain.Testing
{
    public static class TestConfigurationExtensions
    {
        /// <summary>
        /// Sets up in-memory command scheduling for all known aggregate types.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public static Configuration UseInMemoryCommandScheduling(this Configuration configuration)
        {
            AggregateType.KnownTypes.ForEach(t =>
            {
                var initializerType = typeof (InMemoryCommandSchedulerPipelineInitializer<>)
                    .MakeGenericType(t);
                var initializer = ((ISchedulerPipelineInitializer) configuration.Container
                                                                                .Resolve(initializerType));
                initializer.Initialize(configuration);
            });

            return configuration;
        }

        public static Configuration IgnoreScheduledCommands(this Configuration configuration)
        {
            configuration.Container.RegisterGeneric(variantsOf: typeof (ICommandScheduler<>),
                                                    to: typeof (IgnoreCommandScheduling<>));
            return configuration;
        }

        public static Configuration UseInMemoryEventStore(
            this Configuration configuration, 
            bool traceEvents = false)
        {
            configuration.Container
                         .RegisterSingle(c => new ConcurrentDictionary<string, IEventStream>(StringComparer.OrdinalIgnoreCase))
                         .AddStrategy(type => InMemoryEventSourcedRepositoryStrategy(type, configuration.Container))
                         .Register<ICommandPreconditionVerifier>(c => c.Resolve<InMemoryCommandPreconditionVerifier>());

            if (traceEvents)
            {
                var tracingSubscription = configuration.EventBus
                                                       .Events<IEvent>()
                                                       .Subscribe(TraceEvent);
                configuration.RegisterForDisposal(tracingSubscription);
            }

            configuration.IsUsingSqlEventStore(false);

            return configuration;
        }

        private static void TraceEvent(IEvent e)
        {
            Trace.WriteLine(string.Format("{0}.{1}",
                                          e.EventStreamName(),
                                          e.EventName()));
            Trace.WriteLine(
                e.ToJson(Formatting.Indented)
                 .Split('\n')
                 .Select(line => "   " + line)
                 .ToDelimitedString("\n"));
        }

        internal static Func<PocketContainer, object> InMemoryEventSourcedRepositoryStrategy(Type type, PocketContainer container)
        {
            if (type.IsGenericType &&
                (type.GetGenericTypeDefinition() == typeof (IEventSourcedRepository<>) ||
                 type.GetGenericTypeDefinition() == typeof (InMemoryEventSourcedRepository<>)))
            {
                var aggregateType = type.GenericTypeArguments.Single();
                var repositoryType = typeof (InMemoryEventSourcedRepository<>).MakeGenericType(aggregateType);

                var streamName = AggregateType.EventStreamName(aggregateType);

                // get the single registered event stream instance
                var stream = container.Resolve<ConcurrentDictionary<string, IEventStream>>()
                                      .GetOrAdd(streamName,
                                                name => container.Clone()
                                                                 .Register(_ => name)
                                                                 .Resolve<IEventStream>());

                return c => Activator.CreateInstance(repositoryType, stream, c.Resolve<IEventBus>());
            }

            if (type == typeof (IEventStream))
            {
                return c => c.Resolve<InMemoryEventStream>();
            }

            return null;
        }

        public static Configuration UseInMemoryReservationService(this Configuration configuration)
        {
            var inMemoryReservationService = new InMemoryReservationService();
            configuration.Container.RegisterSingle<IReservationService>(c => inMemoryReservationService);
            configuration.Container.RegisterSingle<IReservationQuery>(c => inMemoryReservationService);
            return configuration;
        }

        public static Configuration UseSqlReservationService(this Configuration configuration)
        {
            configuration.Container.Register<IReservationService>(c => new SqlReservationService());
            configuration.Container.Register<IReservationQuery>(c => new SqlReservationQuery());
            return configuration;
        }

        public static IReservationQuery ReservationQuery(this Configuration configuration)
        {
            return configuration.Container.Resolve<IReservationQuery>();
        }
    }
}