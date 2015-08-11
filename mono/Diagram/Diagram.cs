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

