using System;
using System.Collections.Generic;

using RoguelikeRewrite; //todo remove

namespace RoguelikeRewrite2 {
	public class Actor { }
	public static class TodoThisIsJustAStandIn { // the UI registers stuff here
		public static bool CheckAllyCancelPrompt() => false;
		public static bool CheckAcidCancelPrompt() => false;
	}
	public abstract class GameAction {
		public MutableFailureState Cancelled { get; protected set; } = new MutableFailureState(false); //todo: choose a convention for failure names.
		public virtual void Perform() { }
		//todo: i suspect that this might need a bool Performed, or something. Needs to know if it was called. (but the only way to do that is to add another method...)
	}
	public class FailureState {
		protected bool? value; //todo: Should this have a read-only way to check the value?
		protected IList<FailureState> nestedFailures; //todo, name change
		protected bool predictable;
		protected bool ignored;
		protected Func<bool> calculate;
		protected bool KnownTrue { //todo, public or protected? what about a Known property? would that Known property consider nested ones?
			get {
				if(value == true) return true;
				foreach(var f in nestedFailures) {
					if(f.KnownTrue) return true;
				}
				return false;
			}
		}
		public static implicit operator bool(FailureState f) {
			if(f.ignored) return false;
			if(f.KnownTrue) return true;
			if(!f.value.HasValue) { // The goal here is to call calculate as seldom as possible.
				f.value = f.calculate(); //todo: Probably turn this into a property instead of doing it separately more than once. also, check for null? return false if null?
				if(f.value == true) return true; // todo: Definitely consider two separate bools: Known and Value. (but then, this already works as a bool for value...)
			}
			foreach(var nested in f.nestedFailures) {
				if(nested) { // Call this operator on the next in line...
					return true;
				}
			}
			return false;
		}
		public FailureState(bool predictable, params FailureState[] nestedFailures) {
			//todo
		}
		//todo: xml note: be very explicit about what predictable means:
		// ( "Could this failure state's result be (perfectly) predicted before calling this action's constructor?" )
		public FailureState(bool predictable, Func<bool> calculate, params FailureState[] nestedFailures) {
			this.predictable = predictable;
			this.calculate = calculate;
			this.nestedFailures = nestedFailures;
		}
		public bool FailureIsPredictable {
			get {
				if(predictable && value == true) return true; //todo: this needs to retrieve the final value, doesn't it?
				foreach(var todoName in nestedFailures) {
					if(todoName.FailureIsPredictable) return true;
				}
				return false;
			}
		}
	}
	//todo: new type of failurestate here:  the kind that needs RNG and isn't known until actually performed, BUT can be assigned to.
	public class MutableFailureState : FailureState {
		public MutableFailureState(bool predictable, params FailureState[] nestedFailures) : base(predictable, nestedFailures) {

		}
		public MutableFailureState(bool predictable, Func<bool> calculate, params FailureState[] nestedFailures)
			: base(predictable, calculate, nestedFailures) {
		}
		public bool? Value { //todo, name?
			get { return value; } //todo, actually, how about replacing this one with a bool Known? could then test the value directly.
			set { this.value = value; }
		}
		public bool Ignored {
			get { return ignored; }
			set { ignored = value; }
		}
		public void Calculate() {
			//TODO: make sure the purpose & use of this method is known before implementing it.
			//todo, check nested failures FIRST, right?
			value = calculate(); // todo, null check?
		}
	}
	public class AttackResult {
		public FailureState NoKill { get; set; }
	}
	public class AttackAction : GameAction {
		public MutableFailureState AllyCancelled { get; protected set; }
		public MutableFailureState AcidCancelled { get; protected set; }
		public MutableFailureState OutOfRange { get; protected set; }
		public FailureState WeaponNotSwung { get; protected set; }
		public MutableFailureState AttackMissed { get; protected set; }
		public MutableFailureState NoCrit { get; protected set; }

		//xml note: this'll be null until Perform() is called.
		public AttackResult Result { get; protected set; }

		public readonly Actor Source, Target; //todo, this'll need an update if more args or overloads are added.
		public AttackAction(Actor source, Actor target) {
			Source = source;
			Target = target;
			AllyCancelled = new MutableFailureState(false, TodoThisIsJustAStandIn.CheckAllyCancelPrompt);
			AcidCancelled = new MutableFailureState(false, TodoThisIsJustAStandIn.CheckAcidCancelPrompt);
			// todo: Should I also show an example where I have only a single prompt, and I make it part of Cancelled instead of adding a new one?
			//Cancelled = new FailureState(false, AllyCancelled, AcidCancelled);

			//pretend this lambda checks weapon range of source:
			OutOfRange = new MutableFailureState(true, () => false, Cancelled);
			//pretend this lambda checks whether source has the Pacifism status:
			WeaponNotSwung = new FailureState(true, () => false, Cancelled);
			//pretend this lambda checks source hit% and target dodge%:
			AttackMissed = new MutableFailureState(false, () => false, WeaponNotSwung, OutOfRange);
			//pretend this lambda just does a 1 in X chance to crit:
			NoCrit = new MutableFailureState(false, () => false, AttackMissed);
			//
			//todo: a question: At first, I pictured the values of these FailureStates being populated WITHIN this constructor.
			//					Is that not going to happen? Is it *really* never necessary? Can anything not be done lazily like that?
		}
		public override void Perform() {
			if(Cancelled) return;
			if(AttackMissed) {
				// handle miss. print message. etc.
			}
			else {
				int dmg = NoCrit? 4 : 8;
				//deal damage.
				Result = new AttackResult {
					//pretend this lambda checks target HP or whatever.
					NoKill = new FailureState(false, () => false, AttackMissed)
				};
			}
		}
	}
	public class MoveAction : GameAction {
		public MutableFailureState HazardCancelled { get; protected set; }
		public MutableFailureState BlockingTerrain { get; protected set; }
		public FailureState ActorAlreadyPresent { get; protected set; }
		public FailureState FailureToMove { get; protected set; }
		public MoveAction(Actor source, int destX, int destY) {
			//...
		}
		public override void Perform() {
			if(Cancelled) return;
			if(ActorAlreadyPresent) {
				//there's a foo in the way
			}
			if(BlockingTerrain) {
				//there's a bar in the way
			}
			//actually move here.
		}
	}
	public class ConfusedMoveAction : GameAction {
		public MoveAction MoveAction { get; protected set; }
		public ConfusedMoveAction(Actor source) {
			int x = 0, y = 0; // pretend these are set to a position in a random direction from 'source'.
			MoveAction = new MoveAction(source, x, y);
			MoveAction.Cancelled.Value = false; // This line just prevents a curious caller from getting a prompt.
			// Not sure if MoveAction's *other* cancellations should go here too. Might as well.
		}
		public override void Perform() {
			if(Cancelled) return; // *this* action might be cancelled with a "Risk stumbling into lava?" prompt...
			MoveAction.Cancelled.Value = false; // ...but after that point, the MoveAction won't ask for any confirmation.
			MoveAction.Perform();
		}
	}
	public class LungeAttackAction : GameAction {
		public MoveAction MoveAction { get; protected set; }
		public AttackAction AttackAction { get; protected set; }
		public LungeAttackAction(Actor source, Actor target, int destX, int destY) {
			MoveAction = new MoveAction(source, destX, destY);
			AttackAction = new AttackAction(source, target);
		}
		public override void Perform() {
			if(MoveAction.Cancelled || MoveAction.FailureToMove.FailureIsPredictable
				|| AttackAction.Cancelled || AttackAction.AttackMissed.FailureIsPredictable) {
				return;
			}
		}
	}
}
