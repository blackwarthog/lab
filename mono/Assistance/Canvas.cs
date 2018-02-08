using System;
using System.Collections.Generic;

namespace Assistance {
	public class Canvas {
		public static readonly int initialSize = 100;
		public static readonly double incrementScale = 1.2;
	
		private int offsetX = -initialSize/2;
		private int offsetY = -initialSize/2;
		private Cairo.ImageSurface surface = new Cairo.ImageSurface(Cairo.Format.ARGB32, initialSize, initialSize);

		public void draw(Cairo.Context context) {
			context.Save();
			context.Translate(offsetX, offsetY);
			context.SetSource(surface);
			context.Paint();
			context.Restore();
		}

		public void expand(Rectangle rect) {
			int l = offsetX;
			int t = offsetY;
			int r = l + surface.Width;
			int b = t + surface.Height;
			
			int rl = (int)Math.Floor(rect.x0);
			int rt = (int)Math.Floor(rect.y0);
			int rr = Math.Max(rl, (int)Math.Ceiling(rect.x1));
			int rb = Math.Max(rt, (int)Math.Ceiling(rect.y1));
		    
		    int incX = (int)Math.Ceiling(surface.Width*incrementScale);
		    int incY = (int)Math.Ceiling(surface.Height*incrementScale);

		    if (rl < l) l = rl - incX;
		    if (rt < t) t = rt - incY;
		    if (rr > r) r = rr + incX;
		    if (rb > b) b = rb + incY;
		    
		    int w = r - l;
		    int h = b - t;
		    if (l != offsetX || t != offsetY || w != surface.Width || h != surface.Height) {
				Cairo.ImageSurface newSurface = new Cairo.ImageSurface(Cairo.Format.ARGB32, w, h);
		    	Cairo.Context context = new Cairo.Context(newSurface);
		    	context.Translate(offsetX - l, offsetY - t);
		    	context.SetSource(surface);
		    	context.Paint();
				context.GetTarget().Flush();
				context.Dispose();
				surface.Dispose();
				
				offsetX = l;
				offsetY = t;
				surface = newSurface;
			}
		}

		private Cairo.Context getContext() {
			Cairo.Context context = new Cairo.Context(surface);
			context.Antialias = Cairo.Antialias.Gray;
			context.Translate(-offsetX, -offsetY);
			return context;
		}

		public void paintTrack(Track track) {
			expand(track.getBounds());
			Cairo.Context context = getContext();
			track.draw(context);
			context.GetTarget().Flush();
			context.Dispose();
		}
	}
}

