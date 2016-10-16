using System;
using System.Collections.Generic;
using System.Linq;
using UtilityCollections;

namespace StatusSystems {
	internal class RuleChecker<TObject, TStatus> where TStatus : struct {
		private BaseStatusSystem<TObject, TStatus> rules;
		private MultiValueDictionary<TStatus, Relationship> relationships = new MultiValueDictionary<TStatus, Relationship>();
		private DefaultHashSet<TStatus> started = new DefaultHashSet<TStatus>();
		private DefaultHashSet<TStatus> completed = new DefaultHashSet<TStatus>();
		internal RuleChecker(BaseStatusSystem<TObject, TStatus> rules) {
			this.rules = rules;
			CheckRules();
		}
		// There is room for improvement here:  errors/warnings could be proper objects, not just strings.
		internal List<string> GetErrors() {
			List<string> result = new List<string>();
			foreach(Relationship r in relationships.GetAllValues()) {
				if(r.Path.Count == 1) continue;
				if(!r.ChainBroken && r.SourceStatus.Equals(r.TargetStatus) && !r.IsConditional && !r.IsNegative) {
					string error = $"Status \"{r.SourceStatus}\" feeds itself infinitely. "; //todo: gotta try to get the enum name. All enum names, actually!
					error += $"     \r\n Path: {string.Join(" -> ", r.Path)}";
                    result.Add(error);
				}
				if(!r.ChainBroken && r.SourceStatus.Equals(r.TargetStatus) && r.Relation == RelationType.Suppresses) {
					string error = $"Status \"{r.SourceStatus}\" suppresses itself. This will always cause an infinite loop. ";
					error += $"     \r\n Path: {string.Join(" -> ", r.Path)}";
					result.Add(error);
				}
			}
			return result;
		}
		internal List<string> GetWarnings() {
			List<string> result = new List<string>();
			//todo!
			return result;
		}
		private void CheckRules() {
			IEnumerable<KeyValuePair<TStatus, IEnumerable<TStatus>>> allPairs;
			allPairs = rules.statusesFedBy[SourceType.Value];
			allPairs = allPairs.Concat(rules.statusesFedBy[SourceType.Suppression]);
			allPairs = allPairs.Concat(rules.statusesFedBy[SourceType.Prevention]);
			allPairs = allPairs.Concat(rules.statusesCancelledBy);
			allPairs = allPairs.Concat(rules.statusesExtendedBy);
			foreach(var pair in allPairs) { // For every rule...
				foreach(var target in pair.Value) {
					Explore(target);
				}
				Explore(pair.Key);
			}
		}
		private void Record(Relationship relationship) {
			if(relationship != null) relationships.Add(relationship.SourceStatus, relationship);
		}
		private void Explore(TStatus status) {
			if(completed[status]) return;
			started[status] = true;
			Relationship identity = new Relationship{ Path = new List<TStatus> { status }, Relation = RelationType.Self };
			Record(identity); // Record the 'self' relationship for this status.
			foreach(var relationship in GetConnectionsFrom(status)) {
				TStatus targetStatus = relationship.TargetStatus;
				if(!completed[targetStatus] && !started[targetStatus]) {
					Explore(targetStatus);
				}
				if(completed[targetStatus]) {
					MergeExisting(relationship, targetStatus);
				}
				else {
					ExploreManually(status, identity, null);
				}
			}
			completed[status] = true;
		}
		private void ExploreManually(TStatus status, Relationship pathSoFar, DefaultHashSet<TStatus> visited) {
			if(visited == null) visited = new DefaultHashSet<TStatus>();
			visited[status] = true;
			foreach(var relationship in GetConnectionsFrom(status)) {
				TStatus targetStatus = relationship.TargetStatus;
				Relationship fullPathRelationship = GetMerged(pathSoFar, relationship);
				if(!visited[targetStatus]) {
					visited[targetStatus] = true;
					if(completed[targetStatus]) {
						MergeExisting(fullPathRelationship, targetStatus);
					}
					else {
						Record(fullPathRelationship);
						ExploreManually(targetStatus, fullPathRelationship, visited);
					}
				}
				else {
					Record(fullPathRelationship);
				}
			}
			completed[status] = true;
		}
		private IEnumerable<Relationship> GetConnectionsFrom(TStatus status) {
			foreach(TStatus targetStatus in rules.statusesExtendedBy[status]) {
				yield return new Relationship {
					ChainBroken = false, IsConditional = false, IsNegative = false,
					Path = new List<TStatus> { status, targetStatus },
					Relation = RelationType.Extends
				};
			}
			foreach(TStatus targetStatus in rules.statusesCancelledBy[status]) {
				var pair = new StatusPair<TStatus>(status, targetStatus);
				bool conditional = rules.cancellationConditions[pair] != null;
				yield return new Relationship {
					ChainBroken = false, IsConditional = conditional, IsNegative = true,
					Path = new List<TStatus> { status, targetStatus },
					Relation = RelationType.Cancels
				};
			}
			foreach(SourceType sourceType in new SourceType[] { SourceType.Value, SourceType.Suppression, SourceType.Prevention }) {
				bool isNegative = sourceType != SourceType.Value;
				RelationType relation;
				switch(sourceType) {
					case SourceType.Value:
						relation = RelationType.Feeds;
						break;
					case SourceType.Suppression:
						relation = RelationType.Suppresses;
						break;
					case SourceType.Prevention:
						relation = RelationType.Prevents;
						break;
					default: throw new NotImplementedException();
				}
				foreach(TStatus targetStatus in rules.statusesFedBy[sourceType][status]) {
					var pair = new StatusPair<TStatus>(status, targetStatus);
					bool conditional = rules.converters[sourceType].ContainsKey(pair);
					yield return new Relationship {
						ChainBroken = false, IsConditional = conditional, IsNegative = isNegative,
						Path = new List<TStatus> { status, targetStatus },
						Relation = relation
					};
				}
			}
		}
		private void MergeExisting(Relationship relationship, TStatus targetStatus) {
			TStatus status = relationship.SourceStatus;
			foreach(Relationship targetRelationship in relationships[targetStatus]) {
				Record(GetMerged(relationship, targetRelationship));
			}
		}
		private Relationship GetMerged(Relationship first, Relationship second) {
			if(first == null) return second;
			if(second == null) return first;
			if(!first.TargetStatus.Equals(second.SourceStatus)) throw new InvalidOperationException("The 2nd status must start where the 1st one ends.");
			var shortenedSecondPath = second.Path.Skip(1).ToList(); // Remove the duplicate.
			foreach(TStatus firstPathStatus in first.Path) { // If any status appears twice in the combined path, it must END on the 2nd one.
				int idx = shortenedSecondPath.IndexOf(firstPathStatus); // If not, this one is extraneous - return null.
				if(idx != -1 && idx != shortenedSecondPath.Count - 1) return null;
			}
			Relationship result = new Relationship();
			result.Path = new List<TStatus>(first.Path.Concat(shortenedSecondPath)); // Combine the paths.
			result.ChainBroken = first.ChainBroken || second.ChainBroken || first.IsNegative;
			result.IsConditional = first.IsConditional || second.IsConditional;
			result.IsNegative = first.IsNegative ^ second.IsNegative;
			if(second.Relation == RelationType.Self) result.Relation = first.Relation; // Relation represents the final link in the chain.
			else result.Relation = second.Relation; // (But if it's 'self', that doesn't get copied over.)
			return result;
		}
		internal enum RelationType { Self, Extends, Feeds, Suppresses, Cancels, Prevents };
		internal class Relationship {
			public RelationType Relation;
			public bool ChainBroken, IsConditional, IsNegative;
			public List<TStatus> Path;
			public TStatus SourceStatus => Path[0];
			public TStatus TargetStatus => Path[Path.Count - 1];
		}
	}
}
