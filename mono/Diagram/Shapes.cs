using System;
using System.Drawing;

namespace Diagram {
    public class Shapes {
        public static void drawRoundRect(Graphics g, RectangleF rect, float radius, Pen pen, Brush brush) {
            radius = Math.Min(radius, 0.5f*rect.Width);
            radius = Math.Min(radius, 0.5f*rect.Height);
            float d = 2f*radius;

            g.FillPie(brush, rect.Left,    rect.Top,      d, d, -180f, 90f);
            g.FillPie(brush, rect.Right-d, rect.Top,      d, d,  -90f, 90f);
            g.FillPie(brush, rect.Right-d, rect.Bottom-d, d, d,    0f, 90f);
            g.FillPie(brush, rect.Left,    rect.Bottom-d, d, d,   90f, 90f);
            g.FillRectangle(brush, rect.Left+radius, rect.Top, rect.Width-d, rect.Height);
            g.FillRectangle(brush, rect.Left, rect.Top+radius, rect.Width, rect.Height-d);

            g.DrawArc(pen, rect.Left,    rect.Top,      d, d, -180f, 90f);
            g.DrawArc(pen, rect.Right-d, rect.Top,      d, d,  -90f, 90f);
            g.DrawArc(pen, rect.Right-d, rect.Bottom-d, d, d,    0f, 90f);
            g.DrawArc(pen, rect.Left,    rect.Bottom-d, d, d,   90f, 90f);
            g.DrawLine(pen, rect.Left+radius, rect.Top, rect.Right-radius, rect.Top);
            g.DrawLine(pen, rect.Left+radius, rect.Bottom, rect.Right-radius, rect.Bottom);
            g.DrawLine(pen, rect.Left, rect.Top+radius, rect.Left, rect.Bottom-radius);
            g.DrawLine(pen, rect.Right, rect.Top+radius, rect.Right, rect.Bottom-radius);
        }
    }
}

