using System;
using System.Collections.Generic;

namespace Assistance {
	public interface InputModifier {
		void activate();
		List<Track> modify(List<Track> tracks);
		void deactivate();
	}
}

