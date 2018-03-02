using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Assistance {
	public class InputModifierAssistants: InputModifierTangents {
		public readonly Workarea workarea;
		public readonly bool interpolate;
		
		public InputModifierAssistants(Workarea workarea, bool interpolate = false, double precision = 1.0):
			base(precision)
		{
			this.workarea = workarea;
			this.interpolate = interpolate;
		}
		
		public class Modifier: Track.Modifier {
			public Modifier(Track.Handler handler):
				base(handler) { }
			
			public InputManager.KeyPoint.Holder holder = null;
			public List<Guideline> guidelines = new List<Guideline>();
			
			public override Track.WayPoint calcWayPoint(double originalIndex) {
				Track.WayPoint p = original.calcWayPoint(originalIndex);
				return guidelines.Count > 0 ? guidelines[0].transformPoint(p) : p;
			}
		}

		public override List<Track> modify(Track track, InputManager.KeyPoint keyPoint, List<Track> outTracks) {
			Track subTrack;
			Modifier modifier;
			
			if (track.handler == null) {
				if (track.points.Count == 0)
					return;

				Track.Handler handler = new Track.Handler(this, track);
				modifier = new Track.Modifier(track.handler);
				workarea.getGuidelines(modifier.guidelines, track.points[0].point.position);
				if (interpolate && modifier.guidelines.Count == 0)
					{ base.modify(track, keyPoint, outTracks); return; }
				
				track.handler = handler;
				subTrack = new Track(modifier);
				track.handler.tracks.Add(subTrack);
				
				if (modifier.guidelines.Count > 1) {
					modifier.holder = keyPoint.hold();
					outTracks.Add(subTrack);
					return;
				}
			}
			
			subTrack = track.handler.tracks[0];
			if (subTrack.modifier is InputModifierTangents.Modifier)
				{ base.modify(track, keyPoint, outTracks); return; }
			
			modifier = (Modifier)subTrack.modifier;
			outTracks.Add(subTrack);
			
			if (!track.isChanged)
				return;
			
			// remove points
			int start = track.points.Count - track.wayPointsAdded;
			if (start < 0) start = 0;
			if (subTrack.points.Count < start) {
				subTrack.points.RemoveRange(start, subTrack.points.Count - start);
				subTrack.wayPointsRemoved += subTrack.points.Count - start;
			}
			
			bool trackIsLong = track.points.Count > 0 && track.getLast().length > 2.0*Guideline.snapLenght;
			if (!trackIsLong && modifier.holder != null && !modifier.holder.isHolded && modifier.holder.available)
				modifier.holder.reuse();
			
			// select guideline
			if (modifier.holder != null && modifier.holder.isHolded) {
				Guideline guideline = Guideline.findBest(modifier.guidelines, track);
				if (guideline != modifier.guidelines[0]) {
					modifier.guidelines[ modifier.guidelines.IndexOf(guideline) ] = modifier.guidelines[0];
					modifier.guidelines[0] = guideline;
					start = 0;
					subTrack.wayPointsRemoved += subTrack.points.Count;
					subTrack.points.Clear;
				}
				if (trackIsLong)
					modifier.holder.release();
			}
			
			// add points
			for(int i = start; i < track.points.Count; ++i)
				subTrack.points.Add(modifier.calcWayPoint(i));
			subTrack.wayPointsAdded = subTrack.points.Count - start;
		}
	}
}

