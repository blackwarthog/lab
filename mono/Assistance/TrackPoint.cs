using System;

namespace Assistance {
	public struct TrackPoint {
		public Point point;
		public double time;
		public double pressure;
		public Point tilt;
		
		public KeyState<Gdk.Key>.Holder keyState;
		public KeyState<uint>.Holder buttonState;
		
		public TrackPoint spawn(Point point, double time, double pressure, Point tilt) {
			TrackPoint p = this;
			p.point = point;
			p.time = time;
			p.pressure = pressure;
			p.tilt = tilt;
			p.keyState.timeOffset += time - this.time;
			p.buttonState.timeOffset += time - this.time;
			return p;
		}

		public static TrackPoint operator+ (TrackPoint a, TrackPoint b)
			{ return a.spawn(a.point + b.point, a.time + b.time, a.pressure + b.pressure, a.tilt + b.tilt); }
		public static TrackPoint operator- (TrackPoint a, TrackPoint b)
			{ return a.spawn(a.point - b.point, a.time - b.time, a.pressure - b.pressure, a.tilt - b.tilt); }
		public static TrackPoint operator* (TrackPoint a, double b)
			{ return a.spawn(a.point*b, a.time*b, a.pressure*b, a.tilt*b); }
		public static TrackPoint operator* (double b, TrackPoint a)
			{ return a*b; }
		public static TrackPoint operator/ (TrackPoint a, double b)
			{ return a.spawn(a.point/b, a.time/b, a.pressure/b, a.tilt/b); }
	}
}

