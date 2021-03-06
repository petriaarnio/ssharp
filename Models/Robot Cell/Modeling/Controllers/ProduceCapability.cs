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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers
{
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using SafetySharp.Modeling;

	[DebuggerDisplay("Produce")]
	internal class ProduceCapability : Capability
	{
		public ProduceCapability(List<Resource> resources, List<Task> tasks)
		{
			Resources = resources;
			Tasks = tasks;
		}

		public List<Resource> Resources { get; }

		[Hidden(HideElements = true)]
		public List<Task> Tasks { get; }

		public override int Identifier => 1;

		public override void Execute(Agent agent)
		{
			agent.Produce(this);
		}

		public override bool IsEquivalentTo(Capability capability)
		{
			var produce = capability as ProduceCapability;
			if (produce == null)
				return false;

			return Tasks.SequenceEqual(produce.Tasks);
		}
	}
}