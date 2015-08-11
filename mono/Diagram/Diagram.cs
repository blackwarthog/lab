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
using System.Collections.Generic;
using System.Drawing;

namespace Diagram {
    public class Block {
        public string id = "";
        public string caption = "";
        public string text = "";
        public Color color = Color.Black;
    }

    public class Link {
        public string id = "";
        public string srcId = "";
        public string dstId = "";
    }

    public class Diagram {
        public readonly Dictionary<string, Block> blocks = new Dictionary<string, Block>();
        public readonly Dictionary<string, Link> links = new Dictionary<string, Link>();

        public Diagram addBlock(Block block) {
            blocks.Add(block.id, block);
            return this;
        }

        public Diagram addBlock(string id, string caption, string text, Color color, string[] links = null) {
            addBlock(new Block() { id = id, caption = caption, text = text, color = color });
            if (links != null)
                foreach(string link in links)
                    addLink(id, link);
            return this;
        }

        public Diagram addBlock(string id, string caption, string text, string[] links = null) {
            addBlock(id, caption, text, Color.Black, links);
            return this;
        }

        public Diagram addLink(Link link, string srcId = "", string dstId = "") {
            if (srcId != "")
                link.srcId = srcId;
            if (dstId != "")
                link.dstId = dstId;
            if (link.id == "")
                link.id = link.srcId + link.dstId;
            links.Add(link.id, link);
            return this;
        }

        public Diagram addLink(Link link) {
            if (link.id == "")
                link.id = link.srcId + link.dstId;
            links.Add(link.id, link);
            return this;
        }

        public Diagram addLink(string srcId, string dstId) {
            addLink(new Link(), srcId, dstId);
            return this;
        }
    }
}

