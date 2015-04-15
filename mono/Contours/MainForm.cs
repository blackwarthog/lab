using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Contours {
    public class MainForm: Form {
        public MainForm() {
            MouseDown += mouseDown;
            MouseMove += mouseMove;
            MouseUp += mouseUp;
            Paint += paint;
        }

        bool drawing = false;
        ContourFloat contour = new ContourFloat();

        private void mouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                contour.contours.Add(new List<VectorFloat>());
                drawing = true;
                mouseMove(sender, e);
            }
        }

        private void mouseMove(object sender, MouseEventArgs e) {
            if (drawing) {
                contour.contours.Last().Add(new VectorFloat(e.Location.X, e.Location.Y));
                Refresh();
            }
        }

        private void mouseUp(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                mouseMove(sender, e);
                drawing = false;
            }
            if (e.Button == MouseButtons.Right) {
                drawing = false;
                contour.contours.Clear();
                Refresh();
            }
        }

        private void paint(object sender, PaintEventArgs e) {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach(List<VectorFloat> c in contour.contours) {
                if (c != null && c.Count >= 3) {
                    List<PointF> newContour = new List<PointF>();
                    foreach(VectorFloat point in c)
                        newContour.Add(new PointF(point.x, point.y));
                    newContour.Add(newContour.First());
                    e.Graphics.DrawLines(Pens.Black, newContour.ToArray());
                }
            }
        }
    }
}

