using System;

namespace Assistance {
	[Flags]
	public enum Modifiers {
		None = 0,
		Interpolation = 1,
		Guideline = 2,
		Multiline = 4
	};

	
	public class InputHandler {
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

		public void disactivate() { }
	}

	
	public class InputModifier {
		public InputHandler getNext() { return null; }
	
		public void activate()
			{ if (getNext() != null) getNext().activate(); }

		public void keyPress(Gdk.Key key, InputState state)
			{ if (getNext() != null) getNext().keyPress(key, state); }
		public void keyRelease(Gdk.Key key, InputState state)
			{ if (getNext() != null) getNext().keyRelease(key, state); }
		public void buttonPress(Gdk.Device device, uint button, InputState state)
			{ if (getNext() != null) getNext().buttonPress(device, button, state); }
		public void buttonRelease(Gdk.Device device, uint button, InputState state)
			{ if (getNext() != null) getNext().buttonRelease(device, button, state); }
	
		public bool paintBegin()
			{ return getNext() == null ? false : getNext().paintBegin(); }
		public void paintTrackBegin(Track track)
			{ if (getNext() != null) getNext().paintTrackBegin(track); }
		public void paintTrackPoint(Track track)
			{ if (getNext() != null) getNext().paintTrackPoint(track); }
		public void paintTrackEnd(Track track)
			{ if (getNext() != null) getNext().paintTrackEnd(track); }
		public bool paintApply()
			{ return getNext() == null ? false : getNext().paintApply(); }
		public void paintCancel()
			{ if (getNext() != null) getNext().paintCancel(); }

		public void disactivate()
			{ if (getNext() != null) getNext().disactivate(); }
	}
	

	public class Tool: InputHandler {
		public Modifiers getAvailableModifiers()
			{ return Modifiers.None; }
	}
}

