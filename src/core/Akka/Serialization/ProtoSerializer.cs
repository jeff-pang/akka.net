//-----------------------------------------------------------------------
// <copyright file="ProtoSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util.Internal;
using Google.Protobuf;

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
                return ActorRefFrom(bytes);
            }
            else if (type == typeof(ActorPath))
            {
                return ActorPathFrom(bytes);
            }
            else if (type == typeof(Identify))
            {
                return IdentifyFrom(bytes);
            }
            else if (type == typeof(ActorIdentity))
            {
                return ActorIdentityFrom(bytes);
            }
            else if (type == typeof(PoisonPill))
            {
                return PoisonPillFrom(bytes);
            }
            else if (type == typeof(Watch))
            {
                return WatchFrom(bytes);
            }
            else if (type == typeof(Unwatch))
            {
                return UnwatchFrom(bytes);
            }
            else if (type == typeof(Address))
            {
                return AddressFrom(bytes);
            }
            else if (type == typeof(RemoteScope))
            {
                return RemoteScopeFrom(bytes);
            }
            else if (type == typeof(Supervise))
            {
                return SuperviseFrom(bytes);
            }
            else if (type == typeof(DeathWatchNotification))
            {
                return DeathWatchNotificationFrom(bytes);
            }
            else if (type == typeof(Terminate))
            {
                return TerminateFrom(bytes);
            }
            else if (type == typeof(Kill))
            {
                return KillFrom(bytes);
            }

            throw new ArgumentException(typeof(ProtoSerializer) + " cannot deserialize object of type " + type);
        }

        //
        // ToBinary helpers
        //

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

        private Protobuf.Msg.Terminate TerminateMessageBuilder(Terminate terminate)
        {
            return new Protobuf.Msg.Terminate();
        }

        private Protobuf.Msg.Kill KillMessageBuilder(Kill kill)
        {
            return new Protobuf.Msg.Kill();
        }

        //
        // FromBinary helpers
        //

        private IActorRef ActorRefFrom(byte[] bytes)
        {
            var actorRefProto = Protobuf.Msg.ActorRef.Parser.ParseFrom(bytes);

            return system.Provider.ResolveActorRef(actorRefProto.Path);
        }

        private ActorPath ActorPathFrom(byte[] bytes)
        {
            var actorPathProto = Protobuf.Msg.ActorPath.Parser.ParseFrom(bytes);

            ActorPath actorPath;
            if (ActorPath.TryParse(actorPathProto.Path, out actorPath))
            {
                return actorPath;
            }

            return null;
        }

        private Identify IdentifyFrom(byte[] bytes)
        {
            var identifyProto = Protobuf.Msg.Identify.Parser.ParseFrom(bytes);

            var messageId = system.Serialization.Deserialize(
                identifyProto.MessageId.EnclosedMessage.ToByteArray(),
                identifyProto.MessageId.SerializerId,
                identifyProto.MessageId.MessageManifest.ToStringUtf8());

            return new Identify(messageId);
        }

        private ActorIdentity ActorIdentityFrom(byte[] bytes)
        {
            var actorIdentityProto = Protobuf.Msg.ActorIdentity.Parser.ParseFrom(bytes);

            var actorRef = system.Provider.ResolveActorRef(actorIdentityProto.Ref.Path);
            var messageId = system.Serialization.Deserialize(
                actorIdentityProto.CorrelationId.EnclosedMessage.ToByteArray(),
                actorIdentityProto.CorrelationId.SerializerId,
                actorIdentityProto.CorrelationId.MessageManifest.ToStringUtf8());

            return new ActorIdentity(messageId, actorRef);
        }

        private PoisonPill PoisonPillFrom(byte[] bytes)
        {
            return PoisonPill.Instance;
        }

        private Watch WatchFrom(byte[] bytes)
        {
            var watchProto = Protobuf.Msg.Watch.Parser.ParseFrom(bytes);

            var watchee = system.Provider.ResolveActorRef(watchProto.Watchee.Path);
            var watcher = system.Provider.ResolveActorRef(watchProto.Watcher.Path);

            return new Watch(watchee.AsInstanceOf<IInternalActorRef>(), watcher.AsInstanceOf<IInternalActorRef>());
        }

        private Unwatch UnwatchFrom(byte[] bytes)
        {
            var unwatchProto = Protobuf.Msg.Watch.Parser.ParseFrom(bytes);

            var watchee = system.Provider.ResolveActorRef(unwatchProto.Watchee.Path);
            var watcher = system.Provider.ResolveActorRef(unwatchProto.Watcher.Path);

            return new Unwatch(watchee.AsInstanceOf<IInternalActorRef>(), watcher.AsInstanceOf<IInternalActorRef>());
        }

        private Address AddressFrom(byte[] bytes)
        {
            var addressProto = Protobuf.Msg.Address.Parser.ParseFrom(bytes);

            return new Address(
                addressProto.Protocol,
                addressProto.System,
                addressProto.Host,
                addressProto.Port == 0 ? null : (int?)addressProto.Port);
        }

        private object RemoteScopeFrom(byte[] bytes)
        {
            var remoteScopeProto = Protobuf.Msg.RemoteScope.Parser.ParseFrom(bytes);

            var address = new Address(
                remoteScopeProto.Address.Protocol,
                remoteScopeProto.Address.System,
                remoteScopeProto.Address.Host,
                remoteScopeProto.Address.Port == 0 ? null : (int?)remoteScopeProto.Address.Port);

            return new RemoteScope(address);
        }

        private Supervise SuperviseFrom(byte[] bytes)
        {
            var superviseProto = Protobuf.Msg.Supervise.Parser.ParseFrom(bytes);

            return new Supervise(
                system.Provider.ResolveActorRef(superviseProto.Child.Path),
                superviseProto.Async);
        }

        private DeathWatchNotification DeathWatchNotificationFrom(byte[] bytes)
        {
            var deathWatchNotificationProto = Protobuf.Msg.DeathWatchNotification.Parser.ParseFrom(bytes);

            return new DeathWatchNotification(
                system.Provider.ResolveActorRef(deathWatchNotificationProto.Ref.Path),
                deathWatchNotificationProto.ExistenceConfirmed,
                deathWatchNotificationProto.AddressTerminated);
        }

        private Terminate TerminateFrom(byte[] bytes)
        {
            return new Terminate();
        }

        private Kill KillFrom(byte[] bytes)
        {
            return Kill.Instance;
        }
    }
}