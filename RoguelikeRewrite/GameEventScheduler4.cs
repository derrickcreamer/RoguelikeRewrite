using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RoguelikeRewrite4 {
	public class GameEventScheduler {
		public int CurrentTick; //todo, properties...

		public void ExecuteNextEvent() {

		}
		public EventScheduling Schedule(IEvent scheduledEvent, int ticksInFuture, Initiative initiative) {
		}
		public EventScheduling RescheduleCurrent(int ticksInFuture) {
			//todo, reconsider this one. Might be best to just do this explicitly.
			// (what if something calls this and gets the wrong event?)
		}
		public EventScheduling ScheduleDuration(IEvent durationEndEvent, int ticksInFuture) {
		}
		public bool CancelEventScheduling(EventScheduling eventScheduling) => false;
		public Initiative CreateInitiativeBefore(Initiative beforeInitiative) => null;
		public Initiative CreateInitiativeAfter(Initiative afterInitiative) => null;
		public Initiative CreateInitiativeAtStart() => null;
		public Initiative CreateInitiativeAtEnd() => null;
		public Initiative CreateInitiativeBeforeCurrent() => null;
		public Initiative CreateInitiativeAfterCurrent() => null;
		public bool UnregisterInitiative(Initiative initiative) => true;
	}
	public class Initiative {

	}
	public interface IEvent {
		void ExecuteEvent();
		bool IsDead { get; }
	}
	public class EventScheduling {
		public IEvent Event; //todo, properties
		public int CreationTime;
		public int Delay;
		public int ExecutionTime;
		public Initiative Initiative;
	}
	public abstract class Event<T> : IEvent {
		public virtual bool IsDead => false; //todo, does it make sense for this to default to false if not overridden?
		// ... because, the question then becomes whether events should be set to Dead after they execute.

		public void ExecuteEvent() { Execute(); }
		public abstract T Execute();
	}
}
