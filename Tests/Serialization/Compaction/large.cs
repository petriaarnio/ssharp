﻿// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
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

namespace Tests.Serialization.Compaction
{
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime.Serialization;
	using Shouldly;

	internal class Large : SerializationObject
	{
		protected override void Check()
		{
			var cs = new C[35];
			for (var i = 0; i < cs.Length; ++i)
			{
				cs[i] = new C
				{
					A = i % 2 == 0,
					B = (short)(i * 2),
					D = 100 + i,
					E = i % 2 == 0 ? E.A : E.B
				};
			}

			GenerateCode(SerializationMode.Optimized, cs);
			StateVectorSize.ShouldBe(256);

			Serialize();

			foreach (var c in cs)
			{
				c.A = false;
				c.B = 0;
				c.D = 0;
				c.E = E.A;
			}

			Deserialize();

			for (var i = 0; i < cs.Length; ++i)
			{
				cs[i].A.ShouldBe(i % 2 == 0);
				cs[i].B.ShouldBe((short)(i * 2));
				cs[i].D = 100 + i;
				cs[i].E.ShouldBe(i % 2 == 0 ? E.A : E.B);
			}
		}

		private class C
		{
			public bool A;
			public short B;
			public int D;
			public E E;
			public readonly Fault F = new PermanentFault();
		}

		private enum E
		{
			A,
			B
		}
	}
}