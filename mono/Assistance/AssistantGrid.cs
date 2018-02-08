using System;

namespace Assistance {
	public class AssistantGrid: Assistant {
		public ActivePoint center;

		public AssistantGrid(Document document, Point center): base(document) {
			this.center = new ActivePoint(this, ActivePoint.Type.CircleCross, center);
		}

		public override void draw(Cairo.Context context) {
			/*
			foreach(Assistant assistant in canvas.assistants)
				foreach(Point p in assistant.getGridPoints(center.position))
					foreach(Assistant a in canvas.assistants)
						if (a != assistant)
							a.drawGuidlines(context, p);
			*/
		}
	}
}

