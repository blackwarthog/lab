using System;
using System.Drawing;

namespace Diagram {
    public class Test {
        public static Diagram buildTestDiagram() {
            return new Diagram()
                .addBlock(
                    "b1",
                    "Block Number One",
                    "Very cool block, with the best number One. Important thing."
                )
                .addBlock(
                    "b2",
                    "Block Number Two",
                    "Very cool block, with the best number Two. Important thing."
                )
                .addBlock(
                    "b3",
                    "Block Number Three",
                    "Very cool block, with the best number Three. Important thing.",
                    Color.Red
                )
                .addBlock(
                    "b4",
                    "Block Number Four",
                    "Very cool block, with the best number Four. Important thing."
                )
                .addBlock(
                    "b5",
                    "Block Number Five",
                    "Very cool block, with the best number Five. Important thing."
                )
                .addLink("b1", "b3")
                .addLink("b1", "b4")
                .addLink("b2", "b3")
                .addLink("b3", "b4")
                .addLink("b4", "b5");
        }

        public static ActiveDiagram buildTestActiveDiagram(Diagram diagram) {
            ActiveDiagram d = new ActiveDiagram();

            d.diagram = diagram;

            d.captionFont = new Font(FontFamily.GenericSansSerif, 12f, FontStyle.Bold);
            d.textFont = new Font(FontFamily.GenericSansSerif, 10f);
            d.margin = d.captionFont.GetHeight();
            d.padding = d.textFont.GetHeight();
            d.arrowSize = new SizeF(10f, 15f);
            d.pen = new Pen(Brushes.Black, 3f);
            d.brush = Brushes.White;

            d.reloadDiagram();

            return d;
        }

        public static ActiveDiagram buildTestActiveDiagram() {
            return buildTestActiveDiagram(buildTestDiagram());
        }
    }
}

