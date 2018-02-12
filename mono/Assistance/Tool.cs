using System;

namespace Assistance {
	[Flags]
	public enum Modifiers {
		None = 0,
		Interpolation = 1,
		Guideline = 2,
		Multiline = 4
	};

	public class MotionHandler {
		public bool paint_begin() { return false; }
		public void paint_track_begin(Track track) { }
		public void paint_track_point(Track track) { }
		public void paint_track_end(Track track) { }
		public bool paint_apply() { return false; }
		public void paint_cancel() { }
	}

	public class Tool: MotionHandler {
		public void activate() { }
		
		public Modifiers getAvailableModifiers()
			{ return Modifiers.None; }
		
		public void disactivate() { }
	}
}

