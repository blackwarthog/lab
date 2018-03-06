using System;
using System.Collections.Generic;
using Assistance.Drawing;

namespace Assistance {
	public class Modifier: ActivePoint.Owner, InputManager.IModifier {
		public static readonly Pen pen = new Pen("Light Gray");

		public Modifier(Document document): base(document) {
			document.modifiers.Add(this);
			document.workarea.updateModifiers();
		}

		public override void remove() {
			base.remove();
			document.modifiers.Remove(this);
			document.workarea.updateModifiers();
		}

		public override void bringToFront() {
			document.modifiers.Remove(this);
			document.modifiers.Add(this);
		}

		public virtual void activate() { }
		public virtual void modify(Track track, InputManager.KeyPoint keyPoint, List<Track> outTracks) { }
		public virtual void modify(List<Track> tracks, InputManager.KeyPoint keyPoint, List<Track> outTracks)
			{ foreach(Track track in tracks) modify(track, keyPoint, outTracks); }
		public virtual void drawHover(Cairo.Context context, Point hover) { }
		public virtual void drawTrack(Cairo.Context context, Track track) { }
		public virtual void draw(Cairo.Context context, List<Track> tracks, List<Point> hovers) {
			foreach(Track track in tracks) drawTrack(context, track);
			foreach(Point hover in hovers) drawHover(context, hover);
		}
		public virtual void deactivate() { }

		public virtual void draw(Cairo.Context context) { }
	}
}
