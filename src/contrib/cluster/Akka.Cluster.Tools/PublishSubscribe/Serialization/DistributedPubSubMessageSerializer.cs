//-----------------------------------------------------------------------
// <copyright file="DistributedPubSubMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.PubSub.Serializers.Proto;
using Akka.Cluster.Tools.PublishSubscribe.Internal;
using Akka.Serialization;
using Google.Protobuf;
using Address = Akka.Cluster.PubSub.Serializers.Proto.Address;
using Delta = Akka.Cluster.Tools.PublishSubscribe.Internal.Delta;
using Status = Akka.Cluster.PubSub.Serializers.Proto.Status;

namespace Akka.Cluster.Tools.PublishSubscribe.Serialization
{
    /**
     * Protobuf serializer of DistributedPubSubMediator messages.
     */
    /// <summary>
    /// TBD
    /// </summary>
    public class DistributedPubSubMessageSerializer : SerializerWithStringManifest
    {
        /// <summary>
        /// TBD
        /// </summary>
        public const int BufferSize = 1024 * 4;

        /// <summary>
        /// TBD
        /// </summary>
        public const string StatusManifest = "A";
        /// <summary>
        /// TBD
        /// </summary>
        public const string DeltaManifest = "B";
        /// <summary>
        /// TBD
        /// </summary>
        public const string SendManifest = "C";
        /// <summary>
        /// TBD
        /// </summary>
        public const string SendToAllManifest = "D";
        /// <summary>
        /// TBD
        /// </summary>
        public const string PublishManifest = "E";

        private readonly IDictionary<string, Func<byte[], object>> _fromBinaryMap;

        private readonly int _identifier;

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="system">TBD</param>
        public DistributedPubSubMessageSerializer(ExtendedActorSystem system) : base(system)
        {
            _identifier = SerializerIdentifierHelper.GetSerializerIdentifierFromConfig(this.GetType(), system);
            _fromBinaryMap = new Dictionary<string, Func<byte[], object>>
            {
                {StatusManifest, StatusFromBinary},
                {DeltaManifest, DeltaFromBinary},
                {SendManifest, SendFromBinary},
                {SendToAllManifest, SendToAllFromBinary},
                {PublishManifest, PublishFromBinary}
            };
        }

        /// <summary>
        /// TBD
        /// </summary>
        public override int Identifier { get { return _identifier; } }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="obj">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <returns>TBD</returns>
        public override byte[] ToBinary(object obj)
        {
            if (obj is Internal.Status) return Compress(StatusToProto(obj as Internal.Status));
            if (obj is Internal.Delta) return Compress(DeltaToProto(obj as Internal.Delta));
            if (obj is Send) return SendToProto(obj as Send).ToByteArray();
            if (obj is SendToAll) return SendToAllToProto(obj as SendToAll).ToByteArray();
            if (obj is Publish) return PublishToProto(obj as Publish).ToByteArray();

            throw new ArgumentException(string.Format("Can't serialize object of type {0} with {1}", obj.GetType(), GetType()));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="bytes">TBD</param>
        /// <param name="manifestString">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <returns>TBD</returns>
        public override object FromBinary(byte[] bytes, string manifestString)
        {
            Func<byte[], object> deserializer;
            if (_fromBinaryMap.TryGetValue(manifestString, out deserializer))
            {
                return deserializer(bytes);
            }

            throw new ArgumentException(string.Format("Unimplemented deserialization of message with manifest [{0}] in serializer {1}", manifestString, GetType()));
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="o">TBD</param>
        /// <exception cref="ArgumentException">TBD</exception>
        /// <returns>TBD</returns>
        public override string Manifest(object o)
        {
            if (o is Internal.Status) return StatusManifest;
            if (o is Internal.Delta) return DeltaManifest;
            if (o is Send) return SendManifest;
            if (o is SendToAll) return SendToAllManifest;
            if (o is Publish) return PublishManifest;

            throw new ArgumentException(string.Format("Serializer {0} cannot serialize message of type {1}", this.GetType(), o.GetType()));
        }

        private byte[] Compress(IMessage message)
        {
            using (var bos = new MemoryStream(BufferSize))
            using (var gzipStream = new GZipStream(bos, CompressionMode.Compress))
            {
                message.WriteTo(gzipStream);
                gzipStream.Dispose();
                return bos.ToArray();
            }
        }

        private byte[] Decompress(byte[] bytes)
        {
            using (var input = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                var buffer = new byte[BufferSize];
                var bytesRead = input.Read(buffer, 0, BufferSize);
                while (bytesRead > 0)
                {
                    output.Write(buffer, 0, bytesRead);
                    bytesRead = input.Read(buffer, 0, BufferSize);
                }
                return output.ToArray();
            }
        }

        private Address AddressToProto(Actor.Address address)
        {
            if (string.IsNullOrEmpty(address.Host) || !address.Port.HasValue)
                throw new ArgumentException(string.Format("Address [{0}] could not be serialized: host or port missing", address));

            return new Address
            {
                System = address.System,
                Hostname = address.Host,
                Port = (uint)address.Port.Value,
                Protocol = address.Protocol
            };
        }

        private Actor.Address AddressFromProto(Address address)
        {
            return new Actor.Address(address.Protocol, address.System, address.Hostname, (int)address.Port);
        }

        private Akka.Cluster.PubSub.Serializers.Proto.Delta DeltaToProto(Delta delta)
        {
            var buckets = delta.Buckets.Select(b =>
            {
                var entries = b.Content.Select(c =>
                {
                    var bb = new Akka.Cluster.PubSub.Serializers.Proto.Delta.Types.Entry
                    {
                        Key = c.Key,
                        Version = c.Value.Version
                    };

                    if (c.Value.Ref != null)
                    {
                        bb.Ref = Akka.Serialization.Serialization.SerializedActorPath(c.Value.Ref);
                    }
                    return bb;
                });
                var buck = new Akka.Cluster.PubSub.Serializers.Proto.Delta.Types.Bucket{
                    Owner=AddressToProto(b.Owner),
                    Version=b.Version                    
                    };
                buck.Content.AddRange(entries);
                return buck;
            }).ToArray();

            var d = new Akka.Cluster.PubSub.Serializers.Proto.Delta();
            d.Buckets.AddRange(buckets);
            return d;
        }

        private Delta DeltaFromBinary(byte[] binary)
        {
            return DeltaFromProto(Akka.Cluster.PubSub.Serializers.Proto.Delta.Parser.ParseFrom(Decompress(binary)));
        }

        private Delta DeltaFromProto(Akka.Cluster.PubSub.Serializers.Proto.Delta delta)
        {
            return new Delta(delta.Buckets.Select(b =>
            {
                var content = b.Content.Aggregate(ImmutableDictionary<string, ValueHolder>.Empty, (map, entry) =>
                     map.Add(entry.Key, new ValueHolder(entry.Version, entry.Ref !=null ? ResolveActorRef(entry.Ref) : null)));
                return new Bucket(AddressFromProto(b.Owner), b.Version, content);
            }).ToArray());
        }

        private IActorRef ResolveActorRef(string path)
        {
            return system.Provider.ResolveActorRef(path);
        }

        private Status StatusToProto(Internal.Status status)
        {
            var versions = status.Versions.Select(v =>
                new Status.Types.Version { 
                    Address=AddressToProto(v.Key),
                    Timestamp=v.Value }
                    )
                .ToArray();

            var s = new Status { ReplyToStatus = status.IsReplyToStatus };
            s.Versions.AddRange(versions);
            return s;
        }

        private Internal.Status StatusFromBinary(byte[] binary)
        {
            return StatusFromProto(Status.Parser.ParseFrom(Decompress(binary)));
        }

        private Internal.Status StatusFromProto(Status status)
        {
            var isReplyToStatus = status.ReplyToStatus ? status.ReplyToStatus : false;
            return new Internal.Status(status.Versions
                .ToDictionary(
                    v => AddressFromProto(v.Address),
                    v => v.Timestamp), isReplyToStatus);
        }

        private Akka.Cluster.PubSub.Serializers.Proto.Send SendToProto(Send send)
        {
            return new Akka.Cluster.PubSub.Serializers.Proto.Send{
                Path=send.Path,
                LocalAffinity=send.LocalAffinity,
                Payload=PayloadToProto(send.Message)
            };
        }

        private Send SendFromBinary(byte[] binary)
        {
            return SendFromProto(Akka.Cluster.PubSub.Serializers.Proto.Send.Parser.ParseFrom(binary));
        }

        private Send SendFromProto(Akka.Cluster.PubSub.Serializers.Proto.Send send)
        {
            return new Send(send.Path, PayloadFromProto(send.Payload), send.LocalAffinity);
        }

        private Akka.Cluster.PubSub.Serializers.Proto.SendToAll SendToAllToProto(SendToAll sendToAll)
        {
            return new Akka.Cluster.PubSub.Serializers.Proto.SendToAll { 
                Path =sendToAll.Path,
                AllButSelf=sendToAll.ExcludeSelf,
                Payload=PayloadToProto(sendToAll.Message)
            };
        }

        private SendToAll SendToAllFromBinary(byte[] binary)
        {
            return SendToAllFromProto(Akka.Cluster.PubSub.Serializers.Proto.SendToAll.Parser.ParseFrom(binary));
        }

        private SendToAll SendToAllFromProto(Akka.Cluster.PubSub.Serializers.Proto.SendToAll send)
        {
            return new SendToAll(send.Path, PayloadFromProto(send.Payload), send.AllButSelf);
        }

        private Akka.Cluster.PubSub.Serializers.Proto.Publish PublishToProto(Publish publish)
        {
            return new Akka.Cluster.PubSub.Serializers.Proto.Publish
            {
                Topic = publish.Topic,
                Payload = PayloadToProto(publish.Message)
            };
        }

        private Publish PublishFromBinary(byte[] binary)
        {
            return PublishFromProto(Akka.Cluster.PubSub.Serializers.Proto.Publish.Parser.ParseFrom(binary));
        }

        private Publish PublishFromProto(Akka.Cluster.PubSub.Serializers.Proto.Publish publish)
        {
            return new Publish(publish.Topic, PayloadFromProto(publish.Payload));
        }

        private Payload PayloadToProto(object message)
        {
            var serializer = system.Serialization.FindSerializerFor(message);
            var builder = new Payload
            {
                EnclosedMessage = ByteString.CopyFrom(serializer.ToBinary(message)),
                SerializerId = serializer.Identifier
            };

            SerializerWithStringManifest serializerWithManifest;
            if ((serializerWithManifest = serializer as SerializerWithStringManifest) != null)
            {
                var manifest = serializerWithManifest.Manifest(message);
                if (!string.IsNullOrEmpty(manifest))
                    builder.MessageManifest = ByteString.CopyFromUtf8(manifest);
            }
            else
            {
                if (serializer.IncludeManifest)
                    builder.MessageManifest = ByteString.CopyFromUtf8(TypeQualifiedNameForManifest(message.GetType()));
            }

            return builder;
        }

        private object PayloadFromProto(Payload payload)
        {
            var type = !payload.MessageManifest.IsEmpty ? Type.GetType(payload.MessageManifest.ToStringUtf8()) : null;
            return system.Serialization.Deserialize(
                payload.EnclosedMessage.ToByteArray(),
                payload.SerializerId,
                type);
        }
    }
}