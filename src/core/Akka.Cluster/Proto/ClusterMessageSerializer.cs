//-----------------------------------------------------------------------
// <copyright file="ClusterMessageSerializer.cs" company="Akka.NET Project">
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
using Akka.Serialization;
using Akka.Util.Internal;
using Google.Protobuf;

namespace Akka.Cluster.Proto
{
    /// <summary>
    /// Protobuff serializer for cluster messages
    /// </summary>
    internal class ClusterMessageSerializer : Serializer
    {
        public ClusterMessageSerializer(ExtendedActorSystem system)
            : base(system)
        {
            _gossipTimeToLive = new Lazy<TimeSpan>(() => Cluster.Get(system).Settings.GossipTimeToLive);
        }

        private const int BufferSize = 1024 * 4;

        public override int Identifier
        {
            get { return 5; }
        }

        public override bool IncludeManifest
        {
            get { return true; }
        }

        //must be lazy because serializer is initialized from Cluster extension constructor
        private Lazy<TimeSpan> _gossipTimeToLive;

        public override byte[] ToBinary(object obj)
        {
            if (obj is ClusterHeartbeatSender.Heartbeat) return AddressToProtoByteArray(((ClusterHeartbeatSender.Heartbeat)obj).From);
            if (obj is ClusterHeartbeatSender.HeartbeatRsp) return UniqueAddressToProtoByteArray(((ClusterHeartbeatSender.HeartbeatRsp)obj).From);
            if (obj is GossipEnvelope) return GossipEnvelopeToProto((GossipEnvelope) obj).ToByteArray();
            if (obj is GossipStatus) return GossipStatusToProto((GossipStatus) obj).ToByteArray();
            if (obj is InternalClusterAction.Join)
            {
                var join = (InternalClusterAction.Join) obj;
                return JoinToProto(join.Node, join.Roles).ToByteArray();
            }
            if (obj is InternalClusterAction.Welcome)
            {
                var welcome = (InternalClusterAction.Welcome) obj;
                return Compress(WelcomeToProto(welcome.From, welcome.Gossip));
            }
            if (obj is ClusterUserAction.Leave) return AddressToProtoByteArray(((ClusterUserAction.Leave) obj).Address);
            if (obj is ClusterUserAction.Down) return AddressToProtoByteArray(((ClusterUserAction.Down)obj).Address);
            if (obj is InternalClusterAction.InitJoin) return new Msg.Empty().ToByteArray();
            if (obj is InternalClusterAction.InitJoinAck) return AddressToProtoByteArray(((InternalClusterAction.InitJoinAck)obj).Address);
            if (obj is InternalClusterAction.InitJoinNack) return AddressToProtoByteArray(((InternalClusterAction.InitJoinNack)obj).Address);
            if(obj is InternalClusterAction.ExitingConfirmed) return UniqueAddressToProtoByteArray(((InternalClusterAction.ExitingConfirmed)obj).Address);
            throw new ArgumentException(string.Format("Can't serialize object of type {0}", obj.GetType()));
        }

        public override object FromBinary(byte[] bytes, Type type)
        {
            if (type == typeof (InternalClusterAction.Join))
            {
                var m = Msg.Join.Parser.ParseFrom(bytes);
                return new InternalClusterAction.Join(UniqueAddressFromProto(m.Node),
                    ImmutableHashSet.Create<string>(m.Roles.ToArray()));
            }

            if (type == typeof(InternalClusterAction.Welcome))
            {
                var m = Msg.Welcome.Parser.ParseFrom(Decompress(bytes));
                return new InternalClusterAction.Welcome(UniqueAddressFromProto(m.From), GossipFromProto(m.Gossip));
            }

            if (type == typeof(ClusterUserAction.Leave)) return new ClusterUserAction.Leave(AddressFromBinary(bytes));
            if (type == typeof(ClusterUserAction.Down)) return new ClusterUserAction.Down(AddressFromBinary(bytes));
            if (type == typeof(InternalClusterAction.InitJoin)) return new InternalClusterAction.InitJoin();
            if (type == typeof(InternalClusterAction.InitJoinAck)) return new InternalClusterAction.InitJoinAck(AddressFromBinary(bytes));
            if (type == typeof(InternalClusterAction.InitJoinNack)) return new InternalClusterAction.InitJoinNack(AddressFromBinary(bytes));
            if (type == typeof(ClusterHeartbeatSender.Heartbeat)) return new ClusterHeartbeatSender.Heartbeat(AddressFromBinary(bytes));
            if (type == typeof(ClusterHeartbeatSender.HeartbeatRsp)) return new ClusterHeartbeatSender.HeartbeatRsp(UniqueAddressFromBinary(bytes));
            if(type == typeof(InternalClusterAction.ExitingConfirmed)) return new InternalClusterAction.ExitingConfirmed(UniqueAddressFromBinary(bytes));
            if (type == typeof(GossipStatus)) return GossipStatusFromBinary(bytes);
            if (type == typeof(GossipEnvelope)) return GossipEnvelopeFromBinary(bytes);

            throw new ArgumentException("Ned a cluster message class to be able to deserialize bytes in ClusterSerializer.");
        }

        /// <summary>
        /// Compresses the protobuf message using GZIP compression
        /// </summary>
        public byte[] Compress(IMessage message)
        {
            using (var bos = new MemoryStream(BufferSize))
            using (var gzipStream = new GZipStream(bos, CompressionMode.Compress))
            {
                message.WriteTo(gzipStream);
                gzipStream.Dispose();
                return bos.ToArray();
            }
        }

        /// <summary>
        /// Decompresses the protobuf message using GZIP compression
        /// </summary>
        public byte[] Decompress(byte[] bytes)
        {
            using(var input = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                var buffer = new byte[BufferSize];
                var bytesRead = input.Read(buffer, 0, BufferSize);
                while (bytesRead > 0)
                {
                    output.Write(buffer,0,bytesRead);
                    bytesRead = input.Read(buffer, 0, BufferSize);
                }
                return output.ToArray();
            }
        }

        #region Private internals

        // we don't care about races here since it's just a cache
        private volatile string _protocolCache = null;
        private volatile string _systemCache = null;

        private Address AddressFromBinary(byte[] bytes)
        {
            return AddressFromProto(Msg.Address.Parser.ParseFrom(bytes));
        }

        private UniqueAddress UniqueAddressFromBinary(byte[] bytes)
        {
            return UniqueAddressFromProto(Msg.UniqueAddress.Parser.ParseFrom(bytes));
        }

        private Address AddressFromProto(Msg.Address address)
        {
            return new Address(GetProtocol(address), GetSystem(address), address.Hostname, GetPort(address));
        }

        private Msg.Address AddressToProto(Address address)
        {
            if(string.IsNullOrEmpty(address.Host) || !address.Port.HasValue) 
                throw new ArgumentException(string.Format("Address [{0}] could not be serialized: host or port missing.", address), "address");
            return
                new Msg.Address
                {
                    System = address.System,
                    Protocol = address.Protocol,
                    Hostname = address.Host,
                    Port = (uint)address.Port.Value
                };
        }

        private byte[] AddressToProtoByteArray(Address address)
        {
            return AddressToProto(address).ToByteArray();
        }

        private UniqueAddress UniqueAddressFromProto(Msg.UniqueAddress uniqueAddress)
        {
            return new UniqueAddress(AddressFromProto(uniqueAddress.Address), (int)uniqueAddress.Uid);
        }

        private Msg.UniqueAddress UniqueAddressToProto(UniqueAddress uniqueAddress)
        {
            return new
                Msg.UniqueAddress
            {
                Address = AddressToProto(uniqueAddress.Address),
                Uid = (uint)uniqueAddress.Uid
            };
        }

        private byte[] UniqueAddressToProtoByteArray(UniqueAddress uniqueAddress)
        {
            return UniqueAddressToProto(uniqueAddress).ToByteArray();
        }

        private string GetProtocol(Msg.Address address)
        {
            var p = address.Protocol;
            var pc = _protocolCache;
            if (pc == p) return pc;

            _protocolCache = p;
            return p;
        }

        private string GetSystem(Msg.Address address)
        {
            var s = address.System;
            var sc = _systemCache;
            if (sc == s) return sc;

            _systemCache = s;
            return s;
        }

        private int? GetPort(Msg.Address address)
        {
            if (address.Port==0) return null;
            return (int)address.Port;
        }

        // ReSharper disable once InconsistentNaming
        private readonly Dictionary<MemberStatus, Msg.MemberStatus> MemberStatusToProto
            = new Dictionary<MemberStatus, Msg.MemberStatus>()
            {
                {MemberStatus.Joining, Msg.MemberStatus.Joining},
                {MemberStatus.Up, Msg.MemberStatus.Up},
                {MemberStatus.Leaving, Msg.MemberStatus.Leaving},
                {MemberStatus.Exiting, Msg.MemberStatus.Exiting},
                {MemberStatus.Down, Msg.MemberStatus.Down},
                {MemberStatus.Removed, Msg.MemberStatus.Removed}
            };

        private Dictionary<Msg.MemberStatus, MemberStatus> _memberStatusFromProtoCache = null;

        private Dictionary<Msg.MemberStatus, MemberStatus> MemberStatusFromProto
        {
            get
            {
                return _memberStatusFromProtoCache ??
                       (_memberStatusFromProtoCache = MemberStatusToProto.ToDictionary(pair => pair.Value,
                           pair => pair.Key));
            }
        }

        // ReSharper disable once InconsistentNaming
        private readonly Dictionary<Reachability.ReachabilityStatus, Msg.ReachabilityStatus> ReachabilityStatusToProto
            = new Dictionary<Reachability.ReachabilityStatus, Msg.ReachabilityStatus>()
            {
                { Reachability.ReachabilityStatus.Reachable, Msg.ReachabilityStatus.Reachable },
                { Reachability.ReachabilityStatus.Terminated, Msg.ReachabilityStatus.Terminated},
                { Reachability.ReachabilityStatus.Unreachable, Msg.ReachabilityStatus.Unreachable }
            };

        private Dictionary<Msg.ReachabilityStatus, Reachability.ReachabilityStatus> _reachabilityStatusFromProtoCache = null;
        private Dictionary<Msg.ReachabilityStatus, Reachability.ReachabilityStatus> ReachabilityStatusFromProto
        {
            get
            {
                return _reachabilityStatusFromProtoCache ??
                       (_reachabilityStatusFromProtoCache = ReachabilityStatusToProto.ToDictionary(pair => pair.Value,
                           pair => pair.Key));
            }
        }

        private int MapWithErrorMessage<T>(Dictionary<T, int> map, T value, string unknown)
        {
            if (map.ContainsKey(value)) return map[value];
            throw new ArgumentException(string.Format("Unknown {0} [{1}] in cluster message", unknown, value));
        }

        private Msg.Join JoinToProto(UniqueAddress node, ImmutableHashSet<string> roles)
        {
            var join = new Msg.Join{ Node = UniqueAddressToProto(node)};
            join.Roles.AddRange(roles);
            return join;
        }

        private Msg.Welcome WelcomeToProto(UniqueAddress node, Gossip gossip)
        {
            return new Msg.Welcome
            {
                From = UniqueAddressToProto(node),
                Gossip = GossipToProto(gossip)
            };
        }

        private Msg.Gossip GossipToProto(Gossip gossip)
        {
            var allMembers = gossip.Members.ToList();
            var allAddresses = gossip.Members.Select(x => x.UniqueAddress).ToList();
            var addressMapping = allAddresses.ZipWithIndex();
            var allRoles = allMembers.Aggregate(ImmutableHashSet.Create<string>(),
                (set, member) => set.Union(member.Roles));
            var roleMapping = allRoles.ZipWithIndex();
            var allHashes = gossip.Version.Versions.Keys.Select(x => x.ToString()).ToList();
            var hashMapping = allHashes.ZipWithIndex();

            Func<UniqueAddress, int> mapUniqueAddress =
                address => MapWithErrorMessage(addressMapping, address, "address");

            Func<string, int> mapRole = s => MapWithErrorMessage(roleMapping, s, "role");

            Func<Member, Msg.Member> memberToProto = member =>
            {
                var mem = new Msg.Member
                {
                    AddressIndex = mapUniqueAddress(member.UniqueAddress),
                    UpNumber = member.UpNumber,
                    Status = MemberStatusToProto[member.Status]
                };
                mem.RolesIndexes.AddRange(member.Roles.Select(mapRole));
                return mem;
            };

            Func<Reachability, IEnumerable<Msg.ObserverReachability>> reachabilityToProto = reachability =>
            {
                var builderList = new List<Msg.ObserverReachability>();
                foreach (var version in reachability.Versions)
                {
                var subjectReachability = reachability.RecordsFrom(version.Key).Select(
                    r =>
                    {
                        return new Msg.SubjectReachability
                        {
                            AddressIndex = mapUniqueAddress(r.Subject),
                            Version = r.Version,
                            Status = ReachabilityStatusToProto[r.Status]
                        };
                    });

                    var reach = new Msg.ObserverReachability
                    {
                        AddressIndex = mapUniqueAddress(version.Key),
                        Version = version.Value
                    };
                    reach.SubjectReachability.AddRange(subjectReachability);
                    builderList.Add(reach);
                }
                return builderList;
            };

            var reachabilityProto = reachabilityToProto(gossip.Overview.Reachability);
            var membersProto = gossip.Members.Select(memberToProto);
            var seenProto = gossip.Overview.Seen.Select(mapUniqueAddress);

            var overview = new Msg.GossipOverview();
            overview.Seen.AddRange(seenProto);
            overview.ObserverReachability.AddRange(reachabilityProto);

            var g = new Msg.Gossip
            {
                Overview = overview,
                Version = VectorClockToProto(gossip.Version, hashMapping)
            };
            g.AllAddresses.AddRange(allAddresses.Select(x => UniqueAddressToProto(x)));
            g.AllRoles.AddRange(allRoles);
            g.AllHashes.AddRange(allHashes);
            g.Members.AddRange(membersProto);
            return g;
        }

        private Msg.VectorClock VectorClockToProto(VectorClock version, Dictionary<string, int> hashMapping)
        {
            var versions = version.Versions.Select(pair =>
                new Msg.VectorClock.Types.Version
                {
                    HashIndex = MapWithErrorMessage(hashMapping, pair.Key.ToString(), "hash"),
                    Timestamp = pair.Value
                });

            var vclock = new Msg.VectorClock { Timestamp = 0 };
            vclock.Versions.AddRange(versions);
            return vclock;
        }

        private Msg.GossipEnvelope GossipEnvelopeToProto(GossipEnvelope gossipEnvelope)
        {
            return new Msg.GossipEnvelope
            {
                From = UniqueAddressToProto(gossipEnvelope.From),
                To = UniqueAddressToProto(gossipEnvelope.To),
                SerializedGossip = ByteString.CopyFrom(Compress(GossipToProto(gossipEnvelope.Gossip)))
            };
        }

        private GossipEnvelope GossipEnvelopeFromProto(Msg.GossipEnvelope gossipEnvelope)
        {
            var serializedGossip = gossipEnvelope.SerializedGossip;
            return new GossipEnvelope(UniqueAddressFromProto(gossipEnvelope.From), 
                UniqueAddressFromProto(gossipEnvelope.To),
                GossipFromProto(Msg.Gossip.Parser.ParseFrom(Decompress(serializedGossip.ToByteArray()))));
        }

        private Msg.GossipStatus GossipStatusToProto(GossipStatus gossipStatus)
        {
            var allHashes = gossipStatus.Version.Versions.Keys.Select(x => x.ToString()).ToList();
            var hashMapping = allHashes.ZipWithIndex();
            var status = new Msg.GossipStatus
            {
                From = UniqueAddressToProto(gossipStatus.From),
                Version = VectorClockToProto(gossipStatus.Version, hashMapping)
            };
            status.AllHashes.AddRange(allHashes);
            return status;
        }

        private Gossip GossipFromProto(Msg.Gossip gossip)
        {
            var addressMapping = gossip.AllAddresses.Select(UniqueAddressFromProto).ToList();
            var roleMapping = gossip.AllRoles.ToList();
            var hashMapping = gossip.AllHashes.ToList();

            Func<IEnumerable<Msg.ObserverReachability>, Reachability> reachabilityFromProto = reachabilityProto =>
            {
                var recordBuilder = ImmutableList.CreateBuilder<Reachability.Record>();
                var versionsBuilder = ImmutableDictionary.CreateBuilder<UniqueAddress, long>();
                foreach (var o in reachabilityProto)
                {
                    var observer = addressMapping[o.AddressIndex];
                    versionsBuilder.Add(observer, o.Version);
                    foreach (var s in o.SubjectReachability)
                    {
                        var subject = addressMapping[s.AddressIndex];
                        var record = new Reachability.Record(observer, subject, ReachabilityStatusFromProto[s.Status],
                            s.Version);
                        recordBuilder.Add(record);
                    }
                }

                return new Reachability(recordBuilder.ToImmutable(), versionsBuilder.ToImmutable());
            };

            Func<Msg.Member, Member> memberFromProto = member => Member.Create(addressMapping[member.AddressIndex], member.UpNumber>0 ? member.UpNumber : 0, MemberStatusFromProto[member.Status],
                member.RolesIndexes.Select(x => roleMapping[x]).ToImmutableHashSet());

            var members = gossip.Members.Select(memberFromProto).ToImmutableSortedSet(Member.Ordering);
            var reachability = reachabilityFromProto(gossip.Overview.ObserverReachability);
            var seen = gossip.Overview.Seen.Select(x => addressMapping[x]).ToImmutableHashSet();
            var overview = new GossipOverview(seen, reachability);

            return new Gossip(members, overview, VectorClockFromProto(gossip.Version, hashMapping));
        }

        private GossipStatus GossipStatusFromProto(Msg.GossipStatus status)
        {
            return new GossipStatus(UniqueAddressFromProto(status.From), VectorClockFromProto(status.Version, status.AllHashes));
        }

        private VectorClock VectorClockFromProto(Msg.VectorClock version, IList<string> hashMapping)
        {
            return
                VectorClock.Create(
                    version.Versions.ToImmutableSortedDictionary(version1 => VectorClock.Node.FromHash(hashMapping[version1.HashIndex]),
                        version1 => version1.Timestamp));
        }

        private GossipEnvelope GossipEnvelopeFromBinary(byte[] bytes)
        {
            return GossipEnvelopeFromProto(Msg.GossipEnvelope.Parser.ParseFrom(bytes));
        }

        private GossipStatus GossipStatusFromBinary(byte[] bytes)
        {
            return GossipStatusFromProto(Msg.GossipStatus.Parser.ParseFrom(bytes));
        }

        #endregion
    }
}

