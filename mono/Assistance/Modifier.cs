using System;
using System.Collections.Generic;
using Assistance.Drawing;

namespace Assistance {
	public class Modifier: ActivePoint.Owner, InputModifier {
		public static readonly Pen pen = new Pen("Light Gray");

		public Modifier(Document document): base(document) {
			document.modifiers.Add(this);
		}

		public override void remove() {
			base.remove();
			document.modifiers.Remove(this);
		}

		public override void bringToFront() {
			document.modifiers.Remove(this);
			document.modifiers.Add(this);
		}

		public virtual void activate() { }
		public virtual List<Track> modify(List<Track> tracks) { return tracks; }
		public virtual void deactivate() { }

		public virtual void draw(Cairo.Context context) { }
	}
}
