using System;

namespace Contours {
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
                
                if (this.next == other || other.previous == this) {
                    this.next = other.next;
                    other.next.previous = this;
                    other.previous = this.previous;
                    this.previous.next = other;
                    this.previous = other;
                    other.next = this;
                    return;
                }

                if (other.next == this || this.previous == other) {
                    other.next = this.next;
                    this.next.previous = other;
                    this.previous = other.previous;
                    other.previous.next = this;
                    other.previous = this;
                    this.next = other;
                    return;
                }

                if (other.circuit != null) {
                    if (other.circuit.first == other)
                        other.circuit.first = this;
                    if (other.circuit.last == other)
                        other.circuit.last = this;
                }

                if (this.circuit != null) {
                    if (this.circuit.first == this)
                        this.circuit.first = other;
                    if (this.circuit.last == this)
                        this.circuit.last = other;
                }

                Circuit<Parent, Child> otherCircuit = other.circuit;
                Entry otherPrevious = other.previous;
                Entry otherNext = other.next;

                other.circuit = this.circuit;
                other.previous = this.previous;
                other.next = this.next;

                this.circuit = otherCircuit;
                this.previous = otherPrevious;
                this.next = otherNext;

                if (other.previous != null) other.previous.next = other;
                if (other.next != null) other.next.previous = other;
                if (this.previous != null) this.previous.next = this;
                if (this.next != null) this.next.previous = this;
            }

            public static void swapChains(Circuit<Parent, Child> a, Circuit<Parent, Child> b) {
                for(Entry e = a.getFirstEntry(); e != null; e = e.getNextEntryLinear())
                    e.circuit = b;
                for(Entry e = b.getFirstEntry(); e != null; e = e.getNextEntryLinear())
                    e.circuit = a;
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
        
        public void swapWith(Circuit<Parent, Child> other) {
            Entry.swapChains(this, other);
        }
    }
}

