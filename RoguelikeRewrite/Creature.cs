using System;
using GameComponents;

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

	public enum CreatureState { Normal, Angry, Crazy, Dead };
	public class Creature : GameObject {
		public CreatureState State;
		public Creature(GameUniverse g) : base(g) {
			//
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
		/*public static ActionResult Perform(Creature creature, Point destination) {
			WalkAction.Blocked.ConfirmCancellation()
		}*/
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
		/*public static ActionResult Perform(
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

		}*/
	}
}

namespace anotherTry {
	using System.Collections.Generic;
	using testClasses;
	using UtilityCollections;
	using WalkArgs = WalkAction.Args;
	using WalkCondition = WalkAction.CancelCondition;

	public class ActionResult {
		public virtual bool Canceled { get; set; }
		public virtual int Cost { get; set; }
	}
	public class ActionResult<TCancelCondition> : ActionResult {
		public virtual TCancelCondition CancelReason { get; set; }
	}
	public interface ICancelDecider<TCancelCondition, TArgs> {
		bool IsDeterministic(TCancelCondition reason, TArgs args);
		bool ConfirmCancel(TCancelCondition reason, TArgs args);
	}
	//todo, xml stuff for this one
	// So, it's like this:
	// First, we have the automatic responses of the given default. These responses are a deterministic "yes, cancel" or
	//  "no, don't cancel", depending on the given default.
	// Second, we have the automatic responses that are exceptions to the given default. These are still automatic & deterministic
	//  responses - they're just the opposite answer from the default.
	// Third, we have fully deterministic or fully nondeterministic decisions based on args.
	//  An example of the former: "cancel throwing if success chance is under 5%. Otherwise, do not cancel."
	//  An example of the latter: "ask player 'really do that?'"
	// Fourth, we have decisions based on args that might be deterministic, or not, also based on the args.
	//  For example: "cancel automatically if success chance is under 5%. Otherwise, ask 'really do that with an X% chance of success?'"
	//
	public class CancelDecider<TCancelCondition, TArgs> : ICancelDecider<TCancelCondition, TArgs> {
		public virtual bool IsDeterministic(TCancelCondition condition, TArgs args) {
			// No confirm function? Then it's either an automatic yes or no, and therefore deterministic:
			if(!confirmCancelFuncs.ContainsKey(condition)) return true;
			// If a d. function exists, use it:
			if(isDeterministicFuncs.TryGetValue(condition, out Func<TArgs, bool> isD)) return isD(args);
			// Otherwise, if it's deterministic, it was added to the hashset:
			return confirmCancelIsDeterministic.Contains(condition);
		}
		public virtual bool ConfirmCancel(TCancelCondition condition, TArgs args) {
			// If a confirm function exists, use it:
			if(confirmCancelFuncs.TryGetValue(condition, out Func<TArgs, bool> confirm)) return confirm(args);
			// Otherwise, it's either the default response, or the opposite of the default:
			if(exceptionsToDefault.Contains(condition)) return !defaultResponse;
			else return defaultResponse;
		}
		protected bool defaultResponse = true;
		protected HashSet<TCancelCondition> exceptionsToDefault;
		protected Dictionary<TCancelCondition, Func<TArgs, bool>> confirmCancelFuncs = new Dictionary<TCancelCondition, Func<TArgs, bool>>();
		protected Dictionary<TCancelCondition, Func<TArgs, bool>> isDeterministicFuncs = new Dictionary<TCancelCondition, Func<TArgs, bool>>();
		protected HashSet<TCancelCondition> confirmCancelIsDeterministic = new HashSet<TCancelCondition>();
		//todo, xml for all this:
		public CancelDecider(bool defaultResponse = true, params TCancelCondition[] exceptionsToDefaultResponse) {
			this.defaultResponse = defaultResponse;
			exceptionsToDefault = new HashSet<TCancelCondition>(exceptionsToDefaultResponse);
		}
		public virtual CancelDecider<TCancelCondition, TArgs> RegisterDeterministic(
			TCancelCondition condition,
			Func<TArgs, bool> confirmCancel)
		{
			confirmCancelFuncs[condition] = confirmCancel;
			confirmCancelIsDeterministic.Add(condition);
			return this;
		}
		public virtual CancelDecider<TCancelCondition, TArgs> RegisterNondeterministic(
			TCancelCondition condition,
			Func<TArgs, bool> confirmCancel)
		{
			confirmCancelFuncs[condition] = confirmCancel;
			return this;
		}
		public virtual CancelDecider<TCancelCondition, TArgs> RegisterDeterministic(
			IDictionary<TCancelCondition, Func<TArgs, bool>> confirmCancelDict)
		{
			foreach(var pair in confirmCancelDict) {
				confirmCancelFuncs[pair.Key] = pair.Value;
				confirmCancelIsDeterministic.Add(pair.Key);
			}
			return this;
		}
		public virtual CancelDecider<TCancelCondition, TArgs> RegisterNondeterministic(
			IDictionary<TCancelCondition, Func<TArgs, bool>> confirmCancelDict)
		{
			foreach(var pair in confirmCancelDict) {
				confirmCancelFuncs[pair.Key] = pair.Value;
			}
			return this;
		}
		public virtual CancelDecider<TCancelCondition, TArgs> Register(
			TCancelCondition condition,
			Func<TArgs, bool> confirmCancel,
			Func<TArgs, bool> isDeterministic)
		{
			confirmCancelFuncs[condition] = confirmCancel;
			isDeterministicFuncs[condition] = isDeterministic;
			return this;
		}
		public virtual CancelDecider<TCancelCondition, TArgs> RegisterMultipleDeterministic(
			Func<TArgs, bool> confirmCancel,
			params TCancelCondition[] conditions)
		{
			foreach(var condition in conditions) {
				confirmCancelFuncs[condition] = confirmCancel;
				confirmCancelIsDeterministic.Add(condition);
			}
			return this;
		}
		public virtual CancelDecider<TCancelCondition, TArgs> RegisterMultipleNondeterministic(
			Func<TArgs, bool> confirmCancel,
			params TCancelCondition[] conditions)
		{
			foreach(var condition in conditions) {
				confirmCancelFuncs[condition] = confirmCancel;
			}
			return this;
		}
		public virtual CancelDecider<TCancelCondition, TArgs> RegisterMultiple(
			Func<TArgs, bool> confirmCancel,
			Func<TArgs, bool> isDeterministic,
			params TCancelCondition[] conditions)
		{
			foreach(var condition in conditions) {
				confirmCancelFuncs[condition] = confirmCancel;
				isDeterministicFuncs[condition] = isDeterministic;
			}
			return this;
		}
	}
	//todo, xml, note that this one is a bit hacky
	//todo, give an example of usage. (like this:)
		/*xx("TargetMissing", args => {
			var targetArgs = args as ITargetingArgs;
			if(targetArgs != null) {
				return UI.ChooseNewTarget(targetArgs, "gg");
			}
			return null;
		});*/
		//todo: could also have a TypeOverrideCancelDecider. It could, for example, catch all ITargetingArgs.
	public class NameOverrideCancelDecider<TCancelCondition, TArgs> : CancelDecider<TCancelCondition, TArgs> {
		public override bool IsDeterministic(TCancelCondition condition, TArgs args) {
			// No confirm function? Then it's either an automatic yes or no, or it gets handled based on its name:
			if(!confirmCancelFuncs.ContainsKey(condition)) {
				string conditionName = Enum.GetName(typeof(TCancelCondition), condition);
				if(nameOverrideIsDeterministicFuncs.TryGetValue(conditionName, out Func<object, bool?> maybeIsD)) {
					var maybeResult = maybeIsD(args);
					// If the name was found, see if the func returned a result:
					if(maybeResult != null) return maybeResult.Value;
				}
				// Otherwise, it must be deterministic:
				return true;
			}
			// If a confirm function does exist... if a d. function exists, use it:
			if(isDeterministicFuncs.TryGetValue(condition, out Func<TArgs, bool> isD)) return isD(args);
			// Otherwise, if it's deterministic, it was added to the hashset:
			return confirmCancelIsDeterministic.Contains(condition);
		}
		public override bool ConfirmCancel(TCancelCondition condition, TArgs args) {
			// If a confirm function exists, use it:
			if(confirmCancelFuncs.TryGetValue(condition, out Func<TArgs, bool> confirm)) return confirm(args);
			// If not, check whether this condition was listed as an exception to the default response:
			if(exceptionsToDefault.Contains(condition)) return !defaultResponse;
			// Then, check the name of the given enum value:
			string conditionName = Enum.GetName(typeof(TCancelCondition), condition);
			if(nameOverrideConfirmFuncs.TryGetValue(conditionName, out Func<object, bool?> maybeConfirm)) {
				var maybeResult = maybeConfirm(args);
				if(maybeResult != null) return maybeResult.Value;
			}
			// If none of those...
			return defaultResponse;
		}
		public NameOverrideCancelDecider(
			IDictionary<string, Func<object, bool?>> nameOverridesConfirmFuncs,
			IDictionary<string, Func<object, bool?>> nameOverridesIsDeterministicFuncs,
			bool defaultResponse = true,
			params TCancelCondition[] exceptionsToDefaultResponse)
			: base(defaultResponse, exceptionsToDefaultResponse)
		{
			this.nameOverrideConfirmFuncs = new Dictionary<string, Func<object, bool?>>(nameOverridesConfirmFuncs);
			this.nameOverrideIsDeterministicFuncs = new Dictionary<string, Func<object, bool?>>(nameOverridesIsDeterministicFuncs);
		}
		protected Dictionary<string, Func<object, bool?>> nameOverrideConfirmFuncs;
		protected Dictionary<string, Func<object, bool?>> nameOverrideIsDeterministicFuncs;
	}
	public interface ITargetingArgs {
		Point destination{ get; set; } //todo: this'll change a bit.
	}
	public static class UI { // (this is just an example class, delete it)
		public static Point GetTarget(ITargetingArgs args, string s) => new Point();
		public static bool ChooseNewTarget(ITargetingArgs args, string s) {
			args.destination = GetTarget(args, s);
			return args.destination.Equals(null);
		}
		public static bool YesOrNoPrompt(string s) => true;
	}
	public class CancelOverrides<TCancelCondition> {
		public bool NoCancel { get; protected set; }
		public bool? ResultFor(TCancelCondition reason) {
			if(results.TryGetValue(reason, out bool result)) return result;
			else return null;
		}

		protected Dictionary<TCancelCondition, bool> results = new Dictionary<TCancelCondition, bool>();
		public CancelOverrides(bool noCancel) => NoCancel = noCancel;
		//todo, xml: explain that setting values here is how you override the choice of the ICancelDecider.
		public bool? this[TCancelCondition reason] {
			get => ResultFor(reason);
			set {
				if(value == null) results.Remove(reason);
				else results[reason] = value.Value;
			}
		}
	}
	//todo: pretty sure that this should return false if no condition is
	// supplied, JUST so we can drop a new one in without any code - it'll always be false.
	public class ConditionLookup<TCancelCondition, TArgs> {
		protected Dictionary<TCancelCondition, Func<TArgs, bool>> d;
		public bool Check(TCancelCondition reason, TArgs args) => d[reason](args);
		public Func<TArgs, bool> this[TCancelCondition reason] {
			get => d[reason];
			set => d[reason] = value;
		}
	}
	public static class GameAction {
		//todo, xml
		public static HashSet<TCancelCondition> CheckAllConditions<TCancelCondition, TArgs>(
			ConditionLookup<TCancelCondition, TArgs> conditions,
			TArgs args)
		{
			// todo, why not put this method on ConditionLookup, and use its keys instead of Enum.GetValues?
			var trueConditions = new HashSet<TCancelCondition>();
			foreach(TCancelCondition reason in Enum.GetValues(typeof(TCancelCondition))) {
				if(conditions.Check(reason, args)) {
					trueConditions.Add(reason);
				}
			}
			return trueConditions;
		}
		//todo, xml note that overrides can be null.
		// (decider can be null too, yeah?)
		public static TCancelCondition? ConfirmCancel<TCancelCondition, TArgs>(
			IEnumerable<TCancelCondition> trueReasons,
			TArgs args,
			ICancelDecider<TCancelCondition, TArgs> decider,
			CancelOverrides<TCancelCondition> overrides)
			where TCancelCondition : struct
		{
			// First, check for explicitly overridden 'true' values or deterministic results:
			var result = ConfirmCancelSinglePass(trueReasons, args, decider, overrides, true);
			if(result != null) return result;
			// Then, if no result has been found, move to the nondeterministic confirmations (including the *actual* prompts to the player):
			result = ConfirmCancelSinglePass(trueReasons, args, decider, overrides, false);
			// Return result, whether null or not:
			return result;
		}
		//todo, xml, explain confirmDetOnly, and explain return type
		public static TCancelCondition? ConfirmCancelSinglePass<TCancelCondition, TArgs>(
			IEnumerable<TCancelCondition> trueReasons,
			TArgs args,
			ICancelDecider<TCancelCondition, TArgs> decider,
			CancelOverrides<TCancelCondition> overrides,
			bool confirmDeterministicOnly) //todo, consider renaming this bool?
			where TCancelCondition : struct
		{
			if(overrides != null && overrides.NoCancel) return null;
			if(decider == null) return null; // No decider means 'never cancel', I think.
			foreach(TCancelCondition reason in trueReasons) {
				switch(overrides?.ResultFor(reason)) {
					case true:
						return reason;
					case false:
						continue; // skip this reason and continue the loop
					case null:
						// If we're only considering deterministic functions, skip the others:
						if(confirmDeterministicOnly && !decider.IsDeterministic(reason, args)) continue;
						if(decider.ConfirmCancel(reason, args)) return reason;
						break;
				}
			}
			return null;
		}
	}
	public static class WalkAction {
		public class Args : ITargetingArgs {
			public Creature creature;
			public Point destination { get; set; }
			public bool ignoreSolidTerrain = false;
		}

		// todo: these might indeed become 'predictable conditions'
		public enum CancelCondition { BlockedByTerrain, BlockedByCreature, TargetMissing };

		//todo: could these rNGConditions also include basically everything, as soon as another action cares about it
		// OR as soon as it can be overridden?
		//		(but then, overriding cancels is not at all the same as overriding conditions - which I thought was done by bools on the Args?)
		//  for example, the crit or hit chance calculations could go here...?
		public enum RNGConditions { Slipped };

		public static ConditionLookup<CancelCondition, Args> Conditions = new ConditionLookup<CancelCondition, Args> {
			[CancelCondition.BlockedByCreature] = args => {
				if(true) return false;
				return true; // pretend this is actually the code that checks whether this move is blocked.
							 //return args.creature.Game.Creatures[args.destination] != null; // (or whatever)
			},
			[CancelCondition.BlockedByTerrain] = args => true,
			//return args.creature.CanPass(args.destination); // (or whatever)

			// This one could be used for a ranged action requiring a distant target:
			// (and this would be paired with a targeting prompt on the Decider)
			[CancelCondition.TargetMissing] = args => args.creature == null
		};

		public static ConditionLookup<RNGConditions, Args> RandomConditions = new ConditionLookup<RNGConditions, Args> {
			[RNGConditions.Slipped] = args => {
				//todo, args.creature.Game.RNG etc.
				return true;
			}
		};
		//todo: not every action will need one of these, but some actions might have a way to retrieve a
		//  standardized cancellation message: a dictionary of cancelCondition => Func<args, string>.
		public static ActionResult<CancelCondition> Perform(Args args, ICancelDecider<CancelCondition, Args> decider, CancelOverrides<CancelCondition> overrides = null) {
			var trueConditions = GameAction.CheckAllConditions(Conditions, args);
			var cancelReason = GameAction.ConfirmCancel(trueConditions, args, decider, overrides);
			if(cancelReason != null) return new ActionResult<CancelCondition>{ Canceled = true, CancelReason = cancelReason.Value };
			//...now perform the actual action.
			//decider.ConfirmCancel(CancelCondition.TargetMissing, args); // <-- awesome
			return new ActionResult<CancelCondition>{ Cost = 100 }; //todo: or something.
		}
	}
	public static class PushAction {
		public class Args{ }
		public enum CancelCondition{ TargetMissing, PushBlocked };
		public static ConditionLookup<CancelCondition, Args> Conditions = new ConditionLookup<CancelCondition, Args> {
		};
	}
	public static class PushWhileWalkingAction {
		public class Args : ITargetingArgs {
			public Creature creature;
			public Point destination { get; set; }
			//todo: this COULD include WalkAction.Args too! It might or might not only be used for its overrides.
		}
		//public enum CancelReason { BlockedByTerrain, BlockedByCreature, PushBlocked };

		/*public static ConditionLookup<CancelReason, Args> Conditions { get; protected set; } = new ConditionLookup<CancelReason, Args> {
			[CancelReason.BlockedByCreature] = args =>
				WalkAction.Conditions.Check(WalkAction.CancelReason.BlockedByCreature, ConvertArgsToWalk(args)),
			[CancelReason.BlockedByTerrain] = args =>
				WalkAction.Conditions.Check(WalkAction.CancelReason.BlockedByTerrain, ConvertArgsToWalk(args))
			//same deal for PushAction.Blocked
		};*/

		//protected static WalkAction.Args ConvertArgsToWalk(Args args) => null;
		
		/*public static ActionResult Perform(Args args, Decider<CancelReason, Args> decider, CancelOverrides<CancelReason> overrides) {
			var trueConditions = CheckAllConditions(Conditions, args);
			if(ConfirmCancel(trueConditions, args, decider, overrides)) return ActionResult.Cancelled;
			//...now perform the actual action.
			// and todo: big TODO: i don't have non-cancelable failures yet.
			return null;
		}*/
		public static ActionResult Perform(
			Args args,
			ICancelDecider<WalkAction.CancelCondition, WalkAction.Args> walkDecider,
			ICancelDecider<PushAction.CancelCondition, PushAction.Args> pushDecider,
			CancelOverrides<WalkAction.CancelCondition> walkOverrides = null,
			CancelOverrides<PushAction.CancelCondition> pushOverrides = null)
		{
			//
			// in this section we can totally do 2 useful things:
			//  first, if the 2 actions have overlap in their conditions, and if they both might have prompts, then we
			//    can set the 2nd instance of that condition to be ignored.
			//  second, we can override things that are specifically OK in this action, such as
			//    walking into a solid lever, if we decide that the walk should happen before the push.
			//

			WalkAction.Args walkArgs = null; // create these from 'args'.
			PushAction.Args pushArgs = null; // these too.

			var trueWalkConditions = GameAction.CheckAllConditions(WalkAction.Conditions, walkArgs);
			var truePushConditions = GameAction.CheckAllConditions(PushAction.Conditions, pushArgs);

			var walkCancelReason = GameAction.ConfirmCancelSinglePass(trueWalkConditions, walkArgs, walkDecider, walkOverrides, true);
			if(walkCancelReason != null) {
				return new ActionResult<WalkAction.CancelCondition>{ Canceled = true, CancelReason = walkCancelReason.Value };
			}
			var pushCancelReason = GameAction.ConfirmCancelSinglePass(truePushConditions, pushArgs, pushDecider, pushOverrides, true);
			if(pushCancelReason != null) {
				return new ActionResult<PushAction.CancelCondition> { Canceled = true, CancelReason = pushCancelReason.Value };
			}

			walkCancelReason = GameAction.ConfirmCancelSinglePass(trueWalkConditions, walkArgs, walkDecider, walkOverrides, false);
			if(walkCancelReason != null) {
				return new ActionResult<WalkAction.CancelCondition> { Canceled = true, CancelReason = walkCancelReason.Value };
			}
			pushCancelReason = GameAction.ConfirmCancelSinglePass(truePushConditions, pushArgs, pushDecider, pushOverrides, false);
			if(pushCancelReason != null) {
				return new ActionResult<PushAction.CancelCondition> { Canceled = true, CancelReason = pushCancelReason.Value };
			}
			//...now perform the actual action: 
			// (code goes here)
			//
			// (is this part needed? hm):
			// bool slipped = WalkAction.RandomConditions.Check(WalkAction.RNGConditions.Slipped, ConvertArgsToWalk(args));
			//
			// Finally, return a success:
			return new ActionResult { Cost = 100 }; //todo: or something.
		}
	}
}
