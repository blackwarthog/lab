using System;
using System.Collections.Generic;
using System.Linq;

namespace Assistance {
	public class Workarea {
		public readonly Document document;
		
		public Workarea() {
			document = new Document(this);
		}

		public ActivePoint findPoint(Point position) {
			foreach(ActivePoint point in document.points.Reverse<ActivePoint>())
				if (point.isInside(position))
					return point;
			return null;
		}

		public void getGuidelines(List<Guideline> outGuidelines, Point target) {
			foreach(Assistant assistant in document.assistants)
				assistant.getGuidelines(outGuidelines, target);
		}

		public void draw(Cairo.Context context, ActivePoint activePoint, Point target, Track track) {
			// canvas
			document.canvas.draw(context);

			// guidelines and track
			List<Guideline> guidelines = new List<Guideline>();
			if (track != null && track.points.Count > 0) {
				Guideline guideline;
				Track modifiedTrack = modifyTrackByAssistant(track, out guideline);
				
				getGuidelines(guidelines, modifiedTrack.transform(track.points.Last()).point);
				foreach(Guideline gl in guidelines)
					gl.draw(context);

				track.draw(context, true);
				if (guideline != null) guideline.draw(context, true);
				
				List<Track> modifiedTracks = modifyTrackByModifiers(modifiedTrack);
				rebuildTracks(modifiedTracks);
				foreach(Track t in modifiedTracks)
					t.draw(context);
			} else {
				getGuidelines(guidelines, target);
				foreach(Guideline guideline in guidelines)
					guideline.draw(context);
			}
			
			// modifiers
			foreach(Modifier modifier in document.modifiers)
				modifier.draw(context);

			// assistants
			foreach(Assistant assistant in document.assistants)
				assistant.draw(context);

			// active points
			foreach(ActivePoint point in document.points)
				point.draw(context, activePoint == point);
		}
	
		public Track modifyTrackByAssistant(Track track, out Guideline guideline) {
			guideline = null;
			if (track.points.Count < 1)
				return track.createChild(Geometry.noTransform);

			List<Guideline> guidelines = new List<Guideline>();
			getGuidelines(guidelines, track.points[0].point);
			guideline = Guideline.findBest(guidelines, track);
			if (guideline == null)
				return track.createChild(Geometry.noTransform);
			return track.createChild(guideline.transformPoint);
		}
		
		public List<Track> modifyTrackByModifiers(Track track) {
			List<Track> tracks = new List<Track>() { track };
			foreach(Modifier modifier in document.modifiers)
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
				document.canvas.paintTrack(t);
		}
	}
}

