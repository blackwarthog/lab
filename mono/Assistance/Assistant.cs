using System;
using System.Collections.Generic;

namespace Assistance {
	public class Assistant: ActivePoint.Owner {
		public static readonly Drawing.Pen pen = new Drawing.Pen("gray");

		public Assistant(Document document): base(document)
			{ document.assistants.Add(this); }

		public override void remove() {
			base.remove();
			document.assistants.Remove(this);
		}

		public override void bringToFront() {
			document.assistants.Remove(this);
			document.assistants.Add(this);
		}

		public virtual void draw(Cairo.Context context) { }

		public virtual void getGuidelines(List<Guideline> outGuidelines, Point target) { }
	}
}
