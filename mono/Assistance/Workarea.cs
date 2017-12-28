using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace Assistance {
	public class Workarea {
		public readonly List<Assistant> assistants = new List<Assistant>();
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
				getGuidelines(guidelines, track.points[0]);
				Guideline guideline = Guideline.findBest(guidelines, track);
				Track modifiedTrack = guideline == null
				                    ? track.createChildAndBuild(Geometry.noTransform)
				                    : track.createChildAndBuild(guideline.transformPoint);

				if (modifiedTrack.points.Count > 0) {
					guidelines.Clear();	
					getGuidelines(guidelines, modifiedTrack.points.Last());
				}
				foreach(Guideline gl in guidelines)
					gl.draw(g);

				if (guideline != null) {
					track.draw(g, true);
					guideline.draw(g, true);
				}
				modifiedTrack.draw(g);
			} else {
				getGuidelines(guidelines, target);
				foreach(Guideline guideline in guidelines)
					guideline.draw(g);
			}
			
			// assistants
			foreach(Assistant assistant in assistants)
				assistant.draw(g);

			// assistant active points
			foreach(ActivePoint point in points)
				point.draw(g, activePoint == point);
		}
	
		public Track modifyTrack(Track track) {
			if (track.points.Count < 1)
				return track;
			
			List<Guideline> guidelines = new List<Guideline>();
			getGuidelines(guidelines, track.points[0]);
			Guideline guideline = Guideline.findBest(guidelines, track);
			if (guideline == null)
				return track.createChildAndBuild(Geometry.noTransform);
				
			return track.createChildAndBuild(guideline.transformPoint);
		}
		
		public void paintTrack(Track track) {
			canvas.paintTrack( modifyTrack(track) );
		}
	}
}
