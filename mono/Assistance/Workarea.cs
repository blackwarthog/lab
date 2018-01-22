using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace Assistance {
	public class Workarea {
		public readonly List<Assistant> assistants = new List<Assistant>();
		public readonly List<Modifier> modifiers = new List<Modifier>();
		public readonly List<ActivePoint> points = new List<ActivePoint>();
		public readonly Canvas canvas = new Canvas();
		
		public ActivePoint ActivePoint = null;

		public ActivePoint findPoint(Point position) {
			foreach(ActivePoint point in points.Reverse<ActivePoint>())
				if (point.isInside(position))
					return point;
			return null;
		}

		public void getGuidelines(List<Guideline> outGuidelines, Point target) {
			foreach(Assistant assistant in assistants)
				assistant.getGuidelines(outGuidelines, target);
		}

		public void draw(Graphics g, ActivePoint activePoint, Point target, Track track) {
			// canvas
			canvas.draw(g);

			// guidelines and track
			List<Guideline> guidelines = new List<Guideline>();
			if (track != null && track.points.Count > 0) {
				Guideline guideline;
				Track modifiedTrack = modifyTrackByAssistant(track, out guideline);
				
				getGuidelines(guidelines, modifiedTrack.transform(track.points.Last()));
				foreach(Guideline gl in guidelines)	gl.draw(g);

				track.draw(g, true);
				if (guideline != null) guideline.draw(g, true);
				
				List<Track> modifiedTracks = modifyTrackByModifiers(modifiedTrack);
				rebuildTracks(modifiedTracks);
				foreach(Track t in modifiedTracks)
					t.draw(g);
			} else {
				getGuidelines(guidelines, target);
				foreach(Guideline guideline in guidelines)
					guideline.draw(g);
			}
			
			// modifiers
			foreach(Modifier modifier in modifiers)
				modifier.draw(g);

			// assistants
			foreach(Assistant assistant in assistants)
				assistant.draw(g);

			// active points
			foreach(ActivePoint point in points)
				point.draw(g, activePoint == point);
		}
	
		public Track modifyTrackByAssistant(Track track, out Guideline guideline) {
			guideline = null;
			if (track.points.Count < 1)
				return track.createChild(Geometry.noTransform);

			List<Guideline> guidelines = new List<Guideline>();
			getGuidelines(guidelines, track.points[0]);
			guideline = Guideline.findBest(guidelines, track);
			if (guideline == null)
				return track.createChild(Geometry.noTransform);
			return track.createChild(guideline.transformPoint);
		}
		
		public List<Track> modifyTrackByModifiers(Track track) {
			List<Track> tracks = new List<Track>() { track };
			foreach(Modifier modifier in modifiers)
				tracks = modifier.modify(tracks);
			return tracks;
		}

		public void rebuildTracks(List<Track> tracks) {
			foreach(Track track in tracks)
				track.rebuild();
		}

		public List<Track> modifyTrack(Track track) {
			Guideline guideline;
			List<Track> tracks = modifyTrackByModifiers(
				modifyTrackByAssistant(track, out guideline) );
			rebuildTracks(tracks);
			return tracks;
		}
				
		public void paintTrack(Track track) {
			List<Track> tracks = modifyTrack(track);
			foreach(Track t in tracks)
				canvas.paintTrack(t);
		}
	}
}

