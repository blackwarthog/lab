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
                if (getCircuit() == null) return;
                previous.next = next;
                next.previous = previous;
                if (previous == next) {
                    previous = this;
                    next = this;
                    if (previous == next) {
                        circuit.first = circuit.last = null;
                    } else {
                        if (circuit.first == this)
                            circuit.first = next;
                        if (circuit.last == this)
                            circuit.last = previous;
                    }
                }
                --circuit.count;
                circuit = null;
                previous = null;
                next = null;
            }

            public void insertBack(Circuit<Parent, Child> circuit) {
                if (getCircuit() == circuit) return;
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
                if (getCircuit() == circuit) return;
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
                if (entry == null || entry.getCircuit() == null) { unlink(); return; }
                if (getCircuit() == entry.getCircuit()) return;
                unlink();
                circuit = entry.getCircuit();
                previous = entry;
                next = entry.next;
                next.previous = this;
                previous.next = this;
                if (circuit.getLastEntry() == entry)
                    circuit.last = this;
                ++circuit.count;
            }

            public void insertBeforeOf(Entry entry) {
                if (entry == null || entry.getCircuit() == null) { unlink(); return; }
                if (getCircuit() == entry.getCircuit()) return;
                unlink();
                circuit = entry.getCircuit();
                previous = entry.previous;
                next = entry;
                previous.next = this;
                next.previous = this;
                if (circuit.getFirstEntry() == entry)
                    circuit.first = this;
                ++circuit.count;
            }

            void swap(Entry other) {
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

            public Link() {
                shape = new Circuit<Shape, Link>.Entry(this);
                position = new Circuit<Position, Link>.Entry(this);
                contour = new Circuit<Contour, Link>.Entry(this);
            }

            public bool isForward() {
                Contour c = contour.getParent();
                if (c == null) return false;
                return c == null || contour.getCircuit() == c.forward;
            } 

            public void unlink() {
                contour.unlink();
                position.unlink();
                shape.unlink();
            }

            public static Link create(Position position, Circuit<Contour, Link> contourCircuit) {
                Link link = new Link();
                link.shape.insertBack(contourCircuit.getOwner().shape.getParent().links);
                link.position.insertBack(position.links);
                link.contour.insertBack(contourCircuit);
                return link;
            }

            public Link createSplitAfter(Position position) {
                Link link = new Link();
                link.shape.insertBack(shape.getParent().links);
                link.position.insertBack(position.links);
                link.contour.insertAfterOf(contour);
                return link;
            }

            public Link createSplitAfter(Circuit<Position, Link>.Entry position) {
                Link link = new Link();
                link.shape.insertBack(shape.getParent().links);
                link.position.insertAfterOf(position);
                link.contour.insertAfterOf(contour);
                return link;
            }
        }

        public Circuit<Shape, Position> positions;
        public Circuit<Shape, Link> links;
        public Circuit<Shape, Contour> contours;

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

        public void findIntersections() {
            bool retry = true;
            while(retry) {
                retry = false;

                // remove empty contours
                for(Contour contour = contours.getFirst(); !retry && contour != null; contour = contour.shape.getNext()) {
                    if (contour.forward.getCount() < 3 || contour.backward.getCount() < 3) {
                        while(contour.forward.getFirst() != null)
                            contour.forward.getFirst().unlink();
                        while(contour.backward.getFirst() != null)
                            contour.backward.getFirst().unlink();
                        contour.shape.unlink();
                        retry = true;
                        break;
                    }
                }
                if (retry) continue;

                // remove empty positions
                for(Position position = positions.getFirst(); !retry && position != null; position = position.shape.getNext()) {
                    if (position.links.empty()) {
                        position.shape.unlink();
                        retry = true;
                        break;
                    }
                }
                if (retry) continue;

                // merge positions
                for(Position positionA = positions.getFirst(); !retry && positionA != null; positionA = positionA.shape.getNext()) {
                    for(Position positionB = positionA.shape.getNext(); !retry && positionB != null; positionB = positionB.shape.getNext()) {
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
                for(Link linkA0 = links.getFirst(); !retry && linkA0 != null; linkA0 = linkA0.shape.getNext()) {
                    Link linkA1 = linkA0.contour.getNext();
                    if (linkA0.position.getParent() == linkA1.position.getParent()) {
                        linkA1.unlink();
                        retry = true;
                        break;
                    }
                }
                if (retry) continue;

                // check intersections
                for(Link linkA0 = links.getFirst(); !retry && linkA0 != null; linkA0 = linkA0.shape.getNext()) {
                    Link linkA1 = linkA0.contour.getNext();
                    for(Link linkB0 = links.getFirst(); !retry && linkB0 != null; linkB0 = linkB0.shape.getNext()) {
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
        
        void sortLinksAtPositions() {
            // TODO:
        }
        
        void optimizeContours() {
            // TODO:
        }
    }
}

