using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;

namespace Assistance {
	public class Document {
		public readonly Workarea workarea;
		public readonly List<Assistant> assistants = new List<Assistant>();
		public readonly List<Modifier> modifiers = new List<Modifier>();
		public readonly List<ActivePoint> points = new List<ActivePoint>();
		public readonly Canvas canvas = new Canvas();
		
		public Document(Workarea workarea) {
			this.workarea = workarea;
		}
	}
}

