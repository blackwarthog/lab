using System;
using System.Collections.Generic;
using Assistance.Drawing;

namespace Assistance {
	public class Track {
		public interface IOwner { }

		public class Handler {
			public readonly IOwner owner;
			public readonly Track original;
			public readonly List<Track> tracks = new List<Track>();
			public Handler(IOwner owner, Track original) {
				this.owner = owner;
				this.original = original;
			}
		}

		public class Modifier {
			public readonly Handler handler;
			public readonly double timeOffset;

			public Modifier(Handler handler, double timeOffset = 0.0)
				{ this.handler = handler; this.timeOffset = timeOffset; }
			public IOwner owner
				{ get { return handler.owner; } }
			public Track original
				{ get { return handler.original; } }
			public virtual Point calcPoint(double originalIndex) {
				Point p = original.calcPoint(originalIndex);
				p.originalIndex = originalIndex;
				return p;
			}
		}

		public struct Point {
			public Assistance.Point position;
			public double pressure;
			public Assistance.Point tilt;

			public double originalIndex;
			public double time;
			public double length;
			
			public bool final;
	
			public Point(
				Assistance.Point position,
				double pressure = 0.5,
				Assistance.Point tilt = new Assistance.Point(),
				double originalIndex = 0.0,
				double time = 0.0,
				double length = 0.0,
				bool final = false
			) {
				this.position = position;
				this.pressure = pressure;
				this.tilt = tilt;
				this.originalIndex = originalIndex;
				this.time = time;
				this.length = length;
				this.final = final;
			}
		}
		

		private static long lastId;
		
		public readonly long id;
		public readonly Gdk.Device device;
		public readonly long touchId;
		public readonly List<Point> points = new List<Point>();
		public readonly KeyHistory<Gdk.Key>.Holder keyHistory;
		public readonly KeyHistory<uint>.Holder buttonHistory;

		public readonly Modifier modifier;
		
		public Handler handler;
		public int wayPointsRemoved;
		public int wayPointsAdded;

		public Track(
			Gdk.Device device = null,
			long touchId = 0,
			KeyHistory<Gdk.Key>.Holder keyHistory = null,
			KeyHistory<uint>.Holder buttonHistory = null
		) {
			this.id = ++lastId;
			this.device = device;
			this.touchId = touchId;
			this.keyHistory = keyHistory;
			this.buttonHistory = buttonHistory;
		}
		
		public Track(Modifier modifier):
			this( modifier.original.device,
				  modifier.original.touchId,
				  modifier.original.keyHistory.offset( modifier.timeOffset ),
				  modifier.original.buttonHistory.offset( modifier.timeOffset ) )
			{ this.modifier = modifier; }

		public Track original
			{ get { return modifier != null ? modifier.original : null; } }
		public double timeOffset
			{ get { return modifier != null ? modifier.timeOffset : 0.0; } }
		public long ticks
			{ get { return keyHistory.ticks; } }
			
		public bool isChanged
			{ get { return wayPointsAdded != 0 || wayPointsRemoved != 0; } }

		public Track getRoot()
			{ return original == null ? this : original.getRoot(); }
		public int getLevel()
			{ return original == null ? 0 : original.getLevel() + 1; }
		
		public Point getFirst()
			{ return getPoint(0); }
		public Point getLast()
			{ return getPoint(points.Count - 1); }
		public bool isFinished()
			{ return points.Count > 0 && getLast().final; }
		
		public int clampIndex(int index)
			{ return Math.Min(Math.Max(index, 0), points.Count - 1); }
		public int floorIndex(double index, out double frac) {
			int i = (int)Math.Floor(index + Geometry.precision);
			if (i > points.Count - 1)
				{ frac = 0.0; return points.Count - 1; }
			if (i < 0)
				{ frac = 0.0; return 0; }
			frac = Math.Max(0.0, index - (double)i);
			return i;
		}
		public int floorIndexNoClamp(double index)
			{ return (int)Math.Floor(index + Geometry.precision); }
		public int floorIndex(double index)
			{ return clampIndex(floorIndexNoClamp(index)); }
		public int ceilIndexNoClamp(double index)
			{ return (int)Math.Ceiling(index - Geometry.precision); }
		public int ceilIndex(double index)
			{ return clampIndex(ceilIndexNoClamp(index)); }
		
		public Point getPoint(int index) {
			index = clampIndex(index);
			return index < 0 ? new Point() : points[index];
		}
		public Point floorPoint(double index, out double frac)
			{ return getPoint(floorIndex(index, out frac)); }
		public Point floorPoint(double index)
			{ return getPoint(floorIndex(index)); }
		public Point ceilPoint(double index)
			{ return getPoint(ceilIndex(index)); }
		
		private delegate double PointFieldGetter(Point p);
		private double binarySearch(double value, PointFieldGetter getter) {
			// points[a].value <= value < points[b].value
			
			if (points.Count <= 0) return 0.0;
			int a = 0;
			double aa = getter(points[a]);
			if (Geometry.isLess(value, aa)) return 0.0;

			int b = points.Count - 1;
			double bb = getter(points[b]);
			if (Geometry.isGreaterOrEqual(value, bb)) return (double)b;
			
			while(true) {
				int c = (a + b)/2;
				if (a == c) break;
				double cc = getter(points[c]);
				if (Geometry.isLess(value, cc))
					{ b = c; bb = cc; } else { a = c; aa = cc; }
			}

			return Geometry.isLess(aa, bb) ? (double)a + (value - aa)/(bb - aa) : (double)a;
		}
			
		public double indexByOriginalIndex(double originalIndex)
			{ return binarySearch(originalIndex, delegate(Point p) { return p.originalIndex; }); }
		public double indexByTime(double time)
			{ return binarySearch(time, delegate(Point p) { return p.time; }); }
		public double indexByLength(double length)
			{ return binarySearch(length, delegate(Point p) { return p.length; }); }
		
		public double originalIndexByIndex(double index) {
			double frac;
			Point p0 = floorPoint(index, out frac), p1 = ceilPoint(index);
			return Geometry.interpolationLinear(p0.originalIndex, p1.originalIndex, frac);
		}
		public double timeByIndex(double index) {
			double frac;
			Point p0 = floorPoint(index, out frac), p1 = ceilPoint(index);
			return Geometry.interpolationLinear(p0.time, p1.time, frac);
		}
		public double lengthByIndex(double index) {
			double frac;
			Point p0 = floorPoint(index, out frac), p1 = ceilPoint(index);
			return Geometry.interpolationLinear(p0.length, p1.length, frac);
		}

		public Point calcPoint(double index) {
			return modifier == null
			     ? interpolateLinear(index)
			     : modifier.calcPoint( originalIndexByIndex(index) );
		}
		
		public Point interpolateLinear(double index) {
			double frac;
			Point p0 = floorPoint(index, out frac);
			Point p1 = ceilPoint(index);
			return interpolateLinear(p0, p1, frac);
		}

		public static Point interpolateLinear(Point p0, Point p1, double l) {
			if (l <= Geometry.precision) return p0;
			if (l >= 1.0 - Geometry.precision) return p1;
			return new Point(
				Geometry.interpolationLinear(p0.position, p1.position, l),
				Geometry.interpolationLinear(p0.pressure, p1.pressure, l),
				Geometry.interpolationLinear(p0.tilt, p1.tilt, l),
				Geometry.interpolationLinear(p0.originalIndex, p1.originalIndex, l),
				Geometry.interpolationLinear(p0.time, p1.time, l),
				Geometry.interpolationLinear(p0.length, p1.length, l) );
		}
		
		public void print() {
			foreach(Point wp in points)
				Console.Write(
					"{2:f1}, ",
					wp.position.x,
					wp.position.y,
					wp.originalIndex );
			Console.WriteLine();
		}
	}
}

