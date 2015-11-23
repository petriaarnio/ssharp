﻿// The MIT License (MIT)
// 
// Copyright (c) 2014-2015, Institute for Software & Systems Engineering
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace Tests.LtsMin.Invariants.Violated
{
	using SafetySharp.Modeling;
	using Shouldly;

	internal class RequiredPort : LtsMinTestObject
	{
		protected override void Check()
		{
			var d = new D();
			CheckInvariant(d.G != 4, d).ShouldBe(false);
		}

		private class C : Component
		{
			public int F = -1;

			public C()
			{
				Bind(nameof(R), nameof(P));
			}

			private int P()
			{
				return F;
			}

			public extern int R();
		}

		private class D : Component
		{
			public readonly C C = new C();
			public int G;

			public override void Update()
			{
				System.Console.WriteLine($"vorher G = {G}, C.F = {C.F}");

				if (Choose(true, false))
					G = C.R() + 1;
				else
					C.F = Choose(1, 2, 3);

				System.Console.WriteLine($"nachher G = {G}, C.F = {C.F}");
				System.Console.WriteLine("----------------");
			}
		}
	}
}