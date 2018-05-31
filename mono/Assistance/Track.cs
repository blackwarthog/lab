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
		private readonly List<Point> privatePoints = new List<Point>();
		private int privatePointsRemoved;
		private int privatePointsAdded;
		
		public readonly long id;
		public readonly Gdk.Device device;
		public readonly long touchId;
		public readonly KeyHistory<Gdk.Key>.Holder keyHistory;
		public readonly KeyHistory<uint>.Holder buttonHistory;

		public readonly Modifier modifier;
		
		public Handler handler;

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
			
		public int pointsAdded
			{ get { return privatePointsAdded; } }
		public int pointsRemoved
			{ get { return privatePointsRemoved; } }
		public void resetRemoved()
			{ privatePointsRemoved = 0; }
		public void resetAdded()
			{ privatePointsAdded = 0; }
		public void resetCounters()
			{ resetRemoved(); resetAdded(); }
		public void forceRemoved(int pointsRemoved)
			{ privatePointsRemoved = pointsRemoved; }
		public void forceAdded(int pointsAdded)
			{ privatePointsAdded = pointsAdded; }
		
		public bool wasRemoved
			{ get { return pointsRemoved > 0; } }
		public bool wasAdded
			{ get { return pointsAdded > 0; } }
		public bool wasChanged
			{ get { return wasRemoved || wasAdded; } }

		public bool isEmpty
			{ get { return count == 0; } }
		public int count
			{ get { return privatePoints.Count; } }
		public void remove(int count = 1) {
			if (count > this.count) count = this.count;
			if (count <= 0) return;
			privatePoints.RemoveRange(this.count - count, count);
			privatePointsRemoved += count;
		}
		public void truncate(int count)
			{ remove(this.count - count); }
		public void add(Point p) {
			if (!isEmpty) {
				Point previous = getLast();
				// fix originalIndex
				if (p.originalIndex < previous.originalIndex)
					p.originalIndex = previous.originalIndex;
				// fix time
				p.time = Math.Max(p.time, previous.time + Timer.step);
				// calculate length
				p.length = previous.length + (p.position - previous.position).len();
			}
			privatePoints.Add(p);
			++privatePointsAdded;
		}
		
		public Point this[int index]
			{ get { return getPoint(index); } }
		public IEnumerable<Point> points
			{ get { return privatePoints; } }

		public Track getRoot()
			{ return original == null ? this : original.getRoot(); }
		public int getLevel()
			{ return original == null ? 0 : original.getLevel() + 1; }
		
		public Point getFirst()
			{ return getPoint(0); }
		public Point getLast()
			{ return getPoint(count - 1); }
		public bool isFinished()
			{ return count > 0 && getLast().final; }
		
		public int clampIndex(int index)
			{ return Math.Min(Math.Max(index, 0), count - 1); }
		public int floorIndex(double index, out double frac) {
			int i = (int)Math.Floor(index + Geometry.precision);
			if (i > count - 1)
				{ frac = 0.0; return count - 1; }
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
			return index < 0 ? new Point() : privatePoints[index];
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
			
			if (isEmpty) return 0.0;
			int a = 0;
			double aa = getter(privatePoints[a]);
			if (value - aa <= 0.5*Geometry.precision) return (double)a;

			int b = count - 1;
			double bb = getter(privatePoints[b]);
			if (bb - value <= 0.5*Geometry.precision) return (double)b;
			
			while(true) {
				int c = (a + b)/2;
				if (a == c) break;
				double cc = getter(privatePoints[c]);
				if (cc - value > 0.5*Geometry.precision)
					{ b = c; bb = cc; } else { a = c; aa = cc; }
			}

			return bb - aa >= 0.5*Geometry.precision ? (double)a + (value - aa)/(bb - aa) : (double)a;
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
		
		public Assistance.Point calcTangent(double index, double distance = 0.1) {
			double minDistance = 10.0*Geometry.precision;
			if (distance < minDistance) distance = minDistance;
			Point p = calcPoint(index);
			Point pp = calcPoint(indexByLength(p.length - distance));
			Assistance.Point dp = p.position - pp.position;
			double lenSqr = dp.lenSqr();
			return lenSqr > Geometry.precisionSqr ? dp*Math.Sqrt(1.0/lenSqr) : new Assistance.Point();
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
			foreach(Point wp in privatePoints)
				Console.Write(
					"{2:f1}, ",
					wp.position.x,
					wp.position.y,
					wp.originalIndex );
			Console.WriteLine();
		}

		public void verify(string message) {
			bool error = false;
			for(int i = 1; i < count; ++i) {
				Point pp = privatePoints[i-1];
				Point p = privatePoints[i];
				if ( Geometry.isGreater(pp.originalIndex, p.originalIndex)
				  /*|| Geometry.isGreater(pp.length, p.length)
				  || Geometry.isGreater(pp.time, p.time)
				  || pp.final*/ )
				{
					if (!error) Console.WriteLine("Track error: " + message);
					error = true;
					Console.WriteLine("--- index: " + (i-1) + " / " + i);
					Console.WriteLine("    originalIndex: " + pp.originalIndex + " / " + p.originalIndex);
					Console.WriteLine("    length: " + pp.length + " / " + p.length);
					Console.WriteLine("    time: " + pp.time + " / " + p.time);
					Console.WriteLine("    final: " + pp.final + " / " + p.final);
				}	
			}
			if (error) print();
		}
	}
}

