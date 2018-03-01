using System;

namespace Assistance {
	public class Tool {
		[Flags]
		public enum ModifierTypes {
			None = 0,
			Tangents = 1,
			Interpolation = 2,
			Guideline = 4,
			Multiline = 8
		};

		public virtual ModifierTypes getAvailableModifierTypes()
			{ return ModifierTypes.None; }

		public virtual void activate() { }

		public virtual void keyEvent(bool press, Gdk.Key key, InputState state) { }
		public virtual void buttonEvent(bool press, Gdk.Device device, uint button, InputState state) { }
	
		// create new painting level and return true, or do nothing and return false
		// was:            ------O-------O------ 
		// become:         ------O-------O------O
		public virtual bool paintPush() { return false; }
		
		// paint several track-points at the top painting level
		// was:            ------O-------O------
		// become:         ------O-------O------------
		public virtual void paintTracks(List<Track> tracks) { }

		// try to merge N top painting levels and return true, or do nothing and return false
		// was:            ------O-------O------O------
		// become (N = 2): ------O---------------------
		public virtual bool paintApply(int count) { return 0; }

		// reset top level to initial state
		// was:            ------O-------O------O------
		// become:         ------O-------O------O
		public virtual void paintCancel() { }

		// cancel and pop N painting levels
		// was:            ------O-------O------O------
		// become (N = 2): ------O-------
		public virtual void paintPop(int count) { }
		

		public virtual void draw(Cairo.Context context) { }

		public virtual void deactivate() { }
	}
}
