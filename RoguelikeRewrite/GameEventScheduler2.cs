using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilityCollections;
using YA3; //todo, check this

namespace RoguelikeRewrite2 {
	public abstract class Initiative {
		public abstract void add();
		public abstract void remove();
		public abstract bool isDone();
	}
	public class GameEventScheduler {
		PriorityQueue<EventScheduling, int> pq;
		OrderingCollection<Initiative> oc;
		MultiValueDictionary<Initiative, EventScheduling> scheduledEventsForInitiatives; //todo, if i use a subclass of Initiative, this MVD could take that type.

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
			// check MVD, remove if needed
			// if auto & MVD has none left, remove init from oc (does init get marked dead or not?)
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
			return new Initiative2();
		}

		private class Initiative2 : Initiative {
			public Initiative2() {
			}

			public override void add() {
				throw new NotImplementedException();
			}

			public override bool isDone() {
				throw new NotImplementedException();
			}

			public override void remove() {
				throw new NotImplementedException();
			}
		}
	}
}
