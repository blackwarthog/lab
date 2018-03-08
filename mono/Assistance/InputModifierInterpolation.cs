using System;
using System.Collections.Generic;

namespace Assistance {
	public class InputModifierInterpolation: InputManager.Modifier {
		public static readonly int maxRecursion = 8;
	
		public readonly double precision;
		public readonly double precisionSqr;
		
		public InputModifierInterpolation(double precision = 1.0) {
			this.precision = Math.Max(precision, Geometry.precision);
			this.precisionSqr = this.precision*this.precision;
		}
	
		public void addSegment(Track track, Track.WayPoint p0, Track.WayPoint p1, int level = 0) {
			if (level >= maxRecursion || (p1.point.position - p0.point.position).lenSqr() <= precisionSqr)
				{ track.points.Add(p1); return; }
			Track.WayPoint p = track.modifier.calcWayPoint(0.5*(p0.originalIndex + p1.originalIndex));
			addSegment(track, p0, p, level + 1);
			addSegment(track, p, p1, level + 1);
		}
	
		public override void modify(Track track, InputManager.KeyPoint keyPoint, List<Track> outTracks) {
			if (track.handler == null) {
				track.handler = new Track.Handler(this, track);
				track.handler.tracks.Add(new Track( new Track.Modifier(track.handler) ));
			}
			
			Track subTrack = track.handler.tracks[0];
			outTracks.Add(subTrack);
			
			if (!track.isChanged)
				return;
			
			// remove points
			int start = track.points.Count - track.wayPointsAdded;
			if (start < 0) start = 0;
			int subStart = subTrack.floorIndex(subTrack.indexByOriginalIndex(start));
			if (subStart < 0) subStart = 0;
			if (subStart < subTrack.points.Count && subTrack.points[subStart].originalIndex + Geometry.precision < start)
				++subStart;
			
			while(subStart > 0 && subTrack.points[subStart-1].originalIndex + Geometry.precision >= start)
				--subStart;
			if (subStart < subTrack.points.Count) {
				subTrack.wayPointsRemoved += subTrack.points.Count - subStart;
				subTrack.points.RemoveRange(subStart, subTrack.points.Count - subStart);
			}
			
			// add points
			Track.WayPoint p0 = subTrack.modifier.calcWayPoint(start - 1);
			for(int i = start; i < track.points.Count; ++i) {
				Track.WayPoint p1 = subTrack.modifier.calcWayPoint(i);
				addSegment(subTrack, p0, p1);
				p0 = p1;
			}
			subTrack.wayPointsAdded += subTrack.points.Count - subStart;
			
			track.wayPointsRemoved = 0;
			track.wayPointsAdded = 0;
		}
	}
}

