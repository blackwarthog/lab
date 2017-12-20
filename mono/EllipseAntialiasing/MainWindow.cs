using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;

namespace Faraday {
    public class MainWindow : Form {
        static public void Main() { Application.Run(new MainWindow()); }

		double tangentLength = 1000.0;
		double normalLength = 50.0;
		double pointRadius = 4.0;

		double targetX = 0.0;
		double targetY = 0.0;

		double aaRadius = 25.0;
		double radiusX = 400.0;
		double radiusY = 100.0;

		public MainWindow() {
            Paint += OnPaint;
			MouseMove += OnMouseMove;
            WindowState = FormWindowState.Maximized;
        }

        public void OnPaint(Object sender, PaintEventArgs e) {
            Draw(e.Graphics);
        }

        public void OnMouseMove(Object sender, MouseEventArgs e) {
			targetX = e.X - 0.5*ClientSize.Width;
			targetY = e.Y - 0.5*ClientSize.Height;
			Invalidate();
        }

        public void Draw(Graphics g) {
            g.Clear(Color.White);
			g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;

			double centerX = 0.5*ClientSize.Width;
			double centerY = 0.5*ClientSize.Height;

			g.DrawEllipse(
				Pens.Gray,
				(float)(centerX - radiusX),
				(float)(centerY - radiusY),
				(float)(2.0*radiusX),
				(float)(2.0*radiusY));

			g.FillEllipse(
				Brushes.Blue,
				(float)(centerX + targetX - pointRadius),
				(float)(centerY + targetY - pointRadius),
				(float)(2.0*pointRadius),
				(float)(2.0*pointRadius));

			if (Math.Abs(targetX) > 0.0 || Math.Abs(targetY) > 0.0) {
				double targetL = Math.Sqrt(
					targetY*targetY +
					targetX*targetX);
				double targetNormX = targetX/targetL;
				double targetNormY = targetY/targetL;

				g.DrawLine(
					Pens.Black,
					(float)centerX,
					(float)centerY,
					(float)(centerX + tangentLength*targetNormX),
					(float)(centerY + tangentLength*targetNormY));

				double crossL = Math.Sqrt(
					targetY*targetY*radiusX*radiusX +
					targetX*targetX*radiusY*radiusY);
				double crossX = targetX*radiusX*radiusY/crossL;
				double crossY = targetY*radiusX*radiusY/crossL;

				g.FillEllipse(
					Brushes.Blue,
					(float)(centerX + crossX - pointRadius),
					(float)(centerY + crossY - pointRadius),
					(float)(2.0*pointRadius),
					(float)(2.0*pointRadius));

				double tangentL = Math.Sqrt(
					targetY*targetY*radiusX*radiusX*radiusX*radiusX +
					targetX*targetX*radiusY*radiusY*radiusY*radiusY);
				double tangentX = targetY*radiusX*radiusX/tangentL;
				double tangentY = -targetX*radiusY*radiusY/tangentL;

				g.DrawLine(
					Pens.Blue,
					(float)(centerX + crossX - tangentX*tangentLength),
					(float)(centerY + crossY - tangentY*tangentLength),
					(float)(centerX + crossX + tangentX*tangentLength),
					(float)(centerY + crossY + tangentY*tangentLength));

				double normalX = -tangentY;
				double normalY = tangentX;

				g.DrawLine(
					Pens.Blue,
					(float)(centerX + crossX),
					(float)(centerY + crossY),
					(float)(centerX + crossX + normalX*normalLength),
					(float)(centerY + crossY + normalY*normalLength));

				double dot = normalX*targetNormX + normalY*targetNormY;
				double aa = aaRadius/dot;
				//double aaX = aa*targetNormX;
				//double aaY = aa*targetNormY;

				double dotInv =
					tangentL/(targetX*targetX*radiusY*radiusY + targetY*targetY*radiusX*radiusX);
				double aaX = aaRadius*targetX*dotInv;
				double aaY = aaRadius*targetY*dotInv;

				g.DrawLine(
					Pens.Red,
					(float)(centerX + targetX - aaX),
					(float)(centerY + targetY - aaY),
					(float)(centerX + targetX + aaX),
					(float)(centerY + targetY + aaY));
				g.FillEllipse(
					Brushes.Red,
					(float)(centerX + targetX - aaX - pointRadius),
					(float)(centerY + targetY - aaY - pointRadius),
					(float)(2.0*pointRadius),
					(float)(2.0*pointRadius));
				g.FillEllipse(
					Brushes.Red,
					(float)(centerX + targetX + aaX - pointRadius),
					(float)(centerY + targetY + aaY - pointRadius),
					(float)(2.0*pointRadius),
					(float)(2.0*pointRadius));
			}
        }
    }
}

