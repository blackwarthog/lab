using System;
using System.Collections.Generic;

namespace Assistance {
	public class ToolFull: Tool {
		public static readonly Drawing.Pen pen = new Drawing.Pen("Dark Green", 10.0);
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

		private void paintPoint(DynamicSurface surface, Cairo.Context context, Track.WayPoint point) {
			Point p = point.point.position;
			double r = pen.width*point.point.pressure;
			double rr = r + 1.0;
			
			surface.expandContext(ref context, new Rectangle(p.x - rr, p.y - rr, p.x + rr, p.y + rr));

			context.Save();
			pen.apply(context);
			context.Arc(p.x, p.y, r, 0.0, 2.0*Math.PI);
			context.Fill();
			context.Restore();
		}

		public override void paintTracks(List<Track> tracks) {
			DynamicSurface surface = getSurface();
			Cairo.Context context = surface.getContext();
			while(true) {
				Track track = null;
				long minTicks = long.MaxValue;
				double minTimeOffset = 0.0;
				foreach(Track t in tracks) {
					if (t.wayPointsAdded > 0) {
						long ticks = t.ticks;
						double timeOffset = t.timeOffset + t.points[t.points.Count - t.wayPointsAdded].time;
						if ((ticks - minTicks)*Timer.frequency + timeOffset - minTimeOffset < 0.0) {
							track = t;
							minTicks = ticks;
							minTimeOffset = timeOffset;
						}
					}
				}
				if (track == null) break;
				paintPoint(surface, context, track.points[track.points.Count - track.wayPointsAdded]);
				--track.wayPointsAdded;
			}
			context.GetTarget().Flush();
			context.Dispose();
		}
		
		public override int paintApply(int count) {
			int level = stack.Count - count;
			DynamicSurface surface = getSurface(level);
			Cairo.Context context = surface.getContext();
			for(int i = level; i < stack.Count; ++i) {
				surface.expandContext(ref context, stack[i].getBounds(), true);
				stack[i].draw(context);
			}
			context.GetTarget().Flush();
			context.Dispose();
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