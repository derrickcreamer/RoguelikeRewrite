﻿using System;
using GameComponents;

namespace RoguelikeRewrite {
	public enum CreatureState { Normal, Angry, Crazy, Dead };
	public class Creature : GameObject {
		public CreatureState State;
		public Point Position => Creatures.GetPositionOf(this);
		//todo, this will probably be just a getter, switching on species:
		// (but for now i need to set the player's Decider directly)
		public CancelDecider Decider { get; set; }
		public Creature(GameUniverse g) : base(g) {
			//
		}
	}

	public class PlayerCancelDecider : CancelDecider {
		public PlayerCancelDecider(GameUniverse g) : base(g) { }

		public class DecideNotification {
			public object Action;
			public bool CancelAction;
		}
		public override bool Cancels(object ev) {
			var result = Notify(new DecideNotification{ Action = ev });
			return result.CancelAction;
		}
	}
}
