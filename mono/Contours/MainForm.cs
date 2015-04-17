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
        List<List<PointF>> contours = new List<List<PointF>>();

        private void mouseDown(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                contours.Add(new List<PointF>());
                drawing = true;
                mouseMove(sender, e);
            }
        }

        private void mouseMove(object sender, MouseEventArgs e) {
            if (drawing) {
                contours.Last().Add(new PointF(e.Location.X, e.Location.Y));
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
                contours.Clear();
                Refresh();
            }
        }

        private void paint(object sender, PaintEventArgs e) {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach(List<PointF> c in contours) {
                if (c != null && c.Count >= 3) {
                    e.Graphics.DrawLines(Pens.Black, c.ToArray());
                    e.Graphics.DrawLine(Pens.Black, c.First(), c.Last());
                }
            }
        }
    }
}

