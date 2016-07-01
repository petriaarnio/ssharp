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

namespace SafetySharp.Analysis
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Diagnostics;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Threading;
	using System.Threading.Tasks;
	using FormulaVisitors;
	using Modeling;
	using Runtime;
	using Runtime.Serialization;
	using Utilities;
	
	public class ProbabilityChecker : IDisposable
	{
		public struct ProbabilityCalculator
		{
			public ProbabilityCalculator(Func<Probability> useDefaultChecker,Func<ProbabilisticModelChecker,Probability> useCustomChecker)
			{
				Calculate = useDefaultChecker;
				CalculateWithChecker = useCustomChecker;
			}

			// Check with the DefaultChecker of ProbabilityChecker this FormulaChecker was built in
			public Func<Probability> Calculate { get; }
			public Func<ProbabilisticModelChecker,Probability> CalculateWithChecker { get; }
		}

		public struct FormulaCalculator
		{
			public FormulaCalculator(Func<bool> useDefaultChecker, Func<ProbabilisticModelChecker, bool> useCustomChecker)
			{
				Calculate = useDefaultChecker;
				CalculateWithChecker = useCustomChecker;
			}

			// Check with the DefaultChecker of ProbabilityChecker this FormulaChecker was built in
			public Func<bool> Calculate { get; }
			public Func<ProbabilisticModelChecker, bool> CalculateWithChecker { get; }
		}

		public struct RewardCalculator
		{
			public RewardCalculator(Func<RewardResult> useDefaultChecker, Func<ProbabilisticModelChecker, RewardResult> useCustomChecker)
			{
				Calculate = useDefaultChecker;
				CalculateWithChecker = useCustomChecker;
			}

			// Check with the DefaultChecker of ProbabilityChecker this FormulaChecker was built in
			public Func<RewardResult> Calculate { get; }
			public Func<ProbabilisticModelChecker, RewardResult> CalculateWithChecker { get; }
		}


		/// <summary>
		///   Raised when the model checker has written an output. The output is always written to the console by default.
		/// </summary>
		public event Action<string> OutputWritten = Console.WriteLine;

		private object _probabilityMatrixCreationStarted = false;
		private bool _probabilityMatrixWasCreated = false;

		internal CompactProbabilityMatrix CompactProbabilityMatrix { get; private set; }

		private ModelBase _model;
		private readonly ConcurrentBag<Formula> _formulasToCheck = new ConcurrentBag<Formula>();

		public ProbabilisticModelChecker DefaultChecker { get; set; }

		public void InitializeDefaultChecker()
		{
			if (DefaultChecker == null)
			{
				DefaultChecker = new Mrmc(this);
			}
		}


		/// <summary>
		///   The model checker's configuration that determines certain model checker settings.
		/// </summary>
		public AnalysisConfiguration Configuration = AnalysisConfiguration.Default;
		

		// Create Tasks which make the checks (workers)
		// First formulas to check are collected (thus, the probability matrix only has to be calculated once)
		public ProbabilityChecker(ModelBase model)
		{
			Requires.NotNull(model, nameof(model));
			_model = model;
		}
		
		private Probability CalculateProbabilityWithDefaultChecker(Formula formulaToCheck)
		{
			InitializeDefaultChecker();
			return DefaultChecker.CalculateProbability(formulaToCheck);
		}

		private bool CalculateFormulaWithDefaultChecker(Formula formulaToCheck)
		{
			InitializeDefaultChecker();
			return DefaultChecker.CalculateFormula(formulaToCheck);
		}


		private RewardResult CalculateRewardWithDefaultChecker(Formula formulaToCheck)
		{
			InitializeDefaultChecker();
			return DefaultChecker.CalculateReward(formulaToCheck);
		}

		public void CreateProbabilityMatrix()
		{
			Requires.That(IntPtr.Size == 8, "Model checking is only supported in 64bit processes.");
			var alreadyStarted = Interlocked.CompareExchange(ref _probabilityMatrixCreationStarted, true, false);
			if ((bool)alreadyStarted)
				return;

			var stopwatch = new Stopwatch();

			var serializer = new RuntimeModelSerializer();
			serializer.Serialize(_model, _formulasToCheck.ToArray());

			//return CheckInvariant(serializer.Load);

			using (var checker = new MarkovChainBuilder(serializer.Load, message => OutputWritten?.Invoke(message), Configuration))
			{
				var initializationTime = stopwatch.Elapsed;
				stopwatch.Restart();
				
				var sparseProbabilityMatrix = checker.CreateProbabilityMatrix();

				sparseProbabilityMatrix.PrintPathWithStepwiseHighestProbability(10);

				var derivedProbabilityMatrix = sparseProbabilityMatrix.DeriveCompactProbabilityMatrix();
				//var compactToSparse = CompactProbabilityMatrix = derivedProbabilityMatrix.Item1;
				CompactProbabilityMatrix = derivedProbabilityMatrix.Item2;
				//CompactProbabilityMatrix.ValidateStates();
				var creationTime = stopwatch.Elapsed;
				stopwatch.Stop();

				if (true) //Configuration.ProgressReportsOnly
				{
					OutputWritten?.Invoke(String.Empty);
					OutputWritten?.Invoke("===============================================");
					OutputWritten?.Invoke($"Initialization time: {initializationTime}");
					OutputWritten?.Invoke($"Markov chain creation time: {creationTime}");
					OutputWritten?.Invoke($"States: {CompactProbabilityMatrix.States}");
					OutputWritten?.Invoke($"Transitions: {CompactProbabilityMatrix.NumberOfTransitions}");
					OutputWritten?.Invoke($"{(int)(CompactProbabilityMatrix.States / stopwatch.Elapsed.TotalSeconds):n0} states per second");
					OutputWritten?.Invoke($"{(int)(CompactProbabilityMatrix.NumberOfTransitions / stopwatch.Elapsed.TotalSeconds):n0} transitions per second");
					OutputWritten?.Invoke("===============================================");
					OutputWritten?.Invoke(String.Empty);
				}
			}

			_probabilityMatrixWasCreated = true;
			Interlocked.MemoryBarrier();
		}

		public void AssertProbabilityMatrixWasCreated()
		{
			Requires.That(_probabilityMatrixWasCreated, nameof(CreateProbabilityMatrix) + "must be called before");
		}

		public ProbabilityCalculator CalculateProbability(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			var visitor = new IsFormulaReturningProbabilityVisitor();
			visitor.Visit(formula);
			if (!visitor.IsReturningProbability)
				throw new InvalidOperationException("Formula must return probability.");
			
			Interlocked.MemoryBarrier();
			if ((bool)_probabilityMatrixCreationStarted)
			{
				throw new Exception(nameof(CalculateProbability) + " must be called before " + nameof(CreateProbabilityMatrix));
			}

			_formulasToCheck.Add(formula);
			var formulaToCheck = formula;

			Func<Probability> useDefaultChecker = () => CalculateProbabilityWithDefaultChecker(formulaToCheck);
			Func<ProbabilisticModelChecker,Probability> useCustomChecker = customChecker => customChecker.CalculateProbability(formulaToCheck);

			var checker = new ProbabilityCalculator(useDefaultChecker, useCustomChecker);
			return checker;
		}

		public FormulaCalculator CalculateFormula(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			Interlocked.MemoryBarrier();
			if ((bool)_probabilityMatrixCreationStarted)
			{
				throw new Exception(nameof(CalculateFormula) + " must be called before " + nameof(CreateProbabilityMatrix));
			}

			_formulasToCheck.Add(formula);
			var formulaToCheck = formula;

			Func<bool> useDefaultChecker = () => CalculateFormulaWithDefaultChecker(formulaToCheck);
			Func<ProbabilisticModelChecker, bool> useCustomChecker = customChecker => customChecker.CalculateFormula(formulaToCheck);

			var checker = new FormulaCalculator(useDefaultChecker, useCustomChecker);
			return checker;
		}

		public RewardCalculator CalculateReward(Formula formula)
		{
			Requires.NotNull(formula, nameof(formula));

			var visitor = new IsFormulaReturningRewardResultVisitor();
			visitor.Visit(formula);
			if (!visitor.IsReturningRewardResult)
				throw new InvalidOperationException("Formula must return reward.");

			_formulasToCheck.Add(formula);
			var formulaToCheck = formula;

			Interlocked.MemoryBarrier();
			if ((bool)_probabilityMatrixCreationStarted)
			{
				throw new Exception(nameof(CalculateReward) + " must be called before " + nameof(CreateProbabilityMatrix));
			}
			

			Func<RewardResult> useDefaultChecker = () => CalculateRewardWithDefaultChecker(formulaToCheck);
			Func<ProbabilisticModelChecker, RewardResult> useCustomChecker = customChecker => customChecker.CalculateReward(formulaToCheck);

			var checker = new RewardCalculator(useDefaultChecker, useCustomChecker);
			return checker;
		}

		public void Dispose()
		{
			DefaultChecker?.Dispose();
		}
	}
}