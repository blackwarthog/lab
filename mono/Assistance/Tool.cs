using System;

namespace Assistance {
	[Flags]
	public enum Modifiers {
		None = 0,
		Interpolation = 1,
		Guideline = 2,
		Multiline = 4
	};

	public class Tool {
		public Modifiers getAvailableModifiers()
			{ return Modifiers.None; }

		public void activate() { }

		public void keyPress(Gdk.Key key, InputState state) { }
		public void keyRelease(Gdk.Key key, InputState state) { }
		public void buttonPress(Gdk.Device device, uint button, InputState state) { }
		public void buttonRelease(Gdk.Device device, uint button, InputState state) { }
	
		public bool paintBegin() { return false; }
		public void paintTrackBegin(Track track) { }
		public void paintTrackPoint(Track track) { }
		public void paintTrackEnd(Track track) { }
		public bool paintApply() { return false; }
		public void paintCancel() { }

		public void draw(Cairo.Context context) { }

		public void deactivate() { }
	}
}

/*
TODO:
		//////////////////////////////////////////
		// deprecated
		//////////////////////////////////////////

		public static readonly Pen pen = new Pen("Dark Green", 3.0);
		public static readonly Pen penSpecial = new Pen("Blue", 3.0);
		public static readonly Pen penPreview = new Pen("Dark Green", 1.0, 0.25);

		public void draw(Cairo.Context context, bool preview = false) {
			if (preview) {
				if (points.Count < 2)
					return;
				context.Save();
				penPreview.apply(context);
				context.MoveTo(points[0].point.x, points[0].point.y);
				for(int i = 1; i < points.Count; ++i)
					context.LineTo(points[i].point.x, points[i].point.y);
				context.Stroke();
				context.Restore();
			} else {
				context.Save();
				pen.apply(context);
				foreach(TrackPoint p in points) {
					double t = p.keyState.howLongPressed(Gdk.Key.m)
					         + p.buttonState.howLongPressed(3);
					double w = p.pressure*pen.width + 5.0*t;
					context.Arc(p.point.x, p.point.y, 2.0*w, 0.0, 2.0*Math.PI);
					context.Fill();
				}
				context.Restore();
			}
		}
		
		public Rectangle getBounds() {
			if (points.Count == 0)
				return new Rectangle();
			Rectangle bounds = new Rectangle(points[0].point);
			foreach(TrackPoint p in points)
				bounds = bounds.expand(p.point);
			return bounds.inflate(Math.Max(pen.width, penPreview.width) + 2.0);
		}
*/