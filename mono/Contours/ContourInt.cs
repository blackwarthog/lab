using System;
using System.Collections.Generic;
using System.Linq;

namespace Contours {
    public struct VectorInt {
        public int x, y;

        public VectorInt(int x, int y) {
            this.x = x;
            this.y = y;
        }
    }

    public class ContourInt {
        public float scale = 1f;
        public readonly List<List<VectorInt>> contours = new List<List<VectorInt>>();

        public ContourFloat toContourFloat() {
            ContourFloat contourFloat = new ContourFloat();
            foreach(List<VectorInt> contour in contours) {
                List<VectorFloat> newContour = new List<VectorFloat>();
                foreach(VectorInt point in contour)
                    newContour.Add(new VectorFloat(point.x * scale, point.y * scale));
                contourFloat.contours.Add(newContour);
            }
            return contourFloat;
        }
    }
}

