using System;
using System.Collections.Generic;
using System.Linq;
using UtilityCollections;

namespace NewStatusSystems { //todo namespace

	using Converter = Func<int, int>;

	public class BaseStatusTracker<TObject, TStatus> where TStatus : struct {
		protected TObject obj;
		protected BaseStatusSystem<TObject, TStatus> rules;

		public bool GenerateNoMessages { get; set; }
		public bool GenerateNoEffects { get; set; }

		private DefaultValueDictionary<TStatus, int> currentActualValues;
		public int this[TStatus status] {
			get { return currentActualValues[status]; }
			set {
				if(!rules.SingleSource[status]) throw new InvalidOperationException("'SingleSource' must be true in order to set a value directly.");
				foreach(var source in sources[SourceType.Value][status]) {
					source.Value = value;
					return; // If any sources exist, change the value of the first one, then return.
				}
				Add(new Source<TObject, TStatus>(status, value)); // Otherwise, create a new one.
			}
		}
		protected static TStatus Convert<TOtherStatus>(TOtherStatus otherStatus) where TOtherStatus : struct {
			return EnumConverter.Convert<TOtherStatus, TStatus>(otherStatus);
		}
		public bool HasStatus(TStatus status) => currentActualValues[status] > 0;
		public bool HasStatus<TOtherStatus>(TOtherStatus status) where TOtherStatus : struct => HasStatus(Convert(status));

		private Dictionary<SourceType, DefaultValueDictionary<TStatus, int>> currentRaw;
		//public bool IsSuppressed(TStatus status) => currentRaw[SourceType.Suppression][status] > 0;
		//public bool IsPrevented(TStatus status) => currentRaw[SourceType.Prevention][status] > 0;

		private Dictionary<SourceType, MultiValueDictionary<TStatus, Source<TObject, TStatus>>> sources;

		private Dictionary<SourceType, Dictionary<TStatus, Dictionary<TStatus, int>>> internalFeeds;

		private List<DefaultValueDictionary<StatusChange<TStatus>, OnChangedHandler<TObject, TStatus>>> changeStack;

		internal BaseStatusTracker(TObject obj, BaseStatusSystem<TObject, TStatus> rules) {
			this.obj = obj;
			this.rules = rules;
			if(rules != null) rules.TrackerCreated = true;
			currentActualValues = new DefaultValueDictionary<TStatus, int>();
			currentRaw = new Dictionary<SourceType, DefaultValueDictionary<TStatus, int>>();
			sources = new Dictionary<SourceType, MultiValueDictionary<TStatus, Source<TObject, TStatus>>>();
			internalFeeds = new Dictionary<SourceType, Dictionary<TStatus, Dictionary<TStatus, int>>>();
			changeStack = new List<DefaultValueDictionary<StatusChange<TStatus>, OnChangedHandler<TObject, TStatus>>>();
			foreach(SourceType type in Enum.GetValues(typeof(SourceType))) {
				currentRaw[type] = new DefaultValueDictionary<TStatus, int>();
				sources[type] = new MultiValueDictionary<TStatus, Source<TObject, TStatus>>();
				internalFeeds[type] = new Dictionary<TStatus, Dictionary<TStatus, int>>();
			}
		}

		public bool Add(Source<TObject, TStatus> source) {
			if(source == null) throw new ArgumentNullException();
			TStatus status = source.Status;
			SourceType type = source.SourceType;
			if(type == SourceType.Value) {
				if(currentRaw[SourceType.Prevention][status] > 0) return false;
				var preventableStatuses = new List<TStatus> { status }.Concat(rules.statusesExtendedBy[status]);
				foreach(var preventableStatus in preventableStatuses) {
					if(rules.extraPreventionConditions.AnyValues(preventableStatus)) {
						foreach(var condition in rules.extraPreventionConditions[preventableStatus]) {
							if(condition(obj, preventableStatus)) return false;
						}
					}
				}
			}
			if(rules.SingleSource[status]) sources[type].Clear(status); //todo, test this
			if(sources[type].AddUnique(status, source)) {
				source.OnValueChanged += CheckSourceChanged;
				CheckSourceChanged(source);
				return true;
			}
			else return false;
		}
		//todo: definitely need xml comments for these methods
		public Source<TObject, TStatus> Add(TStatus status, int value = 1, int priority = 0, SourceType type = SourceType.Value) {
			var source = new Source<TObject, TStatus>(status, value, priority, type);
			if(Add(source)) return source;
			else return null;
		}
		public Source<TObject, TStatus, TOtherStatus> Add<TOtherStatus>(
			TOtherStatus status, int value = 1, int priority = 0, SourceType type = SourceType.Value)
			where TOtherStatus : struct
		{
			var source = new Source<TObject, TStatus, TOtherStatus>(status, value, priority, type);
			if(Add(source: source)) return source;
			else return null;
		}
		public bool Remove(Source<TObject, TStatus> source) {
			if(source == null) throw new ArgumentNullException();
			TStatus status = source.Status;
			SourceType type = source.SourceType;
			if(sources[type].Remove(status, source)) {
				source.OnValueChanged -= CheckSourceChanged;
				CheckSourceChanged(source);
				return true;
			}
			else return false;
		}
		public void Cancel(TStatus status) {
			foreach(var source in sources[SourceType.Value][status].OrderBy(x => x.Priority)) { //todo, check this - make sure it doesn't keep an iterator to valueSources.
				Remove(source);
			}
			foreach(TStatus extendingStatus in rules.statusesThatExtend[status]) Cancel(extendingStatus);
		}
		public void Cancel<TOtherStatus>(TOtherStatus status) where TOtherStatus : struct => Cancel(Convert(status));
		private OnChangedHandler<TObject, TStatus> GetHandler(TStatus status, bool increased, bool effect) {
			var change = new StatusChange<TStatus>(status, increased, effect);
			OnChangedHandler<TObject, TStatus> result;
			foreach(var dict in changeStack) {
				if(dict.TryGetValue(change, out result)) return result;
			}
			return null;
		}
		private void CheckSourceChanged(Source<TObject, TStatus> source) {
			bool stacked = source.onChangedOverrides != null;
			if(stacked) changeStack.Add(source.onChangedOverrides);
			CheckRawChanged(source.Status, source.SourceType);
			if(stacked) changeStack.RemoveAt(changeStack.Count - 1);
		}
		private void CheckRawChanged(TStatus status, SourceType type) {
			bool stacked = rules.onChangedHandlers[status] != null;
			if(stacked) changeStack.Add(rules.onChangedHandlers[status]);
			var values = sources[type][status].Select(x => x.Value);
			if(internalFeeds[type].ContainsKey(status)) values = values.Concat(internalFeeds[type][status].Values);
			IEnumerable<TStatus> upstreamStatuses; //todo: be sure to explain how this works...
			IEnumerable<TStatus> downstreamStatuses;
			if(type == SourceType.Value) {
				upstreamStatuses = rules.statusesThatExtend[status];
				downstreamStatuses = rules.statusesExtendedBy[status];
			}
			else {
				upstreamStatuses = rules.statusesExtendedBy[status];
				downstreamStatuses = rules.statusesThatExtend[status];
			}
			foreach(TStatus otherStatus in upstreamStatuses) {
				values = values.Concat(sources[type][otherStatus].Select(x => x.Value));
				if(internalFeeds[type].ContainsKey(otherStatus)) values = values.Concat(internalFeeds[type][otherStatus].Values);
			}
			int newValue = rules.GetAggregator(status, type)(values);
			int oldValue = currentRaw[type][status];
			if(newValue != oldValue) {
				currentRaw[type][status] = newValue;
				if(type == SourceType.Value || type == SourceType.Suppression) CheckActualValueChanged(status);
			}
			foreach(TStatus otherStatus in downstreamStatuses) {
				CheckRawChanged(otherStatus, type);
			}
			if(stacked) changeStack.RemoveAt(changeStack.Count - 1);
		}
		/*
		todo: put this text into a better comment format.

so, STATUS CHANGED looks like this:
Using stack, handle message & effect, in whatever order is right.
Using the rules, for each status that is fed by this one (for value, suppression, or prevention):
calculate the NEW FED VALUE from here to there, using the converter from the rules, if it exists. Otherwise, it's the same as the value.
get the OLD FED VALUE from here to there. If no source exists, it is 0. If a source exists, it is that source's value.
Compare those 2 values to see whether a change has occurred. If it has:
if the source exists, update its value.
If not, create a new source with that value, then add it to the target status.
If the value of this status just increased, using the rules, for each status that is cancelled by this one:
call Cancel on it, yeah?
		*/
		private void CheckActualValueChanged(TStatus status) {
			int newValue;
			if(currentRaw[SourceType.Suppression][status] > 0) newValue = 0;
			else newValue = currentRaw[SourceType.Value][status];
			int oldValue = currentActualValues[status];
			if(newValue != oldValue) {
				currentActualValues[status] = newValue;
				bool increased = newValue > oldValue;
				if(!GenerateNoMessages) GetHandler(status, increased, false)?.Invoke(obj, status, oldValue, newValue);
				if(!GenerateNoEffects) GetHandler(status, increased, true)?.Invoke(obj, status, oldValue, newValue);
				UpdateFeed(status, SourceType.Value, newValue);
				if(increased) {
					foreach(TStatus cancelledStatus in rules.statusesCancelledBy[status]) {
						var pair = new StatusPair<TStatus>(status, cancelledStatus);
						var condition = rules.cancellationConditions[pair]; // if a condition exists, it must return true for the
						if(condition == null || condition(newValue)) Cancel(cancelledStatus); // status to be cancelled.
					}
				}
				UpdateFeed(status, SourceType.Suppression, newValue); // Cancellations happen before suppression to prevent some infinite loops
				UpdateFeed(status, SourceType.Prevention, newValue);
			}
		}
		private void UpdateFeed(TStatus status, SourceType type, int newValue) {
			foreach(TStatus fedStatus in rules.statusesFedBy[type][status]) {
				int newFedValue = newValue;
				var pair = new StatusPair<TStatus>(status, fedStatus);
				Converter conv;
				if(rules.converters[type].TryGetValue(pair, out conv)) newFedValue = conv(newFedValue);
				int oldFedValue;
				Dictionary<TStatus, int> fedValues;
				if(internalFeeds[type].TryGetValue(fedStatus, out fedValues)) fedValues.TryGetValue(status, out oldFedValue);
				else oldFedValue = 0;
				if(newFedValue != oldFedValue) {
					if(fedValues == null) {
						fedValues = new Dictionary<TStatus, int>();
						fedValues.Add(status, newFedValue);
						internalFeeds[type].Add(fedStatus, fedValues);
					}
					else fedValues[status] = newFedValue;
					CheckRawChanged(fedStatus, type);
				}
			}
		}
	}
	public class StatusTracker<TObject> : BaseStatusTracker<TObject, int> {
		internal StatusTracker(TObject obj, BaseStatusSystem<TObject, int> rules) : base(obj, rules) { }
	}
	public class StatusTracker<TObject, TStatus1> : StatusTracker<TObject> where TStatus1 : struct {
		internal StatusTracker(TObject obj, BaseStatusSystem<TObject, int> rules) : base(obj, rules) { }
		public int this[TStatus1 status] {
			get { return this[Convert(status)]; }
			set { this[Convert(status)] = value; }
		}
	}
	public class StatusTracker<TObject, TStatus1, TStatus2> : StatusTracker<TObject, TStatus1> where TStatus1 : struct where TStatus2 : struct {
		internal StatusTracker(TObject obj, BaseStatusSystem<TObject, int> rules) : base(obj, rules) { }
		public int this[TStatus2 status] {
			get { return this[Convert(status)]; }
			set { this[Convert(status)] = value; }
		}
	}
}
