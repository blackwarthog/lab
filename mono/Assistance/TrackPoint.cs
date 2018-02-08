using System;

namespace Assistance {
	public struct TrackPoint {
		public Point point;
		public double time;
		public double pressure;
		public Point tilt;

		public TrackPoint(Point point, double time, double pressure = 0.5, Point tilt = new Point()) {
			this.point = point;
			this.time = time;
			this.pressure = pressure;
			this.tilt = tilt;
		}

		public static TrackPoint operator+ (TrackPoint a, TrackPoint b)
			{ return new TrackPoint(a.point + b.point, a.time + b.time, a.pressure + b.pressure, a.tilt + b.tilt); }
		public static TrackPoint operator- (TrackPoint a, TrackPoint b)
			{ return new TrackPoint(a.point - b.point, a.time - b.time, a.pressure - b.pressure, a.tilt - b.tilt); }
		public static TrackPoint operator* (TrackPoint a, double b)
			{ return new TrackPoint(a.point*b, a.time*b, a.pressure*b, a.tilt*b); }
		public static TrackPoint operator* (double b, TrackPoint a)
			{ return a*b; }
		public static TrackPoint operator/ (TrackPoint a, double b)
			{ return new TrackPoint(a.point/b, a.time/b, a.pressure/b, a.tilt/b); }
	}
}

