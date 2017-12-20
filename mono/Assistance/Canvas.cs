using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;

namespace Assistance {
	public class Canvas {
		public static readonly int initialSize = 100;
		public static readonly double incrementScale = 1.2;
	
		private System.Drawing.Point offset = new System.Drawing.Point(-initialSize/2, -initialSize/2);
		private Bitmap bitmap = new Bitmap(initialSize, initialSize);

		public void draw(Graphics g) {
			g.DrawImageUnscaled(bitmap, offset);
		}

		public void expand(Rectangle rect) {
			System.Drawing.Point lt = offset;
			System.Drawing.Point rb = lt + bitmap.Size;
		    System.Drawing.Rectangle recti = rect.toInt();
		    
		    int incX = (int)Math.Ceiling(bitmap.Width*incrementScale);
		    int incY = (int)Math.Ceiling(bitmap.Height*incrementScale);

		    if (recti.Left   < lt.X) lt.X = recti.Left   - incX;
		    if (recti.Top    < lt.Y) lt.Y = recti.Top    - incY;
		    if (recti.Right  > rb.X) rb.X = recti.Right  + incX;
		    if (recti.Bottom > rb.Y) rb.Y = recti.Bottom + incY;
		    
		    Size size = new Size(rb.X - lt.X, rb.Y - lt.Y);
		    if (lt != offset || size != bitmap.Size) {
				Bitmap newBitmap = new Bitmap(size.Width, size.Height);
				Graphics g = Graphics.FromImage(newBitmap);
				g.DrawImageUnscaled(bitmap, new System.Drawing.Point(offset.X - lt.X, offset.Y - lt.Y));
				g.Flush();
				
				offset = lt;
				bitmap = newBitmap;
			}
		}

		private Graphics getGraphics() {
			Graphics g = Graphics.FromImage(bitmap);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
			g.TranslateTransform(-offset.X, -offset.Y);
			return g;
		}

		public void paintTrack(Track track) {
			expand(track.getBounds());
			Graphics g = getGraphics();
			track.draw(g);
			g.Flush();
		}
	}
}

