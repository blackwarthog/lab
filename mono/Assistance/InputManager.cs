using System;
using System.Collections.Generic;

namespace Assistance {
	public class InputManager: Track.IOwner {
		public static readonly Drawing.Pen penPreview = new Drawing.Pen("Dark Green", 3.0, 0.25);
		public static readonly double levelAlpha = 0.8;

		public class TrackHandler: Track.Handler {
			public readonly List<int> keys = new List<int>();
			public TrackHandler(InputManager owner, Track original, int keysCount = 0):
				base(owner, original)
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
		
		public interface IModifier: Track.IOwner {
			void activate();
			void modify(List<Track> tracks, KeyPoint keyPoint, List<Track> outTracks);
			void draw(Cairo.Context context, List<Track> tracks, List<Point> hovers);
			void deactivate();
		}

		public class Modifier: IModifier {
			public virtual void activate() { }
			public virtual void modify(Track track, KeyPoint keyPoint, List<Track> outTracks) { }
			public virtual void modify(List<Track> tracks, KeyPoint keyPoint, List<Track> outTracks)
				{ foreach(Track track in tracks) modify(track, keyPoint, outTracks); }
			public virtual void drawHover(Cairo.Context context, Point hover) { }
			public virtual void drawTrack(Cairo.Context context, Track track) { }
			public virtual void draw(Cairo.Context context, List<Track> tracks, List<Point> hovers) {
				foreach(Track track in tracks) drawTrack(context, track);
				foreach(Point hover in hovers) drawHover(context, hover);
			}
			public virtual void deactivate() { }
		}

		
		public readonly Workarea workarea;
		
		private readonly InputState state = new InputState();
		
		private bool wantActive;
		private bool actualActive;
		private Tool tool;
		private readonly List<IModifier> modifiers = new List<IModifier>();

		private readonly List<List<Track>> tracks = new List<List<Track>>() { new List<Track>() };
		private readonly List<KeyPoint> keyPoints = new List<KeyPoint>();
		private int keyPointsSent;


		public InputManager(Workarea workarea)
			{ this.workarea = workarea; }

		private void paintRollbackTo(int keyIndex, List<Track> subTracks) {
			if (keyIndex >= keyPoints.Count)
				return;

			int level = keyIndex + 1;
			if (level <= keyPointsSent) {
				if (level < keyPointsSent)
					tool.paintPop(keyPointsSent - level);
				tool.paintCancel();
				keyPointsSent = level;
			}
			
			foreach(Track track in subTracks) {
				TrackHandler handler = (TrackHandler)track.handler;
				handler.keys.RemoveRange(level, keyPoints.Count - level);
				int cnt = handler.keys[keyIndex];
				track.wayPointsRemoved = 0;
				track.wayPointsAdded = track.points.Count - cnt;
			}
			for(int i = level; i < keyPoints.Count; ++i) keyPoints[i].available = false;
			keyPoints.RemoveRange(level, keyPoints.Count - level);
		}
		
		private void paintApply(int count, List<Track> subTracks) {
			if (count <= 0)
				return;
				
			int level = keyPoints.Count - count;
			bool resend = true;
			
			if (level < keyPointsSent) {
				// apply
				int applied = tool.paintApply(keyPointsSent - level);
				applied = Math.Max(0, Math.Min(keyPointsSent - level, applied));
				keyPointsSent -= applied;
				if (keyPointsSent == level) resend = false;
			}
			
			if (level < keyPointsSent) {
				// rollback
				tool.paintPop(keyPointsSent - level);
				keyPointsSent = level;
			}

			// remove keypoints
			foreach(Track track in subTracks) {
				TrackHandler handler = (TrackHandler)track.handler;
				if (resend) {
					track.wayPointsRemoved = 0;
					track.wayPointsAdded = track.points.Count - handler.keys[keyPointsSent];
				}
				handler.keys.RemoveRange(level, handler.keys.Count - level);
			}
			for(int i = level; i < keyPoints.Count; ++i) keyPoints[i].available = false;
			keyPoints.RemoveRange(level, keyPoints.Count - level);
		}
		
		private void paintTracks() {
			bool allFinished = true;
			foreach(Track track in tracks[0])
				if (!track.isFinished())
					{ allFinished = false; break; }

			while(true) {
				// run modifiers
				KeyPoint newKeyPoint = new KeyPoint();
				for(int i = 0; i < modifiers.Count; ++i) {
					tracks[i+1].Clear();
					modifiers[i].modify(tracks[i], newKeyPoint, tracks[i+1]);
				}
				List<Track> subTracks = tracks[modifiers.Count];
				
				// create handlers	
				foreach(Track track in subTracks)
					if (track.handler == null)
						track.handler = new TrackHandler(this, track, keyPoints.Count);
	
				if (keyPoints.Count > 0) {
					// rollback
					int rollbackIndex = keyPoints.Count;
					foreach(Track track in subTracks) {
						if (track.wayPointsRemoved > 0) {
							int count = track.points.Count - track.wayPointsAdded;
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
				if (keyPointsSent == keyPoints.Count && subTracks.Count > 0)
					tool.paintTracks(subTracks);
				foreach(Track track in subTracks) {
					track.wayPointsRemoved = 0;
					track.wayPointsAdded = 0;
				}
				
				// is paint finished?
				if (newKeyPoint.isFree) {
					if (allFinished) {
						paintApply(keyPoints.Count, subTracks);
						foreach(List<Track> ts in tracks)
							ts.Clear();
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

		private long getDeviceId(Gdk.Device device)
			{ return device == null ? 0 : device.Handle.ToInt64(); }
				
		private int trackCompare(Track track, Gdk.Device device, long touchId) {
			if (getDeviceId(track.device) < getDeviceId(device)) return -1;
			if (getDeviceId(track.device) > getDeviceId(device)) return 1;
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
			tracks[0].Insert(index, track);
			return track;
		}
		
		private Track getTrack(Gdk.Device device, long touchId, long ticks) {
			if (tracks[0].Count == 0)
				return createTrack(0, device, touchId, ticks);
			int cmp;
			
			int a = 0;
			cmp = trackCompare(tracks[0][a], device, touchId);
			if (cmp == 0) return tracks[0][a];
			if (cmp < 0) return createTrack(a, device, touchId, ticks);
			
			int b = tracks[0].Count - 1;
			cmp = trackCompare(tracks[0][b], device, touchId);
			if (cmp == 0) return tracks[0][b];
			if (cmp > 0) return createTrack(b+1, device, touchId, ticks);
			
			// binary search: tracks[a] < tracks[c] < tracks[b]
			while(true) {
				int c = (a + b)/2;
				if (a == c) break;
				cmp = trackCompare(tracks[0][c], device, touchId);
				if (cmp < 0) b = c; else
					if (cmp > 0) a = c; else
						return tracks[0][c];
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
			foreach(Track track in tracks[0])
				if (!track.isFinished() && track.points.Count > 0)
					addTrackPoint(track, track.getLast().point, track.getLast().time, finish);
			paintTracks();
		}
		
		private void actualActivate() {
			bool wasActive = actualActive;
			actualActive = wantActive && tool != null;
			if (wasActive == actualActive) return;
			
			if (actualActive) {
				foreach(IModifier modifier in modifiers)
					modifier.activate();
				tool.activate();
			} else {
				touchTracks(true);
				tool.deactivate();
				foreach(IModifier modifier in modifiers)
					modifier.deactivate();
			}
		}
		
		public bool isActive()
			{ return actualActive; }
		public void activate()
			{ wantActive = true; actualActivate(); }
		public void deactivate()
			{ wantActive = false; actualActivate(); }
		public void finishTracks()
			{ if (isActive()) touchTracks(true); }
			
		public List<Track> getInputTracks()
			{ return tracks[0]; }
		public List<Track> getOutputTracks()
			{ return tracks[modifiers.Count]; }
		
		
		public void trackEvent(Gdk.Device device, long touchId, Track.Point point, bool final, long ticks) {
			if (isActive()) {
				Track track = getTrack(device, touchId, ticks);
				if (!track.isFinished()) {
					double time = (double)(ticks - track.keyHistory.ticks)*Timer.step - track.keyHistory.timeOffset;
					addTrackPoint(track, point, time, final);
					paintTracks();
				}
			}
		}

		public void keyEvent(bool press, Gdk.Key key, long ticks) {
			state.keyEvent(press, key, ticks);
			if (isActive()) {
				tool.keyEvent(press, key, state);
				touchTracks();
			}
		}
		
		public void buttonEvent(bool press, Gdk.Device device, uint button, long ticks) {
			state.buttonEvent(press, device, button, ticks);
			if (isActive()) {
				tool.buttonEvent(press, device, button, state);
				touchTracks();
			}
		}
	
		public Tool getTool()
			{ return tool; }
		
		public void setTool(Tool tool) {
			if (this.tool != tool) {
				if (actualActive) {
					finishTracks();
					this.tool.deactivate();
				}
				
				this.tool = tool;
				
				if (actualActive) {
					if (this.tool != null)
						this.tool.activate();
					else
						actualActivate();
				}
			}
		}
		
		public int getModifiersCount()
			{ return modifiers.Count; }
		public IModifier getModifier(int index)
			{ return modifiers[index]; }

		public void insertModifier(int index, IModifier modifier) {
			if (actualActive)
				finishTracks();
			modifiers.Insert(index, modifier);
			tracks.Insert(index+1, new List<Track>());
			if (actualActive)
				modifier.activate();
		}
		public void addModifier(IModifier modifier)
			{ insertModifier(getModifiersCount(), modifier); }
		
		public void removeModifier(int index) {
			if (actualActive) {
				finishTracks();
				modifiers[index].deactivate();
			}
			modifiers.RemoveAt(index);
			tracks.RemoveAt(index+1);
		}
		public void removeModifier(IModifier modifier) {
			for(int i = 0; i < getModifiersCount(); ++i)
				if (getModifier(i) == modifier)
					{ removeModifier(i); break; }
		}
		public void clearModifiers() {
			while(getModifiersCount() > 0)
				removeModifier(getModifiersCount() - 1);
		}
	
		
		public void draw(Cairo.Context context, List<Point> hovers) {
			if (!isActive()) return;

			// paint tool
			tool.draw(context);
			
			// paint not sent sub-tracks
			if (keyPointsSent < keyPoints.Count) {
				context.Save();
				penPreview.apply(context);
				foreach(Track track in getOutputTracks()) {
					TrackHandler handler = (TrackHandler)track.handler;
					int start = handler.keys[keyPointsSent] - 1;
					if (start < 0) start = 0;
					if (start < track.points.Count) {
						Drawing.Color color = penPreview.color;
						int level = keyPointsSent;
						
						color.apply(context);
						context.MoveTo(track.points[start].point.position.x, track.points[start].point.position.y);
						for(int i = start + 1; i < track.points.Count; ++i) {
							while(level < handler.keys.Count && handler.keys[level] <= i) {
								context.Stroke();
								context.MoveTo(track.points[i-1].point.position.x, track.points[i-1].point.position.y);
								color.a *= levelAlpha;
								color.apply(context);
								++level;
							}
							context.LineTo(track.points[i].point.position.x, track.points[i].point.position.y);
						}
					}
				}
				context.Stroke();
				context.Restore();
			}
			
			// paint modifiers
			for(int i = 0; i < modifiers.Count; ++i)
				modifiers[i].draw(context, tracks[i], hovers);
		}
	}
}

