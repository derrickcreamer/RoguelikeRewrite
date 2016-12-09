using System;
using System.Collections.Generic;
using Points;
using UtilityCollections;
using Grids;

namespace RoguelikeRewrite {
	public class Creature { }
	public class Game {
		public PriorityQueue<GameEvent, int> Q;
		public Grid<Creature, Point> Creatures;
		//RNG!
		public class GameObject {
			public Game Game;
			public GameObject(Game g) { Game = g; }

			public PriorityQueue<GameEvent, int> Q => Game.Q;
			public Grid<Creature, Point> Creatures => Game.Creatures;
			//rng
			public Creature CreatureAt(Point p) => Game.Creatures[p];
		}
	}
}
