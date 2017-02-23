//-----------------------------------------------------------------------
// <copyright file="PersistentFSMSpec.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2016 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2016 Akka.NET project <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Persistence.Fsm;
using Akka.TestKit;
using FluentAssertions;
using System;
using System.Collections.Generic;
using Xunit;

namespace Akka.Persistence.Tests.Fsm
{
    public class PersistentFSMSpec : PersistenceSpec
    {
        private readonly Random _random = new Random();

        public PersistentFSMSpec()
            : base(Configuration("PersistentFSMSpec"))
        {
        }

        [Fact]
        public void PersistentFSM_must_has_function_as_regular_fsm()
        {
            var dummyReportActorRef = CreateTestProbe().Ref;
            var fsmRef = Sys.ActorOf(WebStoreCustomerFSM.Props(Name, dummyReportActorRef));

            Watch(fsmRef);
            fsmRef.Tell(new FSMBase.SubscribeTransitionCallBack(TestActor));

            var shirt = new Item("1", "Shirt", 59.99F);
            var shoes = new Item("2", "Shoes", 89.99F);
            var coat = new Item("3", "Coat", 119.99F);

            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new AddItem(shirt));
            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new AddItem(shoes));
            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new AddItem(coat));
            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new Buy());
            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new Leave());

            var userState = ExpectMsg<FSMBase.CurrentState<UserState>>();
            userState.FsmRef.Should().Be(fsmRef);
            userState.State.Should().Be(UserState.LookingAround);
            ExpectMsg<EmptyShoppingCart>();

            var transition1 = ExpectMsg<FSMBase.Transition<UserState>>();
            transition1.FsmRef.Should().Be(fsmRef);
            transition1.From.Should().Be(UserState.LookingAround);
            transition1.To.Should().Be(UserState.Shopping);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes, coat);

            var transition2 = ExpectMsg<FSMBase.Transition<UserState>>();
            transition2.FsmRef.Should().Be(fsmRef);
            transition2.From.Should().Be(UserState.Shopping);
            transition2.To.Should().Be(UserState.Paid);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes, coat);

            ExpectTerminated(fsmRef);
        }

        [Fact]
        public void PersistentFSM_must_has_function_as_regular_fsm_on_state_timeout()
        {
            var dummyReportActorRef = CreateTestProbe().Ref;
            var fsmRef = Sys.ActorOf(WebStoreCustomerFSM.Props(Name, dummyReportActorRef));

            Watch(fsmRef);
            fsmRef.Tell(new FSMBase.SubscribeTransitionCallBack(TestActor));

            var shirt = new Item("1", "Shirt", 59.99F);

            fsmRef.Tell(new AddItem(shirt));

            var userState = ExpectMsg<FSMBase.CurrentState<UserState>>();
            userState.FsmRef.Should().Be(fsmRef);
            userState.State.Should().Be(UserState.LookingAround);

            var transition1 = ExpectMsg<FSMBase.Transition<UserState>>();
            transition1.FsmRef.Should().Be(fsmRef);
            transition1.From.Should().Be(UserState.LookingAround);
            transition1.To.Should().Be(UserState.Shopping);

            Within(TimeSpan.FromSeconds(0.9), RemainingOrDefault, () =>
            {
                var transition2 = ExpectMsg<FSMBase.Transition<UserState>>();
                transition2.FsmRef.Should().Be(fsmRef);
                transition2.From.Should().Be(UserState.Shopping);
                transition2.To.Should().Be(UserState.Inactive);
            });

            ExpectTerminated(fsmRef);
        }

        [Fact]
        public void PersistentFSM_must_recover_successfully_with_correct_state_data()
        {
            var dummyReportActorRef = CreateTestProbe().Ref;
            var fsmRef = Sys.ActorOf(WebStoreCustomerFSM.Props(Name, dummyReportActorRef));

            Watch(fsmRef);
            fsmRef.Tell(new FSMBase.SubscribeTransitionCallBack(TestActor));

            var shirt = new Item("1", "Shirt", 59.99F);
            var shoes = new Item("2", "Shoes", 89.99F);
            var coat = new Item("3", "Coat", 119.99F);

            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new AddItem(shirt));
            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new AddItem(shoes));
            fsmRef.Tell(new GetCurrentCart());

            var userState1 = ExpectMsg<FSMBase.CurrentState<UserState>>();
            userState1.FsmRef.Should().Be(fsmRef);
            userState1.State.Should().Be(UserState.LookingAround);
            ExpectMsg<EmptyShoppingCart>();

            var transition1 = ExpectMsg<FSMBase.Transition<UserState>>();
            transition1.FsmRef.Should().Be(fsmRef);
            transition1.From.Should().Be(UserState.LookingAround);
            transition1.To.Should().Be(UserState.Shopping);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes);

            fsmRef.Tell(PoisonPill.Instance);
            ExpectTerminated(fsmRef);

            var recoveredFsmRef = Sys.ActorOf(Props.Create(() => new WebStoreCustomerFSM(Name, dummyReportActorRef)));
            Watch(recoveredFsmRef);
            recoveredFsmRef.Tell(new FSMBase.SubscribeTransitionCallBack(TestActor));

            recoveredFsmRef.Tell(new GetCurrentCart());

            recoveredFsmRef.Tell(new AddItem(coat));
            recoveredFsmRef.Tell(new GetCurrentCart());

            recoveredFsmRef.Tell(new Buy());
            recoveredFsmRef.Tell(new GetCurrentCart());
            recoveredFsmRef.Tell(new Leave());

            var userState2 = ExpectMsg<FSMBase.CurrentState<UserState>>();
            userState2.FsmRef.Should().Be(recoveredFsmRef);
            userState2.State.Should().Be(UserState.Shopping);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes);

            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes, coat);

            var transition2 = ExpectMsg<FSMBase.Transition<UserState>>();
            transition2.FsmRef.Should().Be(recoveredFsmRef);
            transition2.From.Should().Be(UserState.Shopping);
            transition2.To.Should().Be(UserState.Paid);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes, coat);

            ExpectTerminated(recoveredFsmRef);
        }

        [Fact]
        public void PersistentFSM_must_execute_the_defined_actions_following_successful_persistence_of_state_change()
        {
            var reportActorProbe = CreateTestProbe(Sys);
            var fsmRef = Sys.ActorOf(WebStoreCustomerFSM.Props(Name, reportActorProbe.Ref));

            Watch(fsmRef);
            fsmRef.Tell(new FSMBase.SubscribeTransitionCallBack(TestActor));

            var shirt = new Item("1", "Shirt", 59.99F);
            var shoes = new Item("2", "Shoes", 89.99F);
            var coat = new Item("3", "Coat", 119.99F);

            fsmRef.Tell(new AddItem(shirt));
            fsmRef.Tell(new AddItem(shoes));
            fsmRef.Tell(new AddItem(coat));
            fsmRef.Tell(new Buy());
            fsmRef.Tell(new Leave());

            var userState = ExpectMsg<FSMBase.CurrentState<UserState>>();
            userState.FsmRef.Should().Be(fsmRef);
            userState.State.Should().Be(UserState.LookingAround);

            var transition1 = ExpectMsg<FSMBase.Transition<UserState>>();
            transition1.FsmRef.Should().Be(fsmRef);
            transition1.From.Should().Be(UserState.LookingAround);
            transition1.To.Should().Be(UserState.Shopping);

            var transition2 = ExpectMsg<FSMBase.Transition<UserState>>();
            transition2.FsmRef.Should().Be(fsmRef);
            transition2.From.Should().Be(UserState.Shopping);
            transition2.To.Should().Be(UserState.Paid);

            reportActorProbe.ExpectMsg<PurchaseWasMade>().Items.Should().ContainInOrder(shirt, shoes, coat);

            ExpectTerminated(fsmRef);
        }

        [Fact]
        public void PersistentFSM_must_execute_the_defined_actions_following_successful_persistence_of_FSM_stop()
        {
            var reportActorProbe = CreateTestProbe(Sys);
            var fsmRef = Sys.ActorOf(WebStoreCustomerFSM.Props(Name, reportActorProbe.Ref));

            Watch(fsmRef);
            fsmRef.Tell(new FSMBase.SubscribeTransitionCallBack(TestActor));

            var shirt = new Item("1", "Shirt", 59.99F);
            var shoes = new Item("2", "Shoes", 89.99F);
            var coat = new Item("3", "Coat", 119.99F);

            fsmRef.Tell(new AddItem(shirt));
            fsmRef.Tell(new AddItem(shoes));
            fsmRef.Tell(new AddItem(coat));
            fsmRef.Tell(new Leave());

            var userState = ExpectMsg<FSMBase.CurrentState<UserState>>();
            userState.FsmRef.Should().Be(fsmRef);
            userState.State.Should().Be(UserState.LookingAround);

            var transition = ExpectMsg<FSMBase.Transition<UserState>>();
            transition.FsmRef.Should().Be(fsmRef);
            transition.From.Should().Be(UserState.LookingAround);
            transition.To.Should().Be(UserState.Shopping);

            reportActorProbe.ExpectMsg<ShoppingCardDiscarded>();

            ExpectTerminated(fsmRef);
        }

        [Fact]
        public void PersistentFSM_must_recover_successfully_with_correct_state_timeout()
        {
            var dummyReportActorRef = CreateTestProbe().Ref;
            var fsmRef = Sys.ActorOf(WebStoreCustomerFSM.Props(Name, dummyReportActorRef));

            Watch(fsmRef);
            fsmRef.Tell(new FSMBase.SubscribeTransitionCallBack(TestActor));

            var shirt = new Item("1", "Shirt", 59.99F);

            fsmRef.Tell(new AddItem(shirt));

            var userState1 = ExpectMsg<FSMBase.CurrentState<UserState>>();
            userState1.FsmRef.Should().Be(fsmRef);
            userState1.State.Should().Be(UserState.LookingAround);

            var transition1 = ExpectMsg<FSMBase.Transition<UserState>>();
            transition1.FsmRef.Should().Be(fsmRef);
            transition1.From.Should().Be(UserState.LookingAround);
            transition1.To.Should().Be(UserState.Shopping);

            ExpectNoMsg(TimeSpan.FromSeconds(0.6)); // arbitrarily chosen delay, less than the timeout, before stopping the FSM
            fsmRef.Tell(PoisonPill.Instance);
            ExpectTerminated(fsmRef);

            var recoveredFsmRef = Sys.ActorOf(Props.Create(() => new WebStoreCustomerFSM(Name, dummyReportActorRef)));
            Watch(recoveredFsmRef);
            recoveredFsmRef.Tell(new FSMBase.SubscribeTransitionCallBack(TestActor));

            var userState2 = ExpectMsg<FSMBase.CurrentState<UserState>>();
            userState2.FsmRef.Should().Be(recoveredFsmRef);
            userState2.State.Should().Be(UserState.Shopping);

            Within(TimeSpan.FromSeconds(0.9), RemainingOrDefault, () =>
            {
                var transition2 = ExpectMsg<FSMBase.Transition<UserState>>();
                transition2.FsmRef.Should().Be(recoveredFsmRef);
                transition2.From.Should().Be(UserState.Shopping);
                transition2.To.Should().Be(UserState.Inactive);
            });

            ExpectNoMsg(TimeSpan.FromSeconds(0.6)); // arbitrarily chosen delay, less than the timeout, before stopping the FSM
            recoveredFsmRef.Tell(PoisonPill.Instance);
            ExpectTerminated(recoveredFsmRef);

            var recoveredFsmRef2 = Sys.ActorOf(Props.Create(() => new WebStoreCustomerFSM(Name, dummyReportActorRef)));
            Watch(recoveredFsmRef2);
            recoveredFsmRef2.Tell(new FSMBase.SubscribeTransitionCallBack(TestActor));

            var userState3 = ExpectMsg<FSMBase.CurrentState<UserState>>();
            userState3.FsmRef.Should().Be(recoveredFsmRef2);
            userState3.State.Should().Be(UserState.Inactive);
            ExpectTerminated(recoveredFsmRef2);
        }

        [Fact]
        public void PersistentFSM_must_not_trigger_onTransition_for_stay()
        {
            var probe = CreateTestProbe(Sys);
            var fsmRef = Sys.ActorOf(SimpleTransitionFSM.Props(Name, probe.Ref));

            // TODO: this line is not working at the moment, FSM does not call onTransition
            probe.ExpectMsg("LookingAround -> LookingAround", 3.Seconds()); // caused by initialize(), OK

            fsmRef.Tell("goto(the same state)"); // causes goto()
            probe.ExpectMsg("LookingAround -> LookingAround", 3.Seconds());

            fsmRef.Tell("stay");
            probe.ExpectNoMsg(3.Seconds());
        }

        // TODO
        [Fact]
        public void PersistentFSM_must_not_persist_state_change_event_when_staying_in_the_same_state()
        {
            var dummyReportActorRef = CreateTestProbe().Ref;

            var fsmRef = Sys.ActorOf(WebStoreCustomerFSM.Props(Name, dummyReportActorRef));
            Watch(fsmRef);

            var shirt = new Item("1", "Shirt", 59.99F);
            var shoes = new Item("2", "Shoes", 89.99F);
            var coat = new Item("3", "Coat", 119.99F);

            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new AddItem(shirt));
            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new AddItem(shoes));
            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new AddItem(coat));
            fsmRef.Tell(new GetCurrentCart());

            ExpectMsg<EmptyShoppingCart>();

            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes, coat);

            fsmRef.Tell(PoisonPill.Instance);
            ExpectTerminated(fsmRef);

            var persistentEventsStreamer = Sys.ActorOf(PersistentEventsStreamer.Props(Name, TestActor));

            ExpectMsg<ItemAdded>().Item.Should().Be(shirt);
            ExpectMsg<PersistentFSMBase<UserState, IShoppingCart, IDomainEvent>.StateChangeEvent>();

            ExpectMsg<ItemAdded>().Item.Should().Be(shoes);
            ExpectMsg<PersistentFSMBase<UserState, IShoppingCart, IDomainEvent>.StateChangeEvent>();

            ExpectMsg<ItemAdded>().Item.Should().Be(coat);
            ExpectMsg<PersistentFSMBase<UserState, IShoppingCart, IDomainEvent>.StateChangeEvent>();

            Watch(persistentEventsStreamer);
            persistentEventsStreamer.Tell(PoisonPill.Instance);
            ExpectTerminated(persistentEventsStreamer);
        }

        [Fact]
        public void PersistentFSM_must_persist_snapshot()
        {
            var dummyReportActorRef = CreateTestProbe().Ref;

            var fsmRef = Sys.ActorOf(WebStoreCustomerFSM.Props(Name, dummyReportActorRef));
            Watch(fsmRef);

            var shirt = new Item("1", "Shirt", 59.99F);
            var shoes = new Item("2", "Shoes", 89.99F);
            var coat = new Item("3", "Coat", 119.99F);

            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new AddItem(shirt));
            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new AddItem(shoes));
            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new AddItem(coat));
            fsmRef.Tell(new GetCurrentCart());
            fsmRef.Tell(new Buy());
            fsmRef.Tell(new GetCurrentCart());

            ExpectMsg<EmptyShoppingCart>();

            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes);
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes, coat);

            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes, coat);
            ExpectNoMsg(1.Seconds());

            fsmRef.Tell(PoisonPill.Instance);
            ExpectTerminated(fsmRef);

            var recoveredFsmRef = Sys.ActorOf(WebStoreCustomerFSM.Props(Name, dummyReportActorRef));
            recoveredFsmRef.Tell(new GetCurrentCart());
            ExpectMsg<NonEmptyShoppingCart>().Items.Should().ContainInOrder(shirt, shoes, coat);

            Watch(recoveredFsmRef);
            recoveredFsmRef.Tell(PoisonPill.Instance);
            ExpectTerminated(recoveredFsmRef);

            var persistentEventsStreamer = Sys.ActorOf(PersistentEventsStreamer.Props(Name, TestActor));

            // TODO: this should be fixed
            ExpectMsg<SnapshotOffer>();

            Watch(persistentEventsStreamer);
            persistentEventsStreamer.Tell(PoisonPill.Instance);
            ExpectTerminated(persistentEventsStreamer);
        }

        [Fact]
        public void PersistentFSM_must_allow_cancelling_stateTimeout_by_issuing_forMax_null()
        {
            var probe = CreateTestProbe();

            var fsm = Sys.ActorOf(TimeoutFsm.Props(probe.Ref));
            probe.ExpectMsg<PersistentFSMBase<TimeoutFsm.State, string, string>.StateTimeout>();

            fsm.Tell(TimeoutFsm.OverrideTimeoutToInf.Instance);
            probe.ExpectMsg<TimeoutFsm.OverrideTimeoutToInf>();
            probe.ExpectNoMsg(1.Seconds());
        }

        internal class WebStoreCustomerFSM : PersistentFSM<UserState, IShoppingCart, IDomainEvent>
        {
            public WebStoreCustomerFSM(string persistenceId, IActorRef reportActor)
            {
                PersistenceId = persistenceId;

                StartWith(UserState.LookingAround, new EmptyShoppingCart());

                When(UserState.LookingAround, (evt, state) =>
                {
                    if (evt.FsmEvent is AddItem)
                    {
                        var addItem = (AddItem)evt.FsmEvent;
                        return GoTo(UserState.Shopping)
                            .Applying(new ItemAdded(addItem.Item))
                            .ForMax(TimeSpan.FromSeconds(1));
                    }
                    if (evt.FsmEvent is GetCurrentCart)
                    {
                        return Stay().Replying(evt.StateData);
                    }
                    return state;
                });

                When(UserState.Shopping, (evt, state) =>
                {
                    if (evt.FsmEvent is AddItem)
                    {
                        var addItem = (AddItem)evt.FsmEvent;
                        return Stay().Applying(new ItemAdded(addItem.Item)).ForMax(TimeSpan.FromSeconds(1));
                    }

                    if (evt.FsmEvent is Buy)
                    {
                        return GoTo(UserState.Paid)
                            .Applying(new OrderExecuted())
                            .AndThen(cart =>
                            {
                                if (cart is NonEmptyShoppingCart)
                                {
                                    var nonShoppingCart = (NonEmptyShoppingCart)cart;
                                    reportActor.Tell(new PurchaseWasMade(nonShoppingCart.Items));
                                    // TODO: implement
                                    //SaveStateSnapshot();
                                }
                                else if (cart is EmptyShoppingCart)
                                {
                                    // TODO: implement
                                    //SaveStateSnapshot();
                                }
                            });
                    }

                    if (evt.FsmEvent is Leave)
                    {
                        return Stop()
                            .Applying(new OrderDiscarded())
                            .AndThen(cart =>
                            {
                                reportActor.Tell(new ShoppingCardDiscarded());
                                // TODO: implement
                                //SaveStateSnapshot();
                            });
                    }

                    if (evt.FsmEvent is GetCurrentCart)
                    {
                        return Stay().Replying(evt.StateData);
                    }

                    if (evt.FsmEvent is StateTimeout)
                    {
                        return GoTo(UserState.Inactive).ForMax(TimeSpan.FromSeconds(2));
                    }

                    return state;
                });

                When(UserState.Inactive, (evt, state) =>
                {
                    if (evt.FsmEvent is AddItem)
                    {
                        var addItem = (AddItem)evt.FsmEvent;
                        return GoTo(UserState.Shopping)
                            .Applying(new ItemAdded(addItem.Item))
                            .ForMax(TimeSpan.FromSeconds(1));
                    }

                    if (evt.FsmEvent is StateTimeout)
                    {
                        return Stop()
                            .Applying(new OrderDiscarded())
                            .AndThen(cart => reportActor.Tell(new ShoppingCardDiscarded()));
                    }

                    return state;
                });

                When(UserState.Paid, (evt, state) =>
                {
                    if (evt.FsmEvent is Leave)
                    {
                        return Stop();
                    }

                    if (evt.FsmEvent is GetCurrentCart)
                    {
                        return Stay().Replying(evt.StateData);
                    }

                    return state;
                });
            }

            public override string PersistenceId { get; }

            internal static Props Props(string name, IActorRef dummyReportActorRef)
            {
                return Akka.Actor.Props.Create(() => new WebStoreCustomerFSM(name, dummyReportActorRef));
            }

            protected override IShoppingCart ApplyEvent(IDomainEvent evt, IShoppingCart cartBeforeEvent)
            {
                if (evt is ItemAdded)
                {
                    var itemAdded = (ItemAdded)evt;
                    return cartBeforeEvent.AddItem(itemAdded.Item);
                }

                if (evt is OrderExecuted)
                {
                    return cartBeforeEvent;
                }

                if (evt is OrderDiscarded)
                {
                    return cartBeforeEvent.Empty();
                }

                return cartBeforeEvent;
            }
        }
    }

    public class TimeoutFsm : PersistentFSM<TimeoutFsm.State, string, string>
    {
        public enum State
        {
            Init
        }

        public class OverrideTimeoutToInf
        {
            public static readonly OverrideTimeoutToInf Instance = new OverrideTimeoutToInf();
            private OverrideTimeoutToInf() { }
        }

        public TimeoutFsm(IActorRef probe)
        {
            StartWith(State.Init, "");

            When(State.Init, (evt, state) =>
            {
                if (evt.FsmEvent is StateTimeout)
                {
                    probe.Tell(new StateTimeout());
                    return Stay();
                }
                else if (evt.FsmEvent is OverrideTimeoutToInf)
                {
                    probe.Tell(OverrideTimeoutToInf.Instance);
                    return Stay().ForMax(TimeSpan.MaxValue);
                }

                return null;
            }, TimeSpan.FromMilliseconds(300));
        }

        public override string PersistenceId { get; } = "timeout-test";

        protected override string ApplyEvent(string e, string data)
        {
            return "whatever";
        }

        public static Props Props(IActorRef probe)
        {
            return Actor.Props.Create(() => new TimeoutFsm(probe));
        }
    }

    internal class SimpleTransitionFSM : PersistentFSM<UserState, IShoppingCart, IDomainEvent>
    {
        public SimpleTransitionFSM(string persistenceId, IActorRef reportActor)
        {
            PersistenceId = persistenceId;

            StartWith(UserState.LookingAround, new EmptyShoppingCart());

            When(UserState.LookingAround, (evt, state) =>
            {
                if ((string)evt.FsmEvent == "stay")
                {
                    return Stay();
                }
                return GoTo(UserState.LookingAround);
            });

            OnTransition((state, nextState) =>
            {
                reportActor.Tell($"{state} -> {nextState}");
            });
        }

        public override string PersistenceId { get; }

        protected override IShoppingCart ApplyEvent(IDomainEvent domainEvent, IShoppingCart currentData)
        {
            return currentData;
        }

        public static Props Props(string persistenceId, IActorRef reportActor)
        {
            return Actor.Props.Create(() => new SimpleTransitionFSM(persistenceId, reportActor));
        }
    }

    internal class PersistentEventsStreamer : PersistentActor
    {
        private readonly IActorRef _client;
        private readonly string _persistenceId;

        public PersistentEventsStreamer(string persistenceId, IActorRef client)
        {
            _persistenceId = persistenceId;
            _client = client;
        }

        public override string PersistenceId
        {
            get { return _persistenceId; }
        }

        protected override bool ReceiveRecover(object message)
        {
            if (!(message is RecoveryCompleted))
            {
                _client.Tell(message);
            }

            return true;
        }

        protected override bool ReceiveCommand(object message)
        {
            return true;
        }

        public static Props Props(string persistenceId, IActorRef client)
        {
            return Actor.Props.Create(() => new PersistentEventsStreamer(persistenceId, client));
        }
    }

    #region Custome States

    internal enum UserState
    {
        Shopping,
        Inactive,
        Paid,
        LookingAround
    }

    #endregion

    #region Customer states data

    internal class Item
    {
        public Item(string id, string name, float price)
        {
            Id = id;
            Name = name;
            Price = price;
        }

        public string Id { get; }
        public string Name { get; }
        public float Price { get; }

        #region Equals
        protected bool Equals(Item other)
        {
            return string.Equals(Id, other.Id) && string.Equals(Name, other.Name) && Price.Equals(other.Price);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Item)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Id != null ? Id.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ Price.GetHashCode();
                return hashCode;
            }
        }
        #endregion
    }

    internal interface IShoppingCart
    {
        ICollection<Item> Items { get; set; }
        IShoppingCart AddItem(Item item);
        IShoppingCart Empty();
    }

    internal class EmptyShoppingCart : IShoppingCart
    {
        public IShoppingCart AddItem(Item item)
        {
            return new NonEmptyShoppingCart(item);
        }

        public IShoppingCart Empty()
        {
            return this;
        }

        public ICollection<Item> Items { get; set; }
    }

    internal class NonEmptyShoppingCart : IShoppingCart
    {
        public NonEmptyShoppingCart(Item item)
        {
            Items = new List<Item>();
            Items.Add(item);
        }

        public IShoppingCart AddItem(Item item)
        {
            Items.Add(item);
            return this;
        }

        public IShoppingCart Empty()
        {
            return new EmptyShoppingCart();
        }

        public ICollection<Item> Items { get; set; }
    }

    #endregion

    #region Customer commands

    internal interface ICommand
    {
    }

    internal class AddItem : ICommand
    {
        public AddItem(Item item)
        {
            Item = item;
        }

        public Item Item { get; private set; }
    }

    internal class Buy
    {
    }

    internal class Leave
    {
    }

    internal class GetCurrentCart : ICommand
    {
    }

    #endregion

    #region Customer domain events

    internal interface IDomainEvent
    {
    }

    internal class ItemAdded : IDomainEvent
    {
        public ItemAdded(Item item)
        {
            Item = item;
        }

        public Item Item { get; private set; }
    }

    internal class OrderExecuted : IDomainEvent
    {
    }

    internal class OrderDiscarded : IDomainEvent
    {
    }

    #endregion

    #region Side effects - report events to be sent to some

    internal interface IReportEvent
    {
    }

    internal class PurchaseWasMade : IReportEvent
    {
        public PurchaseWasMade(IEnumerable<Item> items)
        {
            Items = items;
        }

        public IEnumerable<Item> Items { get; }
    }

    internal class ShoppingCardDiscarded : IReportEvent
    {
    }

    #endregion
}