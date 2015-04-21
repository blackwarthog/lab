using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Contours {
    public class Shape {
        public class Exception: System.Exception { }
    
        public enum CombinationMode {
            Add,
            Subtract,
            Intersection,
            Xor
        }
    
        class Position {
            public Point point;
            public bool processed = false;
            public readonly Circuit<Position, Link> links;

            public Position() {
                links = new Circuit<Position, Link>(this);
            }

            public Position(Point point): this() { this.point = point; }
        }

        class Contour {
            public bool inverted = false;
            
            public Contour parent;
            public readonly List<Contour> childs = new List<Contour>();
            public readonly Circuit<Contour, Link> links;

            public Contour() {
                links = new Circuit<Contour, Link>(this);
            }
        }

        class Link {
            public bool alien = false;
            public bool inside = false;
            public Position nextPosition = null;

            public readonly Circuit<Position, Link>.Entry position;
            public readonly Circuit<Contour, Link>.Entry contour;

            public Link() {
                position = new Circuit<Position, Link>.Entry(this);
                contour = new Circuit<Contour, Link>.Entry(this);
            }

            public Link split(Shape shape, Position position) {
                if (position.point == this.position.getParent().point) return this;
                if (position.point == nextPosition.point) return nextPosition.links.getFirst();
                
                shape.log.linkSplitted(this, position);
                
                Link link = new Link();
                link.position.insertBack(position.links);
                link.contour.insertAfterOf(contour);
                link.alien = alien;
                link.nextPosition = nextPosition;
                nextPosition = position;
                shape.links.Add(link);
                return link;
            }

            public Link split(Shape shape, Link positionLink) {
                return split(shape, positionLink.position.getParent());
            }
        }
        
        class Log {
            public readonly Shape shape;
            public bool enabled = true;
            public string filename = "log.txt";
            
            public Log(Shape shape) { this.shape = shape;  }
            
            string toString(Position position) {
                return string.Format(
                    "{0,2}#({1,3}, {2,3})",
                    shape.positions.IndexOf(position),
                    position.point.X,
                    position.point.Y);
            }
            
            string toString(Link link) {
                return string.Format(
                    "{0,2}# {1}{2} {3} {4}",
                    shape.links.IndexOf(link),
                    link.alien ? "B" : "A",
                    link.inside ? "I" : "O",
                    toString(link.position.getParent()),
                    toString(link.nextPosition) );
            }
            
            string toString(Contour contour) {
                string s = "";
                for(Link link = contour.links.getFirst(); link != null; link = link.contour.getNextLinear()) {
                    if (s != "") s += ", ";
                    s += string.Format("({0}, {1})", link.position.getParent().point.X, link.position.getParent().point.Y);
                }
                return s;
            }
            
            public void line(string line) {
                if (!enabled) return;
                System.IO.File.AppendAllLines(filename, new string[] { line });
            }
            
            public void linkAdded(Link link) {
                if (!enabled) return;
                line("Link added " + toString(link));
            }
            
            public void linkRemoved(Link link) {
                if (!enabled) return;
                line("Link removed " + toString(link));
            }
            
            public void linkSplitted(Link link, Position position) {
                if (!enabled) return;
                line("Link splitted " + toString(link) + " / " + toString(position));
            }
            
            public void linkSwapped(Link a, Link b) {
                if (!enabled) return;
                line("Link swapped " + toString(a) + " <-> " + toString(b));
            }

            public void positionState(Position position) {
                line("Position " + toString(position));
                for(Link link = position.links.getFirst(); link != null; link = link.position.getNextLinear())
                    line("    link " + toString(link));
            }

            public void state(string title) {
                if (!enabled) return;
                line("-- current state (" + title + ") --");
                line("-- links --");
                foreach(Link link in shape.links)
                    line("    " + toString(link));
                line("-- positions --");
                foreach(Position position in shape.positions)
                    positionState(position);
                line("-- contours --");
                foreach(Contour contour in shape.contours)
                    line("    " + toString(contour));
                line("-- current state end --");
            }
        }


        readonly List<Position> positions = new List<Position>();
        readonly List<Link> links = new List<Link>();
        readonly List<Contour> contours = new List<Contour>();
        readonly List<Contour> rootContours = new List<Contour>();
        readonly Log log;
        
        public Shape() { log = new Log(this); }

        public void clear() {
            positions.Clear();
            links.Clear();
            contours.Clear();
            rootContours.Clear();
        }

        public void setContour(IEnumerable<Point> contour) {
            setContours(new IEnumerable<Point>[] { contour });
        }

        public void setContours(IEnumerable<IEnumerable<IEnumerable<Point>>> contours) {
            List<IEnumerable<Point>> list = new List<IEnumerable<Point>>();
            foreach(IEnumerable<IEnumerable<Point>> c in contours)
                list.AddRange(c);
            setContours(list);
        }

        public void setContours(IEnumerable<IEnumerable<Point>> contours) {
            clear();
            
            log.line("---- setContours begin ----");
            
            foreach(IEnumerable<Point> contour in contours) {
                if (contour.Count() >= 3) {
                    Link first = null;
                    Link previous = null;
                    foreach(Point point in contour) {
                        Position position = new Position(point);
                        positions.Add(position);

                        Link link = new Link();
                        link.position.insertBack(position.links);
                        if (previous != null)
                            previous.nextPosition = link.position.getParent();
                        links.Add(link);
                        
                        if (first == null) first = link;
                        previous = link;
                    }
                    previous.nextPosition = first.position.getParent();
                }
            }
            
            calculate(CombinationMode.Add);

            log.line("---- setContours end ----");
        }
        
        // returns list of root contour groups
        // first contour in group is outer contour, others - inner
        public List<List<List<Point>>> getContours() {
            List<List<List<Point>>> groups = new List<List<List<Point>>>();
            foreach(Contour root in rootContours) {
                List<List<Point>> list = new List<List<Point>>();
                list.Add(contourToList(root));
                foreach(Contour c in root.childs)
                    list.Add(contourToList(c));
                groups.Add(list);
            }
            return groups;
        }

        List<Point> contourToList(Contour contour) {
            List<Point> list = new List<Point>();
            for(Link link = contour.links.getFirst(); link != null; link = link.contour.getNextLinear())
                list.Add(link.position.getParent().point);
            return list;
        }

        void resetTraceInformation() {
            for(int i = 0; i < contours.Count; ++i) {
                contours[i].links.clear();
                contours[i].childs.Clear();
                contours[i].parent = null;
            }
            contours.Clear();
            rootContours.Clear();
        }
        
        void removeLink(Link link) {
            log.linkRemoved(link);
            link.position.unlink();
            link.contour.unlink();
            link.nextPosition = null;
            links.Remove(link);
        }

        void removeEmptyPositions() {
            for(int i = 0; i < positions.Count; ++i)
                if (positions[i].links.empty())
                    positions.RemoveAt(i--);
        }
        
        void findIntersections() {
            bool retry = true;
            while(retry) {
                retry = false;

                // merge positions
                // this procedure may create zero-length links
                for(int i = 0; i < positions.Count; ++i) {
                    for(int j = i+1; j < positions.Count; ++j) {
                        if (positions[i].point == positions[j].point) {
                            while(positions[j].links.getFirst() != null)
                                positions[j].links.getFirst().position.insertBack(positions[i].links);
                            foreach(Link link in links)
                                if (link.nextPosition == positions[j])
                                    link.nextPosition = positions[i];
                            positions.RemoveAt(j--);
                        }
                    }
                }
                
                // remove zero-length links
                // this procedure may create empty positions
                for(int i = 0; i < links.Count; ++i)
                    if (links[i].position.getParent() == links[i].nextPosition)
                        removeLink(links[i--]);
                
                // so we need to...
                removeEmptyPositions();
                                
                // check intersections
                // this procedure may create new positions, new links and ne intersections
                // so we need to repeat all cycle when intersection found
                for(int i = 0; i < links.Count; ++i) {
                    for(int j = i+1; j < links.Count; ++j) {
                        Link a = links[i];
                        Link b = links[j];
                        Position a0 = a.position.getParent();
                        Position a1 = a.nextPosition;
                        Position b0 = b.position.getParent();
                        Position b1 = b.nextPosition;
                        Point c = new Point(0, 0);
                        retry = true; // will reset to false if no intersection
                        
                        switch(Geometry.findIntersection(a0.point, a1.point, b0.point, b1.point, out c))
                        {
                        case Geometry.IntersectionType.Cross:
                            Position p = new Position(c);
                            positions.Add(p);
                            a.split(this, p);
                            b.split(this, p);
                            break;
                        case Geometry.IntersectionType.Touch_a0:
                            b.split(this, a0);
                            break;
                        case Geometry.IntersectionType.Touch_a1:
                            b.split(this, a1);
                            break;
                        case Geometry.IntersectionType.Touch_b0:
                            a.split(this, b0);
                            break;
                        case Geometry.IntersectionType.Touch_b1:
                            a.split(this, b1);
                            break;
                        case Geometry.IntersectionType.Along_a0_b0_a1_b1:
                            a.split(this, b0);
                            b.split(this, a1);
                            break;
                        case Geometry.IntersectionType.Along_a0_b0_b1_a1:
                            a.split(this, b0).split(this, b1);
                            break;
                        case Geometry.IntersectionType.Along_a0_b1_a1_b0:
                            a.split(this, b1);
                            b.split(this, a1);
                            break;
                        case Geometry.IntersectionType.Along_a0_b1_b0_a1:
                            a.split(this, b1).split(this, b0);
                            break;
                        case Geometry.IntersectionType.Along_b0_a0_a1_b1:
                            b.split(this, a0).split(this, a1);
                            break;
                        case Geometry.IntersectionType.Along_b0_a0_b1_a1:
                            a.split(this, b1);
                            b.split(this, a0);
                            break;
                        case Geometry.IntersectionType.Along_b1_a0_a1_b0:
                            b.split(this, a1).split(this, a0);
                            break;
                        case Geometry.IntersectionType.Along_b1_a0_b0_a1:
                            a.split(this, b0);
                            b.split(this, a0);
                            break;
                        default:
                            retry = false;
                            break;
                        }
                    }
                }
            }
        }
        
        void findLinksInside() {
            for(int i = 0; i < links.Count; ++i) {
                int intersectionsCount = 0;
                Point p0 = links[i].position.getParent().point;
                Point p1 = links[i].nextPosition.point;
                Geometry.makeLongestLine(p0, ref p1);
                for(int j = 0; j < links.Count; ++j) {
                    if (i != j && links[i].alien != links[j].alien) {
                        Point c = new Point(0, 0);
                        Point pp0 = links[j].position.getParent().point;
                        Point pp1 = links[j].nextPosition.point;
                        int d = Math.Sign( (long)(p0.Y - p1.Y)*(long)(pp1.X - pp0.X)
                                         + (long)(p1.X - p0.X)*(long)(pp1.Y - pp0.Y) );
                        switch(Geometry.findIntersection(p0, p1, pp0, pp1, out c)) {
                            case Geometry.IntersectionType.Cross:
                                intersectionsCount += 2*d;
                                break;
                            case Geometry.IntersectionType.Touch_b0:
                            case Geometry.IntersectionType.Touch_b1:
                                intersectionsCount += d;
                                break;
                            default:
                                break;
                        }
                    }
                }
                links[i].inside = intersectionsCount == 2;
            }
        }
        
        bool combine(CombinationMode mode, bool self, bool alien) {
            switch(mode) {
                case CombinationMode.Add: return self || alien;
                case CombinationMode.Subtract: return self && !alien;
                case CombinationMode.Intersection: return self && alien;
                case CombinationMode.Xor: return self != alien;
                default: break;
            }
            return false;
        }
        
        void removeUnusedLinks(CombinationMode mode) {
            foreach(Position position in positions)
                position.processed = false;
            foreach(Position position in positions) {
                position.processed = true;
                for(Link la = position.links.getFirst(); la != null;) {
                    Link linkA = la;
                    Position nextPosition = linkA.nextPosition;
                    while(la != null && la.nextPosition == linkA.nextPosition) la = la.position.getNextLinear();
                    
                    bool forwardSelf = false;
                    bool forwardAlien = false;
                    Link forwardLink = null;
                    bool backwardSelf = false;
                    bool backwardAlien = false;
                    Link backwardLink = null;

                    for(Link lb = linkA; lb != null;) {
                        Link linkB = lb;
                        lb = lb.position.getNextLinear();
                        if (linkB.nextPosition == nextPosition) {
                            if (linkB.alien) {
                                forwardAlien = true;
                                if (linkB.inside) forwardSelf = backwardSelf = true;
                            } else {
                                forwardSelf = true;
                                if (linkB.inside) forwardAlien = backwardAlien = true;
                            }
                            if (forwardLink == null) forwardLink = linkB; else removeLink(linkB);
                        }
                    }

                    if (!nextPosition.processed) {
                        for(Link lb = nextPosition.links.getFirst(); lb != null;) {
                            Link linkB = lb;
                            lb = lb.position.getNextLinear();
                            if (linkB.nextPosition == position) {
                                if (linkB.alien) {
                                    backwardAlien = true;
                                    if (linkB.inside) forwardSelf = backwardSelf = true;
                                } else {
                                    backwardSelf = true;
                                    if (linkB.inside) forwardAlien = backwardAlien = true;
                                }
                                if (backwardLink == null) backwardLink = linkB; else removeLink(linkB);
                            }
                        }
                    }
                    
                    bool forward = combine(mode, forwardSelf, forwardAlien);
                    bool backward = combine(mode, backwardSelf, backwardAlien);
                    if (forward && backward) forward = backward = false;
                    if (!forward && forwardLink != null) removeLink(forwardLink);
                    if (!backward && backwardLink != null) removeLink(backwardLink);
                    if (forward && forwardLink == null) {
                        forwardLink = new Link();
                        forwardLink.position.insertBack(position.links);
                        forwardLink.nextPosition = nextPosition;
                        links.Add(forwardLink);
                        log.linkAdded(forwardLink);
                    }
                    if (backward && backwardLink == null) {
                        backwardLink = new Link();
                        backwardLink.position.insertBack(nextPosition.links);
                        backwardLink.nextPosition = position;
                        links.Add(backwardLink);
                        log.linkAdded(backwardLink);
                    }
                }
            }
        }
        
        void buildContours() {
            for(int i = 0; i < links.Count; ++i) {
                if (links[i].contour.getParent() == null) {
                    Contour contour = new Contour();
                    contours.Add(contour);
                    
                    Link link = links[i];
                    do {
                        if (link.contour.getParent() != null)
                            throw new Exception();
                        link.contour.insertBack(contour.links);
                        
                        // find first link CW order (so we turns left)
                        Link nextLink = link.nextPosition.links.getFirst();
                        for(Link l = nextLink.position.getNext(); l != null; l = l.position.getNextLinear())
                            if ( Geometry.isCCW(
                                    link.nextPosition.point,
                                    link.position.getParent().point,
                                    nextLink.nextPosition.point,
                                    l.nextPosition.point ))
                                { nextLink = l; }
                        
                        // select first link by the left (links should be CCW-ordered)
                        link = nextLink;
                    } while (link != links[i]);
                }
            }
        }

        void concatenateLinks() {
            foreach(Contour contour in contours) {
                for(Link linkA = contour.links.getFirst(); linkA != null;) {
                    Link linkB = linkA.contour.getNext();
                    if ( Geometry.isPointAtLine(
                            linkA.nextPosition.point,
                            linkA.position.getParent().point,
                            linkB.nextPosition.point ))
                    {
                        linkA.nextPosition = linkB.nextPosition;
                        removeLink(linkB);
                        if (linkA == linkB) break;
                    } else {
                        linkA = linkA.contour.getNextLinear();
                    }
                }
            }
        }

        // Normally we should not have free links after tracing
        void removeFreeLinks() {
            for(int i = 0; i < links.Count; ++i)
                if (links[i].contour.getParent() == null)
                    throw new Exception();
        }

        int countIntersectionsWithContour(Point p0, Point p1, Contour contour) {
            int count = 0;
            for(Link link = contour.links.getFirst(); link != null; link = link.contour.getNextLinear()) {
                Point pp0 = link.position.getParent().point;
                Point pp1 = link.contour.getNext().position.getParent().point;
                int d = Math.Sign( (long)(p0.Y - p1.Y)*(long)(pp1.X - pp0.X)
                                 + (long)(p1.X - p0.X)*(long)(pp1.Y - pp0.Y) );
                Point c;
                switch(Geometry.findIntersection(p0, p1, pp0, pp1, out c)) {
                case Geometry.IntersectionType.Cross:
                    count += 2*d;
                    break;
                case Geometry.IntersectionType.Touch_b0:
                case Geometry.IntersectionType.Touch_b1:
                    count += d;
                    break;
                default:
                    break;
                }
            }
            return count;
        }

        bool isContourInverted(Contour contour) {
            Link first = contour.links.getFirst();
            Point p0 = first.position.getParent().point;
            Point p1 = first.contour.getNext().position.getParent().point;
            Geometry.makeLongestLine(p0, ref p1);
            return countIntersectionsWithContour(p0, p1, contour) < 0;
        }
        
        bool isContourInside(Contour inner, Contour outer) {
            Link first = inner.links.getFirst();
            Point p0 = first.position.getParent().point;
            Point p1 = first.contour.getNext().position.getParent().point;
            Geometry.makeLongestLine(p0, ref p1);
            return countIntersectionsWithContour(p0, p1, outer) != 0;
        }
        
        void organizeChildContours(Contour contour, Contour parent) {
            // set parent
            contour.parent = parent;

            // remove sub-childs from parent list
            if (parent != null)
                foreach(Contour c in contour.childs)
                    parent.childs.Remove(c);

            // sub-calls
            foreach(Contour c in contour.childs)
                organizeChildContours(c, contour);
        }
        
        void buildContoursHierarhy() {
            // calculate directions of contours
            foreach(Contour contour in contours)
                contour.inverted = isContourInverted(contour);
                
            // find childs
            rootContours.AddRange(contours);
            for(int i = 0; i < contours.Count; ++i) {
                for(int j = i+1; j < contours.Count; ++j) {
                    if (isContourInside(contours[i], contours[j])) {
                        contours[j].childs.Add(contours[i]);
                        rootContours.Remove(contours[i]);
                    } else
                    if (isContourInside(contours[j], contours[i])) {
                        contours[i].childs.Add(contours[j]);
                        rootContours.Remove(contours[j]);
                    }
                }
            }
            
            // organize childs
            foreach(Contour c in rootContours)
                organizeChildContours(c, null);
           
            // remove invisible contours
            /*
            for(int i = 0; i < contours.Count; ++i) {
                bool parentInverted = contours[i].parent == null || contours[i].parent.inverted;
                if (parentInverted == contours[i].inverted) {
                    // remove contour
                    foreach(Contour c in contours[i].childs)
                        c.parent = contours[i].parent;
                    List<Contour> parentList = contours[i].parent == null
                                             ? rootContours
                                             : contours[i].parent.childs;
                    parentList.AddRange(contours[i].childs);
                    
                    contours[i].parent = null;
                    contours[i].childs.Clear();
                    while(!contours[i].links.empty())
                        removeLink(contours[i].links.getFirst());
                    
                    contours.RemoveAt(i--);
                }
            }
            */

            // move contours in the holes to root
            for(int i = 0; i < rootContours.Count; ++i) {
                Contour contourA = rootContours[i];
                foreach(Contour contourB in contourA.childs) {
                    if (contourB.childs.Count != 0) {
                        foreach(Contour c in contourB.childs)
                            c.parent = null;
                        rootContours.AddRange(contourB.childs);
                    }
                }
            }
        }

        void calculate(CombinationMode combinationMode) {
            log.state("calculation begin");
        
            resetTraceInformation();
            findIntersections();
            log.state("intersections solved");

            findLinksInside();
            log.state("inside found");

            removeUnusedLinks(combinationMode);
            log.state("unused links removed");

            buildContours();
            removeFreeLinks();
            concatenateLinks();
            log.state("links concatenated");

            buildContoursHierarhy();
            removeEmptyPositions();

            log.state("calculation complete");
        }
        
        public static Shape combine(CombinationMode mode, Shape a, Shape b) {
            Shape sum = new Shape();
            sum.log.enabled = a.log.enabled && b.log.enabled;

            sum.log.line(string.Format("---- combine {0} begin ----", mode));

            // clone a
            for(int i = 0; i < a.positions.Count; ++i)
                sum.positions.Add(new Position(a.positions[i].point));
            for(int i = 0; i < a.positions.Count; ++i) {
                for(Link link = a.positions[i].links.getFirst(); link != null; link = link.position.getNextLinear()) {
                    Link l = new Link();
                    l.alien = false;
                    l.nextPosition = sum.positions[a.positions.IndexOf(link.nextPosition)];
                    l.position.insertBack(sum.positions[i].links);
                    sum.links.Add(l);
                }
            }
             
            // clone b
            for(int i = 0; i < b.positions.Count; ++i)
                sum.positions.Add(new Position(b.positions[i].point));
            for(int i = 0; i < b.positions.Count; ++i) {
                for(Link link = b.positions[i].links.getFirst(); link != null; link = link.position.getNextLinear()) {
                    Link l = new Link();
                    l.alien = true;
                    l.nextPosition = sum.positions[a.positions.Count + b.positions.IndexOf(link.nextPosition)];
                    l.position.insertBack(sum.positions[a.positions.Count + i].links);
                    sum.links.Add(l);
                }
            }
            
            sum.calculate(mode);

            sum.log.line("---- combine end ----");

            return sum;
        }
        
        public static Shape add(Shape a, Shape b)
            { return combine(CombinationMode.Add, a, b); }

        public static Shape subtract(Shape a, Shape b)
            { return combine(CombinationMode.Subtract, a, b); }

        public static Shape intersection(Shape a, Shape b)
            { return combine(CombinationMode.Intersection, a, b); }

        public static Shape xor(Shape a, Shape b)
            { return combine(CombinationMode.Xor, a, b); }
    }
}

