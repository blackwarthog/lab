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

