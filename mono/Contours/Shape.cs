using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

namespace Contours {
    public class Shape {
        class Exception: System.Exception { }
    
        class Position {
            public Point point;
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

            public readonly Circuit<Contour, Link> forward;
            public readonly Circuit<Contour, Link> backward;

            public Contour() {
                forward = new Circuit<Contour, Link>(this);
                backward = new Circuit<Contour, Link>(this);
            }
        }

        class Link {
            public bool forward = false;
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
                link.forward = forward;
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
                    "{0,2}# {1} {2} {3}",
                    shape.links.IndexOf(link),
                    link.forward ? "->" : "<-",
                    toString(link.position.getParent()),
                    toString(link.nextPosition) );
            }
            
            public void line(string line) {
                if (!enabled) return;
                System.IO.File.AppendAllLines(filename, new string[] { line });
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
                    Link firstForward = null;
                    Link firstBackward = null;
                    Link previousForward = null;
                    Link previousBackward = null;
                    foreach(Point point in contour) {
                        Position position = new Position(point);
                        positions.Add(position);

                        Link forward = new Link();
                        forward.forward = true;
                        forward.position.insertBack(position.links);
                        if (previousForward != null)
                            previousForward.nextPosition = forward.position.getParent();
                        links.Add(forward);
                        
                        Link backward = new Link();
                        backward.forward = false;
                        backward.position.insertBack(position.links);
                        if (previousBackward != null)
                            backward.nextPosition = previousBackward.position.getParent();
                        links.Add(backward);
                        
                        if (firstForward == null) firstForward = forward;
                        if (firstBackward == null) firstBackward = backward;
                        previousForward = forward;
                        previousBackward = backward;
                    }
                    previousForward.nextPosition = firstForward.position.getParent();
                    firstBackward.nextPosition = previousBackward.position.getParent();
                }
            }
            
            calculate(true);

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
            for(Link link = contour.forward.getFirst(); link != null; link = link.contour.getNextLinear())
                list.Add(link.position.getParent().point);
            return list;
        }

        void resetTraceInformation() {
            for(int i = 0; i < contours.Count; ++i) {
                contours[i].forward.clear();
                contours[i].backward.clear();
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
                            while(positions[j].links.getFirst() != null) {
                                Link link = positions[j].links.getFirst();
                                for(Link l = link.nextPosition.links.getFirst(); l != null; l = l.position.getNextLinear())
                                    if (l.nextPosition == positions[j])
                                        l.nextPosition = positions[i];
                                link.position.insertBack(positions[i].links);
                            }
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
        
        // This function removes link from it position
        // and also recursive removes part of contour to nearest fork
        void removeLinkFromPosition(Link link) {
            Position position = link.position.getParent();
            Link nextToRemove = null;
            
            // remove back link
            if (link.nextPosition != null) {
                Link backLink = link.nextPosition.links.getFirst();
                while (backLink != null) {
                    if ( backLink.forward != link.forward
                      && backLink.nextPosition == position )
                    {
                        removeLink(backLink);
                        break;
                    }
                    backLink = backLink.position.getNextLinear();
                }
                
                if (link.nextPosition.links.getCount() == 1)
                    nextToRemove = link.nextPosition.links.getFirst();
            }

            // remove
            removeLink(link);

            // remove next
            if (nextToRemove != null)
                removeLinkFromPosition(nextToRemove);
        }
        
        // Before trace contours we can (and should) to do some actions
        // to optimize links in each vertes.
        // So we can remove duplicated contour chunks
        // and chunks which placed inside contours of similar type
        // and not affecting to final image
        void optimizePositions() {
            // remove duplicates
            log.line("remove duplicates");
            foreach(Position position in positions) {
                for(Link a = position.links.getFirst(); a != null; a = a.position.getNextLinear()) {
                    Position otherPosition = a.nextPosition;
    
                    // count forward and backward links
                    int count = 0;
                    Link forwardLink = null;
                    Link backwardLink = null;
                    Link b = a;
                    do {
                        if (b.nextPosition == otherPosition) {
                            if (b.forward) { forwardLink = b; ++count; }
                                      else { backwardLink = b; --count; }
                        }
                        b.position.getNext();
                    } while(b != a);
    
                    // remove extra links
                    Link linkToSave = count > 0 ? forwardLink
                                    : count < 0 ? backwardLink
                                    : null;
                    b = position.links.getFirst();
                    while(b != null) {
                        Link l = b;
                        b = b.position.getNextLinear();
                        if (l.nextPosition == otherPosition && l != linkToSave) {
                            if (a == l) a = position.links.getFirst();
                            removeLink(l);
                        }
                    }
                    
                    if (position.links.empty()) break;
                }
            }

            log.state("duplicates removed");
            foreach(Position position in positions) {
                if (position.links.getCount() >= 3) {
                    log.line("sort links");
                    log.positionState(position);
                    
                    // sort links
                    Link first = position.links.getFirst();
                    Link a = first;
                    while (true) {
                        Link b = a.position.getNext();
                        Link c = b.position.getNext();
                        if ( !Geometry.isCCW(
                                position.point,
                                a.nextPosition.point,
                                b.nextPosition.point,
                                c.nextPosition.point ))
                        {
                            log.linkSwapped(b, c);
                            b.position.swapWith(c.position);
                            first = a = c;
                            continue;
                        }
                        a = b;
                        if (a == first) break;
                    };
                    log.positionState(position);
                    
                    // remove invisible contours from position
                    a = first = position.links.getFirst();
                    while(true) {
                        bool previous = a.position.getPrevious().forward;
                        bool current = a.forward;
                        bool next = a.position.getNext().forward;
                        if ( (previous &&  current && !next)
                          || (previous && !current && !next) )
                        {
                            // remove link
                            removeLinkFromPosition(a);
                            if (position.links.getCount() < 3) break;
                            a = first = position.links.getFirst();
                        } else {
                            a = a.position.getNext();
                            if (a == first) break;
                        }
                    };
                    log.positionState(position);
                    log.line("sorted");
                }
            }
        }
        
        void buildContours() {
            for(int i = 0; i < links.Count; ++i) {
                if (links[i].forward && links[i].contour.getParent() == null) {
                    Contour contour = new Contour();
                    contours.Add(contour);
                    
                    Link forwardLink = links[i];
                    Link backwardLink = null;

                    do {
                        if (forwardLink == null || !forwardLink.forward || forwardLink.contour.getParent() != null)
                            throw new Exception();
                    
                        // find pair
                        for(Link l = forwardLink.nextPosition.links.getFirst(); l != null; l = l.position.getNextLinear())
                            if (l.nextPosition == forwardLink.position.getParent())
                                { backwardLink = l; break; }
                        if (backwardLink == null || backwardLink.forward || backwardLink.contour.getParent() != null)
                            throw new Exception();
                        
                        forwardLink.contour.insertBack(contour.forward);
                        backwardLink.contour.insertBack(contour.backward);
                        
                        // select first link by the left (links should be CCW-ordered)
                        forwardLink = backwardLink.position.getNext();
                    } while (forwardLink != links[i]);
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
            for(Link link = contour.forward.getFirst(); link != null; link = link.contour.getNextLinear()) {
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
                    count += d;
                    break;
                case Geometry.IntersectionType.Touch_b1:
                    count -= d;
                    break;
                default:
                    break;
                }
            }
            return count;
        }

        bool isContourInverted(Contour contour) {
            Link first = contour.forward.getFirst();
            Point p0 = first.position.getParent().point;
            Point p1 = first.contour.getNext().position.getParent().point;
            Geometry.makeLongestLine(p0, ref p1);
            return countIntersectionsWithContour(p0, p1, contour) < 0;
        }
        
        bool isContourInside(Contour inner, Contour outer) {
            Link first = inner.forward.getFirst();
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
        
        void invertContour(Contour contour) {
            contour.inverted = !contour.inverted;
            contour.forward.swapWith(contour.backward);
            for(Link link = contour.forward.getFirst(); link != null; link.contour.getNextLinear())
                link.forward = !link.forward;
            for(Link link = contour.backward.getFirst(); link != null; link.contour.getNextLinear())
                link.forward = !link.forward;
        }
        
        void buildContoursHierarhy(bool simple) {
            // calculate directions of contours
            foreach(Contour contour in contours)
                contour.inverted = isContourInverted(contour);
                
            // find childs
            rootContours.AddRange(contours);
            for(int i = 0; i < contours.Count; ++i) {
                for(int j = 0; j < contours.Count; ++j) {
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
            for(int i = 0; i < contours.Count; ++i) {
                bool parentInverted = contours[i].parent == null || contours[i].parent.inverted;
                if (parentInverted == contours[i].inverted) {
                    if (simple) {
                        // if simple then invert invisible contours instead of removing
                        invertContour(contours[i]);
                    } else {
                        // remove contour
                        foreach(Contour c in contours[i].childs)
                            c.parent = contours[i].parent;
                        List<Contour> parentList = contours[i].parent == null
                                                 ? rootContours
                                                 : contours[i].parent.childs;
                        parentList.AddRange(contours[i].childs);
                        
                        contours[i].parent = null;
                        contours[i].childs.Clear();
                        while(!contours[i].forward.empty())
                            removeLink(contours[i].forward.getFirst());
                        while(!contours[i].backward.empty())
                            removeLink(contours[i].backward.getFirst());
                        
                        contours.RemoveAt(i--);
                    }
                }
            }

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

        void calculate(bool simple) {
            log.state("calculation begin");
        
            resetTraceInformation();
            findIntersections();
            log.state("intersections solved");

            optimizePositions();

            buildContours();
            removeFreeLinks();
            buildContoursHierarhy(simple);
            removeEmptyPositions();

            log.state("calculation complete");
        }
        
        public void invert() {
            foreach(Contour contour in contours)
                invertContour(contour);
        }
        
        static Shape mix(Shape a, bool invertA, Shape b, bool invertB) {
            Shape sum = new Shape();
            sum.log.enabled = a.log.enabled && b.log.enabled;

            sum.log.line("---- mix begin ----");

            // clone a
            for(int i = 0; i < a.positions.Count; ++i)
                sum.positions.Add(new Position(a.positions[i].point));
            for(int i = 0; i < a.positions.Count; ++i) {
                for(Link link = a.positions[i].links.getFirst(); link != null; link = link.position.getNextLinear()) {
                    Link l = new Link();
                    l.forward = invertA != link.forward;
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
                    l.forward = invertB != link.forward;
                    l.nextPosition = sum.positions[a.positions.Count + b.positions.IndexOf(link.nextPosition)];
                    l.position.insertBack(sum.positions[a.positions.Count + i].links);
                    sum.links.Add(l);
                }
            }
            
            sum.calculate(false);

            sum.log.line("---- mix end ----");

            return sum;
        }
        
        public static Shape add(Shape a, Shape b) {
            return mix(a, false, b, false);
        }

        public static Shape subtract(Shape a, Shape b) {
            return mix(a, false, b, true);
        }

        public static Shape xor(Shape a, Shape b) {
            return add(subtract(a, b), subtract(b, a));
        }

        public static Shape intersection(Shape a, Shape b) {
            return subtract(add(a, b), xor(a, b));
        }
    }
}

