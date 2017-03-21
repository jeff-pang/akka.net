﻿//-----------------------------------------------------------------------
// <copyright file="SerializationSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Dispatch.SysMsg;
using Akka.Routing;
using Akka.Serialization;
using Akka.TestKit;
using Akka.TestKit.TestActors;
using Akka.Util.Internal;
using Xunit;
using FluentAssertions;

namespace Akka.Tests.Serialization
{
    public class ProtoSerializatorSpec : AkkaSpec
    {
        #region actor
        public class Watchee : UntypedActor
        {
            protected override void OnReceive(object message)
            {
                
            }
        }

        public class Watcher : UntypedActor
        {
            protected override void OnReceive(object message)
            {
                
            }
        }
        #endregion

        [Fact]
        public void Can_serialize_ActorRef()
        {
            var actorRef = ActorOf<BlackHoleActor>();
            AssertEqual(actorRef);
        }

        [Fact]
        public void Can_serialize_ActorPath()
        {
            var uri = "akka.tcp://sys@localhost:9000/user/actor";
            var actorPath = ActorPath.Parse(uri);
            AssertEqual(actorPath);
        }

        [Fact]
        public void Can_serialize_Identify()
        {
            var identify = new Identify("message");
            AssertEqual(identify);
        }

        [Fact]
        public void Can_serialize_ActorIdentity()
        {
            var actorRef = ActorOf<BlackHoleActor>();
            var actorIdentity = new ActorIdentity("message", actorRef);
            AssertEqual(actorIdentity);
        }

        [Fact]
        public void Can_serialize_PoisonPill()
        {
            var poisonPill = PoisonPill.Instance;
            AssertEqual(poisonPill);
        }

        [Fact]
        public void Can_serialize_Watch()
        {
            var watchee = ActorOf<Watchee>().AsInstanceOf<IInternalActorRef>();
            var watcher = ActorOf<Watcher>().AsInstanceOf<IInternalActorRef>();
            var watch = new Watch(watchee, watcher);
            AssertEqual(watch);
        }

        [Fact]
        public void Can_serialize_Unwatch()
        {
            var watchee = ActorOf<Watchee>().AsInstanceOf<IInternalActorRef>();
            var watcher = ActorOf<Watcher>().AsInstanceOf<IInternalActorRef>();
            var unwatch = new Unwatch(watchee, watcher);
            AssertEqual(unwatch);
        }

        [Fact]
        public void Can_serialize_DeadwatchNotification()
        {
            var actorRef = ActorOf<BlackHoleActor>();
            var deadwatchNotification = new DeathWatchNotification(actorRef, true, false);
            DeathWatchNotification deserialized = AssertAndReturn(deadwatchNotification);
            deserialized.Actor.Should().Be(actorRef);
            deserialized.AddressTerminated.Should().Be(deadwatchNotification.AddressTerminated);
            deserialized.ExistenceConfirmed.Should().Be(deadwatchNotification.ExistenceConfirmed);
        }

        [Fact]
        public void Can_serialize_Terminate()
        {
            var terminate = new Terminate();
            AssertAndReturn(terminate).Should().BeOfType<Terminate>();
        }

        [Fact]
        public void Can_serialize_Supervise()
        {
            var actorRef = ActorOf<BlackHoleActor>();
            var supervise = new Supervise(actorRef, true);
            Supervise deserialized = AssertAndReturn(supervise);
            deserialized.Child.Should().Be(actorRef);
            deserialized.Async.Should().Be(supervise.Async);
        }

        [Fact]
        public void Can_serialize_Address()
        {
            var address = new Address("akka.tcp", "TestSys", "localhost", 23423);
            AssertEqual(address);
        }

        [Fact]
        public void Can_serialize_Address_without_port()
        {
            var address = new Address("akka.tcp", "TestSys", "localhost");
            AssertEqual(address);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_RemoteScope()
        {
            var address = new Address("akka.tcp", "TestSys", "localhost", 23423);
            var remoteScope = new RemoteScope(address);
            AssertEqual(remoteScope);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_Config()
        {
            var message = ConfigurationFactory.Default();
            var serializer = Sys.Serialization.FindSerializerFor(message);
            serializer.Should().BeOfType<ProtoSerializer>();
            var serialized = serializer.ToBinary(message);
            var deserialized = (Config)serializer.FromBinary(serialized, typeof(Config));

            var config1 = message.ToString();
            var config2 = deserialized.ToString();

            Assert.Equal(config1, config2);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_Decider()
        {
            var decider = Decider.From(
                Directive.Restart,
                Directive.Stop.When<ArgumentException>(),
                Directive.Stop.When<NullReferenceException>());

            var deserialized = AssertAndReturn(decider);
            deserialized.DefaultDirective.Should().Be(decider.DefaultDirective);
            deserialized.Pairs.Should().BeEquivalentTo(decider.Pairs);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_OneForOneStrategy()
        {
            var decider = Decider.From(
                Directive.Restart,
                Directive.Stop.When<ArgumentException>(),
                Directive.Stop.When<NullReferenceException>());

            var message = new OneForOneStrategy(5, 10, decider, true);
            AssertEqual(message);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_AllForOneStrategy()
        {
            var decider = Decider.From(
                Directive.Restart,
                Directive.Stop.When<ArgumentException>(),
                Directive.Stop.When<NullReferenceException>());

            var message = new AllForOneStrategy(5, 10, decider, true);
            AssertEqual(message);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_RoundRobinPool()
        {
            var decider = Decider.From(
                Directive.Restart,
                Directive.Stop.When<ArgumentException>(),
                Directive.Stop.When<NullReferenceException>());

            var supervisor = new OneForOneStrategy(decider);

            var message = new RoundRobinPool(10, new DefaultResizer(0, 1), supervisor, "abc");
            AssertEqual(message);
        }

        [Fact]
        public void Can_serialize_RoundRobinGroup()
        {
            var message = new RoundRobinGroup("abc", Dispatchers.DefaultDispatcherId);
            AssertEqual(message);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_RandomPool()
        {
            var decider = Decider.From(
                Directive.Restart,
                Directive.Stop.When<ArgumentException>(),
                Directive.Stop.When<NullReferenceException>());

            var supervisor = new OneForOneStrategy(decider);

            var message = new RandomPool(10, new DefaultResizer(0, 1), supervisor, "abc");
            AssertEqual(message);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_RandomGroup()
        {
            var message = new RandomGroup("abc", Dispatchers.DefaultDispatcherId);
            AssertEqual(message);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_ConsistentHashPool()
        {
            var decider = Decider.From(
                Directive.Restart,
                Directive.Stop.When<ArgumentException>(),
                Directive.Stop.When<NullReferenceException>());

            var supervisor = new OneForOneStrategy(decider);

            var message = new ConsistentHashingPool(10, new DefaultResizer(0, 2), supervisor, Dispatchers.DefaultDispatcherId, false, 1, null);
            AssertEqual(message);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_ConsistentHashingGroup()
        {
            var message = new ConsistentHashingGroup("abc", Dispatchers.DefaultDispatcherId);
            AssertEqual(message);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_TailChoppingPool()
        {
            var decider = Decider.From(
                Directive.Restart,
                Directive.Stop.When<ArgumentException>(),
                Directive.Stop.When<NullReferenceException>());

            var supervisor = new OneForOneStrategy(decider);
            var message = new TailChoppingPool(10, null, supervisor, "abc", TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(2));
            AssertEqual(message);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_TailChoppingGroup()
        {
            var message = new TailChoppingGroup(new List<string> { "abc" }, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), Dispatchers.DefaultDispatcherId);
            AssertEqual(message);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_ScatterGatherFirstCompletedPool()
        {
            var decider = Decider.From(
                Directive.Restart,
                Directive.Stop.When<ArgumentException>(),
                Directive.Stop.When<NullReferenceException>());

            var supervisor = new OneForOneStrategy(decider);
            var message = new ScatterGatherFirstCompletedPool(10, null, TimeSpan.MaxValue, supervisor, "abc");
            AssertEqual(message);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_ScatterGatherFirstCompletedGroup()
        {
            var message = new ScatterGatherFirstCompletedGroup(new List<string> {"abc"}, TimeSpan.FromSeconds(1), Dispatchers.DefaultDispatcherId);
            AssertEqual(message);
        }

        [Fact(Skip = "Not implemented yet")]
        public void Can_serialize_SmallestMailboxPool()
        {
            var decider = Decider.From(
                Directive.Restart,
                Directive.Stop.When<ArgumentException>(),
                Directive.Stop.When<NullReferenceException>());

            var supervisor = new OneForOneStrategy(decider);

            var message = new SmallestMailboxPool(10, null, supervisor, "abc");
            AssertEqual(message);
        }

        [Fact]
        public void Can_serialize_Kill()
        {
            var kill = Kill.Instance;
            AssertEqual(kill);
        }

        private T AssertAndReturn<T>(T message)
        {
            var serializer = Sys.Serialization.FindSerializerFor(message);
            serializer.Should().BeOfType<ProtoSerializer>();
            var serialized = serializer.ToBinary(message);
            var result = serializer.FromBinary(serialized, typeof(T));
            return (T)result;
        }

        private void AssertEqual<T>(T message)
        {
            var deserialized = AssertAndReturn(message);
            Assert.Equal(message, deserialized);
        }
    }
}
