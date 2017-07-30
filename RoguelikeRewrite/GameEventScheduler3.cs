using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityCollections;
using YA3; //todo, check this

namespace RoguelikeRewrite3 {
	public class Initiative {
		// anything here?
	}
	public class GameEventScheduler {
		PriorityQueue<EventScheduling, int> pq;
		OrderingCollection<Initiative> oc;
		MultiValueDictionary<AutoInitiative, EventScheduling> scheduledEventsForInitiatives;

		public int CurrentTick => 0; //todo
		public IEvent CurrentEvent => null; //todo, what about current EventScheduling?
		public void ExecuteNextEvent() {
			// get next from pq
			// update current tick
			// assign to currentEvent
			// execute
			// call RemoveEventScheduling
		}
		private void RemoveEventScheduling(EventScheduling es) { //todo name...
			// remove from pq
			pq.Remove(es); //todo, is this line correct?
			Initiative init = null; //todo this line should actually get the init from the 'es'
			if(init is AutoInitiative i){
				// check MVD, remove if needed
				scheduledEventsForInitiatives.Remove(i, es);
				if(!scheduledEventsForInitiatives.AnyValues(i)) {
					// if auto & MVD has none left, remove init from oc (does init get marked dead or not?)
					oc.Remove(i);
				}
			}
		}
		public void Schedule(IEvent scheduledEvent, int ticksInFuture, Initiative initiative) {
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
		public Initiative ScheduleDuration(IEvent durationEndEvent, int ticksInFuture) {
			// create new init before current one (manualremove=false)
			// create scheduling
			// add to MVD
			// add to pq
			return new AutoInitiative();
		}

		private class AutoInitiative : Initiative { }
	}
}
