using System;
using Points;

namespace RoguelikeRewrite {
	public class PhysicalObject : Game.GameObject, Positioned {
		public point p { get; protected set; } //todo, protected?
		public event Action<PhysicalObject> onRemoval;
		public void RemoveFromGame() { onRemoval?.Invoke(this); }
		public PhysicalObject(Game g) : base(g) { }
	}
	public class Item { }
	public class Actor : PhysicalObject {
		private void TargetRemoved(PhysicalObject o) { if(o == targetInternal) targetInternal = null; }
		private Actor targetInternal;
		public Actor target {
			get { return targetInternal; }
			set {
				if(value == targetInternal) return;
				if(targetInternal != null) targetInternal.onRemoval -= TargetRemoved;
				if(value != null) value.onRemoval += TargetRemoved;
				targetInternal = value;
			}
		}
		public new void RemoveFromGame() {
			if(target != null) target.onRemoval -= TargetRemoved;
			base.RemoveFromGame();
		}
		public Actor(Game g) : base(g) { }
	}
	public enum FeatureType { };
	public class Feature : PhysicalObject {
		public readonly FeatureType type;
		public Feature(Game g) : base(g) { }
	}
}
