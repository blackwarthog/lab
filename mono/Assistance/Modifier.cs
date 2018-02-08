using System;
using System.Collections.Generic;
using Assistance.Drawing;

namespace Assistance {
	public class Modifier: ActivePoint.Owner {
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

		public virtual void draw(Cairo.Context context) { }

		public virtual void getTransformFuncs(List<Geometry.TransformFunc> transformFuncs) { }
		
		public List<Track> modify(List<Track> tracks) {
			List<Track> outTracks = new List<Track>();
			List<Geometry.TransformFunc> transformFuncs = new List<Geometry.TransformFunc>();
			getTransformFuncs(transformFuncs);
			foreach(Track track in tracks)
				foreach(Geometry.TransformFunc transformFunc in transformFuncs)
					outTracks.Add(track.createChild(transformFunc));
			return outTracks;
		}
	}
}
