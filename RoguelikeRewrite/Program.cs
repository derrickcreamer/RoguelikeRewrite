using System;
using System.Collections.Generic;
using UtilityCollections;
using SunshineConsole;
using GameComponents;
using GameComponents.DirectionUtility;

namespace RoguelikeRewrite {
	class Program {
		static void Main(string[] args) {
			int i = 0;
			/*foreach(var x in EightDirections.Enumerate(false, true, true, Dir8.SW)) {
				var y = x;
			}
			Point p = new Point(0, 0);
			foreach(Point p2 in p.EnumeratePointsAtManhattanDistance(5, true)) {
				Point p3 = p2;
				if(++i >= 1000) break;
			}
			return;*/
			foreach(Point p4 in new Point(15, 15).EnumeratePointsByChebyshevDistance(false, true)) {
				if(++i >= 1000) break;
			}
			ConsoleWindow w = new ConsoleWindow(30, 30, "hmm");
			w.Write(15, 15, '@', OpenTK.Graphics.Color4.YellowGreen);
			foreach(var dir in Dir8.S.GetDirectionsInArc(2, false, true)) {
				Point p5 = new Point(15,15).PointInDir(dir);
				w.Write(30-p5.Y, p5.X, '#', OpenTK.Graphics.Color4.Azure);
				System.Threading.Thread.Sleep(1000);
				if(!w.WindowUpdate()) break;
			}
			System.Threading.Thread.Sleep(4000);
			foreach(Point p4 in new Point(15, 15).EnumeratePointsByManhattanDistance(true, Dir4.W)) {
				w.Write(30-p4.Y, p4.X, '@', OpenTK.Graphics.Color4.Lime);
				System.Threading.Thread.Sleep(100);
				if(!w.WindowUpdate()) break;
			}
			//w.Write(0, 0, '!', OpenTK.Graphics.Color4.Azure);
			//w.WindowUpdate();
			w.Exit();
		}
	}
}
