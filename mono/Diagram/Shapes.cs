/*
    ......... 2015 Ivan Mahonin

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

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

