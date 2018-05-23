using System;
using System.Collections.Generic;

namespace EllipseTruncate {
	public class AngleRange {
		public static readonly double min = -Math.PI;
		public static readonly double max = Math.PI;
		public static readonly double period = max - min;
		
		public static readonly uint half = uint.MaxValue/2+1;
		public static readonly uint step = uint.MaxValue/16+1;

		public static uint discrete(uint a)
			{ return unchecked((a + step/2)/step*step); }
		public static uint toUIntDiscrete(double a)
			{ return discrete(toUInt(a)); }
		public static uint toUInt(double a)
			{ return unchecked((uint)Math.Round((a-min)/period*uint.MaxValue)); }
		public static double toDouble(uint a)
			{ return (double)a/(double)uint.MaxValue*period + min; }

		public struct Entry {
			public uint a0, a1;
			public Entry(uint a0, uint a1)
				{ this.a0 = a0; this.a1 = a1; }
			public bool isEmpty()
				{ return a0 == a1; }
			public Entry flipped()
				{ return new Entry(a1, a0); }
		}

		public bool flip = false;
		public readonly List<uint> angles = new List<uint>();
		
		public AngleRange(bool fill = false)
			{ flip = fill; }
		
		public bool isEmpty()
			{ return angles.Count == 0 && !flip; }
		public bool isFull()
			{ return angles.Count == 0 && flip; }

		public void clear()
			{ angles.Clear(); flip = false; }
		public void fill()
			{ angles.Clear(); flip = true; }
		
		private void doSet(uint a0, uint a1) {
			angles.Clear();
			if (a0 < a1) {
				flip = false;
				angles.Add(a0);
				angles.Add(a1);
			} else {
				flip = true;
				angles.Add(a1);
				angles.Add(a0);
			}
		}
		
		public void set(Entry e) {
			if (e.isEmpty()) { clear(); return; }
			doSet(e.a0, e.a1);
		}
		
		public void set(AngleRange r, bool flip = false) {
			if (r == this) return;
			this.flip = (r.flip != flip);
			angles.Clear();
			angles.AddRange(r.angles);
		}
						
		public bool check() {
			if (angles.Count % 2 != 0)
				return false;
			for(int i = 1; i < angles.Count; ++i)
				if (angles[i-1] >= angles[i])
					return false;
			return true;
		}
		
		public void invert()
			{ flip = !flip; }
		
		private int find(uint a) {
			int i0 = 0, i1 = angles.Count - 1;
			if (a < angles[0]) return i1;
			if (angles[i1] <= a) return i1;
			while(true) {
				int i = (i1 + i0)/2;
				if (i == i0) break;
				if (angles[i] <= a) i0 = i; else i1 = i;
			}
			return i0;
		}
		
		private void remove(int p0, int p1) {
			if (p1 < p0) {
				angles.RemoveRange(p0, angles.Count - p0);
				angles.RemoveRange(0, p1 + 1);
			} else {
				angles.RemoveRange(p0, p1 - p0 + 1);
			}
		}
		
		private void insert(uint a) {
			int p = find(a);
			if (angles[p] == a) angles.RemoveAt(p); else
				if (a < angles[0]) angles.Insert(0, a); else
					angles.Insert(p+1, a);
		}
		
		private int decrease(int i)
			{ return i == 0 ? angles.Count - 1: i - 1; }
		private int increase(int i)
			{ return (i + 1)%angles.Count; }
		
		public void xor(Entry e) {
			if (e.isEmpty()) return;
			if (isEmpty()) { doSet(e.a0, e.a1); return; }
			if (isFull()) { doSet(e.a1, e.a0); return; }
			if (e.a1 < e.a0) flip = !flip;
			insert(e.a0);
			insert(e.a1);
		}

		public void xor(AngleRange r) {
			if (r.isEmpty()) { return; }
			if (r.isFull()) { invert(); return; }
			if (isEmpty()) { set(r); return; }
			if (isFull()) { set(r, true); return; }
			flip = flip != r.flip;
			foreach(uint a in r.angles)
				insert(a);
		}
				
		private bool doAdd(uint a0, uint a1) {
			int p0 = find(a0);
			int p1 = find(a1);
			if (p0 == p1) {
				bool v = (p0%2 != 0) == flip;
				if (angles[p0] != a0 && angles[p0] - a0 <= a1 - a0) {
					if (v) { fill(); return true; }
					doSet(a0, a1);
				} else
				if (!v) {
					if (a1 < a0) flip = true;
					insert(a0);
					insert(a1);
				}
				return false;
			}

			bool v0 = (p0%2 != 0) == flip;
			bool v1 = (p1%2 != 0) == flip;
			remove(increase(p0), p1);
			if (!v0) insert(a0);
			if (!v1) insert(a1);
			if (angles.Count == 0) { flip = true; return true; }
			if (a1 < a0) flip = true;
			return false;
		}

		private bool doSubtract(uint a0, uint a1) {
			int p0 = find(a0);
			int p1 = find(a1);
			if (p0 == p1) {
				bool v = (p0%2 != 0) == flip;
				if (angles[p0] != a0 && angles[p0] - a0 <= a1 - a0) {
					if (!v) { clear(); return true; }
					doSet(a1, a0);
				} else
				if (v) {
					if (a1 < a0) flip = false;
					insert(a0);
					insert(a1);
				}
				return false;
			}

			bool v0 = (p0%2 != 0) == flip;
			bool v1 = (p1%2 != 0) == flip;
			remove(increase(p0), p1);
			if (v0) insert(a0);
			if (v1) insert(a1);
			if (angles.Count == 0) { flip = false; return true; }
			if (a1 < a0) flip = false;
			return false;
		}
				
		public void add(Entry e) {
			if (!isFull() && !e.isEmpty())
				{ if (isEmpty()) doSet(e.a0, e.a1); else doAdd(e.a0, e.a1); }
		}
		
		public void subtract(Entry e) {
			if (!isEmpty() && !e.isEmpty())
				{ if (isFull()) doSet(e.a1, e.a0); else doSubtract(e.a0, e.a1); }
		}
		
		public void intersect(Entry e) {
			if (!isEmpty()) {
				if (e.isEmpty()) clear();
					else if (isFull()) doSet(e.a0, e.a1);
						else doSubtract(e.a1, e.a0);
			}
		}

		public void add(AngleRange r) {
			if (r == this || isFull() || r.isEmpty()) return;
			if (isEmpty()) { set(r); return; }
			if (r.isFull()) { fill(); return; }
			bool f = r.flip;
			uint prev = r.angles[r.angles.Count - 1];
			foreach(uint a in r.angles)
				if (f && doAdd(prev, a)) return;
					else { prev = a; f = !f; }
		}
		
		public void subtract(AngleRange r) {
			if (isEmpty() || r.isEmpty()) return;
			if (r == this || r.isFull()) { clear(); return; }
			if (isFull()) { set(r); invert(); return; }
			bool f = r.flip;
			uint prev = r.angles[r.angles.Count - 1];
			foreach(uint a in r.angles)
				if (f && doSubtract(prev, a)) return;
					else { prev = a; f = !f; }
		}
		
		public void intersect(AngleRange r) {
			if (r == this || isEmpty() || r.isFull()) return;
			if (r.isEmpty()) { clear(); return; }
			if (isFull()) { set(r); return; }
			bool f = !r.flip;
			uint prev = r.angles[r.angles.Count - 1];
			foreach(uint a in r.angles)
				if (f && doSubtract(prev, a)) return;
					else { prev = a; f = !f; }
		}
	}
}

