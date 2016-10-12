using System;
using System.Collections.Generic;
using System.Linq;
using UtilityCollections;

namespace StatusSystems {
	using Aggregator = Func<IEnumerable<int>, int>;
	using Converter = Func<int, int>;
	//sure wish I could use type params with 'using':
	//using OnChangedDictionary<TObject, TStatus> = DefaultValueDefaultValueDictionary<StatusChange<TStatus>, OnChangedHandler<TObject, TStatus>>;

	public enum SourceType { Value, Suppression, Prevention };
	
	public delegate void OnChangedHandler<TObject, TStatus>(TObject obj, TStatus status, int oldValue, int newValue);

	internal struct StatusPair<TStatus> : IEquatable<StatusPair<TStatus>> where TStatus : struct {
		public readonly TStatus status1;
		public readonly TStatus status2; //and now, some boilerplate:
		public StatusPair(TStatus status1, TStatus status2) {
			this.status1 = status1;
			this.status2 = status2;
		}
		public override int GetHashCode() { unchecked { return status1.GetHashCode() * 5557 + status2.GetHashCode(); } }
		public override bool Equals(object other) {
			if(other is StatusPair<TStatus>) return Equals((StatusPair<TStatus>)other);
			else return false;
		}
		public bool Equals(StatusPair<TStatus> other) => status1.Equals(other.status1) && status2.Equals(other.status2);
	}
	internal struct StatusChange<TStatus> : IEquatable<StatusChange<TStatus>> where TStatus : struct {
		public readonly TStatus status;
		public readonly bool increased;
		public readonly bool effect;
		public StatusChange(TStatus status, bool increased, bool effect) {
			this.status = status;
			this.increased = increased;
			this.effect = effect;
		}
		public override int GetHashCode() {
			unchecked {
				int hash = status.GetHashCode() + 857;
				if(increased) hash *= 7919;
				if(effect) hash *= 523;
				return hash;
			}
		}
		public override bool Equals(object other) {
			if(other is StatusChange<TStatus>) return Equals((StatusChange<TStatus>)other);
			else return false;
		}
		public bool Equals(StatusChange<TStatus> other) => status.Equals(other.status) && increased == other.increased && effect == other.effect;
	}

	internal interface IHandlers<TObject, TStatus> {
		OnChangedHandler<TObject, TStatus> GetHandler(TStatus status, TStatus overridden, bool increased, bool effect);
		void SetHandler(TStatus status, TStatus overridden, bool increased, bool effect, OnChangedHandler<TObject, TStatus> handler);
	}

	public class Source<TObject, TStatus> : IHandlers<TObject, TStatus> where TStatus : struct {
		public readonly TStatus Status;
		public readonly SourceType SourceType;
		internal event Action<Source<TObject, TStatus>> OnValueChanged;
		private int internalValue;
		public int Value {
			get { return internalValue; }
			set {
				if(value != internalValue) {
					internalValue = value;
					OnValueChanged?.Invoke(this);
				}
			}
		}
		public int Priority { get; set; }
		//todo: xml/docs, explain this one
		public bool TryGetStatus<TOtherStatus>(out TOtherStatus status) where TOtherStatus : struct {
			status = (TOtherStatus)(object)this.Status;
			return Enum.IsDefined(typeof(TOtherStatus), this.Status);
		}
		internal DefaultValueDictionary<StatusChange<TStatus>, OnChangedHandler<TObject, TStatus>> onChangedOverrides;
		public StatusSystem<TObject, TStatus>.StatusHandlers Overrides(TStatus overridden) => new StatusSystem<TObject, TStatus>.StatusHandlers(this, Status, overridden);
		void IHandlers<TObject, TStatus>.SetHandler(TStatus ignored, TStatus overridden, bool increased, bool effect, OnChangedHandler<TObject, TStatus> handler) {
			if(onChangedOverrides == null) onChangedOverrides = new DefaultValueDictionary<StatusChange<TStatus>, OnChangedHandler<TObject, TStatus>>();
			onChangedOverrides[new StatusChange<TStatus>(overridden, increased, effect)] = handler;
		}
		OnChangedHandler<TObject, TStatus> IHandlers<TObject, TStatus>.GetHandler(TStatus status, TStatus ignored, bool increased, bool effect) {
			if(onChangedOverrides == null) return null;
			return onChangedOverrides[new StatusChange<TStatus>(status, increased, effect)];
		}
		public Source(TStatus status, int value = 1, int priority = 0, SourceType type = SourceType.Value) {
			Status = status;
			internalValue = value;
			Priority = priority;
			SourceType = type;
		}
		public Source(Source<TObject, TStatus> copyFrom, int? value = null, int? priority = null, SourceType? type = null) {
			if(copyFrom == null) throw new ArgumentNullException("copyFrom");
			Status = copyFrom.Status;
			onChangedOverrides = copyFrom.onChangedOverrides;
			if(value == null) internalValue = copyFrom.internalValue;
			else internalValue = value.Value;
			if(priority == null) Priority = copyFrom.Priority;
			else Priority = priority.Value;
			if(type == null) SourceType = copyFrom.SourceType;
			else SourceType = type.Value;
		}
	}
	public class StatusSystem<TObject, TStatus> : IHandlers<TObject, TStatus> where TStatus : struct {
		public class HandlerRules{
			private IHandlers<TObject, TStatus> handlers;
			private TStatus status;
			private TStatus overridden;
			private bool effect;
			public OnChangedHandler<TObject, TStatus> Increased {
				get { return handlers.GetHandler(status, overridden, true, effect); }
				set { handlers.SetHandler(status, overridden, true, effect, value); }
			}
			public OnChangedHandler<TObject, TStatus> Decreased {
				get { return handlers.GetHandler(status, overridden, false, effect); }
				set { handlers.SetHandler(status, overridden, false, effect, value); }
			}
			public OnChangedHandler<TObject, TStatus> Changed {
				set {
					handlers.SetHandler(status, overridden, true, effect, value);
					handlers.SetHandler(status, overridden, false, effect, value);
				}
			}
			internal HandlerRules(IHandlers<TObject, TStatus> handlers, TStatus status, TStatus overridden, bool effect) {
				this.handlers = handlers;
				this.status = status;
				this.overridden = overridden;
				this.effect = effect;
			}
		}
		public class StatusHandlers{
			public readonly HandlerRules Messages, Effects;
			internal StatusHandlers(IHandlers<TObject, TStatus> handlers, TStatus status, TStatus overridden) {
				Messages = new HandlerRules(handlers, status, overridden, false);
				Effects = new HandlerRules(handlers, status, overridden, true);
			}
		}
		public class StatusRules : StatusHandlers {
			private StatusSystem<TObject, TStatus> rules;
			private TStatus status;
			public StatusHandlers Overrides(TStatus overridden) => new StatusHandlers(rules, status, overridden);
			public Aggregator Aggregator {
				get { return rules.valueAggs[status]; }
				set {
					if(value == null) rules.valueAggs.Remove(status);
					else {
						rules.ValidateAggregator(value);
						rules.valueAggs[status] = value;
					}
				}
			}
			public bool SingleSource {
				get { return rules.SingleSource[status]; }
				set { rules.SingleSource[status] = value; }
			}
			public void Extends(TStatus extendedStatus) {
				rules.statusesExtendedBy.AddUnique(status, extendedStatus);
				rules.statusesThatExtend.AddUnique(extendedStatus, status);
			}
			public void Extends(params TStatus[] extendedStatuses) {
				foreach(TStatus extended in extendedStatuses) {
					rules.statusesExtendedBy.AddUnique(status, extended);
					rules.statusesThatExtend.AddUnique(extended, status);
				}
			}
			public void Cancels(params TStatus[] cancelledStatuses) {
				foreach(TStatus cancelled in cancelledStatuses) {
					rules.statusesCancelledBy.AddUnique(status, cancelled);
				}
			}
			public void Cancels(TStatus cancelledStatus, Func<int, bool> condition = null) {
				rules.statusesCancelledBy.AddUnique(status, cancelledStatus);
				if(condition != null) rules.cancellationConditions[new StatusPair<TStatus>(status, cancelledStatus)] = condition;
			}
			//todo: gotta explain this one, certainly
			public void Foils(params TStatus[] foiledStatuses) {
				Cancels(foiledStatuses);
				Suppresses(foiledStatuses);
				Prevents(foiledStatuses); //todo: what about cycles?
			}
			public void Foils(TStatus foiledStatus, Func<int, bool> condition = null) {
				Cancels(foiledStatus, condition);
				Suppresses(foiledStatus,condition);
				Prevents(foiledStatus, condition);
			}
			//todo: if TStatus is int, 2 of these match. (It uses the non-params version.)
			public void Feeds(params TStatus[] fedStatuses) => FeedsInternal(SourceType.Value, fedStatuses);
			public void Suppresses(params TStatus[] suppressedStatuses) => FeedsInternal(SourceType.Suppression, suppressedStatuses);
			public void Prevents(params TStatus[] preventedStatuses) => FeedsInternal(SourceType.Prevention, preventedStatuses);
			private void FeedsInternal(SourceType type, params TStatus[] fedStatuses) {
				foreach(TStatus fedStatus in fedStatuses) {
					rules.statusesFedBy[type].AddUnique(status, fedStatus);
				}
			}
			public void Feeds(TStatus fedStatus, Converter converter) => FeedsInternal(SourceType.Value, fedStatus, converter);
			public void Suppresses(TStatus suppressedStatus, Converter converter) => FeedsInternal(SourceType.Suppression, suppressedStatus, converter);
			public void Prevents(TStatus preventedStatus, Converter converter) => FeedsInternal(SourceType.Prevention, preventedStatus, converter);
			private void FeedsInternal(SourceType type, TStatus fedStatus, Converter converter) {
				rules.statusesFedBy[type].AddUnique(status, fedStatus);
				if(converter != null) {
					rules.ValidateConverter(converter);
					var pair = new StatusPair<TStatus>(status, fedStatus);
					rules.converters[type][pair] = converter;
				}
			}
			public void Feeds(TStatus fedStatus, int fedValue, Func<int, bool> condition = null) => FeedsInternal(SourceType.Value, fedStatus, fedValue, condition);
			//these next 2 might not make much sense to use:
			public void Suppresses(TStatus suppressedStatus, int fedValue, Func<int, bool> condition = null) => FeedsInternal(SourceType.Suppression, suppressedStatus, fedValue, condition);
			public void Prevents(TStatus preventedStatus, int fedValue, Func<int, bool> condition = null) => FeedsInternal(SourceType.Prevention, preventedStatus, fedValue, condition);
			private void FeedsInternal(SourceType type, TStatus fedStatus, int fedValue, Func<int, bool> condition) {
				rules.statusesFedBy[type].AddUnique(status, fedStatus);
				if(condition != null) {
					rules.converters[type][new StatusPair<TStatus>(status, fedStatus)] = i => {
						if(condition(i)) return fedValue; //todo: cache this?
						else return 0; //todo: does this need a check that it returns 0 at the appropriate times?
					};
				}
				else {
					rules.converters[type][new StatusPair<TStatus>(status, fedStatus)] = i => {
						if(i != 0) return fedValue; //todo, !=0? i thought it was >0. //todo: cache this?
						else return 0;
					};
				}
			}
			public void Feeds(TStatus fedStatus, Func<int, bool> condition = null) => FeedsInternal(SourceType.Value, fedStatus, condition);
			public void Suppresses(TStatus suppressedStatus, Func<int, bool> condition = null) => FeedsInternal(SourceType.Suppression, suppressedStatus, condition);
			public void Prevents(TStatus preventedStatus, Func<int, bool> condition = null) => FeedsInternal(SourceType.Prevention, preventedStatus, condition);
			private void FeedsInternal(SourceType type, TStatus fedStatus, Func<int, bool> condition) {
				rules.statusesFedBy[type].AddUnique(status, fedStatus);
				if(condition != null) {
					rules.converters[type][new StatusPair<TStatus>(status, fedStatus)] = i => {
						if(condition(i)) return i; //todo: cache?
						else return 0;
					};
				}
			}
			public void PreventedWhen(Func<TObject, TStatus, bool> preventionCondition) {
				rules.extraPreventionConditions.AddUnique(status, preventionCondition);
			}
			internal StatusRules(StatusSystem<TObject, TStatus> rules, TStatus status) : base(rules, status, status) {
				this.rules = rules;
				this.status = status;
			}
		}
		public StatusRules this[TStatus status] => new StatusRules(this, status);
		private Dictionary<SourceType, Aggregator> defaultAggs;
		public Aggregator DefaultValueAggregator {
			get { return defaultAggs[SourceType.Value]; }
			set {
				if(value == null) throw new ArgumentNullException("value", "Default aggregators cannot be null.");
				ValidateAggregator(value);
				defaultAggs[SourceType.Value] = value;
			}
		}
		internal void ValidateAggregator(Aggregator agg) {
			if(agg(Enumerable.Empty<int>()) != 0) throw new ArgumentException("Aggregators must have a base value of 0.");
		}

		internal DefaultValueDictionary<TStatus, Aggregator> valueAggs;
		internal DefaultHashSet<TStatus> SingleSource { get; private set; }

		internal MultiValueDictionary<TStatus, TStatus> statusesCancelledBy;
		internal MultiValueDictionary<TStatus, TStatus> statusesExtendedBy;
		internal MultiValueDictionary<TStatus, TStatus> statusesThatExtend;

		internal Dictionary<SourceType, MultiValueDictionary<TStatus, TStatus>> statusesFedBy;

		internal Dictionary<SourceType, Dictionary<StatusPair<TStatus>, Converter>> converters;
		internal void ValidateConverter(Converter conv) {
			if(conv(0) != 0) throw new ArgumentException("Converters must output 0 when input is 0.");
		}
		internal DefaultValueDictionary<StatusPair<TStatus>, Func<int, bool>> cancellationConditions;

		private bool trackerCreated;
		public bool IgnoreRuleErrors { get; set; } //todo: so this is the performance one, right? It ignores ALL of them, no matter how breaking.

		public readonly OnChangedHandler<TObject, TStatus> DoNothing;
		public readonly Aggregator Total;
		public readonly Aggregator Bool;
		public readonly Aggregator MaximumOrZero;
		//todo: helpers for creating conditional converters?

		internal DefaultValueDictionary<TStatus, DefaultValueDictionary<StatusChange<TStatus>, OnChangedHandler<TObject, TStatus>>> onChangedHandlers;
		internal MultiValueDictionary<TStatus, Func<TObject, TStatus, bool>> extraPreventionConditions;

		internal Aggregator GetAggregator(TStatus status, SourceType type) {
			if(type == SourceType.Value) {
				Aggregator agg = valueAggs[status];
				if(agg != null) return agg;
			}
			return defaultAggs[type];
		}
		void IHandlers<TObject, TStatus>.SetHandler(TStatus status, TStatus overridden, bool increased, bool effect, OnChangedHandler<TObject, TStatus> handler) {
			if(!onChangedHandlers.ContainsKey(status)) {
				onChangedHandlers.Add(status, new DefaultValueDictionary<StatusChange<TStatus>, OnChangedHandler<TObject, TStatus>>());
			}
			onChangedHandlers[status][new StatusChange<TStatus>(overridden, increased, effect)] = handler;
		}
		OnChangedHandler<TObject, TStatus> IHandlers<TObject, TStatus>.GetHandler(TStatus status, TStatus overridden, bool increased, bool effect) {
			if(!onChangedHandlers.ContainsKey(status)) return null;
			return onChangedHandlers[status][new StatusChange<TStatus>(overridden, increased, effect)];
		}
		//todo: xml note: null is a legal value here, but the user is responsible for ensuring that no OnChanged handlers make use of the 'obj' parameter.
		public StatusTracker<TObject, TStatus> CreateStatusTracker(TObject obj) {
			if(!trackerCreated) {
				trackerCreated = true; //todo: after trackerCreated is true, should it throw on rule changes?
				CheckRuleErrors();
			}
			return new StatusTracker<TObject, TStatus>(obj, this);
		}
		private enum RelationType { Self, Extends, Feeds, Suppresses, Cancels, Prevents }; //todo...this needs to reflect one thing...
		//...like, isn't this only supposed to include the ones that "directly" do these things?
		//I mean, going through some ones that cancel and suppress each other...means that A isn't actually going to feed X.
		// so, doesn't this only matter for unbroken chains? "A will directly <verb> X"
		private class StatusRelationship { //todo, move this to another file?
			public bool ChainBroken, Conditional, IsCycle, IsNegative;
			public List<TStatus> Path;
			public RelationType? Relation;
			//todo? does each of these need to know which type of relationship this link represents?
			//(which of the 6)
			//er, well, that isn't the 'type of relationship' from A to B - it would just be the
			//type of relationship for the last link in the chain.
			//or, more accurately, it would be **what's happening to status B**. That's why it's useful.
			public StatusRelationship(TStatus rootStatus) {
				Path = new List<TStatus> { rootStatus };
				Relation = RelationType.Self;
			}
			public StatusRelationship(StatusRelationship last, TStatus nextStatus,
				RelationType relation, bool nextStepIsNegative, bool nextStepHasCondition)
			{
				if(!last.ChainBroken) Relation = relation; // Relation matters only if the chain is unbroken. "Changing A will directly do <verb> to B."
				if(nextStepIsNegative) {
					ChainBroken = true;
					IsNegative = !last.IsNegative;
				}
				else {
					ChainBroken = last.ChainBroken;
					IsNegative = last.IsNegative;
				}
				if(nextStepHasCondition) Conditional = true;
				else Conditional = last.Conditional;
				Path = new List<TStatus>(last.Path) { nextStatus };
				if(last.Path.Contains(nextStatus)) IsCycle = true;
			}
		}
		private void CheckRuleErrors() { //todo
			//

		}
		//todo! rearrange all this, somehow, eventually.
		private MultiValueDictionary<StatusPair<TStatus>, StatusRelationship> relationships; //todo init
		private void UpdateDict(TStatus targetStatus, TStatus baseStatus, StatusRelationship relationship) {
			relationships.Add(new StatusPair<TStatus>(baseStatus, targetStatus), relationship);
		}
		private void TraverseTree(TStatus targetStatus, TStatus baseStatus, StatusRelationship relationship) {
			UpdateDict(targetStatus, baseStatus, relationship);
			if(relationship.IsCycle) return;
			TraverseTreeForAll(statusesExtendedBy, relationship, targetStatus, baseStatus,
				RelationType.Extends, false,
				(s1, s2) => false);
			TraverseTreeForAll(statusesFedBy[SourceType.Value], relationship, targetStatus, baseStatus,
				RelationType.Feeds, false,
				(s1, s2) => converters[SourceType.Value].ContainsKey(new StatusPair<TStatus>(s1, s2)));
			TraverseTreeForAll(statusesFedBy[SourceType.Suppression], relationship, targetStatus, baseStatus,
				RelationType.Suppresses, true,
				(s1, s2) => converters[SourceType.Suppression].ContainsKey(new StatusPair<TStatus>(s1, s2)));
			TraverseTreeForAll(statusesCancelledBy, relationship, targetStatus, baseStatus,
				RelationType.Cancels, true,
				(s1, s2) => cancellationConditions[new StatusPair<TStatus>(s1, s2)] != null);
			TraverseTreeForAll(statusesFedBy[SourceType.Prevention], relationship, targetStatus, baseStatus,
				RelationType.Prevents, true,
				(s1, s2) => converters[SourceType.Prevention].ContainsKey(new StatusPair<TStatus>(s1, s2)));
		}
		private void TraverseTreeForAll(MultiValueDictionary<TStatus, TStatus> dict,
			StatusRelationship relationship,
			TStatus targetStatus,
			TStatus baseStatus,
			RelationType relation,
			bool negative,
			Func<TStatus, TStatus, bool> hasCondition)
		{
			foreach(var otherStatus in dict[targetStatus]) {
				var nextRelationship = new StatusRelationship(
					relationship, otherStatus, relation, negative, hasCondition(targetStatus, otherStatus));
				TraverseTree(otherStatus, baseStatus, nextRelationship);
			}
		}
		public StatusSystem() {
			DoNothing = (obj, status, ov, nv) => { };
			Total = ints => {
				int total = 0;
				foreach(int i in ints) total += i;
				return total;
			};
			Bool = ints => {
				int total = 0;
				foreach(int i in ints) total += i;
				if(total > 0) return 1;
				else return 0;
			};
			MaximumOrZero = ints => {
				int max = 0;
				foreach(int i in ints) if(i > max) max = i;
				return max;
			};
			defaultAggs = new Dictionary<SourceType, Func<IEnumerable<int>, int>>();
			defaultAggs[SourceType.Value] = Total;
			defaultAggs[SourceType.Suppression] = Bool;
			defaultAggs[SourceType.Prevention] = Bool;
			valueAggs = new DefaultValueDictionary<TStatus, Aggregator>();
			SingleSource = new DefaultHashSet<TStatus>();
			statusesCancelledBy = new MultiValueDictionary<TStatus, TStatus>();
			statusesExtendedBy = new MultiValueDictionary<TStatus, TStatus>();
			statusesThatExtend = new MultiValueDictionary<TStatus, TStatus>();
			statusesFedBy = new Dictionary<SourceType, MultiValueDictionary<TStatus, TStatus>>();
			converters = new Dictionary<SourceType, Dictionary<StatusPair<TStatus>, Func<int, int>>>();
			cancellationConditions = new DefaultValueDictionary<StatusPair<TStatus>, Func<int, bool>>();
			foreach(SourceType type in Enum.GetValues(typeof(SourceType))) {
				statusesFedBy[type] = new MultiValueDictionary<TStatus, TStatus>();
				converters[type] = new Dictionary<StatusPair<TStatus>, Func<int, int>>();
			}
			onChangedHandlers = new DefaultValueDictionary<TStatus, DefaultValueDictionary<StatusChange<TStatus>, OnChangedHandler<TObject, TStatus>>>();
			extraPreventionConditions = new MultiValueDictionary<TStatus, Func<TObject, TStatus, bool>>();
		}
	}
	public class StatusTracker<TObject, TStatus> where TStatus : struct {
		private TObject obj;
		private StatusSystem<TObject, TStatus> rules;

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
				Add(new Source<TObject, TStatus>(status,value)); // Otherwise, create a new one.
			}
		}
		public bool HasStatus(TStatus status) => currentActualValues[status] > 0;

		private Dictionary<SourceType, DefaultValueDictionary<TStatus, int>> currentRaw;
		//public bool IsSuppressed(TStatus status) => currentRaw[SourceType.Suppression][status] > 0;
		//public bool IsPrevented(TStatus status) => currentRaw[SourceType.Prevention][status] > 0;

		private Dictionary<SourceType, MultiValueDictionary<TStatus, Source<TObject, TStatus>>> sources;

		private Dictionary<SourceType, Dictionary<TStatus, Dictionary<TStatus, int>>> internalFeeds;

		private List<DefaultValueDictionary<StatusChange<TStatus>, OnChangedHandler<TObject, TStatus>>> changeStack;

		internal StatusTracker(TObject obj, StatusSystem<TObject, TStatus> rules) {
			this.obj = obj;
			this.rules = rules;
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
				var preventableStatuses = new List<TStatus>{ status }.Concat(rules.statusesExtendedBy[status]);
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
		public void Cancel(TStatus status){
			foreach(var source in sources[SourceType.Value][status].OrderBy(x => x.Priority)) { //todo, check this - make sure it doesn't keep an iterator to valueSources.
				Remove(source);
			}
			foreach(TStatus extendingStatus in rules.statusesThatExtend[status]) Cancel(extendingStatus);
		}
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
			var values = sources[type][status].Select(x=>x.Value);
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
		private void UpdateFeed(TStatus status, SourceType type, int newValue){
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
}
