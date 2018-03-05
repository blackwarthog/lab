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
			public virtual WayPoint calcWayPoint(double originalIndex)
				{ return original.calcWayPoint(originalIndex); }
		}

		public struct Point {
			public Assistance.Point position;
			public double pressure;
			public Assistance.Point tilt;
	
			public Point(
				Assistance.Point position,
				double pressure,
				Assistance.Point tilt
			) {
				this.position = position;
				this.pressure = pressure;
				this.tilt = tilt;
			}
			
			public static Point operator+ (Point a, Point b)
				{ return new Point(a.position + b.position, a.pressure + b.pressure, a.tilt + b.tilt); }
			public static Point operator- (Point a, Point b)
				{ return new Point(a.position - b.position, a.pressure - b.pressure, a.tilt - b.tilt); }
			public static Point operator* (Point a, double b)
				{ return new Point(a.position*b, a.pressure*b, a.tilt*b); }
			public static Point operator* (double a, Point b)
				{ return a*b; }
			public static Point operator/ (Point a, double b)
				{ return a*(1.0/b); }
	
			public bool isEqual(Point other) {
				return position.isEqual(other.position)
					&& Geometry.isEqual(pressure, other.pressure)
					&& tilt.isEqual(other.tilt);
			}
	
			public Point normalize() {
				double l = position.len();
				return l > Geometry.precision ? this/l : this;
			}
		}

		public struct WayPoint {
			public Point point;
			public Point tangent;

			public double originalIndex;
			public double time;
			public double length;
			
			public int depRootIndex;
			public bool final;
	
			public WayPoint(
				Point point,
				Point tangent = new Point(),
				double originalIndex = 0.0,
				double time = 0.0,
				double length = 0.0,
				int depRootIndex = 0,
				bool final = false
			) {
				this.point = point;
				this.tangent = tangent;
				this.originalIndex = originalIndex;
				this.time = time;
				this.length = length;
				this.depRootIndex = depRootIndex;
				this.final = final;
			}
		}
		

		private static long lastId;
		
		public readonly long id;
		public readonly Gdk.Device device;
		public readonly long touchId;
		public readonly List<WayPoint> points = new List<WayPoint>();
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
		
		public WayPoint getFirst()
			{ return getWayPoint(0); }
		public WayPoint getLast()
			{ return getWayPoint(points.Count - 1); }
		public bool isFinished()
			{ return getLast().final; }
		
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
		public int floorIndex(double index)
			{ return clampIndex((int)Math.Floor(index + Geometry.precision)); }
		public int ceilIndex(double index)
			{ return clampIndex((int)Math.Ceiling(index - Geometry.precision)); }
		
		public WayPoint getWayPoint(int index) {
			index = clampIndex(index);
			return index < 0 ? new WayPoint() : points[index];
		}
		public WayPoint floorWayPoint(double index, out double frac)
			{ return getWayPoint(floorIndex(index, out frac)); }
		public WayPoint floorWayPoint(double index)
			{ return getWayPoint(floorIndex(index)); }
		public WayPoint ceilWayPoint(double index)
			{ return getWayPoint(floorIndex(index)); }
		
		private delegate double WayPointFieldGetter(WayPoint p);
		private double binarySearch(double value, WayPointFieldGetter getter) {
			// points[a].value <= value < points[b].value
			
			if (points.Count <= 0) return 0.0;
			int a = 0;
			double aa = getter(points[a]);
			if (Geometry.isLess(aa, value)) return 0.0;

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

			return Geometry.isLess(aa, bb) ? value + (value - aa)/(bb - aa) : value;
		}
			
		public double indexByOriginalIndex(double originalIndex)
			{ return binarySearch(originalIndex, delegate(WayPoint p) { return p.originalIndex; }); }
		public double indexByTime(double time)
			{ return binarySearch(time, delegate(WayPoint p) { return p.time; }); }
		public double indexByLength(double length)
			{ return binarySearch(length, delegate(WayPoint p) { return p.length; }); }
		
		public double originalIndexByIndex(double index) {
			double frac;
			WayPoint p0 = floorWayPoint(index, out frac), p1 = ceilWayPoint(index);
			return Geometry.interpolationLinear(p0.originalIndex, p1.originalIndex, frac);
		}
		public double timeByIndex(double index) {
			double frac;
			WayPoint p0 = floorWayPoint(index, out frac), p1 = ceilWayPoint(index);
			return Geometry.interpolationLinear(p0.time, p1.time, frac);
		}
		public double lengthByIndex(double index) {
			double frac;
			WayPoint p0 = floorWayPoint(index, out frac), p1 = ceilWayPoint(index);
			return Geometry.interpolationLinear(p0.length, p1.length, frac);
		}

		public WayPoint calcWayPoint(double index) {
			return modifier == null
			     ? interpolate(index)
			     : modifier.calcWayPoint( originalIndexByIndex(index) );
		}
		
		public WayPoint interpolate(double index) {
			double frac;
			WayPoint p0 = floorWayPoint(index, out frac);
			WayPoint p1 = ceilWayPoint(index);
			return interpolate(p0, p1, frac);
		}

		public static WayPoint interpolate(WayPoint p0, WayPoint p1, double l) {
			if (l <= Geometry.precision) return p0;
			if (l >= 1.0 - Geometry.precision) return p1;
			return new WayPoint(
				Geometry.Interpolation<Point>.spline(p0.point, p1.point, p0.tangent, p1.tangent, l),
				Geometry.Interpolation<Point>.splineTangent(p0.point, p1.point, p0.tangent, p1.tangent, l),
				Geometry.interpolationLinear(p0.originalIndex, p1.originalIndex, l),
				Geometry.interpolationLinear(p0.time, p1.time, l),
				Geometry.interpolationLinear(p0.length, p1.length, l),
				p1.depRootIndex );
		}
	}
}

