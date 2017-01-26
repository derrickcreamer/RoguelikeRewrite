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
