using System;
using System.Collections.Generic;

namespace Assistance {
	public class ModifierSpiro: Modifier {
		public static readonly double segmentSize = Math.PI/180.0*10.0;
	
		public class Handler: Track.Handler {
			public readonly List<double> angles = new List<double>();
			public Handler(ModifierSpiro owner, Track original):
				base(owner, original) { }
		}
		
		public class Modifier: Track.Modifier {
			public double angle;
			public double radius;
		
			public Modifier(Track.Handler handler, double angle, double radius):
				base(handler) { this.angle = angle; this.radius = radius; }
			public override Track.WayPoint calcWayPoint(double originalIndex) {
				Track.WayPoint p = base.calcWayPoint(originalIndex);
				
				Handler handler = (Handler)this.handler;
				double frac, a0, a1;
				Point p0, p1;
				int i = original.floorIndex(originalIndex, out frac);
				if (frac > Geometry.precision) {
					a0 = handler.angles[i];
					a1 = handler.angles[i+1];
					p0 = original.points[i].point.position;
					p1 = original.points[i+1].point.position;
				} else
				if (i > 0) {
					a0 = handler.angles[i-1];
					a1 = handler.angles[i];
					p0 = original.points[i-1].point.position;
					p1 = original.points[i].point.position;
					frac = 1.0;
				} else {
					a0 = handler.angles[i];
					a1 = handler.angles[i];
					p0 = original.points[i].point.position;
					p1 = original.points[i].point.position;
				}
				
				double angle = Geometry.interpolationLinear(a0, a1, frac) + this.angle;
				double da = a1 - a0;
				
				double radius = 2.0*this.radius*p.point.pressure;
				double s = Math.Sin(angle);
				double c = Math.Cos(angle);
			
				p.point.position += new Point(c, s)*radius;
				p.tangent.position = ((p1 - p0) + new Point(-s, c)*radius*(a1 - a0)).normalize();
				return p;
			}
		}


		public ActivePoint center;
		public int count;
		public double radius;

		public ModifierSpiro(Document document, Point center, int count = 3, double radius = 10.0): base(document) {
			this.center = new ActivePoint(this, ActivePoint.Type.CircleCross, center);
			this.count = count;
			this.radius = radius;
		}

		public override void modify(Track track, InputManager.KeyPoint keyPoint, List<Track> outTracks) {
			if (track.handler == null) {
				track.handler = new Handler(this, track);
				for(int i = 0; i < count; ++i)
					track.handler.tracks.Add(new Track( new Modifier(track.handler, i*2.0*Math.PI/(double)count, radius) ));
			}
			
			outTracks.AddRange(track.handler.tracks);
			if (!track.isChanged)
				return;
			
			Handler handler = (Handler)track.handler;
			
			int start = track.points.Count - track.wayPointsAdded;
			if (start < 0) start = 0;
			
			// remove angles
			if (start < handler.angles.Count)
				handler.angles.RemoveRange(start, handler.angles.Count - start);
			
			// add angles
			for(int i = start; i < track.points.Count; ++i) {
				if (i > 0) {
					double dl = track.points[i].length - track.points[i-1].length;
					double da = track.points[i].point.pressure > Geometry.precision
					          ? 0.25*dl/(2.0*radius*track.points[i].point.pressure) : 0.0;
					handler.angles.Add(handler.angles[i-1] + da);
				} else {
					handler.angles.Add(0.0);
				}
			}
			
			// process sub-tracks
			foreach(Track subTrack in track.handler.tracks) {
				// remove points
				int subStart = subTrack.floorIndex(subTrack.indexByOriginalIndex(start));
				if (subStart < 0) subStart = 0;
				if (subStart < subTrack.points.Count && subTrack.points[subStart].originalIndex + Geometry.precision < start)
					++subStart;
				
				if (subStart < subTrack.points.Count) {
					subTrack.wayPointsRemoved += subTrack.points.Count - subStart;
					subTrack.points.RemoveRange(subStart, subTrack.points.Count - subStart);
				}
				
				// add points
				for(int i = start; i < track.points.Count; ++i) {
					if (i > 0) {
						double prevAngle = handler.angles[i-1];
						double nextAngle = handler.angles[i];
						if (Math.Abs(nextAngle - prevAngle) > 1.5*segmentSize) {
							double step = segmentSize/Math.Abs(nextAngle - prevAngle);
							double end = 1.0 - 0.5*step;
							for(double frac = step; frac < end; frac += step)
								subTrack.points.Add( subTrack.modifier.calcWayPoint((double)i - 1.0 + frac) );
						}
					}
					subTrack.points.Add( subTrack.modifier.calcWayPoint(i) );
				}
				subTrack.wayPointsAdded += subTrack.points.Count - subStart;
			}
			
			track.wayPointsRemoved = 0;
			track.wayPointsAdded = 0;
		}
	}
}

