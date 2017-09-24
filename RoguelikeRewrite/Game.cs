using System;
using System.Collections.Generic;
using UtilityCollections;
using GameComponents;

namespace RoguelikeRewrite {
	public class GameUniverse {
		public bool Suspend;
		public EventScheduler Q;
		public Grid<Creature, Point> Creatures;
		//RNG eventually

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
			Creatures.Add(new Creature(this), new Point(15, 8));
			Creatures.Add(new Creature(this){ State = CreatureState.Crazy }, new Point(8, 17));
			Creatures.Add(new Creature(this) { State = CreatureState.Crazy }, new Point(27, 4));
			Creatures.Add(new Creature(this) { State = CreatureState.Angry }, new Point(7, 15));
		}
	}
	public class GameObject {
		public GameUniverse GameUniverse;
		public GameObject(GameUniverse g) { GameUniverse = g; }

		public EventScheduler Q => GameUniverse.Q;
		public Grid<Creature, Point> Creatures => GameUniverse.Creatures;
		public Creature CreatureAt(Point p) => GameUniverse.Creatures[p];
	}
}
