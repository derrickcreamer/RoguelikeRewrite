using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityCollections;
using YA3; //todo, check this

namespace RoguelikeRewrite {
	public class Initiative {
		bool ManualRemove; //todo, name.
		// todo, anything else?
		//now, here's an interesting thing:  it seems that the manual inits are created 'outside' and
		//the auto ones are all created internally.
		//maybe Initiative has no public constructor, and you get them from the scheduler.
		//The ones you request are 'manual' and the others are auto - does that work?

	}
	public class GameEventScheduler {
		PriorityQueue<EventScheduling, int> pq;
		OrderingCollection<Initiative> oc;
		MultiValueDictionary<Initiative, EventScheduling> scheduledEventsForInitiatives;

		public int CurrentTick => 0; //todo
		public IEvent CurrentEvent => null;
		public void ExecuteNextEvent() {

		}
		public void Schedule(IEvent scheduledEvent, int ticksInFuture, Initiative initiative){
			// ensure init is in oc?
			// create scheduling
			// add to MVD if needed (??? is this applicable? wouldn't this need to be a manual one? ???)
			// add to pq
		}
		public void RescheduleCurrent(int ticksInFuture) {
			// create scheduling with new ticks, same event, same init as current.
			// add to MVD if needed
			// add to pq
		}
		public void ScheduleDuration(IEvent durationEndEvent, int ticksInFuture) {
			// create new init before current one (manualremove=false)
			// create scheduling
			// add to MVD
			// add to pq
		}
	}
}
