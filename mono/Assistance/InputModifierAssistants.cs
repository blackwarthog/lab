using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace Assistance {
	public class InputModifierAssistants: InputManager.Modifier {
		public readonly Workarea workarea;
		public readonly List<Guideline> shownGuidelines = new List<Guideline>();
				
		public InputModifierAssistants(Workarea workarea)
			{ this.workarea = workarea; }
		
		public class Modifier: Track.Modifier {
			public Modifier(Track.Handler handler):
				base(handler) { }
			
			public InputManager.KeyPoint.Holder holder = null;
			public List<Guideline> guidelines = new List<Guideline>();
			
			public override Track.Point calcPoint(double originalIndex) {
				Track.Point p = base.calcPoint(originalIndex);
				return guidelines.Count > 0 ? guidelines[0].transformPoint(p) : p;
			}
		}

		public override void modify(Track track, InputManager.KeyPoint keyPoint, List<Track> outTracks) {
			Track subTrack;
			Modifier modifier;
			
			if (track.handler == null) {
				if (track.points.Count == 0)
					return;

				track.handler = new Track.Handler(this, track);
				modifier = new Modifier(track.handler);
				workarea.getGuidelines(modifier.guidelines, track.points[0].position);
				
				subTrack = new Track(modifier);
				track.handler.tracks.Add(subTrack);
				
				if (modifier.guidelines.Count > 1) {
					modifier.holder = keyPoint.hold();
					outTracks.Add(subTrack);
					return;
				}
			}
			
			subTrack = track.handler.tracks[0];
			modifier = (Modifier)subTrack.modifier;
			outTracks.Add(subTrack);
			
			if (!track.isChanged)
				return;
			
			// remove points
			int start = track.points.Count - track.wayPointsAdded;
			if (start < 0) start = 0;
			if (subTrack.points.Count < start) {
				subTrack.wayPointsRemoved += subTrack.points.Count - start;
				subTrack.points.RemoveRange(start, subTrack.points.Count - start);
			}
			
			bool trackIsLong = track.points.Count > 0 && (track.getLast().length >= Guideline.maxLenght || track.isFinished());
			if (!trackIsLong && modifier.holder != null && !modifier.holder.isHolded && modifier.holder.available)
				modifier.holder.reuse();
			
			// select guideline
			if (modifier.holder != null && modifier.holder.isHolded) {
				Guideline guideline = Guideline.findBest(modifier.guidelines, track);
				if (guideline != null && guideline != modifier.guidelines[0]) {
					modifier.guidelines[ modifier.guidelines.IndexOf(guideline) ] = modifier.guidelines[0];
					modifier.guidelines[0] = guideline;
					start = 0;
					subTrack.wayPointsRemoved += subTrack.points.Count;
					subTrack.points.Clear();
				}
				if (trackIsLong)
					modifier.holder.release();
			}
			
			// add points
			for(int i = start; i < track.points.Count; ++i)
				subTrack.points.Add(modifier.calcPoint(i));
			subTrack.wayPointsAdded = subTrack.points.Count - start;
			
			track.wayPointsRemoved = 0;
			track.wayPointsAdded = 0;
		}

		public override void drawHover(Cairo.Context context, Point hover) {
			workarea.getGuidelines(shownGuidelines, hover);
			foreach(Guideline guideline in shownGuidelines)
				guideline.draw(context);
			shownGuidelines.Clear();
		}
				
		public override void drawTrack(Cairo.Context context, Track track) {
			if (track.handler == null) return;
			Track subTrack = track.handler.tracks[0];
			if (subTrack.points.Count > 0)
				drawHover(context, subTrack.getLast().position);
		}
	}
}

