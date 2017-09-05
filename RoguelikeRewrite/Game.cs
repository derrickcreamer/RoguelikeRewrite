using System;
using System.Collections.Generic;
using UtilityCollections;
using GameComponents;

namespace RoguelikeRewrite {
	public class GameUniverse {
		public bool Suspend;
		public GameEventQueue Q;
		public Grid<Creature, Point> Creatures;
		//RNG!

		public void Run() {
			Suspend = false;
			while(!Suspend) {
				Q.ExecuteNextEvent();
			}
		}
		public GameUniverse() {
			//todo, RNG seed here?
			//one parameterless constructor, one RNG seeded, and one for loading a saved game? or not?
			// should the constructor do all this stuff directly, or should there be some kind of Reset() method, just in case?
			Q = new GameEventQueue();
			Creatures = new Grid<Creature, Point>();
		}
	}
	public class GameObject { // todo: One more major question: Do different GameObjects actually kind of want different sets? Like the map generator only having the DungeonRNG, not the normal RNG.
		public GameUniverse GameUniverse;
		public GameObject(GameUniverse g) { GameUniverse = g; }

		public GameEventQueue Q => GameUniverse.Q;
		public Grid<Creature, Point> Creatures => GameUniverse.Creatures;
		//rng
		public Creature CreatureAt(Point p) => GameUniverse.Creatures[p];
	}
}
