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

namespace SafetySharp.CaseStudies.RobotCell.Modeling.Controllers.Reconfiguration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;

    using Odp;
    using Odp.Reconfiguration;
    using SafetySharp.Modeling;

    internal interface IPerformanceMeasurementController
    {
        Dictionary<uint, List<Tuple<TimeSpan, TimeSpan, long>>> CollectedTimeValues { get; }
    }

    public class PerformanceMeasurementController : IController, IPerformanceMeasurementController
    {
        private readonly IController _actingController;

        [Hidden(HideElements = true)]
        public Dictionary<uint, List<Tuple<TimeSpan,TimeSpan, long>>> CollectedTimeValues { get; } = new Dictionary<uint, List<Tuple<TimeSpan, TimeSpan, long>>>();

        [Hidden(HideElements = true)]
        private readonly Dictionary<uint, Stopwatch> _stopwatchs = new Dictionary<uint, Stopwatch>();
        
        public PerformanceMeasurementController(IController actingController)
        {
            _actingController = actingController;
            foreach (var agent in Agents)
            {
                CollectedTimeValues.Add(agent.ID, new List<Tuple<TimeSpan, TimeSpan, long>>());
                _stopwatchs.Add(agent.ID, Stopwatch.StartNew());
            }
        }

        public async Task<ConfigurationUpdate> CalculateConfigurations(object context, ITask task)
        {
            MicrostepScheduler.StartPerformanceMeasurement(_actingController);
            var resultingTasks = await _actingController.CalculateConfigurations(context, task);
            var reconfTime = MicrostepScheduler.StopPerformanceMeasurement(_actingController);
            foreach (var agent in resultingTasks.InvolvedAgents)
            {
                _stopwatchs[agent.ID].Stop();
                CollectedTimeValues[agent.ID].Add(new Tuple<TimeSpan, TimeSpan, long>(_stopwatchs[agent.ID].Elapsed-reconfTime.Elapsed, reconfTime.Elapsed, DateTime.Now.Ticks));
                _stopwatchs[agent.ID].Restart();
            }
            return resultingTasks;
        }

        public BaseAgent[] Agents => _actingController.Agents;

        public event Action<ITask, ConfigurationUpdate> ConfigurationsCalculated
        {
            add { _actingController.ConfigurationsCalculated += value; }
            remove { _actingController.ConfigurationsCalculated -= value; }
        }
    }
}
