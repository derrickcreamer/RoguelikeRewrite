using System;
using GameComponents;

namespace RoguelikeRewrite {
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
		//public bool Canceled;
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
			this.Creature = creature;
			this.Destination = destination;
		}

		// This next part seems like a good idea: properties for everything that doesn't change
		//  the game state, and methods (called CalculateFoo by convention?) for things that do.
		
		//todo: should TerrainIsBlocking consider IgnoreBlockingTerrain or not?
		// sounds like a tough question.
		public bool TerrainIsBlocking => !IgnoreBlockingTerrain && false;// todo, actually like: Creature.CanEnter(TerrainAt(Destination));
		public bool OutOfRange => !IgnoreRange && false; //todo: Creature.Position.DistanceFrom(Destination) > 1;

		public bool CalculateSlipped() {
			return false;
			// todo: actually use the RNG and return true 20% of the time unless Creature is flying or otherwise stable
		}

		public override WalkResult Execute() {
			//integrity was checked at construction - could the values or integrity have changed since then?
			//is another check needed here? (certain args are required and can't be null, Creatures must be on the map, etc.)
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
		public Creature Caster;
		public Point Target;
		public ICancelDecider<FireballEvent> Decider = null;

		public FireballEvent(Creature caster, Point target) : base(caster.GameUniverse) {
			this.Caster = caster;
			this.Target = target;
		}

		public override FireballResult Execute() {
			if(!NoCancel && Decider?.Cancels(this) == true) return Cancel();

			return new FireballResult();
		}
	}
}
