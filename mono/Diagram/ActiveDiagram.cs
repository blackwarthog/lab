using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Drawing;
using System.IO;
using System.Globalization;

namespace Diagram {

    public class ActiveBlock {
        public Block block;
        public readonly List<ActiveLink> links = new List<ActiveLink>();
        public PointF position;

        public Font captionFont;
        public Font textFont;
        public float margin;
        public float padding;
        public Pen pen;
        public Brush brush;

        public string caption;
        public string text;
        public SizeF captionSize;
        public SizeF textSize;
        public float distance;

        public SizeF size;
        public SizeF clientSize;

        public PointF getCenter() {
            return new PointF(
                position.X + 0.5f*size.Width,
                position.Y + 0.5f*size.Height );
        }

        public void measure() {
            float width = captionFont.GetHeight() * 15f;
            caption = TextUtils.wrap(block.caption, width, captionFont);
            captionSize = TextUtils.measure(caption, captionFont);
            text = TextUtils.wrap(block.text, width, textFont);
            textSize = TextUtils.measure(text, textFont);
            if (caption != "" && text != "")
                distance = 0.3f * Math.Max(captionFont.GetHeight(), textFont.GetHeight());
            clientSize = new SizeF(
                Math.Max(captionSize.Width, textSize.Width),
                captionSize.Height + distance + textSize.Height );
            size = new SizeF(clientSize.Width + 2f*padding, clientSize.Height + 2f*padding);
        }

        public void draw(Graphics g) {
            Shapes.drawRoundRect(g, new RectangleF(position, size), 2f*padding, pen, brush);

            g.DrawString(
                caption,
                captionFont,
                Brushes.Black,
                new RectangleF(
                    position.X + 0.5f*(size.Width - captionSize.Width),
                    position.Y + padding,
                    captionSize.Width,
                    captionSize.Height ));

            g.DrawString(
                text,
                textFont,
                Brushes.Black,
                new RectangleF(
                    position.X + padding,
                    position.Y + padding + captionSize.Height + distance,
                    clientSize.Width,
                    clientSize.Height - captionSize.Height - distance ));
        }

        class LinkDesc {
            public ActiveLink link;
            public PointF src;
            public PointF dst;
        }

        class SideDesc {
            public PointF a;
            public PointF b;
            public PointF normal;
            public readonly List<LinkDesc> links = new List<LinkDesc>();

            public SideDesc(PointF a, PointF b) {
                this.a = a;
                this.b = b;
                float l = Geometry.lineLength(a, b);
                normal = new PointF((b.Y - a.Y)/l, (a.X - b.X)/l);
            }
        }

        public void placeLinks() {
            PointF lt = position;
            PointF rb = new PointF(lt.X + size.Width, lt.Y + size.Height);
            PointF lb = new PointF(lt.X, rb.Y);
            PointF rt = new PointF(rb.X, lt.Y);
            PointF center = getCenter();

            List<SideDesc> sides = new List<SideDesc>() {
                new SideDesc(lt, rt),
                new SideDesc(rt, rb),
                new SideDesc(rb, lb),
                new SideDesc(lb, lt) };

            foreach(ActiveLink link in links) {
                link.visible = false;
                LinkDesc linkDesc = new LinkDesc();
                linkDesc.link = link;
                if (link.src == this) {
                    linkDesc.src = center;
                    linkDesc.dst = link.dst.getCenter();
                } else {
                    linkDesc.src = link.src.getCenter();
                    linkDesc.dst = center;
                }
                foreach(SideDesc side in sides) {
                    PointF p;
                    if (Geometry.findIntersection(side.a, side.b, linkDesc.src, linkDesc.dst, out p)) {
                        link.visible = true;
                        linkDesc.src = p;
                        side.links.Add(linkDesc);
                        break;
                    }
                }
            }

            foreach(SideDesc side in sides) {
                side.links.Sort(delegate(LinkDesc a, LinkDesc b) {
                    return Geometry.comparePointsAtLine(a.src, b.src, side.a, side.b); });
                for(int i = 0; i < side.links.Count; ++i) {
                    ActiveLink link = side.links[i].link;
                    PointF p = Geometry.pointAtLine(side.a, side.b, i, side.links.Count, padding);
                    if (link.src == this) {
                        link.srcBase = p;
                        link.srcTangent = side.normal;
                        link.pen = pen;
                    } else {
                        link.dstBase = p;
                        link.dstTangent = new PointF(-side.normal.X, -side.normal.Y);
                    }
                }
            }
        }
    }

    public class ActiveLink {
        public Link link;
        public ActiveBlock src;
        public ActiveBlock dst;

        public SizeF arrowSize;
        public Pen pen;

        public bool visible;
        public PointF srcBase;
        public PointF srcTangent;
        public PointF dstBase;
        public PointF dstTangent;

        public PointF[] bezier;
        public PointF[] arrow;

        public static readonly PointF[] arrowTemplate = new PointF[] {
            new PointF(0f, 0f),
            new PointF(1f, 1f),
            new PointF(0f, 0.75f) };

        public static readonly float arrowOffset = 0.75f;

        public static PointF[] makeArrow(PointF point, PointF tangent, SizeF size) {
            PointF[] arrow = new PointF[2*arrowTemplate.Length];
            PointF tx = new PointF( -0.5f*tangent.Y*size.Width, 0.5f*tangent.X*size.Width  );
            PointF ty = new PointF( -tangent.X*size.Height, -tangent.Y*size.Height );
            for(int i = 0; i < arrowTemplate.Length; ++i) {
                arrow[i] = new PointF(
                    point.X + tx.X*arrowTemplate[i].X + ty.X*arrowTemplate[i].Y,
                    point.Y + tx.Y*arrowTemplate[i].X + ty.Y*arrowTemplate[i].Y );
                arrow[2*arrowTemplate.Length - i - 1] = new PointF(
                    point.X - tx.X*arrowTemplate[i].X + ty.X*arrowTemplate[i].Y,
                    point.Y - tx.Y*arrowTemplate[i].X + ty.Y*arrowTemplate[i].Y );
            }
            return arrow;
        }

        public void place() {
            if (!visible) return;

            float dx = 0.5f*Math.Abs(dstBase.X - srcBase.X);
            float dy = 0.5f*Math.Abs(dstBase.Y - srcBase.Y);
            float l = (float)Math.Sqrt(dx*dx + dy*dy);
            if (l < Geometry.precision) { visible = false; return; }
            dx = Math.Max(dx, 0.25f*l);
            dy = Math.Max(dy, 0.25f*l);

            float sl = l;
            if (Math.Abs(srcTangent.X) > Geometry.precision)
                sl = Math.Min(sl, dx/Math.Abs(srcTangent.X));
            if (Math.Abs(srcTangent.Y) > Geometry.precision)
                sl = Math.Min(sl, dy/Math.Abs(srcTangent.Y));
            PointF st = new PointF(srcTangent.X*sl, srcTangent.Y*sl);

            float dl = l;
            if (Math.Abs(dstTangent.X) > Geometry.precision)
                dl = Math.Min(dl, dx/Math.Abs(dstTangent.X));
            if (Math.Abs(dstTangent.Y) > Geometry.precision)
                dl = Math.Min(dl, dy/Math.Abs(dstTangent.Y));
            PointF dt = new PointF(dstTangent.X*sl, dstTangent.Y*sl);

            bezier = new PointF[] {
                srcBase,
                new PointF(srcBase.X + st.X, srcBase.Y + st.Y),
                new PointF(dstBase.X - dt.X, dstBase.Y - dt.Y),
                new PointF(dstBase.X - dstTangent.X*(arrowOffset*arrowSize.Height + 0.5f*pen.Width),
                           dstBase.Y - dstTangent.Y*(arrowOffset*arrowSize.Height + 0.5f*pen.Width))
            };

            arrow = makeArrow(
                new PointF(dstBase.X - 0.5f*dstTangent.X*pen.Width,
                           dstBase.Y - 0.5f*dstTangent.Y*pen.Width),
                dstTangent,
                arrowSize );
        }

        public void draw(Graphics g) {
            if (!visible) return;
            g.DrawBeziers(pen, bezier);
            g.FillPolygon(new SolidBrush(pen.Color), arrow);
            //g.DrawPolygon(pen, arrow);
        }
    }

    public class ActiveDiagram {
        public Diagram diagram;

        public readonly Dictionary<string, PointF> positions = new Dictionary<string, PointF>();

        public Font captionFont;
        public Font textFont;
        public float margin;
        public float padding;
        public SizeF arrowSize;
        public Pen pen;
        public Brush brush;

        public readonly Dictionary<string, ActiveBlock> blocks = new Dictionary<string, ActiveBlock>();
        public readonly Dictionary<string, ActiveLink> links = new Dictionary<string, ActiveLink>();

        public RectangleF bounds;

        public void savePositions(string filename) {
            remeberPositions();
            CultureInfo ci = new CultureInfo("en-US");
            List<string> lines = new List<string>();
            foreach(KeyValuePair<string, PointF> pair in positions) {
                lines.Add(pair.Key);
                lines.Add(pair.Value.X.ToString(ci));
                lines.Add(pair.Value.Y.ToString(ci));
            }
            File.Create(filename).Close();
            File.WriteAllLines(filename, lines);
        }

        public void loadPositions(string filename) {
            CultureInfo ci = new CultureInfo("en-US");
            string[] lines = File.ReadAllLines(filename);
            for(int i = 0; i < lines.Length - 2; i += 3) {
                string k = lines[i];
                PointF p = new PointF(
                    float.Parse(lines[i+1], ci),
                    float.Parse(lines[i+2], ci) );
                if (positions.ContainsKey(k))
                    positions[k] = p;
                else
                    positions.Add(k, p);
            }
            restorePositions();
        }

        public void remeberPositions() {
            foreach(KeyValuePair<string, ActiveBlock> pair in blocks)
                if (positions.ContainsKey(pair.Key))
                    positions[pair.Key] = pair.Value.position;
                else
                    positions.Add(pair.Key, pair.Value.position);
        }

        public void restorePositions() {
            foreach(KeyValuePair<string, ActiveBlock> pair in blocks)
                if (positions.ContainsKey(pair.Key))
                    pair.Value.position = positions[pair.Key];
            placeLinks();
        }

        // insert/remove blocks and links calculate sizes
        public void reloadDiagram() {
            remeberPositions();

            bool retry;

            // blocks
            foreach(KeyValuePair<string, ActiveBlock> pair in blocks)
                pair.Value.block = null;
            foreach(KeyValuePair<string, Block> pair in diagram.blocks) {
                if (!blocks.ContainsKey(pair.Key))
                    blocks.Add(pair.Key, new ActiveBlock());
                ActiveBlock b = blocks[pair.Key];
                b.block = pair.Value;
                b.links.Clear();
                b.captionFont = captionFont;
                b.textFont = textFont;
                b.margin = margin;
                b.padding = padding;
                b.pen = new Pen(new SolidBrush(b.block.color), pen.Width);
                b.brush = brush;
            }
            retry = true;
            while(retry) {
                retry = false;
                foreach(KeyValuePair<string, ActiveBlock> pair in blocks)
                    if (pair.Value.block == null)
                        { blocks.Remove(pair.Key); retry = true; break; }
            }

            // links
            foreach(KeyValuePair<string, ActiveLink> pair in links)
                pair.Value.link = null;
            foreach(KeyValuePair<string, Link> pair in diagram.links) {
                if ( blocks.ContainsKey(pair.Value.srcId)
                  && blocks.ContainsKey(pair.Value.dstId) )
                {
                    if (!links.ContainsKey(pair.Key))
                        links.Add(pair.Key, new ActiveLink());
                    ActiveLink l = links[pair.Key];
                    l.link = pair.Value;
                    l.src = blocks[l.link.srcId];
                    l.src.links.Add(l);
                    l.dst = blocks[l.link.dstId];
                    l.dst.links.Add(l);
                    l.arrowSize = arrowSize;
                    l.pen = pen;
                }
            }
            retry = true;
            while(retry) {
                retry = false;
                foreach(KeyValuePair<string, ActiveLink> pair in links)
                    if (pair.Value.link == null)
                        { links.Remove(pair.Key); retry = true; break; }
            }

            measureBlocks();
        }

        public void measureBlocks() {
            foreach(KeyValuePair<string, ActiveBlock> pair in blocks)
                pair.Value.measure();
            placeLinks();
        }

        public void placeLinks() {
            foreach(KeyValuePair<string, ActiveBlock> pair in blocks)
                pair.Value.placeLinks();
            foreach(KeyValuePair<string, ActiveLink> pair in links)
                pair.Value.place();
            recalcBounds();
        }

        public void recalcBounds() {
            if (blocks.Count == 0) { bounds = new RectangleF(); return; }
            float minx = float.PositiveInfinity;
            float miny = float.PositiveInfinity;
            float maxx = float.NegativeInfinity;
            float maxy = float.NegativeInfinity;
            foreach(KeyValuePair<string, ActiveBlock> pair in blocks) {
                minx = Math.Min(minx, pair.Value.position.X - pair.Value.margin);
                miny = Math.Min(miny, pair.Value.position.Y - pair.Value.margin);
                maxx = Math.Max(maxx, pair.Value.position.X + pair.Value.size.Width + pair.Value.margin);
                maxy = Math.Max(maxy, pair.Value.position.Y + pair.Value.size.Height + pair.Value.margin);
            }
            bounds = new RectangleF(minx, miny, maxx - minx, maxy - miny);
        }

        public void draw(Graphics g) {
            foreach(KeyValuePair<string, ActiveLink> pair in links)
                pair.Value.draw(g);
            foreach(KeyValuePair<string, ActiveBlock> pair in blocks)
                pair.Value.draw(g);
        }
    }
}

