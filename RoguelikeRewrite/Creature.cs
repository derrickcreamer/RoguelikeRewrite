using System;
using Points;

namespace RoguelikeRewrite {
	public static class MoveAction2 {
		public static void Perform(Creature creature, Point destination,
			bool ignoreAllPrompts, // todo: notice that there are multiple ways this could be done, too.
			bool ignoreFirePrompt, // It could be done with boolean ignoreX parameters (which can only be disabled, never auto succeed),
			bool? firePromptResult) // or it could be done with nullable bool parameters that actually reflect the bool's state.
		{
			//predictable failures
			//	-check those files to see what I figured out WRT predictable failures, and whether there's some kind of ordering.
			//		-iirc i said that the actual execution of the action does things in whatever order it wants,
			//		 and then any calling code can do whatever...hmm.
			//			-does this mean that the calling code should NOT call Perform() if the player chooses to cancel at a prompt?
			//			 That might be one way to do it.
			//confirmation prompts
			//random failures
			//
		}
	}

	public class Creature : GameObject {
		public Creature(Game g) : base(g) {
			//
		}
		public void Walk(Point p) {
			//todo...this might be static only.
		}
	}
}

namespace todoremove {
	public class Creature{ }
	public struct Point{ }
	public class ActionResult {
		public bool Cancelled;
	}
	public class CancellableCondition<T1, T2> {
		protected Func<T1, T2, bool> verify;
		public bool VerifyCondition(T1 one, T2 two) => verify(one, two);
		public bool ConfirmCancellation(T1 one, T2 two) => Confirm(one, two); //todo, what if it's missing?
		public Func<T1, T2, bool> Confirm; // todo, maybe this could be an event instead - though that might mess up multiple instances.
	}
	public static class WalkAction {
		//perform (creature, point)
		public static readonly CancellableCondition<Creature, Point> Blocked;
		public static bool CheckBlocked(Creature creature, Point destination) {
			// look at map @ destination.
			// return false if creature can walk through that terrain.
			return true;
		}
		public static bool ConfirmWalkBlocked(Creature creature, Point destination){
			if(!CheckBlocked(creature, destination)) return true; //not blocked!
			if(BlockedHandler?.Invoke(creature, destination) == true) return true; // "yes, go ahead"
			return false;
			//yes, make this into a class. Confirm<action><problem> is pretty much going to be the same every time.
			//Maybe it takes a bool for the default return value here?
		}
		public static Func<Creature, Point, bool> BlockedHandler;
	}
	public static class PushWhileWalkingAction {
		public static ActionResult Perform(Creature creature, Point destination) {
			WalkAction.Blocked.ConfirmCancellation()
		}
	}
}

namespace testClasses {
	public class Creature{ }
	public struct Point{ }
}

namespace nextTry {
	using System.Collections.Generic;
	using testClasses;

	public class ActionResult{
		//todo, what about carrying a string as part of the result?
		public static readonly ActionResult Cancelled; //todo, spelling? field, property? etc.
		//todo, should cost always be included too?
	}
	public class Decider<TCancelReason, TArgs> { //todo, "CancelDecider"?
		public bool ConfirmCancel(TCancelReason reason, TArgs args) => false;
		public bool IsDeterministic(TCancelReason reason, TArgs args) => false;
	}
	public class CancelOverrides<TCancelReason> {
		public bool NoCancel_TodoForAll;
		public bool NoCancel_TodoForOne(TCancelReason reason) => noCancelReasons.Contains(reason);
		//todo:  errr, actually, should this be a bool, or should it be a nullable bool to represent the predetermined answers?
		// could be a dictionary< reason, bool?> with a default value or something.
		private HashSet<TCancelReason> noCancelReasons;
		//todo constructor
		//copy constructor, plus one with a bool and params reasons?
	}
	public class ConditionLookup<TCancelReason, TArgs> {
		private Dictionary<TCancelReason, Func<TArgs, bool>> d;
		public bool Check(TCancelReason reason, TArgs args) => d[reason](args);
		public Func<TArgs, bool> this[TCancelReason reason] {
			get => d[reason];
			set => d[reason] = value;
		}
	}
	public abstract class GameAction {
		public static HashSet<TCancelReason> CheckAllConditions<TCancelReason, TArgs>(
			ConditionLookup<TCancelReason, TArgs> conditions, 
			TArgs args)
		{
			var trueConditions = new HashSet<TCancelReason>();
			foreach(TCancelReason reason in Enum.GetValues(typeof(TCancelReason))) {
				if(conditions.Check(reason, args)) {
					trueConditions.Add(reason);
				}
			}
			return trueConditions;
		}
		public static bool ConfirmCancel<TCancelReason, TArgs>(
			IEnumerable<TCancelReason> trueReasons,
			TArgs args,
			Decider<TCancelReason, TArgs> decider,
			CancelOverrides<TCancelReason> overrides)
		{
			if(overrides.NoCancel_TodoForAll) return false;
			foreach(TCancelReason reason in trueReasons) {
				if(overrides.NoCancel_TodoForOne(reason)) continue;
				if(decider.IsDeterministic(reason, args) && decider.ConfirmCancel(reason, args)) return true;
			}
			foreach(TCancelReason reason in trueReasons) {
				if(overrides.NoCancel_TodoForOne(reason)) continue;
				if(decider.ConfirmCancel(reason, args)) return true;
			}
			return false;
		}
	}
	public abstract class WalkAction : GameAction {
		public class Args {
			public Creature creature;
			public Point destination;
		}

		// todo: these might indeed become 'predictable conditions'
		public enum CancelReason { BlockedByTerrain, BlockedByCreature, TargetMissing };

		public enum RNGConditions { Slipped };

		public static ConditionLookup<CancelReason, Args> Conditions { get; protected set; } = new ConditionLookup<CancelReason, Args> {
			[CancelReason.BlockedByCreature] = args => {
				if(true) return false;
				return true; // pretend this is actually the code that checks whether this move is blocked.
							 //return args.creature.Game.Creatures[args.destination] != null; // (or whatever)
			},
			[CancelReason.BlockedByTerrain] = args => true,
			//return args.creature.CanPass(args.destination); // (or whatever)
			
			// This one could be used for a ranged action requiring a distant target:
			// (and this would be paired with a targeting prompt on the Decider)
			[CancelReason.TargetMissing] = args => args.creature == null
		};

		public static ConditionLookup<RNGConditions, Args> RandomConditions { get; set; } = new ConditionLookup<RNGConditions, Args> {
			[RNGConditions.Slipped] = args => {
				//todo, args.creature.Game.RNG etc.
				return true;
			}
		};
		public static ActionResult Perform(Args args, Decider<CancelReason, Args> decider, CancelOverrides<CancelReason> overrides) {
			var trueConditions = CheckAllConditions(Conditions, args);
			if(ConfirmCancel(trueConditions, args, decider, overrides)) return ActionResult.Cancelled;
			//...now perform the actual action.
			decider.ConfirmCancel(CancelReason.TargetMissing, args); // <-- awesome
			return null;
		}
	}
	public abstract class PushWhileWalkingAction : GameAction {
		public class Args {
			public Creature creature;
			public Point destination;
			//todo: this COULD include WalkAction.Args too! It might or might not only be used for its overrides.
		}
		//public enum CancelReason { BlockedByTerrain, BlockedByCreature, PushBlocked };

		//todo note!  is it easy to create a Decider for PushWhileWalking? Can the functions from Push and Walk be reused easily?
		// Is it bad that I don't have any way to actually say "USE THE DECIDER FOR WALKACTION!!" or "PASS IN A DECIDER FOR THESE ACTIONS" ?
		// But then, I have control over Perform, don't I?
		// I can add or replace deciders if I want. Maybe this action doesn't even HAVE conditions or cancel reasons of its own.

		/*public static ConditionLookup<CancelReason, Args> Conditions { get; protected set; } = new ConditionLookup<CancelReason, Args> {
			[CancelReason.BlockedByCreature] = args =>
				WalkAction.Conditions.Check(WalkAction.CancelReason.BlockedByCreature, ConvertArgsToWalk(args)),
			[CancelReason.BlockedByTerrain] = args =>
				WalkAction.Conditions.Check(WalkAction.CancelReason.BlockedByTerrain, ConvertArgsToWalk(args))
			//same deal for PushAction.Blocked
		};*/
		protected static WalkAction.Args ConvertArgsToWalk(Args args) => null;
		/*public static ActionResult Perform(Args args, Decider<CancelReason, Args> decider, CancelOverrides<CancelReason> overrides) {
			var trueConditions = CheckAllConditions(Conditions, args);
			if(ConfirmCancel(trueConditions, args, decider, overrides)) return ActionResult.Cancelled;
			//...now perform the actual action.
			// and todo: big TODO: i don't have non-cancelable failures yet.
			return null;
		}*/
		public static ActionResult Perform(
			Args args,
			Decider<WalkAction.CancelReason, WalkAction.Args> walkDecider,
			Decider<PushAction.CancelReason, PushAction.Args> pushDecider,
			CancelOverrides<WalkAction.CancelReason> walkOverrides,
			CancelOverrides<PushAction.CancelReason> pushOverrides)
		{
			var trueWalkConditions = CheckAllConditions(WalkAction.Conditions, null);
			var truePushConditions = CheckAllConditions(PushAction.Conditions, null);
			if(ConfirmCancel) // so okay, this one will need a "deterministic only" version to let me do what i want here.
				{

			}
			bool slipped = WalkAction.RandomConditions.Check(WalkAction.RNGConditions.Slipped, ConvertArgsToWalk(args));
			// hmm, will all of these ^ be checked explicitly?

		}
	}
}

namespace anotherTry {
	using System.Collections.Generic;
	using testClasses;

	public class ActionResult {
		public virtual bool Canceled { get; set; }
		public virtual string Message { get; set; }
		public virtual int Cost { get; set; }
	}
	public interface ICancelDecider<TCancelReason, TArgs> {
		bool IsDeterministic(TCancelReason reason, TArgs args);
		bool ConfirmCancel(TCancelReason reason, TArgs args);
	}
	public class X : ICancelDecider<WalkAction.CancelReason, WalkAction.Args>,
		ICancelDecider<int, PushWhileWalkingAction.Args> {

		Dictionary<WalkAction.CancelReason, Func<WalkAction.Args, bool>> walkPrompts;
		Dictionary<int, Func<PushWhileWalkingAction.Args, bool>> pushWalkPrompts;
		void init() {
			walkPrompts[WalkAction.CancelReason.BlockedByCreature] = args => {
				args.target = UI.ChooseTarget(args.creature);
				return args.target == null;
			};
		}
		// hmm...possible options...
		// well, i can't share any (args) => (bool) functions unless the args are the same type.
		// I could try inheritance, but that would probably fall apart with complex matches.
		// I could try interfaces for the args, but the UI hooks are the ones that care what type it is.
		// anyway, i guess the place to start is to figure out WHERE these hooks would be created.
		//	-would they be on the same object or not?
		//
		// just what data does ChooseTarget need, anyway?

		public bool ConfirmCancel(int reason, PushWhileWalkingAction.Args args) {
			throw new NotImplementedException();
		}

		public bool ConfirmCancel(WalkAction.CancelReason reason, WalkAction.Args args) {
			throw new NotImplementedException();
		}

		public bool IsDeterministic(int reason, PushWhileWalkingAction.Args args) {
			throw new NotImplementedException();
		}

		public bool IsDeterministic(WalkAction.CancelReason reason, WalkAction.Args args) {
			throw new NotImplementedException();
		}
	}
	/*public class Decider<TCancelReason, TArgs> { //todo, "CancelDecider"?
		public bool ConfirmCancel(TCancelReason reason, TArgs args) => false;
		public bool IsDeterministic(TCancelReason reason, TArgs args) => false;
		//should the constructor take a bool for whether it defaults to yes or no, when nothing has been provided for a certain reason?
	}*/
	public class CancelOverrides<TCancelReason> {
		public bool NoCancel { get; protected set; }
		public bool? ResultFor(TCancelReason reason) {
			if(results.TryGetValue(reason, out bool result)) return result;
			else return null;
		}

		protected Dictionary<TCancelReason, bool> results = new Dictionary<TCancelReason, bool>();
		public CancelOverrides(bool noCancel) => NoCancel = noCancel;
		//todo, xml: explain that setting values here is how you override the choice of the ICancelDecider.
		public bool? this[TCancelReason reason] {
			get => ResultFor(reason);
			set {
				if(value == null) results.Remove(reason);
				else results[reason] = value.Value;
			}
		}
	}
	public class ConditionLookup<TCancelReason, TArgs> {
		protected Dictionary<TCancelReason, Func<TArgs, bool>> d;
		public bool Check(TCancelReason reason, TArgs args) => d[reason](args);
		public Func<TArgs, bool> this[TCancelReason reason] {
			get => d[reason];
			set => d[reason] = value;
		}
	}
	public static class GameAction {
		//todo, xml
		public static HashSet<TCancelReason> CheckAllConditions<TCancelReason, TArgs>(
			ConditionLookup<TCancelReason, TArgs> conditions,
			TArgs args)
		{
			var trueConditions = new HashSet<TCancelReason>();
			foreach(TCancelReason reason in Enum.GetValues(typeof(TCancelReason))) {
				if(conditions.Check(reason, args)) {
					trueConditions.Add(reason);
				}
			}
			return trueConditions;
		}
		//todo, xml note that overrides can be null.
		//todo, could the decider be null too? Maybe that should mean 'never cancel'.
		public static bool ConfirmCancel<TCancelReason, TArgs>(
			IEnumerable<TCancelReason> trueReasons,
			TArgs args,
			ICancelDecider<TCancelReason, TArgs> decider,
			CancelOverrides<TCancelReason> overrides)
		{
			//TODO, fix this so it works with null 'overrides'
			if(overrides.NoCancel) return false;
			// First, check for explicitly overridden 'true' values or deterministic results:
			foreach(TCancelReason reason in trueReasons) {
				switch(overrides.ResultFor(reason)) {
					case true:
						return true;
					case false:
						continue; // skip this reason and continue the loop
					case null:
						if(decider.IsDeterministic(reason, args) && decider.ConfirmCancel(reason, args)) return true;
						break;
				}
			}
			// Then, if no result has been found, move to the nondeterministic confirmations (including the *actual* prompts to the player):
			foreach(TCancelReason reason in trueReasons) {
				if(overrides.ResultFor(reason) == null && decider.ConfirmCancel(reason, args)) return true;
			}
			return false;
		}
	}
	public static class WalkAction {
		public class Args {
			public Creature creature;
			public Point destination;
		}

		// todo: these might indeed become 'predictable conditions'
		public enum CancelReason { BlockedByTerrain, BlockedByCreature, TargetMissing };

		public enum RNGConditions { Slipped };

		public static ConditionLookup<CancelReason, Args> Conditions = new ConditionLookup<CancelReason, Args> {
			[CancelReason.BlockedByCreature] = args => {
				if(true) return false;
				return true; // pretend this is actually the code that checks whether this move is blocked.
							 //return args.creature.Game.Creatures[args.destination] != null; // (or whatever)
			},
			[CancelReason.BlockedByTerrain] = args => true,
			//return args.creature.CanPass(args.destination); // (or whatever)

			// This one could be used for a ranged action requiring a distant target:
			// (and this would be paired with a targeting prompt on the Decider)
			[CancelReason.TargetMissing] = args => args.creature == null
		};

		public static ConditionLookup<RNGConditions, Args> RandomConditions = new ConditionLookup<RNGConditions, Args> {
			[RNGConditions.Slipped] = args => {
				//todo, args.creature.Game.RNG etc.
				return true;
			}
		};
		public static ActionResult Perform(Args args, ICancelDecider<CancelReason, Args> decider, CancelOverrides<CancelReason> overrides = null) {
			var trueConditions = GameAction.CheckAllConditions(Conditions, args);
			if(GameAction.ConfirmCancel(trueConditions, args, decider, overrides)) return new ActionResult(){ Canceled = true };
			//...now perform the actual action.
			decider.ConfirmCancel(CancelReason.TargetMissing, args); // <-- awesome
			return null;
		}
	}
	public abstract class PushWhileWalkingAction {
		public class Args {
			public Creature creature;
			public Point destination;
			//todo: this COULD include WalkAction.Args too! It might or might not only be used for its overrides.
		}
		//public enum CancelReason { BlockedByTerrain, BlockedByCreature, PushBlocked };

		//todo note!  is it easy to create a Decider for PushWhileWalking? Can the functions from Push and Walk be reused easily?
		// Is it bad that I don't have any way to actually say "USE THE DECIDER FOR WALKACTION!!" or "PASS IN A DECIDER FOR THESE ACTIONS" ?
		// But then, I have control over Perform, don't I?
		// I can add or replace deciders if I want. Maybe this action doesn't even HAVE conditions or cancel reasons of its own.

		/*public static ConditionLookup<CancelReason, Args> Conditions { get; protected set; } = new ConditionLookup<CancelReason, Args> {
			[CancelReason.BlockedByCreature] = args =>
				WalkAction.Conditions.Check(WalkAction.CancelReason.BlockedByCreature, ConvertArgsToWalk(args)),
			[CancelReason.BlockedByTerrain] = args =>
				WalkAction.Conditions.Check(WalkAction.CancelReason.BlockedByTerrain, ConvertArgsToWalk(args))
			//same deal for PushAction.Blocked
		};*/
		protected static WalkAction.Args ConvertArgsToWalk(Args args) => null;
		/*public static ActionResult Perform(Args args, Decider<CancelReason, Args> decider, CancelOverrides<CancelReason> overrides) {
			var trueConditions = CheckAllConditions(Conditions, args);
			if(ConfirmCancel(trueConditions, args, decider, overrides)) return ActionResult.Cancelled;
			//...now perform the actual action.
			// and todo: big TODO: i don't have non-cancelable failures yet.
			return null;
		}*/
		public static ActionResult Perform(
			Args args,
			ICancelDecider<WalkAction.CancelReason, WalkAction.Args> walkDecider,
			ICancelDecider<PushAction.CancelReason, PushAction.Args> pushDecider,
			CancelOverrides<WalkAction.CancelReason> walkOverrides,
			CancelOverrides<PushAction.CancelReason> pushOverrides) {
			var trueWalkConditions = CheckAllConditions(WalkAction.Conditions, null);
			var truePushConditions = CheckAllConditions(PushAction.Conditions, null);
			if(GameAction.ConfirmCancel) // so okay, this one will need a "deterministic only" version to let me do what i want here.
				{

			}
			bool slipped = WalkAction.RandomConditions.Check(WalkAction.RNGConditions.Slipped, ConvertArgsToWalk(args));
			// hmm, will all of these ^ be checked explicitly?

		}
	}
}
