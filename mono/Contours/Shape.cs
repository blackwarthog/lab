using System;
using System.Collections.Generic;
using System.Linq;

namespace Contours {
    public enum IntersectionType {
        None,
        Cross,
        Identical,
        Inverted,
        Touch_a0,
        Touch_a1,
        Touch_b0,
        Touch_b1,
        Touch_a0_b0,
        Touch_a0_b1,
        Touch_a1_b0,
        Touch_a1_b1,
        Along_a0_b0_a1_b1,
        Along_a0_b0_b1_a1,
        Along_a0_b1_a1_b0,
        Along_a0_b1_b0_a1,
        Along_b0_a0_a1_b1,
        Along_b0_a0_b1_a1,
        Along_b1_a0_a1_b0,
        Along_b1_a0_b0_a1
    }

    public class Circuit<Parent, Child> where Parent: class where Child: class {
        public class Entry {
            readonly Child owner;
            Circuit<Parent, Child> circuit;
            Entry previous;
            Entry next;

            public Entry(Child owner) { this.owner = owner; }

            public Child getOwner() { return owner; }
            public Circuit<Parent, Child> getCircuit() { return circuit; }
            public Parent getParent() {
                Circuit<Parent, Child> circuit = getCircuit();
                return circuit == null ? null : circuit.getOwner();
            }

            public Entry getPreviousEntry() { return previous; }
            public Entry getNextEntry() { return next; }

            public Entry getPreviousEntryLinear() {
                Entry e = getPreviousEntry();
                return e == null || e == circuit.last ? null : e;
            }
            public Entry getNextEntryLinear() {
                Entry e = getNextEntry();
                return e == null || e == circuit.first ? null : e;
            }

            public Child getPrevious() {
                Entry e = getPreviousEntry();
                return e == null ? null : e.getOwner();
            }
            public Child getNext() {
                Entry e = getNextEntry();
                return e == null ? null : e.getOwner();
            }

            public Child getPreviousLinear() {
                Entry e = getPreviousEntryLinear();
                return e == null ? null : e.getOwner();
            }
            public Child getNextLinear() {
                Entry e = getNextEntryLinear();
                return e == null ? null : e.getOwner();
            }

            public void unlink() {
                if (previous != null) previous.next = next;
                if (next != null) next.previous = previous;
                
                if (circuit != null) {
                    if (circuit.first == this) {
                        circuit.first = next != this ? next : null;
                        circuit.last = previous != this ? previous : null;
                    }
                    --circuit.count;
                    circuit = null;
                }
                previous = null;
                next = null;
            }

            public void insertBack(Circuit<Parent, Child> circuit) {
                unlink();
                if (circuit != null) {
                    if (circuit.empty()) {
                        this.circuit = circuit;
                        previous = next = this;
                        circuit.first = circuit.last = this;
                        ++circuit.count;
                    } else {
                        insertAfterOf(circuit.getLastEntry());
                    }
                }
            }

            public void insertFront(Circuit<Parent, Child> circuit) {
                unlink();
                if (circuit != null) {
                    if (circuit.empty()) {
                        this.circuit = circuit;
                        previous = next = this;
                        circuit.first = circuit.last = this;
                        ++circuit.count;
                    } else {
                        insertBeforeOf(circuit.getFirstEntry());
                    }
                }
            }

            public void insertAfterOf(Entry entry) {
                if (entry == this) return;
                unlink();
                if (entry == null || entry.getCircuit() == null) return;
                
                previous = entry;
                next = entry.next;
                previous.next = this;
                if (next != null) next.previous = this;
                circuit = entry.getCircuit();

                if (circuit != null) {
                    if (circuit.getLastEntry() == entry)
                        circuit.last = this;
                    ++circuit.count;
               }
            }

            public void insertBeforeOf(Entry entry) {
                if (entry == this) return;
                unlink();
                if (entry == null || entry.getCircuit() == null) return;
                
                previous = entry.previous;
                next = entry;
                if (previous != null) previous.next = this;
                next.previous = this;
                circuit = entry.getCircuit();

                if (circuit != null) {
                    if (circuit.getFirstEntry() == entry)
                        circuit.first = this;
                    ++circuit.count;
                }
            }

            public void swapWith(Entry other) {
                if (other == this || other == null) return;

                Circuit<Parent, Child> otherCircuit = other.circuit;
                Entry otherPrevious = other.previous;
                Entry otherNext = other.next;

                other.circuit = circuit;
                other.previous = previous;
                other.next = next;

                if (otherCircuit != null) {
                    if (otherCircuit.first == other)
                        otherCircuit.first = this;
                    if (otherCircuit.last == other)
                        otherCircuit.last = this;
                }

                if (circuit != null) {
                    if (circuit.first == this)
                        circuit.first = other;
                    if (circuit.last == this)
                        circuit.last = other;
                }

                circuit = otherCircuit;
                previous = otherPrevious;
                next = otherNext;
            }
        }
        
        readonly Parent owner;
        Entry first;
        Entry last;
        int count = 0;

        public Circuit(Parent owner) { this.owner = owner; }

        public Parent getOwner() { return owner; }
        public int getCount() { return count; }

        public Entry getFirstEntry() { return first; }
        public Entry getLastEntry() { return last; }

        public Child getFirst() {
            Entry e = getFirstEntry();
            return e == null ? null : e.getOwner();
        }
        public Child getLast() {
            Entry e = getLastEntry();
            return e == null ? null : e.getOwner();
        }

        public bool empty() { return getFirstEntry() == null; }

        public void clear() {
            while(!empty()) getFirstEntry().unlink();
        }
    }

    public class Shape {
        public class Position {
            public int x = 0;
            public int y = 0;
            public readonly Circuit<Shape, Position>.Entry shape;
            public readonly Circuit<Position, Link> links;

            public Position() {
                shape = new Circuit<Shape, Position>.Entry(this);
                links = new Circuit<Position, Link>(this);
            }

            public Position(int x, int y): this() { this.x = x; this.y = y; }
            
            public VectorInt toVectorInt() { return new VectorInt(x, y); }
        }

        public class Contour {
            public bool inverted = false;
            
            public Contour parent;
            public List<Contour> childs;

            public readonly Circuit<Shape, Contour>.Entry shape;
            public readonly Circuit<Contour, Link> forward;
            public readonly Circuit<Contour, Link> backward;

            public Contour() {
                shape = new Circuit<Shape, Contour>.Entry(this);
                forward = new Circuit<Contour, Link>(this);
                backward = new Circuit<Contour, Link>(this);
            }
        }

        public class Link {
            public readonly Circuit<Shape, Link>.Entry shape;
            public readonly Circuit<Position, Link>.Entry position;
            public readonly Circuit<Contour, Link>.Entry contour;
            
            public bool forward = false;
            public Position target = null;

            public Link() {
                shape = new Circuit<Shape, Link>.Entry(this);
                position = new Circuit<Position, Link>.Entry(this);
                contour = new Circuit<Contour, Link>.Entry(this);
            }

            public void unlink() {
                contour.unlink();
                position.unlink();
                shape.unlink();
                target = null;
            }

            public static Link create(Position position, Circuit<Contour, Link> contourCircuit) {
                Link link = new Link();
                link.shape.insertBack(contourCircuit.getOwner().shape.getParent().links);
                link.position.insertBack(position.links);
                link.contour.insertBack(contourCircuit);
                link.forward = contourCircuit == contourCircuit.getOwner().forward;
                return link;
            }

            public Link createSplitAfter(Position position) {
                Link link = new Link();
                link.forward = forward;
                link.shape.insertBack(shape.getParent().links);
                link.position.insertBack(position.links);
                link.contour.insertAfterOf(contour);
                return link;
            }

            public Link createSplitAfter(Circuit<Position, Link>.Entry position) {
                Link link = new Link();
                link.forward = forward;
                link.shape.insertBack(shape.getParent().links);
                link.position.insertAfterOf(position);
                link.contour.insertAfterOf(contour);
                return link;
            }
        }

        public Circuit<Shape, Position> positions;
        public Circuit<Shape, Link> links;
        public Circuit<Shape, Contour> contours;
        public List<Contour> rootContours;

        public void addContours(ContourInt contours) {
            foreach(List<VectorInt> contour in contours.contours)
                addContour(contour);
        }

        public void addContour(ICollection<VectorInt> points) {
            if (points.Count < 3)
                return;

            Contour contour = new Contour();
            contour.shape.insertBack(contours);
            foreach(VectorInt point in points) {
                Position position = new Position(point.x, point.y);
                position.shape.insertBack(positions);
                Link.create(position, contour.forward);
                Link.create(position, contour.backward);
            }
        }

        public static int compare(VectorInt a0, VectorInt a1, VectorInt base0, VectorInt base1) {
            if (base0.x < base1.x && a0.x < a1.x) return -1;
            if (base0.x < base1.x && a1.x < a0.x) return  1;
            if (base1.x < base0.x && a0.x < a1.x) return  1;
            if (base1.x < base0.x && a1.x < a0.x) return -1;
            if (base0.y < base1.x && a0.y < a1.x) return -1;
            if (base0.y < base1.x && a1.y < a0.x) return  1;
            if (base1.y < base0.x && a0.y < a1.x) return  1;
            if (base1.y < base0.x && a1.y < a0.x) return -1;
            return 0;
        }

        public static IntersectionType findIntersection(VectorInt a0, VectorInt a1, VectorInt b0, VectorInt b1, out VectorInt c) {
            c = new VectorInt();
            VectorInt da = new VectorInt(a1.x - a0.x, a1.y - a0.y);
            VectorInt db = new VectorInt(b1.x - b0.x, b1.y - b0.y);

            if (a0.x == b0.x && a0.y == b0.y && a1.x == b1.x && a1.y == b1.y)
                return IntersectionType.Identical;

            if (a0.x == b1.x && a0.y == b1.y && a1.x == b0.x && a1.y == b0.y)
                return IntersectionType.Inverted;

            long divider = (long)da.x*(long)db.y - (long)db.x*(long)da.y;
            if (divider == 0) {
                if ((long)da.x*(long)(b0.y - a0.y) != (long)da.y*(long)(b0.x - a0.x))
                    return IntersectionType.None;

                int a0b0 = compare(a0, b0, a0, a1);
                int a0b1 = compare(a0, b1, a0, a1);
                int a1b0 = compare(a1, b0, a0, a1);
                int a1b1 = compare(a1, b1, a0, a1);
                int b0b1 = compare(b0, b1, a0, a1);
                int b0a0 = -a0b0;
                int b0a1 = -a1b0;
                int b1a0 = -a0b1;
                int b1a1 = -a1b1;
                int b1b0 = -b0b1;

                // a0a1b0b1
                if (a1b0 == 0 && b0b1 < 0)
                    return IntersectionType.Touch_a1_b0;
                // a0a1b1b0
                if (a1b1 == 0 && b1b0 < 0)
                    return IntersectionType.Touch_a1_b1;
                // b0b1a0a1
                if (b0b1 < 0 && b1a0 == 0)
                    return IntersectionType.Touch_a0_b1;
                // b1b0a0a1
                if (b1b0 < 0 && b0a0 == 0)
                    return IntersectionType.Touch_a0_b0;

                if (a0b0 <= 0 && b0a1 <= 0 && a1b1 <= 0)
                    return IntersectionType.Along_a0_b0_a1_b1;
                if (a0b0 <= 0 && b0b1 <= 0 && b1a1 <= 0)
                    return IntersectionType.Along_a0_b0_b1_a1;
                if (a0b1 <= 0 && b1a1 <= 0 && a1b0 <= 0)
                    return IntersectionType.Along_a0_b1_a1_b0;
                if (a0b1 <= 0 && b1b0 <= 0 && b0a1 <= 0)
                    return IntersectionType.Along_a0_b1_b0_a1;
                if (b0a0 <= 0 && /*  a0a1  */ a1b1 <= 0)
                    return IntersectionType.Along_b0_a0_a1_b1;
                if (b0a0 <= 0 && a0b1 <= 0 && b1a1 <= 0)
                    return IntersectionType.Along_b0_a0_b1_a1;
                if (b1a0 <= 0 && /*  a0a1  */ a1b0 <= 0)
                    return IntersectionType.Along_b1_a0_a1_b0;
                if (b1a0 <= 0 && a0b0 <= 0 && b0a1 <= 0)
                    return IntersectionType.Along_b1_a0_b0_a1;

                return IntersectionType.None;
            }

            if (a0.x == b0.x && a0.y == b0.y)
                return IntersectionType.Touch_a0_b0;
            if (a0.x == b1.x && a0.y == b1.y)
                return IntersectionType.Touch_a0_b1;
            if (a1.x == b0.x && a1.y == b0.y)
                return IntersectionType.Touch_a1_b0;
            if (a1.x == b1.x && a1.y == b1.y)
                return IntersectionType.Touch_a1_b1;

            long numeratorX = (long)da.x*((long)b1.y*(long)b0.x - (long)b0.y*(long)b1.x)
                            - (long)db.x*((long)a1.y*(long)a0.x - (long)a0.y*(long)a1.x);
            long numeratorY = (long)db.y*((long)a1.x*(long)a0.y - (long)a0.x*(long)a1.y)
                            - (long)da.y*((long)b1.x*(long)b0.y - (long)b0.x*(long)b1.y);
            VectorInt p = new VectorInt((int)(numeratorX/divider), (int)(numeratorY/divider));
            if (compare(p, a0, a0, a1) < 0 || compare(p, a1, a0, a1) > 0)
                return IntersectionType.None;

            if (p.x == a0.x && p.y == a0.y)
                return IntersectionType.Touch_a0;
            if (p.x == a1.x && p.y == a1.y)
                return IntersectionType.Touch_a1;
            if (p.x == b0.x && p.y == b0.y)
                return IntersectionType.Touch_b0;
            if (p.x == b1.x && p.y == b1.y)
                return IntersectionType.Touch_b1;

            c = p;
            return IntersectionType.Cross;
        }

        public bool removeEmptyContours() {
            bool removed = false;
        
            bool retry = true;
            while(retry) {
                retry = false;
                for(Contour contour = contours.getFirst(); !retry && contour != null; contour = contour.shape.getNextLinear()) {
                    if (contour.forward.getCount() < 3 || contour.backward.getCount() < 3) {
                        while(contour.forward.getFirst() != null)
                            contour.forward.getFirst().unlink();
                        while(contour.backward.getFirst() != null)
                            contour.backward.getFirst().unlink();
                        contour.shape.unlink();
                        retry = true;
                        removed = true;
                        break;
                    }
                }
                if (retry) continue;
            }
        }

        public void findIntersections() {
            bool retry = true;
            while(retry) {
                retry = false;

                retry = removeEmptyContours();
                if (retry) continue;

                // remove empty positions
                for(Position position = positions.getFirst(); !retry && position != null; position = position.shape.getNextLinear()) {
                    if (position.links.empty()) {
                        position.shape.unlink();
                        retry = true;
                        break;
                    }
                }
                if (retry) continue;

                // merge positions
                for(Position positionA = positions.getFirst(); !retry && positionA != null; positionA = positionA.shape.getNextLinear()) {
                    for(Position positionB = positionA.shape.getNextLinear(); !retry && positionB != null; positionB = positionB.shape.getNextLinear()) {
                        if (positionA.x == positionB.x && positionA.y == positionB.y) {
                            while(positionB.links.getFirst() != null)
                                positionB.links.getFirst().position.insertBack(positionA.links);
                            positionB.shape.unlink();
                            retry = true;
                            break;
                        }
                    }
                }
                if (retry) continue;

                // remove zero-length links
                for(Link linkA0 = links.getFirst(); !retry && linkA0 != null; linkA0 = linkA0.shape.getNextLinear()) {
                    Link linkA1 = linkA0.contour.getNext();
                    if (linkA0.position.getParent() == linkA1.position.getParent()) {
                        linkA1.unlink();
                        retry = true;
                        break;
                    }
                }
                if (retry) continue;

                // check intersections
                for(Link linkA0 = links.getFirst(); !retry && linkA0 != null; linkA0 = linkA0.shape.getNextLinear()) {
                    Link linkA1 = linkA0.contour.getNext();
                    for(Link linkB0 = links.getFirst(); !retry && linkB0 != null; linkB0 = linkB0.shape.getNextLinear()) {
                        Link linkB1 = linkB0.contour.getNext();
                        VectorInt cross = new VectorInt(0, 0);
                        Position position;
                        retry = true;
                        switch( findIntersection( linkA0.position.getParent().toVectorInt(),
                                                  linkA1.position.getParent().toVectorInt(),
                                                  linkB0.position.getParent().toVectorInt(),
                                                  linkB1.position.getParent().toVectorInt(),
                                                  out cross ))
                        {
                        case IntersectionType.Cross:
                            position = new Position(cross.x, cross.y);
                            position.shape.insertBack(positions);
                            linkA0.createSplitAfter(position);
                            linkB0.createSplitAfter(position);
                            break;
                        case IntersectionType.Touch_a0:
                            linkB0.createSplitAfter(linkA0.position);
                            break;
                        case IntersectionType.Touch_a1:
                            linkB0.createSplitAfter(linkA1.position);
                            break;
                        case IntersectionType.Touch_b0:
                            linkA0.createSplitAfter(linkB0.position);
                            break;
                        case IntersectionType.Touch_b1:
                            linkA0.createSplitAfter(linkB1.position);
                            break;
                        case IntersectionType.Along_a0_b0_a1_b1:
                            linkA0.createSplitAfter(linkB0.position);
                            linkB0.createSplitAfter(linkA1.position);
                            break;
                        case IntersectionType.Along_a0_b0_b1_a1:
                            linkA0.createSplitAfter(linkB0.position)
                                  .createSplitAfter(linkB1.position);
                            break;
                        case IntersectionType.Along_a0_b1_a1_b0:
                            linkA0.createSplitAfter(linkB1.position);
                            linkB0.createSplitAfter(linkA1.position);
                            break;
                        case IntersectionType.Along_a0_b1_b0_a1:
                            linkA0.createSplitAfter(linkB1.position)
                                  .createSplitAfter(linkB0.position);
                            break;
                        case IntersectionType.Along_b0_a0_a1_b1:
                            linkB0.createSplitAfter(linkA0.position)
                                  .createSplitAfter(linkA1.position);
                            break;
                        case IntersectionType.Along_b0_a0_b1_a1:
                            linkA0.createSplitAfter(linkB1.position);
                            linkB0.createSplitAfter(linkA0.position);
                            break;
                        case IntersectionType.Along_b1_a0_a1_b0:
                            linkB0.createSplitAfter(linkA1.position)
                                  .createSplitAfter(linkA0.position);
                            break;
                        case IntersectionType.Along_b1_a0_b0_a1:
                            linkA0.createSplitAfter(linkB0.position);
                            linkB0.createSplitAfter(linkA0.position);
                            break;
                        default:
                            retry = false;
                            break;
                        }
                    }
                }
                if (retry) continue;
            }
        }
        
        void unlinkContoursChains() {
            while(!contours.empty()) {
                Contour contour = contours.getFirst();
                while(!contour.forward.empty()) {
                    Link link = contour.forward.getFirst();
                    link.target = link.contour.getNext().position.getParent();
                    link.contour.unlink();
                }
                while(!contour.backward.empty()) {
                    Link link = contour.backward.getFirst();
                    link.target = link.contour.getNext().position.getParent();
                    link.contour.unlink();
                }
                contour.shape.unlink();
                contour.childs.Clear();
                contour.parent = null;
            }
            rootContours.Clear();
        }
        
        static bool compareAngle(VectorInt a, VectorInt b, VectorInt c) {
            int d = a.x*c.y - a.y*c.x;
            // angle AC < 180 deg
            if (d > 0)
                return a.x*b.y >= a.y*b.x && c.x*b.y <= c.y*b.x;
            // angle AC > 180 deg
            if (d < 0)
                return a.x*b.y >= a.y*b.x || c.x*b.y <= c.y*b.x;
            // angle AC == 180 deg
            if ((a.x >= 0) != (c.x >= 0) || (a.y >= 0) != (c.y >= 0))
                return a.x*b.y >= a.y*b.x;
            // angle AC == 0 deg
            return true;
        }

        static bool compareAngle(VectorInt center, VectorInt a, VectorInt b, VectorInt c) {
            return compareAngle( new VectorInt(a.x - center.x, a.y - center.y),
                                 new VectorInt(b.x - center.x, b.y - center.y),
                                 new VectorInt(c.x - center.x, c.y - center.y) );
        }
        
        void sortLinksAtPosition(Position position) {
            if (position.links.getCount() < 3) return;
            Link first = position.links.getFirst();
            Link linkA = first;
            while (true) {
                Link linkB = linkA.position.getNext();
                Link linkC = linkB.position.getNext();
                if ( !compareAngle(
                        position.toVectorInt(),
                        linkA.target.toVectorInt(),
                        linkB.target.toVectorInt(),
                        linkC.target.toVectorInt() ))
                {
                    linkB.position.swapWith(linkC.position);
                    first = linkA = linkC;
                    continue;
                }
                linkA = linkB;
                if (linkA == first) break;
            };
        }
        
        void sortLinksAtPositions() {
            for(Position position = positions.getFirst(); position != null; position = position.shape.getNextLinear())
                sortLinksAtPosition(position);
        }
        
        void removeLinkFromPosition(Link link) {
            Position position = link.position.getParent();
            Link nextToRemove = null;
            
            // remove back link            
            if (link.target != null) {
                Link backLink = link.target.links.getFirst();
                while (backLink != null) {
                    Link l = backLink;
                    backLink = backLink.position.getNextLinear();
                    if ( l.forward != link.forward
                      && l.contour.getNext() != null
                      && l.contour.getNext().position.getParent() == position )
                    {
                        l.unlink();
                        break;
                    }
                }
                
                if (link.target.links.getCount() == 1)
                    nextToRemove = link.target.links.getFirst();
            }

            // remove
            link.unlink();

            // remove next
            if (nextToRemove != null)
                removeLinkFromPosition(nextToRemove);
        }
        
        void removeDuplicateLinksFromPosition(Position position) {
            for(Link linkA = position.links.getFirst(); linkA != null; linkA = linkA.position.getNextLinear()) {
                Position otherPosition = linkA.target;

                // count forward and backward links
                int count = 0;
                Link forwardLink = null;
                Link backwardLink = null;
                Link linkB = linkA;
                do {
                    if (linkB.target == otherPosition) {
                        if (linkB.forward) { forwardLink = linkB; ++count; }
                                      else { backwardLink = linkB; --count; }
                    }
                    linkB.position.getNext();
                } while(linkB != linkA);

                // remove extra links
                Link linkToSave = count > 0 ? forwardLink
                                : count < 0 ? backwardLink
                                : null;
                linkB = position.links.getFirst();
                while(linkB != null) {
                    if (linkB.target == otherPosition && linkB != linkToSave) {
                        removeLinkFromPosition(linkA);
                        // reset linkA
                        linkB = linkA = position.links.getFirst();
                    } else {
                        linkB = linkB.position.getNextLinear();
                    }
                }
            }
        }
        
        void removeInvisibleContoursFromPosition(Position position) {
            if (position.links.getCount() < 3) return;
            Link first = position.links.getFirst();
            Link link = first.position.getNext();
            while(link != first) {
                bool previous = link.position.getPrevious().forward;
                bool current = link.forward;
                bool next = link.position.getNext().forward;
                if ( ( previous &&  current && !next)
                  || (!previous && !current &&  next) )
                {
                    // remove link
                    removeLinkFromPosition(link);
                    first = position.links.getFirst();
                    link = first.position.getNext();
                }
            };
        }
        
        void removeEmptyPositions() {
            for(Position position = positions.getFirst(); position != null;) {
                if (position.links.empty()) {
                    Position p = position;                
                    position = position.shape.getNextLinear();
                    p.shape.unlink();
                } else {
                    position = position.shape.getNextLinear();
                }
            }
        }
        
        void optimizePositions() {
            // remove extra links
            for(Position position = positions.getFirst(); position != null; position = position.shape.getNextLinear()) {
                removeDuplicateLinksFromPosition(position);
                removeInvisibleContoursFromPosition(position);
            }
        }
        
        void traceContours() {
            for(Link linkA = links.getFirst(); linkA != null; linkA = linkA.shape.getNext()) {
                if (linkA.forward && linkA.contour.getParent() == null) {
                    Contour contour = new Contour();
                    contour.shape.insertBack(contours);
                    
                    Link forwardLink = linkA;
                    Link backwardLink = null;

                    do {
                        // find pair
                        for(Link l = linkA.target.links.getFirst(); l != null; l = l.position.getNext())
                            if (l.target == linkA.position.getParent())
                                { backwardLink = l; break; }
                        if (backwardLink == null)
                            throw new Exception();
                        
                        forwardLink.contour.insertBack(contour.forward);
                        backwardLink.contour.insertBack(contour.backwardLink);
                        
                        forwardLink = backwardLink.position.getNext();
                        if ( !forwardLink.forward
                          || forwardLink == backwardLink
                          || forwardLink.contour.getParent() != null )
                            throw new Exception();
                    } while (forwardLink != linkA);
                }
            }
        }

        void removeFreeLinks() {
            for(Link linkA = links.getFirst(); linkA != null; linkA = linkA.shape.getNext())
                if (linkA.contour.getParent() == null)
                    throw Exception();
        }
        
        void makeContourRay(Contour contour, bool invert, out VectorInt p0, out VectorInt p1) {
            Link first = contour.forward.getFirst();
            VectorInt previousPosition = first.contour.getPrevious().position.getParent().toVectorInt();
            VectorInt currentPosition = first.position.getParent().toVectorInt();
            VectorInt nextPosition = first.contour.getNext().position.getParent().toVectorInt();
            VectorInt direction = new VectorInt(
                    previousPosition.y - nextPosition.y,
                    nextPosition.x - previousPosition.x );
            
            if (invert) { direction.x = -direction.x; direction.y = -direction.y; }
            
            int amplifierX =
                direction.x > 0 ? (ContourInt.MaxValue - currentPosition.x)/direction.x + 1
              : direction.x < 0 ? (ContourInt.MaxValue + currentPosition.x)/(-direction.x) + 1
              : int.MaxValue;
            int amplifierY =
                direction.y > 0 ? (ContourInt.MaxValue - currentPosition.y)/direction.y + 1
              : direction.y < 0 ? (ContourInt.MaxValue + currentPosition.y)/(-direction.y) + 1
              : int.MaxValue;
            int amplifier = Math.Min(amplifierX, amplifierY);
            VectorInt farPosition = new VectorInt(
                    currentPosition.x + direction.x*amplifier,
                    currentPosition.y + direction.y*amplifier );
                    
            p0 = currentPosition;
            p1 = farPosition;
        }
        
        int countContourIntersections(Contour contour, VectorInt p0, VectorInt p1) {
            int count = 0;
            for(Link link = contour.forward.getFirst(); link != null; link = link.contour.getNextLinear()) {
                VectorInt pp0 = link.position.getParent().toVectorInt();
                VectorInt pp1 = link.contour.getNext().position.getParent().toVectorInt();
                long d = (long)(p0.y - p1.y)*(long)(pp1.x - pp0.x)
                       + (long)(p1.x - p0.x)*(long)(pp1.y - pp0.y);
                d = Math.Sign(d);
                VectorInt c;
                switch(findIntersection(p0, p1, pp0, pp1, out c)) {
                case IntersectionType.Cross:
                    count += 2*d;
                    break;
                case IntersectionType.Touch_b0:
                    count += d;
                    break;
                case IntersectionType.Touch_b1:
                    count -= d;
                    break;
                default:
                    break;
                }
            }
            return count;
        }

        bool isContourInverted(Contour contour) {
            VectorInt p0, p1;
            makeContourRay(contour, false, out p0, out p1);
            return countContourIntersections(contour, p0, p1) == 0;
        }
        
        bool isContourInside(Contour inner, Contour outer) {
            VectorInt p0, p1;
            makeContourRay(inner, inner.inverted, out p0, out p1);
            return countContourIntersections(outer, p0, p1) != 0;
        }
        
        Contour findParent(Contour contour, Dictionary<Contour, Dictionary<Contour, bool> > parents) {
            if (!parents.ContainsKey(contour)) return null;
            if (parents[contour].Count == 1) return parents[contour].Keys.First;
            
        }
        
        void sortChilds(Contour contour, Contour parent) {
            // set parent
            contour.parent = parent;

            // remove sub-childs from parent list
            if (parent != null)
                foreach(Contour c in contour.childs)
                    parent.childs.Remove(c);

            // sub-calls
            foreach(Contour c in contour.childs)
                sortChilds(c, contour);
        }
        
        void sortContours() {
            // calculate directions of contours
            for(Contour contourA = contours.getFirst(); contourA != null; contourA = contourA.shape.getNext()) {
                contourA.inverted = isContourInverted(contourA);
                rootContours.Add(contourA);
            }
                
            // find childs
            for(Contour contourA = contours.getFirst(); contourA != null; contourA = contourA.shape.getNext()) {
                bool isRoot = true;
                for(Contour contourB = contourA.shape.getNextLinear(); contourB != null; contourB = contourB.shape.getNext())
                    if (isContourInside(contourA, contourB)) {
                        contourB.childs.Add(contourA);
                        rootContours.Remove(contourA);
                    } else
                    if (isContourInside(contourA, contourB)) {
                        contourA.childs.Add(contourB);
                        rootContours.Remove(contourB);
                    }
            }
            
            // sort childs
            foreach(Contour c in rootContours)
                sortChilds(c, null);
        }

        void removeInvisibleContours() {
            // TODO:
        }
        
        void optimizeContours() {
            findIntersections();
            unlinkContoursChains();
            sortLinksAtPositions();
            optimizePositions();
            traceContours();
            removeEmptyContours();
            removeInvisibleContours();
            removeEmptyPositions();
        }
    }
}

