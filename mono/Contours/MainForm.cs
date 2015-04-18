using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Contours {
    public class MainForm: Form {
        Button bTest;
        ComboBox cbTests;
        ComboBox cbViews;
    
        public MainForm() {
            Width = 800;
            Height = 600;
        
            bTest = new Button();
            bTest.Left = 20;
            bTest.Top = 20;
            bTest.Text = "test";
            bTest.Click += bTestClicked;
            Controls.Add(bTest);

            cbTests = new ComboBox();
            cbTests.Left = bTest.Right + 20;
            cbTests.Top = 20;
            cbTests.SelectedIndexChanged += cbTestsChanged;
            Controls.Add(cbTests);

            cbViews = new ComboBox();
            cbViews.Left = cbTests.Right + 20;
            cbViews.Top = 20;
            cbViews.SelectedIndexChanged += cbViewsChanged;
            Controls.Add(cbViews);
                        
            MouseDown += mouseDown;
            MouseMove += mouseMove;
            MouseUp += mouseUp;
            Paint += paint;
        }

        bool drawing = false;
        List<List<PointF>> contours = new List<List<PointF>>();

        void bTestClicked(object sender, EventArgs e) {
            Test.loadTestsFromFile("tests.txt");
            bool success = Test.runAll();
            Test.saveReport("report.txt");
            
            foreach(Test test in Test.tests)
                cbTests.Items.Add(test.name);
            
            if (!success) MessageBox.Show("Tests failed");
        }

        void cbTestsChanged(object sender, EventArgs e) {
            cbViews.Items.Clear();
            foreach(Test test in Test.tests) {
                if (cbTests.Text == test.name) {
                    foreach(string name in test.input.Keys)
                        cbViews.Items.Add("i: " + name);
                    foreach(string name in test.output.Keys)
                        cbViews.Items.Add("o: " + name);
                }
            }
        }

        void cbViewsChanged(object sender, EventArgs e) {
            Refresh();
        }

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

        void drawContour(Graphics g, Color color, List<PointF> c) {
            if (c != null && c.Count >= 3) {
                g.DrawLines(new Pen(new SolidBrush(color)), c.ToArray());
                g.DrawLine(new Pen(new SolidBrush(color)), c.First(), c.Last());
            }
        }

        void drawContour(Graphics g, Color color, List<Point> c) {
            if (c != null && c.Count >= 3) {
                g.DrawLines(new Pen(new SolidBrush(color)), c.ToArray());
                g.DrawLine(new Pen(new SolidBrush(color)), c.First(), c.Last());
            }
        }

        private void paint(object sender, PaintEventArgs e) {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            foreach(List<PointF> c in contours)
                drawContour(e.Graphics, Color.Gray, c);
            
            List<List<List<Point>>> testContours = null;
            foreach(Test test in Test.tests) {
                if (cbTests.Text == test.name) {
                    foreach(KeyValuePair<string, List<List<List<Point>>>> pair in test.input)
                        if (cbViews.Text == "i: " + pair.Key)
                            testContours = pair.Value;
                    foreach(KeyValuePair<string, List<List<List<Point>>>> pair in test.output)
                        if (cbViews.Text == "o: " + pair.Key)
                            testContours = pair.Value;
                }
            }
            
            System.Drawing.Drawing2D.Matrix m = e.Graphics.Transform;
            e.Graphics.TranslateTransform(50, 100);
            if (testContours != null) {
                foreach(List<List<Point>> group in testContours) {
                    Color color = Color.Black;
                    foreach(List<Point> c in group) {
                        drawContour(e.Graphics, color, c);
                        color = Color.Blue;
                    }
                }
            }
            e.Graphics.Transform = m;
        }
    }
}

