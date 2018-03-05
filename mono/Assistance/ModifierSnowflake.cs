using System;
using System.Collections.Generic;

namespace Assistance {
	public class ModifierSnowflake: Modifier {
		public class Modifier: Track.Modifier {
			Point center, px, py;
		
			public Modifier(Track.Handler handler, Point center, double angle, bool flip):
				base(handler)
			{
				this.center = center;
				double s = Math.Sin(angle);
				double c = Math.Cos(angle);
				if (flip) { px = new Point(c, s); py = new Point(s,-c); }
				     else { px = new Point(c,-s); py = new Point(s, c); }
			}
			
			public InputManager.KeyPoint.Holder holder = null;
			public List<Guideline> guidelines = new List<Guideline>();
			
			public override Track.WayPoint calcWayPoint(double originalIndex) {
				Track.WayPoint p = original.calcWayPoint(originalIndex);
				Point pp = p.point.position - center;
				p.point.position = center + new Point(Point.dot(pp, px), Point.dot(pp, py));
				p.tangent.position = new Point(Point.dot(p.tangent.position, px), Point.dot(p.tangent.position, py));
				return p;
			}
		}


		public ActivePoint center;
		public int rays;

		public ModifierSnowflake(Document document, Point center, int rays = 6): base(document) {
			this.center = new ActivePoint(this, ActivePoint.Type.CircleCross, center);
			this.rays = rays;
		}

		public override void draw(Cairo.Context context) {
			for(int i = 0; i < rays/2; ++i) {
				Point pp0 = center.position;
				Point pp1 = center.position + new Point(100.0, 0.0).rotate( i*2.0*Math.PI/(double)rays );
				Rectangle bounds = Drawing.Helper.getBounds(context);
				Geometry.truncateInfiniteLine(bounds, ref pp0, ref pp1);
				
				context.Save();
				pen.apply(context);
				context.MoveTo(pp0.x, pp0.y);
				context.LineTo(pp1.x, pp1.y);
				context.Stroke();
				context.Restore();
			}
		}
		
		public override void modify(Track track, InputManager.KeyPoint keyPoint, List<Track> outTracks) {
			if (track.handler == null) {
				track.handler = new Track.Handler(this, track);
				for(int i = 0; i < rays; ++i) {
					double angle = i*2.0*Math.PI/(double)rays;
					track.handler.tracks.Add(new Track( new Modifier(track.handler, center.position, angle, false) ));
					track.handler.tracks.Add(new Track( new Modifier(track.handler, center.position, angle, true) ));
				}
			}
			
			outTracks.AddRange(track.handler.tracks);
			if (!track.isChanged)
				return;
			
			int start = track.points.Count - track.wayPointsAdded;
			if (start < 0) start = 0;
			foreach(Track subTrack in track.handler.tracks) {
				// remove points
				if (subTrack.points.Count < start) {
					subTrack.points.RemoveRange(start, subTrack.points.Count - start);
					subTrack.wayPointsRemoved += subTrack.points.Count - start;
				}
				
				// add points
				Track.WayPoint p0 = track.getWayPoint(start - 1);
				for(int i = start; i < track.points.Count; ++i)
					subTrack.points.Add( subTrack.calcWayPoint(i) );
				subTrack.wayPointsAdded += subTrack.points.Count - start;
			}
		}
	}
}

