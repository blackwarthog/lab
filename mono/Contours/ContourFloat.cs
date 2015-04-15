using System;
using System.Collections.Generic;
using System.Linq;

namespace Contours {
    public struct VectorFloat {
        public float x, y;

        public VectorFloat(float x, float y) {
            this.x = x;
            this.y = y;
        }
    }

    public class ContourFloat {
        public readonly List<List<VectorFloat>> contours = new List<List<VectorFloat>>();

        public ContourInt toContourInt(float detalization = 10000f) {
            ContourInt contourInt = new ContourInt();

            bool found = false;
            VectorFloat min = new VectorFloat(0f, 0f);
            VectorFloat max = new VectorFloat(0f, 0f);
            foreach(List<VectorFloat> contour in contours) {
                foreach(VectorFloat point in contour) {
                    if (!found) {
                        min = max = point;
                        found = true;
                    } else {
                        if (min.x > point.x)
                            min.x = point.x;
                        if (min.y > point.y)
                            min.y = point.y;
                        if (max.x > point.x)
                            max.x = point.x;
                        if (max.y > point.y)
                            max.y = point.y;
                    }
                }
            }

            if (found) {
                contourInt.scale = Math.Max(max.x - min.x, max.y - min.y)/detalization;
                foreach(List<VectorFloat> contour in contours) {
                    List<VectorInt> newContour = new List<VectorInt>();
                    foreach(VectorFloat point in contour)
                        newContour.Add(new VectorInt((int)Math.Round(point.x*contourInt.scale), (int)Math.Round(point.y*contourInt.scale)));
                    contourInt.contours.Add(newContour);
                }
            }

            return contourInt;
        }
    }
}

