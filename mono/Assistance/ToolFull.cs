using System;
using System.Collections.Generic;

namespace Assistance {
	public class ToolFull: Tool {
		public static readonly Drawing.Pen pen = new Drawing.Pen("Dark Green", 3.0);
		public static readonly double levelAlpha = 0.8;
	
		
		private readonly List<DynamicSurface> stack = new List<DynamicSurface>();

		
		public ToolFull(Workarea workarea): base(workarea) { }
		
		public override ModifierTypes getAvailableModifierTypes()
			{ return ModifierTypes.All; }
		
		private DynamicSurface getSurface(int level)
			{ return level > 0 ? stack[level - 1] : workarea.document.canvas; }
		private DynamicSurface getSurface()
			{ return getSurface(stack.Count); }
		
		public override bool paintPush() {
			stack.Add(new DynamicSurface());
			return true;
		}

		private void paintPoint(DynamicSurface surface, Track.WayPoint point) {
			Point p = point.point.position;
			double r = pen.width*point.point.pressure;
			Drawing.Color color = pen.color;
			if (r < 0.01) r = 0.01;
			if (r > Geometry.precision && r < 0.5)
				{ color.a *= r/0.5; r = 0.5; }
			double rr = r + 1.0;
			//rr += 10.0*(Math.Abs(point.tangent.position.x) + Math.Abs(point.tangent.position.y))
			
			surface.expand(new Rectangle(p.x - rr, p.y - rr, p.x + rr, p.y + rr));

			surface.context.Save();
			pen.apply(surface.context);
			color.apply(surface.context);
			surface.context.Arc(p.x, p.y, r, 0.0, 2.0*Math.PI);
			surface.context.Fill();

			//pen.apply(surface.context);
			//color.a = 0.5;
			//color.apply(surface.context);
			//surface.context.LineWidth = 0.5;
			//surface.context.MoveTo(p.x, p.y);
			//surface.context.RelLineTo(-10.0*point.tangent.position.y, 10.0*point.tangent.position.x);
			//surface.context.Stroke();

			surface.context.Restore();
		}

		public override void paintTracks(List<Track> tracks) {
			DynamicSurface surface = getSurface();
			while(true) {
				Track track = null;
				long minTicks = 0;
				double minTimeOffset = 0.0;
				foreach(Track t in tracks) {
					if (t.wayPointsAdded > 0) {
						long ticks = t.ticks;
						double timeOffset = t.timeOffset + t.points[t.points.Count - t.wayPointsAdded].time;
						if (track == null || (ticks - minTicks)*Timer.frequency + timeOffset - minTimeOffset < 0.0) {
							track = t;
							minTicks = ticks;
							minTimeOffset = timeOffset;
						}
					}
				}
				if (track == null) break;
				paintPoint(surface, track.points[track.points.Count - track.wayPointsAdded]);
				--track.wayPointsAdded;
			}
		}
		
		public override int paintApply(int count) {
			int level = stack.Count - count;
			DynamicSurface surface = getSurface(level);
			for(int i = level; i < stack.Count; ++i) {
				surface.expand(stack[i].getBounds(), true);
				stack[i].draw(surface.context);
			}
			paintPop(count);
			return count;
		}
		
		public override void paintCancel()
			{ getSurface().clear(); }
		
		public override void paintPop(int count) {
			int level = stack.Count - count;
			for(int i = stack.Count - 1; i >= level; --i)
				stack[i].Dispose();
			stack.RemoveRange(level, count);
		}

		public override void draw(Cairo.Context context) {
			double alpha = levelAlpha;
			foreach(DynamicSurface surface in stack)
				{ surface.draw(context, alpha); alpha *= levelAlpha; }
		}
	}
}