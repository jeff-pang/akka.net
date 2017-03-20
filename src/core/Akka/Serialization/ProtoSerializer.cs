//-----------------------------------------------------------------------
// <copyright file="ProtoSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
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

        public override int Identifier { get; } = -50;

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
            if (type == typeof(ActorPath))
            {
                return ActorPathFrom(bytes);
            }
            if (type == typeof(Identify))
            {
                return IdentifyFrom(bytes);
            }
            if (type == typeof(ActorIdentity))
            {
                return ActorIdentityFrom(bytes);
            }
            if (type == typeof(PoisonPill))
            {
                return PoisonPillFrom(bytes);
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

            if (serializer is SerializerWithStringManifest ser2)
            {
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
            message.Ref = new Protobuf.Msg.ActorRef();
            message.Ref.Path = Serialization.SerializedActorPath(actorIdentity.Subject);
            message.CorrelationId = new Protobuf.Msg.Payload();

            var serializer = system.Serialization.FindSerializerFor(actorIdentity.MessageId);

            if (serializer is SerializerWithStringManifest ser2)
            {
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
    }
}