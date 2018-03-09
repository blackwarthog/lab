using System;
using System.Collections.Generic;
using System.Linq;

namespace Assistance {
	public class Workarea {
		public readonly Document document;
		public readonly InputManager inputManager;
		
		private readonly InputModifierTangents modifierTangents;
		private readonly InputModifierAssistants modifierAssistants;
		private readonly InputModifierSegmentation modifierSegmentation;
		
		public Workarea() {
			document = new Document(this);
			inputManager = new InputManager(this);
			
			modifierTangents = new InputModifierTangents();
			modifierAssistants = new InputModifierAssistants(this);
			modifierSegmentation = new InputModifierSegmentation();
		}
		
		public Tool getTool()
			{ return inputManager.getTool(); }
			
		public void setTool(Tool tool, bool force = false) {
			if (getTool() != tool || force) {
				inputManager.deactivate();
				inputManager.clearModifiers();
				if (tool != null) {
					Tool.ModifierTypes types = tool.getAvailableModifierTypes();
					if ((Tool.ModifierTypes.Tangents & types) != 0)
						inputManager.addModifier(modifierTangents);
					if ((Tool.ModifierTypes.Guideline & types) != 0)
						inputManager.addModifier(modifierAssistants);
					if ((Tool.ModifierTypes.Multiline & types) != 0)
						foreach(Modifier modifier in document.modifiers)
							inputManager.addModifier(modifier);
					if ((Tool.ModifierTypes.Segmentation & types) != 0)
						inputManager.addModifier(modifierSegmentation);
				}
				inputManager.setTool(tool);
				inputManager.activate();
			}
		}
		
		public void updateModifiers()
			{ setTool(getTool(), true); }

		public ActivePoint findPoint(Point position) {
			foreach(ActivePoint point in document.points.Reverse<ActivePoint>())
				if (point.isInside(position))
					return point;
			return null;
		}

		public void getGuidelines(List<Guideline> outGuidelines, Point target) {
			foreach(Assistant assistant in document.assistants)
				assistant.getGuidelines(outGuidelines, target);
		}

		public void draw(Cairo.Context context, List<Point> hovers, ActivePoint activePoint) {
			// canvas
			document.canvas.draw(context);

			// input manager
			inputManager.draw(context, hovers);
			
			// modifiers
			foreach(Modifier modifier in document.modifiers)
				modifier.draw(context);

			// assistants
			foreach(Assistant assistant in document.assistants)
				assistant.draw(context);

			// active points
			foreach(ActivePoint point in document.points)
				point.draw(context, activePoint == point);
		}
	}
}

