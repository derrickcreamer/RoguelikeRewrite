using System;
using System.Collections.Generic;
using UtilityCollections;
using GameComponents;

namespace RoguelikeRewrite {
	public class GameUniverse {
		public bool Suspend;
		public Creature Player;
		public EventScheduler Q;
		public Grid<Creature, Point> Creatures;
		//RNG eventually

		public event Action<object> OnNotify; // name?
		public T Notify<T>(T notification) {
			OnNotify?.Invoke(notification);
			return notification;
		}
		public void Run() {
			Suspend = false;
			while(!Suspend) {
				Q.ExecuteNextEvent();
			}
		}
		public GameUniverse() {
			//one parameterless constructor, one RNG seeded, and one for loading a saved game? or not?
			// should the constructor do all this stuff directly, or should there be some kind of Reset() method, just in case?
			Q = new EventScheduler();
			Creatures = new Grid<Creature, Point>(p => p.X >= 0 && p.X < 30 && p.Y >= 0 && p.Y < 20);

			// now some setup. It seems likely that a bunch of this will be handed off to things like the dungeon generator:
			Player = new Creature(this){ Decider = new PlayerCancelDecider(this) };
			Creatures.Add(Player, new Point(15, 8));
			Q.Schedule(new PlayerTurnEvent(this), 120, null);
			Creature c = new Creature(this) { State = CreatureState.Crazy };
			Creatures.Add(c, new Point(8, 17));
			Q.Schedule(new AiTurnEvent(c), 120, null);
			c = new Creature(this) { State = CreatureState.Crazy };
			Creatures.Add(c, new Point(27, 4));
			Q.Schedule(new AiTurnEvent(c), 120, null);
			c = new Creature(this) { State = CreatureState.Angry };
			Creatures.Add(c, new Point(7, 15));
			Q.Schedule(new AiTurnEvent(c), 120, null);
		}
	}
	public class GameObject {
		public GameUniverse GameUniverse;
		public GameObject(GameUniverse g) { GameUniverse = g; }

		public virtual T Notify<T>(T notification) => GameUniverse.Notify(notification);
		public Creature Player => GameUniverse.Player;
		public EventScheduler Q => GameUniverse.Q;
		public Grid<Creature, Point> Creatures => GameUniverse.Creatures;
		public Creature CreatureAt(Point p) => GameUniverse.Creatures[p];
	}
}
