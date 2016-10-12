using System;
using System.Collections.Generic;
using Points;
using Grids;

namespace RoguelikeRewrite {
	public class Game {
		public Queue Q;
		private Grid<Tile> tiles;
		private Grid<Actor> actors;
		private Grid<Item> items;
		private MultiGrid<Feature> features;

		public ReadonlyGrid<Tile> Tiles => tiles;
		public ReadonlyGrid<Actor> Actors => actors;
		public ReadonlyGrid<Item> Items => items;
		public ReadOnlyMultiGrid<Feature> Features => features;

		public Grid<Tile> InternalMutableTiles => tiles;
		public Grid<Actor> InternalMutableActors => actors;
		public Grid<Item> InternalMutableItems => items;
		public MultiGrid<Feature> InternalMutableFeatures => features;

		//add RNGs here too?

		public class GameObject {
			public Game Game;
			public GameObject(Game g) { Game = g; }

			public Queue Q => Game.Q;
			public ReadonlyGrid<Actor> Actors => Game.actors;
			public ReadonlyGrid<Tile> Tiles => Game.tiles;
			public ReadonlyGrid<Item> Items => Game.items;
			public ReadOnlyMultiGrid<Feature> Features => Game.features;

			public Actor ActorAt(Positioned p) => Game.actors[p]; //keep the xAt methods? If so, add a (point) overload.
			public Tile TileAt(Positioned p) => Game.Tiles[p];
		}
	}
}
