//-----------------------------------------------------------------------
// <copyright file="ProtoSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util;
using Akka.Util.Internal;
using Google.Protobuf;
using Akka.Dispatch.SysMsg;
using Akka.Routing;

namespace Akka.Serialization
{
    /// <summary>
    /// TBD
    /// </summary>
    public class ProtoSerializer : Serializer
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        public ProtoSerializer(ExtendedActorSystem system) : base(system)
        {
        }

        public override int Identifier { get; } = 21;

        /// <summary>
        /// TBD
        /// </summary>
        public override bool IncludeManifest => true;

        /// <summary>
        /// Serializes persistent messages. Delegates serialization of a persistent
        /// message's payload to a matching `akka.serialization.Serializer`.
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <returns>TBD</returns>
        public override byte[] ToBinary(object obj)
        {
            if (obj is IActorRef)
            {
                return ActorRefMessageBuilder((IActorRef)obj).ToByteArray();
            }
            else if (obj is ActorPath)
            {
                return ActorPathMessageBuilder((ActorPath)obj).ToByteArray();
            }
            else if (obj is Identify)
            {
                return IdentifyMessageBuilder((Identify)obj).ToByteArray();
            }
            else if (obj is ActorIdentity)
            {
                return ActorIdentityMessageBuilder((ActorIdentity)obj).ToByteArray();
            }
            else if (obj is PoisonPill)
            {
                return PoisonPillMessageBuilder((PoisonPill)obj).ToByteArray();
            }
            else if (obj is Watch)
            {
                return WatchMessageBuilder((Watch)obj).ToByteArray();
            }
            else if (obj is Unwatch)
            {
                return UnwatchMessageBuilder((Unwatch)obj).ToByteArray();
            }
            else if (obj is Address)
            {
                return AddressMessageBuilder((Address)obj).ToByteArray();
            }
            else if (obj is RemoteScope)
            {
                return RemoteScopeMessageBuilder((RemoteScope)obj).ToByteArray();
            }
            else if (obj is Supervise)
            {
                return SuperviseBuilder((Supervise)obj).ToByteArray();
            }
            else if (obj is DeathWatchNotification)
            {
                return DeathWatchNotificationBuilder((DeathWatchNotification)obj).ToByteArray();
            }
            else if (obj is Terminate)
            {
                return TerminateMessageBuilder((Terminate)obj).ToByteArray();
            }
            else if (obj is Kill)
            {
                return KillMessageBuilder((Kill)obj).ToByteArray();
            }
            else if (obj is Config)
            {
                return ConfigMessageBuilder((Config)obj).ToByteArray();
            }
            else if (obj is SupervisorStrategy)
            {
                return SupervisorStrategyMessageBuilder((SupervisorStrategy)obj).ToByteArray();
            }
            else if (obj is RoundRobinPool)
            {
                return RoundRobinPoolMessageBuilder((RoundRobinPool)obj).ToByteArray();
            }
            else if (obj is RoundRobinGroup)
            {
                return RoundRobinGroupMessageBuilder((RoundRobinGroup)obj).ToByteArray();
            }

            throw new ArgumentException($"Can't serialize object of type {obj.GetType()}");
        }

        /// <summary>
        /// Deserializes persistent messages. Delegates deserialization of a persistent
        /// message's payload to a matching `akka.serialization.Serializer`.
        /// </summary>
        /// <param name="bytes">TBD</param>
        /// <param name="type">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <returns>TBD</returns>
        public override object FromBinary(byte[] bytes, Type type)
        {
            if (type == typeof(IActorRef))
            {
                return ActorRefFrom(Protobuf.Msg.ActorRef.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(ActorPath))
            {
                return ActorPathFrom(Protobuf.Msg.ActorPath.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(Identify))
            {
                return IdentifyFrom(Protobuf.Msg.Identify.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(ActorIdentity))
            {
                return ActorIdentityFrom(Protobuf.Msg.ActorIdentity.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(PoisonPill))
            {
                return PoisonPillFrom(bytes);
            }
            else if (type == typeof(Watch))
            {
                return WatchFrom(Protobuf.Msg.Watch.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(Unwatch))
            {
                return UnwatchFrom(Protobuf.Msg.Watch.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(Address))
            {
                return AddressFrom(Protobuf.Msg.Address.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(RemoteScope) || type == typeof(Scope))
            {
                return RemoteScopeFrom(Protobuf.Msg.RemoteScope.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(Supervise))
            {
                return SuperviseFrom(Protobuf.Msg.Supervise.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(DeathWatchNotification))
            {
                return DeathWatchNotificationFrom(system, Protobuf.Msg.DeathWatchNotification.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(Terminate))
            {
                return TerminateFrom(bytes);
            }
            else if (type == typeof(Kill))
            {
                return KillFrom(bytes);
            }
            else if (type == typeof(Config))
            {
                return ConfigFrom(Protobuf.Msg.Config.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(SupervisorStrategy) || type == typeof(AllForOneStrategy) || type == typeof(OneForOneStrategy))
            {
                return SupervisorStrategyFrom(Protobuf.Msg.SupervisorStrategy.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(RoundRobinPool))
            {
                return RoundRobinPoolFrom(Protobuf.Msg.RoundRobinPool.Parser.ParseFrom(bytes));
            }
            else if (type == typeof(RoundRobinGroup))
            {
                return RoundRobinGroupFrom(Protobuf.Msg.RoundRobinGroup.Parser.ParseFrom(bytes));
            }

            throw new ArgumentException(typeof(ProtoSerializer) + " cannot deserialize object of type " + type);
        }

        //
        // ToBinary helpers
        //

        private Protobuf.Msg.ActorRef ActorRefMessageBuilder(IActorRef actorRef)
        {
            var message = new Protobuf.Msg.ActorRef();
            message.Path = Serialization.SerializedActorPath(actorRef);

            return message;
        }

        private Protobuf.Msg.ActorPath ActorPathMessageBuilder(ActorPath actorPath)
        {
            var message = new Protobuf.Msg.ActorPath
            {
                Path = actorPath.ToSerializationFormat()
            };

            return message;
        }

        private Protobuf.Msg.Identify IdentifyMessageBuilder(Identify identify)
        {
            var message = new Protobuf.Msg.Identify();
            message.MessageId = new Protobuf.Msg.Payload();
            var serializer = system.Serialization.FindSerializerFor(identify.MessageId);

            if (serializer is SerializerWithStringManifest)
            {
                var ser2 = (SerializerWithStringManifest)serializer;
                var manifest = ser2.Manifest(identify.MessageId);
                message.MessageId.MessageManifest = ByteString.CopyFromUtf8(manifest);
            }
            else
            {
                if (serializer.IncludeManifest)
                {
                    message.MessageId.MessageManifest = ByteString.CopyFromUtf8(TypeQualifiedNameForManifest(identify.MessageId.GetType()));
                }
            }

            message.MessageId.EnclosedMessage = ByteString.CopyFrom(serializer.ToBinary(identify.MessageId));
            message.MessageId.SerializerId = serializer.Identifier;

            return message;
        }

        private Protobuf.Msg.ActorIdentity ActorIdentityMessageBuilder(ActorIdentity actorIdentity)
        {
            var message = new Protobuf.Msg.ActorIdentity();
            message.Ref = ActorRefMessageBuilder(actorIdentity.Subject);
            message.CorrelationId = new Protobuf.Msg.Payload();

            var serializer = system.Serialization.FindSerializerFor(actorIdentity.MessageId);

            if (serializer is SerializerWithStringManifest)
            {
                var ser2 = (SerializerWithStringManifest)serializer;
                var manifest = ser2.Manifest(actorIdentity.MessageId);
                message.CorrelationId.MessageManifest = ByteString.CopyFromUtf8(manifest);
            }
            else
            {
                if (serializer.IncludeManifest)
                {
                    message.CorrelationId.MessageManifest = ByteString.CopyFromUtf8(TypeQualifiedNameForManifest(actorIdentity.MessageId.GetType()));
                }
            }

            message.CorrelationId.EnclosedMessage = ByteString.CopyFrom(serializer.ToBinary(actorIdentity.MessageId));
            message.CorrelationId.SerializerId = serializer.Identifier;

            return message;
        }

        private Protobuf.Msg.PoisonPill PoisonPillMessageBuilder(PoisonPill poisonPill)
        {
            return new Protobuf.Msg.PoisonPill();
        }

        private Protobuf.Msg.Watch WatchMessageBuilder(Watch watch)
        {
            var message = new Protobuf.Msg.Watch();
            message.Watchee = ActorRefMessageBuilder(watch.Watchee);
            message.Watcher = ActorRefMessageBuilder(watch.Watcher);
            return message;
        }

        private Protobuf.Msg.Unwatch UnwatchMessageBuilder(Unwatch unwatch)
        {
            var message = new Protobuf.Msg.Unwatch();
            message.Watchee = ActorRefMessageBuilder(unwatch.Watchee);
            message.Watcher = ActorRefMessageBuilder(unwatch.Watcher);
            return message;
        }

        private Protobuf.Msg.Address AddressMessageBuilder(Address address)
        {
            var message = new Protobuf.Msg.Address();
            message.Protocol = address.Protocol;
            message.System = address.System;
            message.Host = address.Host;
            message.Port = address.Port ?? 0;
            return message;
        }

        private Protobuf.Msg.RemoteScope RemoteScopeMessageBuilder(RemoteScope remoteScope)
        {
            var message = new Protobuf.Msg.RemoteScope();
            message.Address = AddressMessageBuilder(remoteScope.Address);
            return message;
        }

        private Protobuf.Msg.Supervise SuperviseBuilder(Supervise supervise)
        {
            var message = new Protobuf.Msg.Supervise();
            message.Child = ActorRefMessageBuilder(supervise.Child);
            message.Async = supervise.Async;
            return message;
        }

        private Protobuf.Msg.DeathWatchNotification DeathWatchNotificationBuilder(DeathWatchNotification deathWatchNotification)
        {
            var message = new Protobuf.Msg.DeathWatchNotification();
            message.Ref = ActorRefMessageBuilder(deathWatchNotification.Actor);
            message.AddressTerminated = deathWatchNotification.AddressTerminated;
            message.ExistenceConfirmed = deathWatchNotification.ExistenceConfirmed;
            return message;
        }

        internal static Protobuf.Msg.Terminate TerminateMessageBuilder(Terminate terminate)
        {
            return new Protobuf.Msg.Terminate();
        }

        internal static Protobuf.Msg.Kill KillMessageBuilder(Kill kill)
        {
            return new Protobuf.Msg.Kill();
        }

        internal static Protobuf.Msg.Config ConfigMessageBuilder(Config config)
        {
            var message = new Protobuf.Msg.Config();
            message.Config_ = config.ToString(true);
            return message;
        }

        internal static Protobuf.Msg.Decider DeciderMessageBuilder(DeployableDecider decider)
        {
            var message = new Protobuf.Msg.Decider();
            message.DefaultDirective = decider.DefaultDirective.ToString();
            foreach (var pair in decider.Pairs)
            {
                message.Pairs.Add(pair.Key.TypeQualifiedName(), pair.Value.ToString());
            }

            return message;
        }

        internal static Protobuf.Msg.SupervisorStrategy SupervisorStrategyMessageBuilder(SupervisorStrategy supervisorStrategy)
        {
            if (supervisorStrategy.Decider is LocalOnlyDecider)
                throw new SerializationException("LocalOnlyDecider is not supported"); 

            var message = new Protobuf.Msg.SupervisorStrategy();

            if (supervisorStrategy is AllForOneStrategy)
            {
                var allForOneStrategy = (AllForOneStrategy)supervisorStrategy;
                message.MaxNumberOfRetries = allForOneStrategy.MaxNumberOfRetries;
                message.WithinTimeMilliseconds = allForOneStrategy.WithinTimeRangeMilliseconds;
                message.Decider = DeciderMessageBuilder(allForOneStrategy.Decider as DeployableDecider);
            }
            else
            {
                var oneForOneStrategy = (OneForOneStrategy)supervisorStrategy;
                message.MaxNumberOfRetries = oneForOneStrategy.MaxNumberOfRetries;
                message.WithinTimeMilliseconds = oneForOneStrategy.WithinTimeRangeMilliseconds;
                message.Decider = DeciderMessageBuilder(oneForOneStrategy.Decider as DeployableDecider);
            }

            message.StrategyType = supervisorStrategy.GetType().TypeQualifiedName();

            return message;
        }

        internal static Protobuf.Msg.Resizer ResizerMessageBuilder(Resizer resizer)
        {
            var defaultResizer = resizer as DefaultResizer;

            if (defaultResizer != null)
            {
                var message = new Protobuf.Msg.Resizer();
                message.Lower = defaultResizer.LowerBound;
                message.Upper = defaultResizer.UpperBound;
                message.PressureThreshold = defaultResizer.PressureThreshold;
                message.RampupRate = defaultResizer.RampupRate;
                message.BackoffThreshold = defaultResizer.BackoffThreshold;
                message.BackoffRate = defaultResizer.BackoffRate;
                message.MessagesPerResize = defaultResizer.MessagesPerResize;

                return message;
            }

            throw new SerializationException("DefaultResizer only supported");
        }

        internal static Protobuf.Msg.RoundRobinPool RoundRobinPoolMessageBuilder(RoundRobinPool roundRobinPool)
        {
            var message = new Protobuf.Msg.RoundRobinPool();
            message.NumberOfInstances = roundRobinPool.NrOfInstances;
            message.UsePoolDispatcher = roundRobinPool.UsePoolDispatcher;
            message.Resizer = ResizerMessageBuilder(roundRobinPool.Resizer);
            message.SupervisorStrategy = SupervisorStrategyMessageBuilder(roundRobinPool.SupervisorStrategy);
            message.RouterDispatcher = roundRobinPool.RouterDispatcher;

            return message;
        }

        internal static Protobuf.Msg.RoundRobinGroup RoundRobinGroupMessageBuilder(RoundRobinGroup roundRobinGroup)
        {
            var message = new Protobuf.Msg.RoundRobinGroup();
            message.RouterDispatcher = roundRobinGroup.RouterDispatcher;
            foreach (var path in roundRobinGroup.Paths)
            {
                message.Paths.Add(path);
            }

            return message;
        }

        //
        // FromBinary helpers
        //

        private IActorRef ActorRefFrom(Protobuf.Msg.ActorRef actorRefProto)
        {
            return system.Provider.ResolveActorRef(actorRefProto.Path);
        }

        internal static ActorPath ActorPathFrom(Protobuf.Msg.ActorPath actorPathProto)
        {
            ActorPath actorPath;
            if (ActorPath.TryParse(actorPathProto.Path, out actorPath))
            {
                return actorPath;
            }

            return null;
        }

        private Identify IdentifyFrom(Protobuf.Msg.Identify identifyProto)
        {
            var messageId = system.Serialization.Deserialize(
                identifyProto.MessageId.EnclosedMessage.ToByteArray(),
                identifyProto.MessageId.SerializerId,
                identifyProto.MessageId.MessageManifest.ToStringUtf8());

            return new Identify(messageId);
        }

        private ActorIdentity ActorIdentityFrom(Protobuf.Msg.ActorIdentity actorIdentityProto)
        {
            var actorRef = system.Provider.ResolveActorRef(actorIdentityProto.Ref.Path);
            var messageId = system.Serialization.Deserialize(
                actorIdentityProto.CorrelationId.EnclosedMessage.ToByteArray(),
                actorIdentityProto.CorrelationId.SerializerId,
                actorIdentityProto.CorrelationId.MessageManifest.ToStringUtf8());

            return new ActorIdentity(messageId, actorRef);
        }

        internal static PoisonPill PoisonPillFrom(byte[] bytes)
        {
            return PoisonPill.Instance;
        }

        private Watch WatchFrom(Protobuf.Msg.Watch watchProto)
        {
            var watchee = system.Provider.ResolveActorRef(watchProto.Watchee.Path);
            var watcher = system.Provider.ResolveActorRef(watchProto.Watcher.Path);

            return new Watch(watchee.AsInstanceOf<IInternalActorRef>(), watcher.AsInstanceOf<IInternalActorRef>());
        }

        private Unwatch UnwatchFrom(Protobuf.Msg.Watch unwatchProto)
        {
            var watchee = system.Provider.ResolveActorRef(unwatchProto.Watchee.Path);
            var watcher = system.Provider.ResolveActorRef(unwatchProto.Watcher.Path);

            return new Unwatch(watchee.AsInstanceOf<IInternalActorRef>(), watcher.AsInstanceOf<IInternalActorRef>());
        }

        internal static Address AddressFrom(Protobuf.Msg.Address addressProto)
        {
            return new Address(
                addressProto.Protocol,
                addressProto.System,
                addressProto.Host,
                addressProto.Port == 0 ? null : (int?)addressProto.Port);
        }

        internal static object RemoteScopeFrom(Protobuf.Msg.RemoteScope remoteScopeProto)
        {
            return new RemoteScope(AddressFrom(remoteScopeProto.Address));
        }

        private Supervise SuperviseFrom(Protobuf.Msg.Supervise superviseProto)
        {
            return new Supervise(
                system.Provider.ResolveActorRef(superviseProto.Child.Path),
                superviseProto.Async);
        }

        internal static DeathWatchNotification DeathWatchNotificationFrom(ExtendedActorSystem sys, Protobuf.Msg.DeathWatchNotification deathWatchNotificationProto)
        {
            return new DeathWatchNotification(
                sys.Provider.ResolveActorRef(deathWatchNotificationProto.Ref.Path),
                deathWatchNotificationProto.ExistenceConfirmed,
                deathWatchNotificationProto.AddressTerminated);
        }

        internal static Terminate TerminateFrom(byte[] bytes)
        {
            return new Terminate();
        }

        internal static Kill KillFrom(byte[] bytes)
        {
            return Kill.Instance;
        }

        internal static Config ConfigFrom(Protobuf.Msg.Config configProto)
        {
            return ConfigurationFactory.ParseString(configProto.Config_);
        }

        internal static DeployableDecider DeciderFrom(Protobuf.Msg.Decider deployableDeciderProto)
        {
            Directive defaultDirective;
            Enum.TryParse(deployableDeciderProto.DefaultDirective, out defaultDirective);

            var pairs = new List<KeyValuePair<Type, Directive>>();
            foreach (var pair in deployableDeciderProto.Pairs)
            {
                Directive pairDirective;
                Enum.TryParse(pair.Value, out pairDirective);
                pairs.Add(new KeyValuePair<Type, Directive>(Type.GetType(pair.Key), pairDirective));
            }

            return new DeployableDecider(defaultDirective, pairs);
        }

        internal static SupervisorStrategy SupervisorStrategyFrom(Protobuf.Msg.SupervisorStrategy supervisorStrategyProto)
        {
            Type strategyType = Type.GetType(supervisorStrategyProto.StrategyType);

            if (strategyType == typeof(AllForOneStrategy))
            {
                return new AllForOneStrategy(
                    supervisorStrategyProto.MaxNumberOfRetries,
                    supervisorStrategyProto.WithinTimeMilliseconds,
                    DeciderFrom(supervisorStrategyProto.Decider));
            }
            else
            {
                return new OneForOneStrategy(
                    supervisorStrategyProto.MaxNumberOfRetries,
                    supervisorStrategyProto.WithinTimeMilliseconds,
                    DeciderFrom(supervisorStrategyProto.Decider));
            }
        }

        internal static Resizer ResizerFrom(Protobuf.Msg.Resizer resizer)
        {
            return new DefaultResizer(
                resizer.Lower,
                resizer.Upper,
                resizer.PressureThreshold,
                resizer.RampupRate,
                resizer.BackoffThreshold,
                resizer.BackoffRate,
                resizer.MessagesPerResize);
        }

        internal static RoundRobinPool RoundRobinPoolFrom(Protobuf.Msg.RoundRobinPool roundRobinPoolProto)
        {
            return new RoundRobinPool(
                roundRobinPoolProto.NumberOfInstances,
                ResizerFrom(roundRobinPoolProto.Resizer),
                SupervisorStrategyFrom(roundRobinPoolProto.SupervisorStrategy),
                roundRobinPoolProto.RouterDispatcher,
                roundRobinPoolProto.UsePoolDispatcher);
        }

        internal static RoundRobinGroup RoundRobinGroupFrom(Protobuf.Msg.RoundRobinGroup roundRobinGroupProto)
        {
            var paths = new List<string>(roundRobinGroupProto.Paths.Count);
            foreach (var path in roundRobinGroupProto.Paths)
            {
                paths.Add(path);
            }

            return new RoundRobinGroup(paths, roundRobinGroupProto.RouterDispatcher);
        }
    }
}