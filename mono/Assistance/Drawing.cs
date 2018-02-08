using System;

namespace Assistance {
	namespace Drawing {
		public class Helper {
			public static Rectangle getBounds(Cairo.Context context) {
				double w = 1.0;
				double h = 1.0;
				if (context.GetTarget() is Cairo.ImageSurface) {
					Cairo.ImageSurface surface = (Cairo.ImageSurface)context.GetTarget();
					w = surface.Width;
					h = surface.Height;
				}
				
				Point[] corners = new Point[] {
					new Point(0.0, 0.0),
					new Point(  w, 0.0),
					new Point(  w,   h),
					new Point(0.0,   h) };

				Rectangle bounds = new Rectangle();
				for(int i = 0; i < corners.Length; ++i) {
					double x = corners[i].x;
					double y = corners[i].y;
					context.DeviceToUser(ref x, ref y);
					if (i == 0)
						bounds = new Rectangle(x, y);
					else
						bounds = bounds.expand(new Point(x, y));
				}
				
				return bounds;
			}
		}
		
	
		public struct Color {
			public double r, g, b, a;
			public Color(string name, double alpha = 1.0) {
				Gdk.Color c = new Gdk.Color();
				if (!Gdk.Color.Parse(name, ref c))
					Console.Error.WriteLine("Color [" + name + "] not found");
				this.r = (double)c.Red/65535.0;
				this.g = (double)c.Green/65535.0;
				this.b = (double)c.Blue/65535.0;
				this.a = alpha;
			}
			public Color(double r, double g, double b, double a = 1.0) {
				this.r = r;
				this.g = g;
				this.b = b;
				this.a = a;
			}
			public void apply(Cairo.Context context) {
				context.SetSourceRGBA(r, g, b, a);
			}
		}
	
	
		public struct Pen {
			public Color color;
			public double width;
			public Pen(string color, double width = 1.0, double alpha = 1.0) {
				this.color = new Color(color, alpha);
				this.width = width;
			}
			public void apply(Cairo.Context context) {
				color.apply(context);
				context.LineWidth = width;
			}
		}


		public struct Brush {
			public Color color;
			public Brush(string color, double alpha = 1.0) {
				this.color = new Color(color, alpha);
			}
			public void apply(Cairo.Context context) {
				color.apply(context);
			}
		}
	}
}

