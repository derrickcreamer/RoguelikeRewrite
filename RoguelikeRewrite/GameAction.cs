using System;
using System.Collections.Generic;
using System.Linq;

namespace RoguelikeRewrite3 {
	interface IActionTarget {
		GameAction GetDefaultBumpAction(); // or something!
		// I think this'll be useful because an object can return any action it wants, not necessarily one that refers to itself.
	}


	public enum ActionResult { Success, Failure, Cancellation };
	//maybe ActionResult is a class, not an enum, so it can have an enum type, plus optional 'action taken instead', etc...

	public class GameAction {
		public bool AllowCancel; //todo, put this in constructor or not?
		public bool UnpredictableFailure, UnpredictableCancellation; //todo, xml
		public class FailureCondition {
			public bool WasPredictable; //todo, name?
			public bool? Failure; //todo: "has value" or "value known"  ("has value" plus the actual value?)
			//possible failure message
			//todo: remember that failure messages are important here. Some actions might use or hide failure messages of their sub-actions.
			//cancel msg
			//condition!
		}
		public class CancellationCondition { // does this share a base class with FailureCondition?
			public bool? Cancellation;
			//"has value" or "value known"  ("has value" plus the actual value?)
			//condition!
			//prompt func that returns bool
		}
	}
	class MoveAction : GameAction {
		public MoveAction(bool allowCancel, int fakeParam) {
			AllowCancel = allowCancel;
			//todo, important!  Is the user allowed to change allowCancel?
			// More important than that, does the value of allowCancel matter at all *before* Perform is called?
			//  (Other than influencing the result of WillCancel, of course. That's okay.)
			//  (If it doesn't, we're good!)

			//todo: here we'd use the real arguments to determine failure, by checking for blocking terrain at the destination.
			BlockingTerrain = new FailureCondition { Failure = true, WasPredictable = true };
		}
		public FailureCondition BlockingTerrain;
		public IEnumerable<FailureCondition> FailureConditions {
			get {
				yield return BlockingTerrain; //todo, add as needed.
			}
		}
		public IEnumerable<CancellationCondition> CancellationConditions {
			get {
				return Enumerable.Empty<CancellationCondition>(); //todo, add as needed.
			}
		}
		//public bool WillFail { //todo: does this consider autocancel? This might need to be a method instead.
		public bool WillCancel {
			get {
				if(!AllowCancel) return false;
				foreach(var cc in CancellationConditions) {
					if(cc.Cancellation.HasValue && cc.Cancellation.Value) return true;
				}
				return false;
			}
		}
		//todo, what about MightCancel? where does it end?
		// well, instead of WasCancelled, just have a nullable ActionResult for final result.
		public bool WillSucceed {
			get {
				if(UnpredictableFailure || UnpredictableCancellation) return false; //todo, what to do here?
				foreach(var fc in FailureConditions) {
					if(fc.Failure.HasValue && fc.Failure.Value) return false; //todo, is this right?
				}
				if(AllowCancel) {
					foreach(var cc in CancellationConditions) {
						if(cc.Cancellation.HasValue && cc.Cancellation.Value) return false; //todo, is this right?
					}
				}
				return true;
			}
		}
		public bool PendingConfirmation {
			get {
				return false; //todo, look for unresolved prompts.
			}
		}
		//bools go here: will{succeed|fail|cancel}, pendingConfirmation, alreadySucceeded, already run?
		//what else?
		public ActionResult Perform() {
			//here we check whether cancellation is allowed. if so, look for early outs before considering prompts.
			//when prompts ARE reached, do them in the user-specified order, if any.
			return ActionResult.Cancellation;
		}
		//todo: xml note: this is equivalent to...
		public static ActionResult Perform(bool allowCancel, int fakeParam) {
			return new MoveAction(allowCancel, fakeParam).Perform();
		}
	}
}
