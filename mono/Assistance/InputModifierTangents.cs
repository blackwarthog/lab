using System;
using System.Collections.Generic;

namespace Assistance {
	public class InputModifierTangents: InputManager.Modifier {
		public struct Tangent {
			public Point position;
			public double pressure;
			public Point tilt;
			
			public Tangent(
				Point position,
				double pressure = 0.0,
				Point tilt = new Point() )
			{
				this.position = position;
				this.pressure = pressure;
				this.tilt = tilt;
			}
		}
	
		public class Modifier: Track.Modifier {
			public Modifier(Track.Handler handler):
				base(handler) { }
			
			public InputManager.KeyPoint.Holder holder = null;
			public readonly List<Tangent> tangents = new List<Tangent>();
			
			public override Track.Point calcPoint(double originalIndex) {
				double frac;
				int i0 = original.floorIndex(originalIndex, out frac);
				int i1 = original.ceilIndex(originalIndex);
				Track.Point p = i0 < 0 ? new Track.Point()
				              : interpolateSpline(
									original[i0],
									original[i1],
									i0 < tangents.Count ? tangents[i0] : new Tangent(),
									i1 < tangents.Count ? tangents[i1] : new Tangent(),
									frac );
				p.originalIndex = originalIndex;
				return p;
			}
		}

		public static Track.Point interpolateSpline(Track.Point p0, Track.Point p1, Tangent t0, Tangent t1, double l) {
			if (l <= Geometry.precision) return p0;
			if (l >= 1.0 - Geometry.precision) return p1;
			return new Track.Point(
				Geometry.interpolationSpline(p0.position, p1.position, t0.position, t1.position, l),
				Geometry.interpolationSpline(p0.pressure, p1.pressure, t0.pressure, t1.pressure, l),
				Geometry.interpolationSpline(p0.tilt, p1.tilt, t0.tilt, t1.tilt, l),
				Geometry.interpolationLinear(p0.originalIndex, p1.originalIndex, l),
				Geometry.interpolationLinear(p0.time, p1.time, l),
				Geometry.interpolationLinear(p0.length, p1.length, l) );
		}

		public override void modify(Track track, InputManager.KeyPoint keyPoint, List<Track> outTracks) {
			if (track.handler == null) {
				track.handler = new Track.Handler(this, track);
				track.handler.tracks.Add(new Track( new Modifier(track.handler) ));
			}
			
			Track subTrack = track.handler.tracks[0];
			Modifier modifier = (Modifier)subTrack.modifier;
			outTracks.Add(subTrack);
			
			if ( !track.wasChanged
			  && track.count == subTrack.count
			  && track.count == modifier.tangents.Count )
			  	return;
			
			if (!track.wasChanged && subTrack.count == track.count - 1) {
				// add temporary point
				modifier.tangents.Add(new Tangent());
				subTrack.add(track.getLast());
			} else {
				// apply permanent changes
				
				// remove points
				int start = track.count - track.pointsAdded;
				if (start < 0) start = 0;
				if (start > 1) --start;
				subTrack.truncate(start);
				if (start < modifier.tangents.Count)
					modifier.tangents.RemoveRange(start, modifier.tangents.Count - start);
				
				// add first point
				int index = start;
				if (index == 0) {
					modifier.tangents.Add(new Tangent());
					subTrack.add(track.getLast());
					++index;
				}
				
				// add points with tangents
				if (track.count > 2) {
					while(index < track.count) {
						Track.Point p0 = track[index-1];
						Track.Point p1 = track[index];
						Track.Point p2 = track[index+1];
						double dt = p2.time - p0.time;
						double k = dt > Geometry.precision ? (p1.time - p0.time)/dt : 0.0;
						Tangent tangent = new Tangent(
							(p2.position - p0.position)*k,
							(p2.pressure - p0.pressure)*k,
							(p2.tilt - p0.tilt)*k );
						modifier.tangents.Add(tangent);
						subTrack.add(p1);
						++index;
					}
				}
				
				track.resetCounters();
				
				// release previous key point
				if (modifier.holder != null) {
					modifier.holder.Dispose();
					modifier.holder = null;
				}
				
				if (track.isFinished()) {
					// finish
					modifier.tangents.Add(new Tangent());
					subTrack.add(track.getLast());
				} else {
					// save key point
					modifier.holder = keyPoint.hold();
				}
			}
		}
	}
}

