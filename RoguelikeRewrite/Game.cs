using System;
using System.Collections.Generic;
using Points;
using UtilityCollections;
using Grids;

namespace RoguelikeRewrite {
	public class Game {
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
		public Game() {
			//todo, RNG seed here?
			//one parameterless constructor, one RNG seeded, and one for loading a saved game? or not?
			// should the constructor do all this stuff directly, or should there be some kind of Reset() method, just in case?
			Q = new GameEventQueue();
			Creatures = new Grid<Creature, Point>();
		}
	}
	public class GameObject { // todo: One more major question: Do different GameObjects actually kind of want different sets? Like the map generator only having the DungeonRNG, not the normal RNG.
		public Game Game;
		public GameObject(Game g) { Game = g; }

		public GameEventQueue Q => Game.Q;
		public Grid<Creature, Point> Creatures => Game.Creatures;
		//rng
		public Creature CreatureAt(Point p) => Game.Creatures[p];
	}
}
