using System;
using System.Collections.Generic;

namespace Assistance {
	public class DynamicSurface: IDisposable {
		private readonly double incrementScale;
	
		private int offsetX;
		private int offsetY;
		private Cairo.ImageSurface surface;
		private Cairo.Context privateContext;
		
		public DynamicSurface(double incrementScale = 1.2)
			{ this.incrementScale = incrementScale; }

		public bool isEmpty
			{ get { return surface == null; } }
			
		public int left
			{ get { return isEmpty ? 0 : offsetX; } }
		public int top
			{ get { return isEmpty ? 0 : offsetY; } }
		public int width
			{ get { return isEmpty ? 0 : surface.Width; } }
		public int height
			{ get { return isEmpty ? 0 : surface.Height; } }
		
		public Cairo.Context context
			{ get { return privateContext; } }

		public Rectangle getBounds() {
			return isEmpty ? new Rectangle()
			     : new Rectangle((double)offsetX, (double)offsetY, (double)(offsetX + surface.Width), (double)(offsetY + surface.Height));
		}

		public void flush()
			{ if (surface != null) surface.Flush(); }

		public void draw(Cairo.Context context, double alpha = 1.0) {
			if (isEmpty) return;
			flush();
			context.Save();
			context.Translate(offsetX, offsetY);
			context.SetSource(surface);
			if (alpha >= 1.0 - Geometry.precision)
				context.Paint(); else context.PaintWithAlpha(alpha);
			context.Restore();
		}

		public void clear() {
			if (isEmpty) return;
			privateContext.Dispose();
			privateContext = null;
			surface.Dispose();
			surface = null;
        }

		public bool expand(Rectangle rect, bool noScale = false) {
			rect = new Rectangle(rect.p0).expand(rect.p1);
		
			int rl = (int)Math.Floor(rect.x0 + Geometry.precision);
			int rt = (int)Math.Floor(rect.y0 + Geometry.precision);
			int rr = Math.Max(rl, (int)Math.Ceiling(rect.x1 - Geometry.precision)) + 1;
			int rb = Math.Max(rt, (int)Math.Ceiling(rect.y1 - Geometry.precision)) + 1;
		    
		    int l, t, r, b;
		    if (surface == null) {
				l = rl; t = rt; r = rr; b = rb;
		    } else {
				l = offsetX;
				t = offsetY;
				r = l + surface.Width;
				b = t + surface.Height;
			}
			
		    int incX = noScale ? 0 : Math.Max(0, (int)Math.Ceiling( (incrementScale - 1.0)*(Math.Max(r, rr) - Math.Min(l, rl)) ));
		    int incY = noScale ? 0 : Math.Max(0, (int)Math.Ceiling( (incrementScale - 1.0)*(Math.Max(b, rb) - Math.Min(t, rt)) ));

		    if (rl < l) l = rl - incX;
		    if (rt < t) t = rt - incY;
		    if (rr > r) r = rr + incX;
		    if (rb > b) b = rb + incY;
		    
		    int w = r - l;
		    int h = b - t;
		    if (surface != null && l == offsetX && t == offsetY && w == surface.Width && h == surface.Height)
		    	return false;
		    if (w <= 0 || h <= 0)
		    	return false;
			
			Cairo.ImageSurface newSurface = new Cairo.ImageSurface(Cairo.Format.ARGB32, w, h);
			Cairo.Context newContext = new Cairo.Context(newSurface);
	    	if (!isEmpty) {
	    		flush();
	    		newContext.Save();
		    	newContext.Translate(offsetX - l, offsetY - t);
		    	newContext.SetSource(surface);
		    	newContext.Paint();
		    	newContext.Restore();
				clear();
			}
			offsetX = l;
			offsetY = t;
			surface = newSurface;
			privateContext = newContext;
			privateContext.Antialias = Cairo.Antialias.Gray;
			privateContext.Translate(-offsetX, -offsetY);
			
			return true;
		}

		public void Dispose()
			{ Dispose(true); GC.SuppressFinalize(this); }
		protected virtual void Dispose(bool disposing)
			{  clear(); }
		~DynamicSurface()
			{ Dispose(false); }
	}
}

