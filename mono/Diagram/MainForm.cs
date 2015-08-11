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
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Diagram {
    public class MainForm: Form {
        Button bSave;
        SaveFileDialog sfdSave;

        ActiveDiagram diagram;
        ActiveBlock mouseBlock;
        PointF mouseBlockOffset;
    
        public MainForm() {
            diagram = Test.buildTestActiveDiagram(DiaSynfig.build());

            Width = 800;
            Height = 600;
        
            bSave = new Button();
            bSave.Left = 20;
            bSave.Top = 20;
            bSave.Text = "save";
            bSave.Click += bTestClicked;
            Controls.Add(bSave);

            sfdSave = new SaveFileDialog();
            sfdSave.OverwritePrompt = true;

            Paint += onPaint;
            MouseDown += onMouseDown;
            MouseUp += onMouseUp;
            MouseMove += onMouseMove;
            FormClosed += onClose;

            try { diagram.loadPositions("positions.ini"); } catch (Exception) { }
            Invalidate();
        }

        void onClose(object sender, EventArgs e) {
            try { diagram.savePositions("positions.ini"); }
            catch (Exception ex) { MessageBox.Show(ex.Message); }
            diagram.savePositions("positions.ini");
        }

        void bTestClicked(object sender, EventArgs e) {
            onClose(null, null);
            if (sfdSave.ShowDialog() == DialogResult.OK) {
                Bitmap b = new Bitmap(
                    (int)Math.Ceiling(diagram.bounds.Width),
                    (int)Math.Ceiling(diagram.bounds.Height) );
                Graphics g = Graphics.FromImage(b);
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.FillRectangle(Brushes.White, new RectangleF(0f, 0f, b.Width, b.Height));
                g.TranslateTransform(-diagram.bounds.Left, -diagram.bounds.Top);
                diagram.draw(g);
                g.Flush();
                b.Save(sfdSave.FileName);
            }
        }

        void onMouseDown(Object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                mouseBlock = null;

                foreach(KeyValuePair<string, ActiveBlock> pair in diagram.blocks) {
                    ActiveBlock b = pair.Value;
                    if ( e.X >= b.position.X
                      && e.Y >= b.position.Y
                      && e.X <= b.position.X + b.size.Width
                      && e.Y <= b.position.Y + b.size.Height )
                    {
                        mouseBlock = b;
                        mouseBlockOffset = new PointF(
                            e.X - mouseBlock.position.X,
                            e.Y - mouseBlock.position.Y );
                        //break;
                    }
                }
            }
        }

        void onMouseUp(Object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                mouseBlock = null;
            }
        }

        void onMouseMove(Object sender, MouseEventArgs e) {
            if (mouseBlock != null) {
                mouseBlock.position = new PointF(
                    e.X - mouseBlockOffset.X,
                    e.Y - mouseBlockOffset.Y );
                diagram.placeLinks();
                Invalidate();
            }
        }

        public void onPaint(Object sender, PaintEventArgs e) {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            diagram.draw(e.Graphics);
        }
    }
}

