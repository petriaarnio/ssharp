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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers.Reconfiguration.PerformanceMeasurement
{
	using System;
	using System.Collections.Generic;
	using System.Threading.Tasks;
	using Odp.Reconfiguration;
	using SafetySharp.Modeling;

	/// <summary>
	///   A compositional <see cref="IReconfigurationStrategy"/> implementation that measures the time
	///   required for the reconfiguration and emits it through an event.
	/// </summary>
	public class PerformanceMeasurementReconfigurationStrategy : IReconfigurationStrategy
	{
		private readonly IReconfigurationStrategy _strategy;

		public PerformanceMeasurementReconfigurationStrategy(IReconfigurationStrategy strategy)
		{
			_strategy = strategy;
		}

		public event Action<IEnumerable<ReconfigurationRequest>, TimeSpan> MeasuredReconfigurations; 

		public async Task Reconfigure(IEnumerable<ReconfigurationRequest> reconfigurations)
		{
			var reconfTime = await AsyncPerformance.Measure(() => _strategy.Reconfigure(reconfigurations));
			MeasuredReconfigurations?.Invoke(reconfigurations, reconfTime.Elapsed);
		}
	}
}
