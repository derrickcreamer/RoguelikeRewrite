using System;
using System.Collections.Generic;

namespace RoguelikeRewrite {
	public enum AttrType { };
	public enum EventType { EveryTurnUpdate, Move, RemoveAttr, RemoveGas, RestoreNormalLighting, SpawnWanderingMonster, SpawnCultists, 
		CheckRelativelySafe, Burrowing, RegeneratingFromDeath, Reassembling, Poltergeist, Grenade, BlastFungus, 
		FireGeyserTimer, FireGeyserEruption, Stalagmite, Breach, ShieldingZone
	};
	public class GameEvent {
		public readonly EventType type;
		public readonly int timeCreated;
		public readonly int delay;
		public readonly int tiebreaker;
		public int executionTime => timeCreated + delay;
		public bool dead; //make it so you can set it to dead, but not back again?
		public PhysicalObject target;
		public List<PhysicalObject> area;
		public int value;
		public int secondaryValue;
		public string msg;
		private void TargetRemoved(PhysicalObject o) { if(o == target) dead = true; }
		private void AreaRemoved(PhysicalObject o) {
			if(area.Remove(o as Tile)) { }
		}
		public GameEvent() {
			target.onRemoval += TargetRemoved;
		} //tiebreaker for constructors???
		public GameEvent(EventType type, int delay, PhysicalObject target = null, List<Tile> area = null, int value = 0, int secondaryValue = 0) {
			this.type = type;
		}
		public GameEvent(AttrType attr, int delay, PhysicalObject target, int value = 1, int secondaryValue = 0, string msg = null) {

		}
		public void Execute() {
			//unsub from target & area stuff here
		}
	}
}
