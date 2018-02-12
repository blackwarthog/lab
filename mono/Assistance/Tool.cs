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
		public void activate() { }
		
		public int getAvailableStackSize()
			{ return 1; }
		public bool getIsCancellable()
			{ return false; }
		public Modifiers getAvailableModifiers()
			{ return Modifiers.None; }
		
		public bool paint_begin(Track track) { return false; }
		public void paint_point(TrackPoint point, Track track) { }
		public bool paint_apply() { return false; }
		public void paint_cancel() { }
		
		public void disactivate() { }
	}
}

