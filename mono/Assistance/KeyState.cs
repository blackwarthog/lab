using System;

namespace Assistance {
	public class KeyState<T> where T: IComparable, new() {
		public struct Holder {
			public KeyState<T> state;
			public long ticks;
			public double timeOffset;
			
			public Holder(KeyState<T> state, long ticks = 0, double timeOffset = 0.0) {
				this.state = state;
				this.ticks = ticks;
				this.timeOffset = timeOffset;
			}
			
			public KeyState<T> find(T value)
				{ return state == null ? null : state.find(value); }
			public bool isEmpty
				{ get { return state == null || state.isEmpty; } }
			public bool isPressed(T value)
				{ return find(value) != null; }
			public double howLongPressed(T value)
				{ return howLongPressed(find(value), ticks, timeOffset); }

			public static double howLongPressed(KeyState<T> state, long ticks, double timeOffset) {
				return state == null ? 0.0
				     : Math.Max(Timer.step, (ticks - state.ticks)*Timer.step + timeOffset);
			}
		}
		
		public static readonly T none = new T();
		public static readonly KeyState<T> empty = new KeyState<T>();
		
		public readonly KeyState<T> previous;
		public readonly long ticks;
		public readonly T value;

		public KeyState(): this(null, 0, none) { }

		private KeyState(KeyState<T> previous, long ticks, T value) {
			this.previous = previous;
			this.ticks = ticks;
			this.value = value;
		}
		
		public KeyState<T> find(T value) {
			if (value.CompareTo(none) == 0)
				return null;
			if (value.CompareTo(this.value) == 0)
				return this;
			if (previous == null)
				return null;
			return previous.find(value);
		}
		
		private KeyState<T> makeChainWithout(KeyState<T> ks) {
			if (this == ks || previous == null) return previous;
			return new KeyState<T>(previous.makeChainWithout(ks), ticks, value);
		}
		
		public KeyState<T> change(bool press, T value, long ticks) {
			if (value.CompareTo(none) == 0)
				return this;
			if (ticks <= this.ticks)
				ticks = this.ticks + 1;

			KeyState<T> p = find(value);
			if (press) {
				if (p != null) return this;
				return new KeyState<T>(isEmpty ? null : this, ticks, value);
			}

			if (p == null) return this;
			KeyState<T> chain = makeChainWithout(p);
			return chain == null ? new KeyState<T>() : chain;
		}
		
		public bool isEmpty
			{ get { return value.CompareTo(none) == 0 && (previous == null || previous.isEmpty); } }
		public bool isPressed(T value)
			{ return find(value) != null; }

		public KeyState<T> press(T value, long ticks)
			{ return change(true, value, ticks); }
		public KeyState<T> release(T value, long ticks)
			{ return change(false, value, ticks); }
	}
}

