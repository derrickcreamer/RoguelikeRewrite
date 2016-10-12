using System;

namespace RoguelikeRewrite {
	public enum TileType { };
	public class Tile : PhysicalObject {
		public readonly TileType type;
		public bool passable;
		public bool opaque;
		public bool revealedByLight;
		public Tile(Game g) : base(g) { }
	}
}
