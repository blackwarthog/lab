using System;
using System.Collections.Generic;

namespace Assistance {
	public class InputModifierTangents: InputManager.Modifier {
		public class Modifier: Track.Modifier {
			public Modifier(Handler handler):
				base(handler) { }
			
			public InputManager.KeyPoint.Holder holder = null;
			public readonly List<Track.Point> tangents = new List<Track.Point>();
			
			public override Track.WayPoint calcWayPoint(double originalIndex) {
				double frac;
				int i0 = original.floorIndex(originalIndex, out frac);
				int i1 = original.ceilIndex(originalIndex);
				Track.WayPoint p0 = original.getWayPoint(i0);
				Track.WayPoint p1 = original.getWayPoint(i1);
				p0.tangent = tangents[i0];
				p1.tangent = tangents[i1];
				return Track.interpolate(p0, p1, frac);
			}
		}

		public override List<Track> modify(Track track, InputManager.KeyPoint keyPoint, List<Track> outTracks) {
			if (track.handler == null) {
				track.handler = new Track.Handler(this, track);
				track.handler.tracks.Add(new Track( new Modifier(track.handler) ));
			}
			
			Track subTrack = track.handler.tracks[0];
			Modifier modifier = (Modifier)subTrack.modifier;
			outTracks.Add(subTrack);
			
			if ( !track.isChanged
			  && track.points.Count == subTrack.points.Count
			  && track.points.Count == modifier.tangents.Count )
			  	return;
			
			if (!track.isChanged && subTrack.points.Count == track.points.Count - 1) {
				// add temporary point
				modifier.tangents.Add(new Track.Point());
				subTrack.points.Add(track.getLast());
				++subTrack.wayPointsAdded;
			} else {
				// apply permanent changes
				
				// remove points
				int start = track.points.Count - track.wayPointsAdded;
				if (start < 0) start = 0;
				if (start > 1) --start;
				if (subTrack.points.Count < start) {
					subTrack.points.RemoveRange(start, subTrack.points.Count - start);
					subTrack.wayPointsRemoved += subTrack.points.Count - start;
				}
				if (modifier.tangents.Count < start)
					modifier.tangents.RemoveRange(start, modifier.tangents.Count - start);
				
				// add first point
				int index = start;
				if (index == 0) {
					modifier.tangents.Add(new Track.Point());
					subTrack.points.Add(track.getLast());
					++index;
				}
				
				// add points with tangents
				if (track.points.Count > 2) {
					while(index < track.points.Count - 1) {
						Track.WayPoint p = track.points[index];
						double t0 = track.points[index-1].time;
						double t2 = track.points[index+1].time;
						double dt = t2 - t0;
						p.tangent = dt > Geometry.precision
						          ? (track.points[index+1].point - track.points[index-1].point)*(p.time - t0)/dt
						          : new Track.Point();
						modifier.tangents.Add(p.tangent);
						subTrack.points.Add(p);
						++index;
					}
				}
				
				track.wayPointsRemoved = 0;
				track.wayPointsAdded = 0;
				subTrack.wayPointsAdded += index - start;
				
				// release previous key point
				if (modifier.holder) modifier.holder.Dispose();
				
				if (track.isFinished) {
					// finish
					modifier.tangents.Add(new Track.Point());
					subTrack.points.Add(track.getLast());
					++subTrack.wayPointsAdded;
				} else {
					// save key point
					modifier.holder = keyPoint.hold();
				}
			}
		}
	}
}

