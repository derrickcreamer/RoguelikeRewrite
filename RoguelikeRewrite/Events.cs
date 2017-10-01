using System;
using GameComponents;
using GameComponents.DirectionUtility;

namespace RoguelikeRewrite {
	//todo, what about some kind of TimedEvent / ITimedEvent here, from which we can get a total time spent?
	public abstract class Event : GameObject, IEvent {
		public Event(GameUniverse g) : base(g) { }
		void IEvent.ExecuteEvent() { Execute(); }
		public abstract void Execute();
	}
	public abstract class Event<TResult> : GameObject, IEvent {
		public Event(GameUniverse g) : base(g) { }
		void IEvent.ExecuteEvent() { Execute(); }
		public abstract TResult Execute();
	}
	public abstract class CancelableEvent<TResult> : Event<TResult> where TResult : CancelableResult, new() {
		public CancelableEvent(GameUniverse g) : base(g) { }
		//note that the NoCancel bool indicates that cancellations will be ignored and
		// SHOULD not be used, but it's not a hard requirement.
		public virtual bool NoCancel { get; set; }
		public virtual TResult Cancel() => new TResult(){ Canceled = true };
	}
	public class CancelableResult {
		public virtual bool Canceled { get; set; }
	}
	public interface ICancelDecider<TEvent> {
		bool Cancels(TEvent e);
	}
	public abstract class CancelDecider<TEvent> : ICancelDecider<TEvent> {
		public virtual bool? WillCancel(TEvent e) => null;
		public abstract bool Cancels(TEvent e);
	}




	//each event would probably get its own file, in another folder, eventually:

	public class WalkCancelDecider : CancelDecider<WalkEvent> {
		public override bool? WillCancel(WalkEvent e) => e.OutOfRange || e.TerrainIsBlocking;
		public override bool Cancels(WalkEvent e) {
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

	public class WalkResult : CancelableResult {
		public bool Succeeded;
	}
	public class WalkEvent : CancelableEvent<WalkResult> {
		// (to make events easy to read, I'd probably include default values on all optional stuff, like this:)
		public Creature Creature;
		// (should some of these be readonly or properties? hmm)
		//todo, i know SOME of these need to be properties, so, it's annoying, but i guess they should all be.
		// (some'll need to be properties for interfaces like ITargetedEvent)
		public Point Destination;
		public bool IgnoreRange = false;
		public bool IgnoreBlockingTerrain = false;
		public ICancelDecider<WalkEvent> Decider = null;

		public WalkEvent(Creature creature, Point destination) : base(creature.GameUniverse) {
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




	public class FireballResult : CancelableResult {
		public bool Succeeded;
	}
	public class FireballEvent : CancelableEvent<FireballResult> {

		public static event Action<Point, int> OnExplosion;

		public Creature Caster;
		public Point Target;
		public ICancelDecider<FireballEvent> Decider = null;

		public FireballEvent(Creature caster, Point target) : base(caster.GameUniverse) {
			this.Caster = caster;
			this.Target = target;
		}

		public override FireballResult Execute() {
			if(!NoCancel && Decider?.Cancels(this) == true) return Cancel();

			for(int i = 0; i<=2; ++i) {
				//todo, animation? here's an attempt:
				OnExplosion?.Invoke(Target, i);
				foreach(Creature c in Creatures[Target.EnumeratePointsAtManhattanDistance(i, true)]) {
					c.State = CreatureState.Dead;
					//todo, does anything else need to be done here?
				}
			}

			//todo, any need for a built in convenient 'Success' method?
			return new FireballResult();
		}
	}

	public class AiTurnEvent : Event {
		public Creature Creature;
		public AiTurnEvent(Creature creature) : base(creature.GameUniverse) {
			this.Creature = creature;
		}

		public override void Execute() {
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

	public class PlayerActionChoice { //todo, needs a better name i think
		public IEvent ChosenAction;
	}

	public class PlayerTurnEvent : Event {

		public static event Action TurnStarted; //todo, 'OnTurnStarted'? what's the convention here?
		public static event Action<PlayerActionChoice> ChoosePlayerAction;
		//todo: hmm, this one doesn't really NEED to take a creature, if it only affects the player.
		public PlayerTurnEvent(GameUniverse g) : base(g) { }

		public override void Execute() {
			//now what does this one actually do?
			// IIRC, it needs to call a hook that'll determine what action to take?
			// and then either take that action or just do nothing.

			// so first it'll check anything that needs checking...if there is anything. not sure.

			//next is the hook

			TurnStarted?.Invoke();

			PlayerActionChoice choice = new PlayerActionChoice();

			ChoosePlayerAction?.Invoke(choice);

			if(choice.ChosenAction == null) {
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
			if(choice.ChosenAction is WalkEvent || choice.ChosenAction is FireballEvent) {
				choice.ChosenAction.ExecuteEvent();
			}

			Q.Schedule(new PlayerTurnEvent(GameUniverse), 120, null); //todo, player initiative

		}
	}
}
