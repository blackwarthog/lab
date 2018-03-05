using System;
using System.Collections.Generic;
using Assistance.Drawing;

namespace Assistance {
	public class Modifier: ActivePoint.Owner, InputManager.IModifier {
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
		public virtual void modify(Track track, InputManager.KeyPoint keyPoint, List<Track> outTracks) { }
		public virtual void modify(List<Track> tracks, InputManager.KeyPoint keyPoint, List<Track> outTracks)
			{ foreach(Track track in tracks) modify(track, keyPoint, outTracks); }
		public virtual void draw(Cairo.Context context, Track track) { }
		public virtual void draw(Cairo.Context context, List<Track> tracks)
			{ foreach(Track track in tracks) draw(context, track); }
		public virtual void deactivate() { }

		public virtual void draw(Cairo.Context context) { }
	}
}
