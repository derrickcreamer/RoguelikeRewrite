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

	public class BaseStatusSystem<TObject, TBaseStatus> : IHandlers<TObject, TBaseStatus> where TBaseStatus : struct {
		public class HandlerRules {
			private IHandlers<TObject, TBaseStatus> handlers;
			private TBaseStatus status;
			private TBaseStatus overridden;
			private bool effect;
			public OnChangedHandler<TObject, TBaseStatus> Increased {
				get { return handlers.GetHandler(status, overridden, true, effect); }
				set { handlers.SetHandler(status, overridden, true, effect, value); }
			}
			public OnChangedHandler<TObject, TBaseStatus> Decreased {
				get { return handlers.GetHandler(status, overridden, false, effect); }
				set { handlers.SetHandler(status, overridden, false, effect, value); }
			}
			//todo: xml comments here to explain
			public OnChangedHandler<TObject, TBaseStatus> Changed {
				set {
					handlers.SetHandler(status, overridden, true, effect, value);
					handlers.SetHandler(status, overridden, false, effect, value);
				}
			}
			internal HandlerRules(IHandlers<TObject, TBaseStatus> handlers, TBaseStatus status, TBaseStatus overridden, bool effect) {
				this.handlers = handlers;
				this.status = status;
				this.overridden = overridden;
				this.effect = effect;
			}
		}
		public class StatusHandlers {
			public readonly HandlerRules Messages, Effects;
			internal StatusHandlers(IHandlers<TObject, TBaseStatus> handlers, TBaseStatus status, TBaseStatus overridden) {
				Messages = new HandlerRules(handlers, status, overridden, false);
				Effects = new HandlerRules(handlers, status, overridden, true);
			}
		}
		public class StatusRules : StatusHandlers {
			protected BaseStatusSystem<TObject, TBaseStatus> rules;
			protected TBaseStatus status;
			protected static TBaseStatus Convert<TStatus>(TStatus status) where TStatus : struct {
				return EnumConverter.Convert<TStatus, TBaseStatus>(status);
			}
			public StatusHandlers Overrides(TBaseStatus overridden) => new StatusHandlers(rules, status, overridden);
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
			public void Extends(TBaseStatus extendedStatus) {
				rules.statusesExtendedBy.AddUnique(status, extendedStatus);
				rules.statusesThatExtend.AddUnique(extendedStatus, status);
			}
			//todo: why not (1) remove the single-status versions, and (2) allow conditions for params?
			// (answers might be (1) performance which is irrelevant at init, and (2) maybe it'd be confusing?
			//								(2) Also the condition would need to appear first.
			public void Extends(params TBaseStatus[] extendedStatuses) {
				foreach(TBaseStatus extended in extendedStatuses) {
					rules.statusesExtendedBy.AddUnique(status, extended);
					rules.statusesThatExtend.AddUnique(extended, status);
				}
			}
			public void Cancels(params TBaseStatus[] cancelledStatuses) {
				foreach(TBaseStatus cancelled in cancelledStatuses) {
					rules.statusesCancelledBy.AddUnique(status, cancelled);
				}
			}
			public void Cancels(TBaseStatus cancelledStatus, Func<int, bool> condition = null) {
				rules.statusesCancelledBy.AddUnique(status, cancelledStatus);
				if(condition != null) rules.cancellationConditions[new StatusPair<TBaseStatus>(status, cancelledStatus)] = condition;
			}
			//todo: gotta explain this one, certainly
			public void Foils(params TBaseStatus[] foiledStatuses) {
				Cancels(foiledStatuses);
				Suppresses(foiledStatuses);
				Prevents(foiledStatuses); //todo: what about cycles?
			}
			public void Foils(TBaseStatus foiledStatus, Func<int, bool> condition = null) {
				Cancels(foiledStatus, condition);
				Suppresses(foiledStatus, condition);
				Prevents(foiledStatus, condition);
			}
			//todo: if TStatus is int, 2 of these match. (It uses the non-params version.)
			public void Feeds(params TBaseStatus[] fedStatuses) => FeedsInternal(SourceType.Value, fedStatuses);
			public void Suppresses(params TBaseStatus[] suppressedStatuses) => FeedsInternal(SourceType.Suppression, suppressedStatuses);
			public void Prevents(params TBaseStatus[] preventedStatuses) => FeedsInternal(SourceType.Prevention, preventedStatuses);
			protected void FeedsInternal(SourceType type, params TBaseStatus[] fedStatuses) {
				foreach(TBaseStatus fedStatus in fedStatuses) {
					rules.statusesFedBy[type].AddUnique(status, fedStatus);
				}
			}
			public void Feeds(TBaseStatus fedStatus, Converter converter) => FeedsInternal(SourceType.Value, fedStatus, converter);
			public void Suppresses(TBaseStatus suppressedStatus, Converter converter) => FeedsInternal(SourceType.Suppression, suppressedStatus, converter);
			public void Prevents(TBaseStatus preventedStatus, Converter converter) => FeedsInternal(SourceType.Prevention, preventedStatus, converter);
			protected void FeedsInternal(SourceType type, TBaseStatus fedStatus, Converter converter) {
				rules.statusesFedBy[type].AddUnique(status, fedStatus);
				if(converter != null) {
					rules.ValidateConverter(converter);
					var pair = new StatusPair<TBaseStatus>(status, fedStatus);
					rules.converters[type][pair] = converter;
				}
			}
			public void Feeds(TBaseStatus fedStatus, int fedValue, Func<int, bool> condition = null) => FeedsInternal(SourceType.Value, fedStatus, fedValue, condition);
			//these next 2 might not make much sense to use:
			public void Suppresses(TBaseStatus suppressedStatus, int fedValue, Func<int, bool> condition = null) => FeedsInternal(SourceType.Suppression, suppressedStatus, fedValue, condition);
			public void Prevents(TBaseStatus preventedStatus, int fedValue, Func<int, bool> condition = null) => FeedsInternal(SourceType.Prevention, preventedStatus, fedValue, condition);
			protected void FeedsInternal(SourceType type, TBaseStatus fedStatus, int fedValue, Func<int, bool> condition) {
				rules.statusesFedBy[type].AddUnique(status, fedStatus);
				if(condition != null) {
					rules.converters[type][new StatusPair<TBaseStatus>(status, fedStatus)] = i => {
						if(condition(i)) return fedValue; //todo: cache this?
						else return 0; //todo: does this need a check that it returns 0 at the appropriate times?
					};
				}
				else {
					rules.converters[type][new StatusPair<TBaseStatus>(status, fedStatus)] = i => {
						if(i != 0) return fedValue; //todo, !=0? i thought it was >0. //todo: cache this?
						else return 0;
					};
				}
			}
			public void Feeds(TBaseStatus fedStatus, Func<int, bool> condition = null) => FeedsInternal(SourceType.Value, fedStatus, condition);
			public void Suppresses(TBaseStatus suppressedStatus, Func<int, bool> condition = null) => FeedsInternal(SourceType.Suppression, suppressedStatus, condition);
			public void Prevents(TBaseStatus preventedStatus, Func<int, bool> condition = null) => FeedsInternal(SourceType.Prevention, preventedStatus, condition);
			protected void FeedsInternal(SourceType type, TBaseStatus fedStatus, Func<int, bool> condition) {
				rules.statusesFedBy[type].AddUnique(status, fedStatus);
				if(condition != null) {
					rules.converters[type][new StatusPair<TBaseStatus>(status, fedStatus)] = i => {
						if(condition(i)) return i; //todo: cache?
						else return 0;
					};
				}
			}
			public void PreventedWhen(Func<TObject, TBaseStatus, bool> preventionCondition) {
				rules.extraPreventionConditions.AddUnique(status, preventionCondition);
			}
			internal StatusRules(BaseStatusSystem<TObject, TBaseStatus> rules, TBaseStatus status) : base(rules, status, status) {
				this.rules = rules;
				this.status = status;
			}
		}
		public StatusRules this[TBaseStatus status] => new StatusRules(this, status);
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

		internal DefaultValueDictionary<TBaseStatus, Aggregator> valueAggs;
		internal DefaultHashSet<TBaseStatus> SingleSource { get; private set; }

		internal MultiValueDictionary<TBaseStatus, TBaseStatus> statusesCancelledBy;
		internal MultiValueDictionary<TBaseStatus, TBaseStatus> statusesExtendedBy;
		internal MultiValueDictionary<TBaseStatus, TBaseStatus> statusesThatExtend;

		internal Dictionary<SourceType, MultiValueDictionary<TBaseStatus, TBaseStatus>> statusesFedBy;

		internal Dictionary<SourceType, Dictionary<StatusPair<TBaseStatus>, Converter>> converters;
		internal void ValidateConverter(Converter conv) {
			if(conv(0) != 0) throw new ArgumentException("Converters must output 0 when input is 0.");
		}
		internal DefaultValueDictionary<StatusPair<TBaseStatus>, Func<int, bool>> cancellationConditions;

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

		public readonly OnChangedHandler<TObject, TBaseStatus> DoNothing;
		public readonly Aggregator Total;
		public readonly Aggregator Bool;
		public readonly Aggregator MaximumOrZero;
		//todo: helpers for creating conditional converters?

		internal DefaultValueDictionary<TBaseStatus, DefaultValueDictionary<StatusChange<TBaseStatus>, OnChangedHandler<TObject, TBaseStatus>>> onChangedHandlers;
		internal MultiValueDictionary<TBaseStatus, Func<TObject, TBaseStatus, bool>> extraPreventionConditions;

		internal Aggregator GetAggregator(TBaseStatus status, SourceType type) {
			if(type == SourceType.Value) {
				Aggregator agg = valueAggs[status];
				if(agg != null) return agg;
			}
			return defaultAggs[type];
		}
		void IHandlers<TObject, TBaseStatus>.SetHandler(TBaseStatus status, TBaseStatus overridden, bool increased, bool effect, OnChangedHandler<TObject, TBaseStatus> handler) {
			if(!onChangedHandlers.ContainsKey(status)) {
				onChangedHandlers.Add(status, new DefaultValueDictionary<StatusChange<TBaseStatus>, OnChangedHandler<TObject, TBaseStatus>>());
			}
			onChangedHandlers[status][new StatusChange<TBaseStatus>(overridden, increased, effect)] = handler;
		}
		OnChangedHandler<TObject, TBaseStatus> IHandlers<TObject, TBaseStatus>.GetHandler(TBaseStatus status, TBaseStatus overridden, bool increased, bool effect) {
			if(!onChangedHandlers.ContainsKey(status)) return null;
			return onChangedHandlers[status][new StatusChange<TBaseStatus>(overridden, increased, effect)];
		}
		//todo: xml note: null is a legal value here, but the user is responsible for ensuring that no OnChanged handlers make use of the 'obj' parameter.
		protected void CheckRuleErrors() { } //todo
		public BaseStatusTracker<TObject, TBaseStatus> CreateStatusTracker(TObject obj) {
			return new BaseStatusTracker<TObject, TBaseStatus>(obj, this);
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
			valueAggs = new DefaultValueDictionary<TBaseStatus, Aggregator>();
			SingleSource = new DefaultHashSet<TBaseStatus>();
			statusesCancelledBy = new MultiValueDictionary<TBaseStatus, TBaseStatus>();
			statusesExtendedBy = new MultiValueDictionary<TBaseStatus, TBaseStatus>();
			statusesThatExtend = new MultiValueDictionary<TBaseStatus, TBaseStatus>();
			statusesFedBy = new Dictionary<SourceType, MultiValueDictionary<TBaseStatus, TBaseStatus>>();
			converters = new Dictionary<SourceType, Dictionary<StatusPair<TBaseStatus>, Func<int, int>>>();
			cancellationConditions = new DefaultValueDictionary<StatusPair<TBaseStatus>, Func<int, bool>>();
			foreach(SourceType type in Enum.GetValues(typeof(SourceType))) {
				statusesFedBy[type] = new MultiValueDictionary<TBaseStatus, TBaseStatus>();
				converters[type] = new Dictionary<StatusPair<TBaseStatus>, Func<int, int>>();
			}
			onChangedHandlers = new DefaultValueDictionary<TBaseStatus, DefaultValueDictionary<StatusChange<TBaseStatus>, OnChangedHandler<TObject, TBaseStatus>>>();
			extraPreventionConditions = new MultiValueDictionary<TBaseStatus, Func<TObject, TBaseStatus, bool>>();
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
