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

namespace Visualization
{
	using System.Windows.Controls;
	using global::Funkfahrbetrieb;
	using global::Funkfahrbetrieb.Context;
	using SafetySharp.Analysis;
	using SafetySharp.Modeling;
	using SafetySharp.Runtime;
	using SafetySharp.Runtime.Reflection;

	public partial class Funkfahrbetrieb
	{
		private readonly RealTimeSimulator _simulator;
		private readonly Train _train;
		private readonly Barrier _barrier;

		public Funkfahrbetrieb()
		{
			InitializeComponent();

			// Initialize visualization resources

			// Initialize the simulation environment
			var model = Model.Create(new Specification());
			foreach (var fault in model.GetFaults())
				fault.OccurrenceKind = OccurrenceKind.Never;

			_simulator = new RealTimeSimulator(model, stepDelay: 1000);
			_simulator.ModelStateChanged += (o, e) => UpdateModelState();
			SimulationControls.SetSimulator(_simulator);

			// Extract the components
			_train = (Train)_simulator.Model.RootComponents[2];
			_barrier = (Barrier)_simulator.Model.RootComponents[3];

			// Initialize the visualization state
			UpdateModelState();

			SimulationControls.ChangeSpeed(8);
		}

		private void UpdateModelState()
		{
			Canvas.SetLeft(Train, _train.Position / 2.0 - Train.ActualWidth / 2);
			Canvas.SetLeft(Barrier, Specification.CrossingPosition / 2.0);

			BarrierRotation.Angle = _barrier.Angle * 8;
		}
	}
}