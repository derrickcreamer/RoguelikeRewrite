using System;
using System.Collections.Generic;
using UtilityCollections;

namespace RoguelikeRewrite {
	public class GameEvent {
		public int executionTime;
		public void Execute() { }
	}
	public class GameEventQueue {
		private PriorityQueue<GameEvent, int> pq = new PriorityQueue<GameEvent, int>(e => e.executionTime);
		public GameEvent CurrentEvent = null;
		public void ExecuteNextEvent() {
			CurrentEvent = pq.Peek();
			int turn; //todo
			turn = CurrentEvent.executionTime;
			//todo: null cached status, cached lighting?
			CurrentEvent.Execute();
			pq.Dequeue();
			//todo: cleanup here. remove dead stuff, etc.
		}
	}
}

namespace AnotherGameEventTry {
	public abstract class GameEventResult { }
	public class EmptyResult : GameEventResult { }
	public abstract class GameEvent {
		public abstract GameEventResult Do();
	}

	public class MonsterSpawnEventResult : GameEventResult { }
	public class MonsterSpawnEvent : GameEvent {
		public override GameEventResult Do() {
			return new EmptyResult();
			return new MonsterSpawnEventResult();
		}
	}
}

/*namespace YetAnother {
	public class EventResult { }
	public abstract class GameEvent {
		public abstract EventResult Do();
	}

	public class MonsterSpawnEventResult : GameEventResult { }
	public class MonsterSpawnEvent : GameEvent {
		public override GameEventResult Do() {
			return new EmptyResult();
			return new MonsterSpawnEventResult();
		}
	}
}*/
namespace YA2 {
	public class EvResult{ }
	public abstract class Event {
		public abstract void Do();
	}
	public class Event<T> : Event {
		public override void Do(){ DoOther(); }
		public virtual T DoOther(){ return default(T); }
	}
	public static class Doer {
		public static void Do(Event e) {
			e.Do();
		}
	}
}
namespace YA3 {
	public class EventScheduling { //todo, name?
		IEvent scheduledEvent;
		public int delay, executionTick; //todo
		//init
		//todo, timing stuff here, right?
		//todo, will alreadyexecuted/isdead be moved here?
	}
	public class GameQueue {
		List<EventScheduling> stuff;
	}
	public interface IEvent {
		void Do();
		//int Tiebreaker{ get; } // TiebreakOrder?
	}
	public abstract class Event<T> : IEvent {
		public bool AlreadyExecuted => false; //todo, will these 2(?) be moved to EventScheduling?

		public bool IsDead => false;

		public void Do() { DoOther(); } //todo, i think this works, but these obviously need renaming.
		public abstract T DoOther();
	}
	public static class Doer {
		public static void Do(IEvent e) {
			e.Do();
		}
	}
	public class SpawnArgs{ }
	public class SpawnResult{ }
	public class SpawnEvent : Event<SpawnResult> {
		private SpawnArgs args;
		public SpawnArgs Args => args;
		public SpawnEvent(SpawnArgs args) {
			//if(args.Target != null) args.Target.OnDeath += x => dead = true;
			//if(args.Destination != null) args.Destination.OnDestruction += x => {
			//  args.Destination = FindNewDestination();
			//};
			//etc...and then don't forget to unsub when the event happens.
		}

		public override SpawnResult DoOther() {
			throw new NotImplementedException(); //todo
		}
		/*public override SpawnResult DoOther() {
	var x = new[] {
		1, 2
	};
	new List<RoguelikeRewrite.GameEvent> {
		new RoguelikeRewrite.GameEvent {
		 executionTime = 1
		} 
	}
	if(Args != null) {
		return new SpawnResult();
		// args.Target.OnDeath -= 
	}
	Doer.Do(this);

	return null;
}*/
	}
	public class TestCreature {
		bool dead;

		void TakeDamage() {
			// pretend to subtract hp here
			// if hp <= 0...
			dead = true;
			onDeath?.Invoke();
		}

		private Action onDeath;

		public event Action OnDeath {
			add {
				if(dead) value.Invoke();
				else onDeath += value;
			}
			remove {
				onDeath -= value;
			}
		}
	}
}