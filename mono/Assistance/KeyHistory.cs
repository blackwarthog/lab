using System;
using System.Collections.Generic;

namespace Assistance {
	public class KeyHistory<T> where T: IComparable, new() {
		public class Holder: IDisposable {
			public readonly KeyHistory<T> history;
			public readonly long ticks;
			public readonly double timeOffset;

			private readonly long heldTicks;
			private bool disposed = false;

			public Holder(KeyHistory<T> history, long ticks, double timeOffset = 0.0) {
				this.history = history;
				this.ticks = ticks;
				this.timeOffset = timeOffset;
				heldTicks = history.hold(ticks);
			}
			
			public Holder offset(double timeOffset) {
				return Geometry.isEqual(timeOffset, 0.0)
				     ? this
				     : new Holder(history, ticks, this.timeOffset + timeOffset);
			}
			
			public KeyState<T>.Holder get(double time) {
				long dticks = (long)Math.Ceiling(Timer.frequency*(time + timeOffset));
				KeyState<T> state = history.get(ticks + dticks);
				return new KeyState<T>.Holder(state, ticks, timeOffset + time);
			}

			public void Dispose() { 
				Dispose(true);
				GC.SuppressFinalize(this);           
			}
			
			protected virtual void Dispose(bool disposing) {
				if (disposed) return;
				history.release(heldTicks);
				disposed = true;
			}
			
			~Holder()
				{ Dispose(false); }
		}
	
		private readonly List<KeyState<T>> states = new List<KeyState<T>>() { new KeyState<T>() };
		private readonly List<long> locks = new List<long>();
		
		public KeyState<T> current
			{ get { return states[ states.Count - 1 ]; } }
		
		public void change(bool press, T value, long ticks)	{
			states.Add(current.change(press, value, ticks));
			autoRemove();
		}
		public KeyState<T> press(T value, long ticks)
			{ return change(true, value, ticks); }
		public KeyState<T> release(T value, long ticks)
			{ return change(false, value, ticks); }
		
		private int findLock(long ticks) {
			// locks[a] <= ticks < locks[b]
			int a = 0;
			int b = states.Count - 1;
			if (locks[a] < locks) return -1;
			if (ticks >= locks[b]) return b;
			while(True) {
				int c = (a + b)/2;
				if (a == c) break;
				if (ticks < locks[c]) b = c; else a = c;
			}
			return a;
		}
		
		private void autoRemove() {
			long ticks = locks.Count > 0 ? locks[0] : long.MaxValue;
			while(states.Count > 1 && (states[0].ticks < ticks || states[0].isEmpty))
				states.RemoveAt(0);
		}
		
		private long hold(long ticks) {
			long heldTicks = Math.Max(ticks, states[0].ticks);
			locks.Insert(findLock(heldTicks) + 1, heldTicks);
			return heldTicks;
		}
		
		private void release(long heldTicks) {
			int i = findLock(heldTicks);
			if (i >= 0 && locks[i] == heldTicks) locks.RemoveAt(i);
			autoRemove();
		}
		
		private KeyState<T> get(long ticks) {
			// state[a].ticks <= ticks < state[b].ticks 
			int a = 0;
			int b = states.Count - 1;
			if (states[a].ticks < ticks) return new KeyState<T>();
			if (ticks >= states[b].ticks) return states[b].ticks;
			while(True) {
				int c = (a + b)/2;
				if (a == c) break;
				if (ticks < states[c].ticks) b = c; else a = c;
			}
			return states[a];
		}
	}
}

