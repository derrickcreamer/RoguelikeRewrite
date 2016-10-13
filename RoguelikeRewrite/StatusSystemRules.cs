using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UtilityCollections;

namespace NewStatusSystems { //todo, remember to change namespace

	using Aggregator = Func<IEnumerable<int>, int>;
	using Converter = Func<int, int>;

	[StructLayout(LayoutKind.Explicit)]
	internal struct SharedEnum<TEnum1, TEnum2> where TEnum1 : struct where TEnum2 : struct {
		[FieldOffset(0)]
		public TEnum1 e1;
		[FieldOffset(0)]
		public TEnum2 e2;
	}
	internal static class EnumConverter {
		//todo: xml note: This is intended to convert freely (and without boxing) between ints and int-backed enums.
		public static TResult Convert<T, TResult>(T value) where T : struct where TResult : struct {
			var shared = new SharedEnum<T, TResult>();
			shared.e1 = value;
			return shared.e2;
		}
	}

	public enum SourceType { Value, Suppression, Prevention };

	public delegate void OnChangedHandler<TObject, TStatus>(TObject obj, TStatus status, int oldValue, int newValue);

	internal interface IHandlers<TObject, TStatus> {
		OnChangedHandler<TObject, TStatus> GetHandler(TStatus status, TStatus overridden, bool increased, bool effect);
		void SetHandler(TStatus status, TStatus overridden, bool increased, bool effect, OnChangedHandler<TObject, TStatus> handler);
	}

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

	public class BaseStatusSystem<TObject, TStatus> : IHandlers<TObject, TStatus> where TStatus : struct {
		public class HandlerRules {
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
			//todo: xml comments here to explain
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
		public class StatusHandlers {
			public readonly HandlerRules Messages, Effects;
			internal StatusHandlers(IHandlers<TObject, TStatus> handlers, TStatus status, TStatus overridden) {
				Messages = new HandlerRules(handlers, status, overridden, false);
				Effects = new HandlerRules(handlers, status, overridden, true);
			}
		}
		public class StatusRules : StatusHandlers {
			protected BaseStatusSystem<TObject, TStatus> rules;
			protected TStatus status;
			protected static TStatus Convert<TOtherStatus>(TOtherStatus otherStatus) where TOtherStatus : struct {
				return EnumConverter.Convert<TOtherStatus, TStatus>(otherStatus);
			}
			public StatusHandlers Overrides(TStatus overridden) => new StatusHandlers(rules, status, overridden);
			public StatusHandlers Overrides<TAnyStatus>(TAnyStatus overridden) where TAnyStatus : struct {
				return Overrides(Convert(overridden));
			}
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
			//todo: why not (1) remove the single-status versions, and (2) allow conditions for params?
			// (answers might be (1) performance which is irrelevant at init, and (2) maybe it'd be confusing?
			//								(2) Also the condition would need to appear first.
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
				Suppresses(foiledStatus, condition);
				Prevents(foiledStatus, condition);
			}
			//todo: if TStatus is int, 2 of these match. (It uses the non-params version.)
			public void Feeds(params TStatus[] fedStatuses) => FeedsInternal(SourceType.Value, fedStatuses);
			public void Suppresses(params TStatus[] suppressedStatuses) => FeedsInternal(SourceType.Suppression, suppressedStatuses);
			public void Prevents(params TStatus[] preventedStatuses) => FeedsInternal(SourceType.Prevention, preventedStatuses);
			protected void FeedsInternal(SourceType type, params TStatus[] fedStatuses) {
				foreach(TStatus fedStatus in fedStatuses) {
					rules.statusesFedBy[type].AddUnique(status, fedStatus);
				}
			}
			public void Feeds(TStatus fedStatus, Converter converter) => FeedsInternal(SourceType.Value, fedStatus, converter);
			public void Suppresses(TStatus suppressedStatus, Converter converter) => FeedsInternal(SourceType.Suppression, suppressedStatus, converter);
			public void Prevents(TStatus preventedStatus, Converter converter) => FeedsInternal(SourceType.Prevention, preventedStatus, converter);
			protected void FeedsInternal(SourceType type, TStatus fedStatus, Converter converter) {
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
			protected void FeedsInternal(SourceType type, TStatus fedStatus, int fedValue, Func<int, bool> condition) {
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
			protected void FeedsInternal(SourceType type, TStatus fedStatus, Func<int, bool> condition) {
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
			internal StatusRules(BaseStatusSystem<TObject, TStatus> rules, TStatus status) : base(rules, status, status) {
				this.rules = rules;
				this.status = status;
			}
		}
		public StatusRules this[TStatus status] => new StatusRules(this, status);
		protected Dictionary<SourceType, Aggregator> defaultAggs;
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

		protected bool trackerCreated;
		internal bool TrackerCreated {
			get { return trackerCreated; }
			set {
				if(trackerCreated) return; // Once true, it stays true and does nothing else.
				trackerCreated = value; //todo: after trackerCreated is true, should it throw on rule changes?
				if(value && !IgnoreRuleErrors) CheckRuleErrors();
			}
		}
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
		protected void CheckRuleErrors() { } //todo
		public BaseStatusTracker<TObject, TStatus> CreateStatusTracker(TObject obj) {
			return new BaseStatusTracker<TObject, TStatus>(obj, this);
		}
		public BaseStatusSystem() {
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
	public class StatusSystem<TObject> : BaseStatusSystem<TObject, int> {
		new public StatusTracker<TObject, int> CreateStatusTracker(TObject obj) => null; //todo
	}
	public class StatusSystem<TObject, TStatus1> : StatusSystem<TObject> where TStatus1 : struct {
		public StatusRules this[TStatus1 status] => null; //todo
		new public StatusTracker<TObject, TStatus1> CreateStatusTracker(TObject obj) => null; //todo
	}
	public class StatusSystem<TObject, TStatus1, TStatus2> : StatusSystem<TObject, TStatus1> where TStatus1 : struct where TStatus2 : struct{
		public StatusRules this[TStatus2 status] => null; //todo
		new public StatusTracker<TObject, TStatus1, TStatus2> CreateStatusTracker(TObject obj) => null; //todo
	}
}
