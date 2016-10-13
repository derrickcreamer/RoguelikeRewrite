using System;
using UtilityCollections;

namespace NewStatusSystems {
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
			return Enum.IsDefined(typeof(TOtherStatus), this.Status); //todo! This obviously only works for enums. What to do?
		}
		internal DefaultValueDictionary<StatusChange<TStatus>, OnChangedHandler<TObject, TStatus>> onChangedOverrides;
		public BaseStatusSystem<TObject, TStatus>.StatusHandlers Overrides(TStatus overridden) => new BaseStatusSystem<TObject, TStatus>.StatusHandlers(this, Status, overridden);
		public BaseStatusSystem<TObject, TStatus>.StatusHandlers Overrides<TOtherStatus>(TOtherStatus overridden) where TOtherStatus : struct
			=> new BaseStatusSystem<TObject, TStatus>.StatusHandlers(this, Status, Convert(overridden)); //todo: Just make sure this one works.
		void IHandlers<TObject, TStatus>.SetHandler(TStatus ignored, TStatus overridden, bool increased, bool effect, OnChangedHandler<TObject, TStatus> handler) {
			if(onChangedOverrides == null) onChangedOverrides = new DefaultValueDictionary<StatusChange<TStatus>, OnChangedHandler<TObject, TStatus>>();
			onChangedOverrides[new StatusChange<TStatus>(overridden, increased, effect)] = handler;
		}
		OnChangedHandler<TObject, TStatus> IHandlers<TObject, TStatus>.GetHandler(TStatus status, TStatus ignored, bool increased, bool effect) {
			if(onChangedOverrides == null) return null;
			return onChangedOverrides[new StatusChange<TStatus>(status, increased, effect)];
		}
		protected static TStatus Convert<TOtherStatus>(TOtherStatus otherStatus) where TOtherStatus : struct {
			return EnumConverter.Convert<TOtherStatus, TStatus>(otherStatus);
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
	public class Source<TObject, TBaseStatus, TOtherStatus> : Source<TObject, TBaseStatus>
		where TBaseStatus : struct
		where TOtherStatus : struct
	{
		public Source(TOtherStatus status, int value = 1, int priority = 0, SourceType type = SourceType.Value)
			: base(Convert(status), value, priority, type)
		{
			this.Status = status;
			this.BaseStatus = Convert(status);
		}
		new public readonly TOtherStatus Status; //todo make sure this *works* well.
		public readonly TBaseStatus BaseStatus;
	}
}
