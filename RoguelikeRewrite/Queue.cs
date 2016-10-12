using System;
using System.Collections.Generic;

namespace RoguelikeRewrite {
	public class Queue {
		static bool suspend;
		public GameEvent Next => null;
		public void Run() {
			while(!suspend) {
				/*current_event = list.First.Value;
				turn = current_event.TimeToExecute();
				cached_status = null;
				cached_lighting = null;
				current_event.Execute();
				list.Remove(current_event);*/
				//
				//remove dead stuff here?
			}
		}
	}
}
