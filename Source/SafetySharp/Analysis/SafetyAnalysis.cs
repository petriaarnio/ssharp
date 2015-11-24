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

namespace SafetySharp.Analysis
{
	using System.Collections.Generic;
	using Modeling;
	using Runtime.Reflection;
	using Utilities;

	/// <summary>
	///   Performs safety analyses on a model.
	/// </summary>
	public sealed class SafetyAnalysis
	{
		/// <summary>
		///   The model that is analyzed.
		/// </summary>
		private readonly Model _model;

		/// <summary>
		///   The model checker that is used for the analysis.
		/// </summary>
		private readonly IModelChecker _modelChecker;

		/// <summary>
		///   Initializes a new instance.
		/// </summary>
		/// <param name="modelChecker">The model checker that should be used for the analysis.</param>
		/// <param name="model">The model that should be analyzed.</param>
		public SafetyAnalysis(IModelChecker modelChecker, Model model)
		{
			Requires.NotNull(modelChecker, nameof(modelChecker));
			Requires.NotNull(model, nameof(model));

			_modelChecker = modelChecker;
			_model = model;
		}

		/// <summary>
		///   Computes the minimal cut sets for the <paramref name="hazard" />.
		/// </summary>
		/// <param name="hazard">The hazard the minimal cut sets should be computed for.</param>
		public IEnumerable<int> ComputeMinimalCutSets(Formula hazard)
		{
			Requires.NotNull(hazard, nameof(hazard));
			Requires.OfType<StateFormula>(hazard, nameof(hazard), "Hazards are required to be state formulas.");

			var faults = _model.GetFaults();
			var faultSets = new List<int>(1 << faults.Length);
			GeneratePowerSet(faultSets, faults.Length);

			var criticalSets = new List<int>();
			foreach (var set in faultSets)
			{
				for (var i = 0; i < faults.Length; ++i)
					faults[i].OccurrenceKind = (set & i) == i ? OccurrenceKind.Always : OccurrenceKind.Never;

				if (_modelChecker.CheckInvariant(_model, ((StateFormula)hazard).Expression) != null)
					criticalSets.Add(set);
			}

			return criticalSets;
		}

		public static void GeneratePowerSet(List<int> sets, int count)
		{
			if (count == 0)
				sets.Add(0);
			else
			{
				GeneratePowerSet(sets, count - 1);

				var setsCount = sets.Count;
				for (var i = 0; i < setsCount; ++i)
					sets.Add(sets[i] | 1 << (count - 1));
			}
		}
	}
}