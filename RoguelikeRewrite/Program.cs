﻿using System;
using StatusSystems;


namespace RoguelikeRewrite {
	class Program {
		public enum Status { Stunned, Poisoned, Spored, Burning, ImmuneBurning, Slimed, Oiled, Frozen, Grabbed, Grabbing, Flying, Hasted, Confused };
		public enum Spell { ProtectionFromMentalHarm = Status.Confused + 1, WindForm };
		public class Actor { }
		static void Main(string[] args) {
			var rules = new StatusSystem<Actor,Status>();
			rules.DefaultValueAggregator = rules.Bool;
			rules.ParseRulesText("statustest.txt",ExtraEnums.Create<Spell>());
		}
	}
}