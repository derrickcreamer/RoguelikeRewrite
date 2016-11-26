using System;
using System.Collections.Generic;
using UtilityCollections;
using SunshineConsole;

namespace RoguelikeRewrite {
	class Program {
		static void Main(string[] args) {
			ConsoleWindow w = new ConsoleWindow(10, 10, "hmm");
			w.Write(0, 0, '!', OpenTK.Graphics.Color4.Azure);
			w.WindowUpdate();
			w.Exit();
		}
	}
}
