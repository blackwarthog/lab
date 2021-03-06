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
			public double speed;
		
			public Modifier(Track.Handler handler, double angle, double radius, double speed = 0.25):
				base(handler) { this.angle = angle; this.radius = radius; this.speed = speed; }

			public override Track.Point calcPoint(double originalIndex) {
				Track.Point p = base.calcPoint(originalIndex);
				
				if (p.length > 2.0) {
					double frac;
					int i0 = original.floorIndex(originalIndex, out frac);
					int i1 = original.ceilIndex(originalIndex);
					if (i0 < 0) return p;
	
					Handler handler = (Handler)this.handler;
					double angle = this.angle + speed*Geometry.interpolationLinear(
						handler.angles[i0], handler.angles[i1], frac);
					double radius = 2.0*this.radius*p.pressure;
					
					Point tangent = original.calcTangent(originalIndex, Math.Abs(2.0*this.radius/speed));
					p.position += tangent.rotate(angle)*radius;
					p.pressure *= 0.5*(1.0 + Math.Cos(angle));
				} else {
					p.pressure = 0.0;
				}
				
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
			if (!track.wasChanged)
				return;
			
			Handler handler = (Handler)track.handler;
			
			int start = track.count - track.pointsAdded;
			if (start < 0) start = 0;
			
			// remove angles
			if (start < handler.angles.Count)
				handler.angles.RemoveRange(start, handler.angles.Count - start);
			
			// add angles
			for(int i = start; i < track.count; ++i) {
				if (i > 0) {
					double dl = track[i].length - track[i-1].length;
					double da = track[i].pressure > Geometry.precision
					          ? dl/(2.0*radius*track[i].pressure) : 0.0;
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
				if (subStart < subTrack.count && subTrack[subStart].originalIndex + Geometry.precision < start)
					++subStart;
				subTrack.truncate(subStart);
				
				// add points
				for(int i = start; i < track.count; ++i) {
					if (i > 0) {
						double prevAngle = handler.angles[i-1];
						double nextAngle = handler.angles[i];
						if (Math.Abs(nextAngle - prevAngle) > 1.5*segmentSize) {
							double step = segmentSize/Math.Abs(nextAngle - prevAngle);
							double end = 1.0 - 0.5*step;
							for(double frac = step; frac < end; frac += step)
								subTrack.add( subTrack.modifier.calcPoint((double)i - 1.0 + frac) );
						}
					}
					subTrack.add( subTrack.modifier.calcPoint(i) );
				}
			}
			
			track.resetCounters();
		}
	}
}

