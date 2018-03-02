using System;
using System.Collections.Generic;

namespace Assistance {
	public class InputManager: Track.Owner {
		public static readonly Drawing.Pen penPreview = new Drawing.Pen("Dark Green", 1.0, 0.25);

		public class TrackHandler: Track.Handler {
			public readonly List<int> keys = new List<int>();
			public TrackHandler(InputManager owner, Track original, int keysCount = 0):
				base(owner, track)
				{ for(int i = 0; i < keysCount; ++i) keys.Add(0); }
		}
		
		public class KeyPoint {
			public class Holder: IDisposable {
				public readonly KeyPoint keyPoint;
				private bool holded = false;
				
				public Holder(KeyPoint keyPoint)
					{ this.keyPoint = keyPoint; reuse(); }

				public bool available
					{ get { return keyPoint.available; } }
				public bool isHolded
					{ get { return holded; } }
				public bool reuse() {
					if (!holded) ++keyPoint.refCount;
					holded = true;
					return keyPoint.available;
				}
				public void release() {
					if (holded) --keyPoint.refCount;
					holded = false;
				}
					
				public void Dispose()
					{ Dispose(true); GC.SuppressFinalize(this); }
				protected virtual void Dispose(bool disposing)
					{  release(); }
				~Holder()
					{ Dispose(false); }
			}
			
			private int refCount = 0;
			public bool available = true;
			
			public Holder hold()
				{ return new Holder(this); }
			public bool isFree
				{ get { return refCount <= 0; } }
		}
		
		public class Modifier: Track.Owner {
			public virtual void activate() { }
			public virtual void modify(Track tracks, KeyPoint keyPoint, List<Track> outTracks)
				{ }
			public virtual void modify(List<Track> tracks, KeyPoint keyPoint, List<Track> outTracks)
				{ foreach(Track track in tracks) modify(track, keyPoint, outTracks); }
			public virtual void deactivate() { }
		}

		
		public readonly Workarea workarea;
		
		private readonly InputState state = new InputState();

		private Tool tool;
		private readonly List<Modifier> modifiers = new List<Modifier>();

		private readonly List<Track> tracks = new List<Track>();
		private readonly List<KeyPoint> keyPoints = new List<KeyPoint>();
		private int keyPointsSent;

		private List<Track> subTracks = null;
		private readonly List<Track>[] subTracksBuf = new List<Track>[] { new List<Track>(), new List<Track>() };


		InputManager(Workarea workarea)
			{ this.workarea = workarea; }


		private void paintRollbackTo(int keyIndex, List<Track> subTracks) {
			if (keyIndex >= keyPoints.Count)
				return;
			
			int level = keyIndex + 1;
			if (level <= keyPointsSent) {
				if (level <= keyPointsSent)
					tool.paintPop(keyPointsSent - level);
				tool.paintCancel();
				keyPointsSent = level;
			}
			
			foreach(Track track in subTracks) {
				TrackHandler handler = (TrackHandler)track.handler;
				handler.keys.RemoveRange(level, keyPoints.Count - level);
				int index = handler.keys[keyIndex];
				track.wayPointsRemoved = 0;
				track.wayPointsAdded = track.points.Count - index - 1;
			}
			for(int i = level; i < keyPoints.Count; ++i) keyPoints[i].available = false;
			keyPoints.RemoveRange(level, keyPoints.Count - level);
		}
		
		private void paintApply(int count, Dictionary<long, Track> subTracks) {
			if (count <= 0)
				return;
			
			int level = keyPoints.Count - count;
			if (level >= keyPointsSent || tool.paintApply(keyPointsSent - level)) {
				// apply
				foreach(Track track in subTracks)
					((TrackHandler)track.handler).keys.RemoveRange(level, keyPoints.Count - level);
			} else {
				// rollback
				tool.paintPop(keyPointsSent - level);
				foreach(Track track in subTracks) {
					TrackHandler handler = (TrackHandler)track.handler;
					int index = handler.keys[level];
					handler.keys.RemoveRange(level, keyPoints.Count - level);
					track.wayPointsRemoved = 0;
					track.wayPointsAdded = track.points.Count - index - 1;
				}
			}

			for(int i = level; i < keyPoints.Count; ++i) keyPoints[i].available = false;
			keyPoints.RemoveRange(level, keyPoints.Count - level);
			if (level < keyPointsSent)
				keyPointsSent = level;
		}
		
		private void paintTracks() {
			bool allFinished = true;
			foreach(Track track in tracks)
				if (!track.isFinished)
					{ allFinished = false; break; }

			while(true) {
				// run modifiers
				KeyPoint newKeyPoint = new KeyPoint();
				subTracks = tracks;
				int i = 0;
				foreach(Modifier modifier in modifiers) {
					List<Track> outTracks = subTracksBuf[i];
					modifier.modify(subTracks, keyEvent, outTracks);
					subTracks = outTracks;
					i = 1 - i;
				}
				
				// create handlers	
				foreach(Track track in subTracks)
					if (track.handler == null)
						track.handler = new TrackHandler(this, track, keyPoints.Count);
	
				if (keyPoints.Count > 0) {
					// rollback
					int rollbackIndex = keyPoints.Count;
					foreach(Track track in subTracks) {
						if (track.wayPointsRemoved > 0) {
							int count = track.points.Count - track.wayPointsAdded + track.wayPointsRemoved;
							TrackHandler handler = (TrackHandler)track.handler;
							while(rollbackIndex > 0 && (rollbackIndex >= keyPoints.Count || handler.keys[rollbackIndex] > count))
								--rollbackIndex;
						}
					}
					paintRollbackTo(rollbackIndex, subTracks);
	
					// apply
					int applyCount = 0;
					while(applyCount < keyPoints.Count && keyPoints[keyPoints.Count - applyCount - 1].isFree)
						++applyCount;
					paintApply(applyCount, subTracks);
				}
				
				// send to tool
				if (keyPointsSent == keyPoints.Count)
					tool.paintTracks(subTracks);
				
				// is paint finished?
				if (newKeyPoint.isFree) {
					if (allFinished) {
						paintApply(keyPoints.Count, subTracks);
						tracks.Clear();
						this.subTracks = null;
					} else {
						this.subTracks = subTracks;
					}
					break;
				}
				
				// create key point
				if (tool.paintPush()) ++keyPointsSent;
				keyPoints.Add(newKeyPoint);
				foreach(Track track in subTracks)
					((TrackHandler)track.handler).keys.Add(track.points.Count);
			}
		}
		
		private int trackCompare(Track track, Gdk.Device device, long touchId) {
			if (track.device < device) return -1;
			if (track.device > device) return 1;
			if (track.touchId < touchId) return -1;
			if (track.touchId > touchId) return 1;
			return 0;
		}
		
		private Track createTrack(int index, Gdk.Device device, long touchId, long ticks) {
			Track track = new Track(
				device,
				touchId,
				state.keyHistoryHolder(ticks),
				state.buttonHistoryHolder(device, ticks) );
			tracks.Insert(index, track);
			return track;
		}
		
		private Track getTrack(Gdk.Device device, long touchId, long ticks) {
			int cmp;
			
			int a = 0;
			cmp = trackCompare(tracks[a], device, touchId);
			if (cmp == 0) return tracks[a];
			if (cmp < 0) return createTrack(a, device, touchId, ticks);
			
			int b = tracks.Count - 1;
			cmp = trackCompare(tracks[b], device, touchId);
			if (cmp == 0) return tracks[b];
			if (cmp > 0) return createTrack(b+1, device, touchId, ticks);
			
			// binary search: tracks[a] < tracks[c] < tracks[b]
			while(true) {
				int c = (a + b)/2;
				if (a == c) break;
				cmp = trackCompare(tracks[c], device, touchId);
				if (cmp < 0) b = c; else
					if (cmp > 0) a = c; else
						return tracks[c];
			}
			return createTrack(b, device, touchId, ticks);
		}
		
		private void addTrackPoint(Track track, Track.Point point, double time, bool final) {
			// fix time
			if (track.points.Count > 0)
				time = Math.Max(time, track.getLast().time + Timer.step);
			
			// calc length
			double length = track.points.Count > 0
			              ? (point.position - track.getLast().point.position).lenSqr() + track.getLast().length
			              : 0.0;
			
			// add
			track.points.Add( new Track.WayPoint(
				point,
				new Track.Point(),
				(double)track.points.Count,
				time,
				length,
				track.points.Count,
				final ));
			++track.wayPointsAdded;
		}
		
		private void touchTracks(bool finish = false) {
			foreach(Track track in tracks)
				if (!track.isFinished() && track.points.Count > 0)
					addTrackPoint(track, track.getLast().point, track.getLast().time, finish);
			paintTracks();
		}
		
		private void finishTracks()
			{ touchTracks(true); }
		
		
		public void trackEvent(long touchId, Gdk.Device device, Track.Point point, bool final, long ticks) {
			if (tool != null) {
				Track track = getTrack(touchId, device, ticks);
				if (!track.isFinished) {
					double time = (double)(ticks - track.keyHistory.ticks)*Timer.step - track.keyHistory.timeOffset;
					addTrackPoint(track, point, time, final);
					paintTracks();
				}
			}
		}

		public void keyEvent(bool press, Gdk.Key key, long ticks) {
			state.keyEvent(press, key, ticks);
			if (tool != null) {
				tool.keyEvent(press, key, state);
				touchTracks();
			}
		}
		
		public void buttonEvent(bool press, Gdk.Device device, uint button, long ticks) {
			state.buttonEvent(press, device, button, ticks);
			if (tool != null) {
				tool.buttonEvent(press, device, button, state);
				touchTracks();
			}
		}
	
		
		public Tool getTool()
			{ return tool; }
		
		public void setTool(Tool tool) {
			if (this.tool == tool) {
				if (this.tool != null) {
					finishTracks();
					this.tool.deactivate();
				}
				
				this.tool = tool;
				
				if (this.tool != null)
					this.tool.activate();
			}
		}
		
		public int getModifiersCount()
			{ return modifiers.Count; }
		public Modifier getModifier(int index)
			{ return modifiers[index]; }

		public void insertModifier(int index, Modifier modifier) {
			if (this.tool != null) finishTracks();
			modifiers.Insert(index, modifier);
			modifier.activate();
		}
		public void addModifier(Modifier modifier)
			{ insertModifier(getModifiersCount(), modifier); }
		
		public void removeModifier(int index) {
			if (this.tool != null) finishTracks();
			modifiers[i].deactivate();
			modifiers.RemoveAt(index);
		}
		public void removeModifier(Modifier modifier) {
			for(int i = 0; i < getModifiersCount(); ++i)
				if (getModifier(i) == modifier)
					{ removeModifier(i); break; }
		}
		public void clearModifiers() {
			while(getModifiersCount() > 0)
				removeModifier(0);
		}
	
		
		public void draw(Cairo.Context context) {
			// paint not sent sub-tracks
			if (subTracks != null && keyPointsSent < keyPoints.Count) {
				context.Save();
				penPreview.apply(context);
				foreach(Track track in subTracks) {
					TrackHandler handler = (TrackHandler)track.handler;
					int start = handler.keys[keyPointsSent];
					if (start < track.points.Count) {
						context.MoveTo(track.points[start].point.position.x, track.points[start].point.position.y);
						for(int i = start + 1; i < track.points.Count; ++i)
							context.MoveTo(track.points[i].point.position.x, track.points[i].point.position.y);
					}
				}
				context.Stroke();
				context.Restore();
			}
		}
	}
}

