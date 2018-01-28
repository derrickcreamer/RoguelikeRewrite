using System;
using GameComponents;
using GameComponents.DirectionUtility;

namespace RoguelikeRewrite {
	// Event is a separate branch here, used only for a few event types (like player and AI turns).
	// Most events will return Results objects and inherit from Event<TResult>.
	//todo, xml: no return value
	public abstract class SimpleEvent : GameObject, IEvent {
		public SimpleEvent(GameUniverse g) : base(g) { }
		public abstract void ExecuteEvent();
	}
	public abstract class Event<TResult> : GameObject, IEvent {
		public Event(GameUniverse g) : base(g) { }
		void IEvent.ExecuteEvent() { Execute(); }
		public abstract TResult Execute();
	}
	public class EventResult {
		public virtual bool InvalidEvent { get; set; }
	}
	public interface IActionResult {
		bool InvalidEvent { get; }
		bool Canceled { get; }
		long Cost { get; }
	} // these 2 interfaces exist to be used by the 'player turn' action.
	public interface IActionEvent {
		IActionResult Execute();
	}
	public class ActionResult : EventResult, IActionResult {
		public virtual bool Canceled { get; set; }
		//todo, xml: this value should be ignored if InvalidEvent and/or Canceled
		public virtual long Cost { get; set; } = 120; //todo, default? "1.Turn()" or anything?
	}
	public abstract class ActionEvent<TResult> : Event<TResult>, IActionEvent where TResult : ActionResult, new() {
		//note that the NoCancel bool indicates that cancellations will be ignored and
		// SHOULD not be used, but it's not a hard requirement.
		public virtual bool NoCancel { get; set; }
		public ActionEvent(GameUniverse g) : base(g) { }
		IActionResult IActionEvent.Execute() => Execute();
		protected virtual TResult Error() => new TResult(){ InvalidEvent = true, Cost = GetCost() };
		protected virtual TResult Cancel() => new TResult() { Canceled = true, Cost = GetCost() };
		protected virtual TResult Done() => new TResult(){ Cost = GetCost() }; //todo! not sure about this. Does it imply any kind of success or failure?
		public virtual bool IsInvalid => false;
		protected virtual long GetCost() => 120L; // actually 1.Turn() or Turns(1) or whatever
	}
	//todo, xml: this should return false for types it doesn't recognize
	public interface ICancelDecider {
		bool Cancels(object ev);
	}
	public abstract class CancelDecider : GameObject, ICancelDecider {
		public CancelDecider(GameUniverse g) : base(g) { }

		public virtual bool? WillCancel(object ev) => null;
		public abstract bool Cancels(object ev);
	}
	//I'm feeling like this class generally isn't the best idea. Not sure if it'll remain in the final version:
	public abstract class CancelDecider<T> : CancelDecider {
		public CancelDecider(GameUniverse g) : base(g) { }

		public override bool? WillCancel(object ev) {
			if(ev is T t) return WillCancel(t);
			else return false;
		}
		public override bool Cancels(object ev) {
			if(ev is T t) return Cancels(t);
			else return false;
		}
		public virtual bool? WillCancel(T ev) => null;
		public abstract bool Cancels(T ev);
	}
	public abstract class EasyEvent<TResult> : ActionEvent<TResult> where TResult : ActionResult, new() {
		public EasyEvent(GameUniverse g) : base(g) { }
		//todo, xml: null is fine
		public abstract ICancelDecider Decider { get; }
		//todo, xml: this happens after the validity check & cancel check
		protected abstract TResult ExecuteFinal();
		public sealed override TResult Execute() {
			if(IsInvalid) return Error();
			if(!NoCancel && Decider?.Cancels(this) == true) return Cancel();
			return ExecuteFinal();
		}
	}
	public abstract class CreatureEvent<TResult> : EasyEvent<TResult> where TResult : ActionResult, new() {
		public virtual Creature Creature { get; set; }
		public CreatureEvent(Creature creature) : base(creature.GameUniverse) { this.Creature = creature; }
		public override ICancelDecider Decider => Creature?.Decider;
		public override bool IsInvalid => Creature == null || Creature.State == CreatureState.Dead;
		public class NoEffectNotification { } //todo, note that this has the same problem as described below. This might need to be a separate non-nested class.
		//public static event Action<CreatureEvent<TResult>> NothingHappened; //todo: this was added to demonstrate how the 'nothing happens' message might work for *items*...
		//protected virtual void InvokeNothingHappened() => NothingHappened?.Invoke(this); //todo: but also note that it won't work as-is, because of the type param.
	}
	//todo, is it worth it to have some kind of boolean pass/fail actionresult built-in for cases like this? :
	public class WalkResult : ActionResult {
		public bool Succeeded;
	}
	public class WalkEvent : CreatureEvent<WalkResult> {
		public Point Destination;
		public bool IgnoreRange = false;
		public WalkEvent(Creature creature, Point destination) : base(creature) {
			this.Destination = destination;
		}
		public bool OutOfRange => !IgnoreRange && Creature.Position.ChebyshevDistanceFrom(Destination) > 1;
		//todo: IsInvalid shows the call to base.IsValid which actually checks the same thing right now:
		public override bool IsInvalid => Creature == null || base.IsInvalid; /* or destination not on map */
		protected override WalkResult ExecuteFinal() {
			if(OutOfRange || /*TerrainIsBlocking ||*/ CreatureAt(Destination) != null) {
				// todo, there would be some kind of opportunity to print a message here.
				return new WalkResult(); //return the failure
			}
			bool moved = Creatures.Move(Creature, Destination);
			if(moved) return new WalkResult { Succeeded = true };
			else return new WalkResult();
		}
	}




	//each event would probably get its own file, in another folder, eventually:

	public class WalkCancelDecider : CancelDecider<WalkEventOld> {
		public WalkCancelDecider(GameUniverse g) : base(g) { }

		public override bool? WillCancel(WalkEventOld e) => e.OutOfRange || e.TerrainIsBlocking;
		public override bool Cancels(WalkEventOld e) {
			//todo, does THIS need to check the event's args for validity?
			// what guarantees am I giving & given during this method call?
			//if(e.OutOfRange || e.TerrainIsBlocking) return true;
			if(WillCancel(e) == true) return true;

			//if any nondetermistic stuff happens, it happens here
			// - prompting player "really do that?"  (i think this would be handled with a subscription-event here somewhere?)
			// - RNG stuff

			return false;
		}
	}

	public class WalkEventOld : ActionEvent<WalkResult> {
		// (to make events easy to read, I'd probably include default values on all optional stuff, like this:)
		public Creature Creature;
		// (should some of these be readonly or properties? hmm)
		//todo, i know SOME of these need to be properties, so, it's annoying, but i guess they should all be.
		// (some'll need to be properties for interfaces like ITargetedEvent)
		public Point Destination;
		public bool IgnoreRange = false;
		public bool IgnoreBlockingTerrain = false;
		public CancelDecider<WalkEventOld> Decider = null;

		public WalkEventOld(Creature creature, Point destination) : base(creature.GameUniverse) {
			//todo: the constructor will check the basic integrity of these args.
			//For example, a Creature that isn't on the map might throw an exception here, while
			// a Creature trying to move into another creature's cell is still valid despite
			// the inevitable result of Canceled or Failed.
			// -- actually I think the ctor won't check anything. Execution might return Invalid, instead?
			// -- maybe a property or method to check basic validity?
			// -- (dead creatures can't walk, can't walk to a location outside the map, etc.)
			this.Creature = creature;
			this.Destination = destination;
		}

		// This next part seems like a good idea: properties for everything that doesn't change
		//  the game state, and methods (called CalculateFoo by convention?) for things that do.
		
		public bool TerrainIsBlocking => !IgnoreBlockingTerrain && false;// todo, actually like: Creature.CanEnter(TerrainAt(Destination));
		//todo, should eventually add a set of game-specific extensions, so this would just be DistanceFrom:
		public bool OutOfRange => !IgnoreRange && Creature.Position.ChebyshevDistanceFrom(Destination) > 1;

		public bool CalculateSlipped() {
			return false;
			// todo: actually use the RNG and return true 20% of the time unless Creature is flying or otherwise stable
		}

		public override WalkResult Execute() {
			//integrity was checked at construction - could the values or integrity have changed since then?
			//is another check needed here? (certain args are required and can't be null, Creatures must be on the map, etc.)
			//how should the overall flow of these checks happen? perhaps it's fine to create a WalkEvent with
			// totally invalid data (like missing a Creature) and it doesn't throw at that time, but has an IsValid/IsLegal bool?
			// and, i suppose, it would only throw if you actually tried to execute an invalid event?
			// -- see above: no check on construction. Probably checks here and might return 'Invalid'.
			if(!NoCancel && Decider?.Cancels(this) == true) return Cancel();

			if(OutOfRange || TerrainIsBlocking || CreatureAt(Destination) != null) {
				// todo, there would be some kind of opportunity to print a message here.
				return new WalkResult(); //return the failure
			}
			bool moved = Creatures.Move(Creature, Destination);
			if(moved) return new WalkResult{ Succeeded = true };
			else return new WalkResult();

		}
		//todo: and then they need to be serializable in here too...
	}




	public class FireballEvent : CreatureEvent<ActionResult> {

		public class ExplosionNotification {
			public FireballEvent Event;
			public int Radius;
		}
		//the int is the current radius. xml comment doesn't work here. how should i communicate this?
		//public static event Action<(FireballEvent ev, int radius)> OnExplosion;

		public Point? Target;

		public FireballEvent(Creature caster, Point? target) : base(caster) {
			this.Target = target;
		}

		protected override ActionResult ExecuteFinal() {
			if(Target == null) {
				//todo "you waste the spell"
				return Done();
			}
			for(int i = 0; i<=2; ++i) {
				//todo, animation? here's an attempt:
				Notify(new ExplosionNotification{ Event = this, Radius = i });
				//OnExplosion?.Invoke((this, i));
				foreach(Creature c in Creatures[Target.Value.EnumeratePointsAtManhattanDistance(i, true)]) {
					c.State = CreatureState.Dead;
					//todo, does anything else need to be done here?
				}
			}
			return Done();
		}
	}

	public class AiTurnEvent : SimpleEvent {
		public Creature Creature;
		public AiTurnEvent(Creature creature) : base(creature.GameUniverse) {
			this.Creature = creature;
		}

		public override void ExecuteEvent() {

			if(Creature.State == CreatureState.Dead) return;
			// todo: All this actual AI code *probably* won't go directly in the event like this.
			// It'll probably be a method on the Creature, and this event will just call it.
			foreach(Creature c in Creatures[Creature.Position.EnumeratePointsAtChebyshevDistance(1, true, false)]) {
				if(c == Player) {
					//todo, message about being fangoriously devoured
					Player.State = CreatureState.Dead;
					//todo, what else?
					return;
				}
			}
			// Otherwise, just change state:
			if(Creature.State == CreatureState.Angry) Creature.State = CreatureState.Crazy;
			else if(Creature.State == CreatureState.Crazy) Creature.State = CreatureState.Angry;

			Q.Schedule(new AiTurnEvent(Creature), 120, null); //todo, creature initiative
		}
	}

	public class PlayerTurnEvent : SimpleEvent {
		public IActionEvent ChosenAction = null;

		public class TurnStartNotification {
			public PlayerTurnEvent Event;
		}
		public class ChoosePlayerActionNotification {
			public PlayerTurnEvent Event;
		}
		//public static event Action<PlayerTurnEvent> OnTurnStarted;
		//public static event Action<PlayerTurnEvent> ChoosePlayerAction;

		public PlayerTurnEvent(GameUniverse g) : base(g) { }

		public override void ExecuteEvent() {
			Notify(new TurnStartNotification{ Event = this });
			//OnTurnStarted?.Invoke(this);
			if(Player.State == CreatureState.Dead) return;
			Notify(new ChoosePlayerActionNotification{ Event = this });
			//todo, i wonder if it would save time, or be confusing, if I had THIS form and also another form for convenience...
			//  ...maybe there's still only one going out, but from in here we can Notify(this, SimpleNotification.PlayerTurnStarted); ?
			//  seems like it would run into the naming problems like before, but it would be a bit easier otherwise.
			//ChoosePlayerAction?.Invoke(this);
			if(ChosenAction == null) {
				//todo: it *might* be necessary to create & use a DoNothing action here, if important things happen during that action.
				//todo: schedule turn for 1 turn in the future
				return;
			}

			//then check the result of the hook and make sure a valid event was chosen

			//then execute

			/*switch(choice.ChosenAction) {
				case WalkEvent e:
					//todo, probably THIS one will be used if i'm going to check whether the player is actually the actor here.
					break;
				case FireballEvent e:
					break;
			}*/
			if(ChosenAction is WalkEvent || ChosenAction is FireballEvent) {
				var result = ChosenAction.Execute(); //todo, wait, don't i need to check for cancellation here?
				if(result.InvalidEvent) {
					throw new InvalidOperationException($"Invalid event passed to player turn action [{ChosenAction.GetType().ToString()}]");
				}
				if(result.Canceled) {
					Q.ScheduleImmediately(new PlayerTurnEvent(GameUniverse));
					//todo, does this reschedule at 0, or just loop and ask again?
				}
				else {
					var time = result.Cost;
					Q.Schedule(new PlayerTurnEvent(GameUniverse), time, null); //todo, player initiative
				}
			}
			else {
				Q.Schedule(new PlayerTurnEvent(GameUniverse), 120, null); //todo, player initiative
			}
		}
	}
}
