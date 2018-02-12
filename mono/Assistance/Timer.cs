using System;

namespace Assistance {
	public class Timer {
		public static readonly long frequency = System.Diagnostics.Stopwatch.Frequency;
		public static readonly double step = 1.0/(double)frequency;

   		private static readonly System.Diagnostics.Stopwatch instance = new System.Diagnostics.Stopwatch();
   		
   		static Timer() { instance.Start(); }
   		
   		public static long ticks()
   			{ return instance.ElapsedTicks; }
	}
}

