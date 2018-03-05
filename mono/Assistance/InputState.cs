using System;
using System.Collections.Generic;

namespace Assistance {
	public class InputState {
		public long ticks;
		public KeyHistory<Gdk.Key> keyHistory = new KeyHistory<Gdk.Key>();
		public readonly Dictionary<Gdk.Device, KeyHistory<uint>> buttonHistories = new Dictionary<Gdk.Device, KeyHistory<uint>>();
		
		public void touch(long ticks) {
			if (this.ticks < ticks)
				this.ticks = ticks;
			else
				++this.ticks;
		}
		
		public KeyState<Gdk.Key> keyState
			{ get { return keyHistory.current; } }
		
		public void keyEvent(bool press, Gdk.Key key, long ticks) {
			touch(ticks);
			keyHistory.change(press, key, this.ticks);
		}
		public void keyPress(Gdk.Key key, long ticks)
			{ keyEvent(true, key, ticks); }
		public void keyRelease(Gdk.Key key, long ticks)
			{ keyEvent(false, key, ticks); }
		
		public KeyState<Gdk.Key> keyFind(Gdk.Key key)
			{ return keyState.find(key); }
		public bool isKeyPressed(Gdk.Key key)
			{ return keyFind(key) != null; }
		public double howLongKeyPressed(Gdk.Key key, long ticks, double timeOffset = 0.0)
			{ return KeyState<Gdk.Key>.Holder.howLongPressed(keyFind(key), ticks, timeOffset); }
		public double howLongKeyPressed(Gdk.Key key)
			{ return howLongKeyPressed(key, ticks); }

		public KeyHistory<uint> buttonHistory(Gdk.Device device) {
			KeyHistory<uint> history;
			if (!buttonHistories.TryGetValue(device, out history))
				history = new KeyHistory<uint>();
				buttonHistories[device] = history;
			return history;
		}
		public KeyState<uint> buttonState(Gdk.Device device)
			{ return buttonHistory(device).current; }

		public void buttonEvent(bool press, Gdk.Device device, uint button, long ticks) {
			touch(ticks);
			buttonHistory(device).change(press, button, this.ticks);
		}
		public void buttonPress(Gdk.Device device, uint button, long ticks)
			{ buttonEvent(true, device, button, ticks); }
		public void buttonRelease(Gdk.Device device, uint button, long ticks)
			{ buttonEvent(false, device, button, ticks); }
		public void buttonEvent(bool press, uint button, long ticks)
			{ buttonEvent(press, null, button, ticks); }
		public void buttonPress(uint button, long ticks)
			{ buttonEvent(true, button, ticks); }
		public void buttonRelease(uint button, long ticks)
			{ buttonEvent(false, button, ticks); }

		public KeyState<uint> buttonFind(Gdk.Device device, uint button)
			{ return buttonState(device).find(button); }
		public bool isButtonPressed(Gdk.Device device, uint button)
			{ return buttonFind(device, button) != null; }
		public double howLongButtonPressed(Gdk.Device device, uint button, long ticks, double timeOffset = 0.0)
			{ return KeyState<uint>.Holder.howLongPressed(buttonFind(device, button), ticks, timeOffset); }
		public double howLongButtonPressed(Gdk.Device device, uint button)
			{ return howLongButtonPressed(device, button, ticks); }

		public KeyState<uint> buttonFindDefault(uint button)
			{ return buttonFind(null, button); }
		public bool isButtonPressedDefault(uint button)
			{ return isButtonPressed(null, button); }
		public double howLongButtonPressedDefault(uint button, long ticks, double timeOffset = 0.0)
			{ return howLongButtonPressed(null, button, ticks, timeOffset); }
		public double howLongButtonPressedDefault(uint button)
			{ return howLongButtonPressedDefault(button, ticks); }

		public KeyState<uint> buttonFindAny(uint button, out Gdk.Device device) {
			device = null;
			KeyState<uint> state = null;
			foreach(KeyValuePair<Gdk.Device, KeyHistory<uint>> pair in buttonHistories) {
				KeyState<uint> s = pair.Value == null ? null : pair.Value.current.find(button);
				if (s != null && (state == null || s.ticks < state.ticks))
					{ state = s; device = pair.Key; }
			}
			return state;
		}
		public KeyState<uint> buttonFindAny(uint button)
			{ Gdk.Device device; return buttonFindAny(button, out device); }
		public bool isButtonPressedAny(uint button)
			{ return buttonFindAny(button) != null; }
		public double howLongButtonPressedAny(uint button, long ticks, double timeOffset = 0.0)
			{ return KeyState<uint>.Holder.howLongPressed(buttonFindAny(button), ticks, timeOffset); }
		public double howLongButtonPressedAny(uint button)
			{ return howLongButtonPressedAny(button, ticks); }

		public KeyState<Gdk.Key>.Holder keyStateHolder(long ticks, double timeOffset = 0.0)
			{ return new KeyState<Gdk.Key>.Holder(keyState, ticks, timeOffset); }
		public KeyState<Gdk.Key>.Holder keyStateHolder()
			{ return keyStateHolder(ticks); }
		public KeyHistory<Gdk.Key>.Holder keyHistoryHolder(long ticks, double timeOffset = 0.0)
			{ return new KeyHistory<Gdk.Key>.Holder(keyHistory, ticks, timeOffset); }
		public KeyHistory<Gdk.Key>.Holder keyHistoryHolder()
			{ return keyHistoryHolder(ticks); }

		public KeyState<uint>.Holder buttonStateHolder(Gdk.Device device, long ticks, double timeOffset = 0.0)
			{ return new KeyState<uint>.Holder(buttonState(device), ticks, timeOffset); }
		public KeyState<uint>.Holder buttonStateHolder(Gdk.Device device)
			{ return buttonStateHolder(device, ticks); }
		public KeyHistory<uint>.Holder buttonHistoryHolder(Gdk.Device device, long ticks, double timeOffset = 0.0)
			{ return new KeyHistory<uint>.Holder(buttonHistory(device), ticks, timeOffset); }
		public KeyHistory<uint>.Holder buttonHistoryHolder(Gdk.Device device)
			{ return buttonHistoryHolder(device, ticks); }
	}
}

