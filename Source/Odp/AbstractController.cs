﻿// The MIT License (MIT)
// 
// Copyright (c) 2014-2016, Institute for Software & Systems Engineering
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

namespace SafetySharp.Odp
{
	using System.Collections.Generic;
	using Modeling;

	public abstract class AbstractController<A, T, R> : Component, IController<A, T, R>
		where A : BaseAgent<A, T, R>
		where T : class, ITask
	{
		[Hidden(HideElements = true)]
		public A[] Agents { get; }

		public AbstractController(A[] agents)
		{
			Agents = agents;
		}

		public virtual bool ReconfigurationFailure
		{
			get;
			protected set;
		}

		public abstract Dictionary<A, IEnumerable<Role<A, T, R>>> CalculateConfigurations(params T[] tasks);

		protected Role<A,T,R> GetRole(T recipe, A input, Condition<A,T>? previous)
		{
			var role = new Role<A,T,R>();

			// update precondition
			role.PreCondition.Task = recipe;
			role.PreCondition.Port = input;
			role.PreCondition.ResetState();
			if (previous != null)
				role.PreCondition.CopyStateFrom(previous.Value);

			// update postcondition
			role.PostCondition.Task = recipe;
			role.PostCondition.Port = null;
			role.PostCondition.ResetState();
			role.PostCondition.CopyStateFrom(role.PreCondition);

			role.Clear();

			return role;
		}
	}
}
