using System;
using System.Collections.Generic;
using RoguelikeRewrite;

namespace GameActionsTodo {
	public class FailureState {
		public bool Known => false;
		public bool KnownTrue => false;
		public bool KnownFalse => false;
		public bool CalculateValue() {
			if(value == null) value = calculate();
			return value.Value;
		}
		public bool RecalculateValue() {
			value = calculate();
			return value.Value;
		}
		public void SetValue(bool value) => this.value = value;
		protected bool? value;
		protected Func<bool> calculate;
	}
	public class AttackAction {
		/*public MutableFailureState AllyCancelled { get; protected set; }
		public MutableFailureState AcidCancelled { get; protected set; }
		public MutableFailureState OutOfRange { get; protected set; }
		public FailureState WeaponNotSwung { get; protected set; }
		public MutableFailureState AttackMissed { get; protected set; }
		public MutableFailureState NoCrit { get; protected set; }*/
	}
}
