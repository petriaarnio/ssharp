﻿// The MIT License (MIT)
// 
// Copyright (c) 2014-2017, Institute for Software & Systems Engineering
// Copyright (c) 2017, Stefan Fritsch
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

namespace SafetySharp.Bayesian
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using ISSE.SafetyChecking.Modeling;
    using Modeling;
    using Newtonsoft.Json;

    public class BayesianNetworkCreator
    {
        private const double Tolerance = 0.000000001;

        public BayesianLearningConfiguration Config { get; set; }

        private readonly ModelBase _model;
        private readonly int _stepBounds;
        private BooleanRandomVariable _hazardVar;
        private IList<FaultRandomVariable> _faultVars;
        private IList<McsRandomVariable> _mcsVars;
        private IList<BooleanRandomVariable> _states;

        public BayesianNetworkCreator(ModelBase model, int stepBounds, BayesianLearningConfiguration? config = null)
        {
            Config = config ?? BayesianLearningConfiguration.Default;
            _model = model;
            _stepBounds = stepBounds;
        }

        /// <summary>
        /// Learn a bayesian network for the given hazard, states and faults with a constraint-based learning algorithm and model checking.
        /// </summary>
        /// <param name="hazard">The hazard expression which should be analyzed</param>
        /// <param name="states">An optional dictionary for arbitrary named state expressions that should be analyzed</param>
        /// <param name="faults">An optional fault list to restrict the analyzed set or to give a chronological order for causal fallback algorithms</param>
        /// <returns>A learning bayesian network including a DAG and a fully calculated probability distribution</returns>
        public BayesianNetwork LearnConstraintBasedBayesianNetwork(Func<bool> hazard, Dictionary<string, Func<bool>> states = null, IList<Fault> faults = null)
        {
            CreateRandomVariables(hazard, states, faults);
            var allVars = AllRandomVariables();
            var probCalculator = new OnDemandProbabilityDistributionCalculator(_model, allVars, _stepBounds, Tolerance, Config);

            var independenceCalculator = new IndependencyCalculator(probCalculator, Tolerance, Config);
            var independencies = independenceCalculator.FindIndependencies(_faultVars, _mcsVars, _states, _hazardVar);
            independenceCalculator.PrettyPrintIndependencies();

            var structureLearner = new ConstraintBasedStructureLearner(allVars, independencies);
            structureLearner.LearnDag();
            structureLearner.UseDccaForOrientations(_mcsVars, _faultVars, _hazardVar);
            var dag = structureLearner.DagPatternToDag();
            Console.Out.WriteLine($"Calculated {probCalculator.NumberOfCalculatedDistributions()} out of {probCalculator.NumberOfMaxDistributions()} possible distributions");

            var bayesianNetwork = BayesianNetwork.FromDagPattern(dag, probCalculator);
            bayesianNetwork.PrintBayesianNetwork();
            Console.Out.WriteLine($"Calculated {probCalculator.NumberOfCalculatedDistributions()} out of {probCalculator.NumberOfMaxDistributions()} possible distributions");
            CheckResultingNetwork(bayesianNetwork);
            StoreBayesianNetwork(bayesianNetwork);

            return bayesianNetwork;
        }

        /// <summary>
        /// Learn a bayesian network for the given hazard, states and faults with a model selection learning algorithm.
        /// </summary>
        /// <param name="pathForGeneratedFiles">Path to use for generated file creation</param>
        /// <param name="numberOfSimulations">The number of simulation data rows to generate for learning the bayesian network</param>
        /// <param name="hazard">The hazard expression which should be analyzed</param>
        /// <param name="states">An optional dictionary for arbitrary named state expressions that should be analyzed</param>
        /// <param name="faults">An optional fault list to restrict the analyzed set</param>
        /// <returns>A learning bayesian network including a DAG and a fully calculated probability distribution</returns>
        public BayesianNetwork LearnScoreBasedBayesianNetwork(string pathForGeneratedFiles, int numberOfSimulations, Func<bool> hazard, Dictionary<string, Func<bool>> states = null, IList<Fault> faults = null)
        {
            CreateRandomVariables(hazard, states, faults);
            var variableSimulator = new RandomVariableSimulator(_model, _stepBounds, pathForGeneratedFiles, Config);
            var dataFile = variableSimulator.GenerateSimulationData(_faultVars, _mcsVars, _states, _hazardVar, numberOfSimulations);
            var dataFiles = new List<string>{ dataFile };
            dataFiles.AddRange(Config.FurtherSimulationDataFiles);

            var structureLearning = new ScoreBasedStructureLearner(dataFiles, pathForGeneratedFiles, Config);
            var bayesianNetwork = structureLearning.LearnBayesianNetwork(_faultVars, _mcsVars, _states, _hazardVar);
            bayesianNetwork.PrintBayesianNetwork();
            CheckResultingNetwork(bayesianNetwork);
            StoreBayesianNetwork(bayesianNetwork);
            return bayesianNetwork;
        }

        private void CreateRandomVariables(Func<bool> hazard, Dictionary<string, Func<bool>> states, IList<Fault> faults)
        {
            var usedFaults = faults ?? _model.Faults;
            var usedStates = states ?? new Dictionary<string, Func<bool>>();

            var randomVariableCreator = new RandomVariableFactory(_model);
            _hazardVar = randomVariableCreator.FromState(hazard, "H");
            _states = usedStates.Select(state => randomVariableCreator.FromState(state.Value, state.Key)).ToList();
            _faultVars = randomVariableCreator.FromFaults(usedFaults);
            _mcsVars = randomVariableCreator.FromDccaLimitedByFaults(hazard, _faultVars);
        }

        private IList<RandomVariable> AllRandomVariables()
        {
            var allVars = new List<RandomVariable>();
            allVars.AddRange(_faultVars);
            allVars.AddRange(_states);
            allVars.AddRange(_mcsVars);
            allVars.Add(_hazardVar);
            return allVars;
        }

        private void PrintBayesianNetwork(BayesianNetwork bayesianNetwork)
        {
            bayesianNetwork.PrintBayesianNetwork();
        }

        /// <summary>
        /// Checks the resulting DAG for causal edges that should be included and warns if some are absent.
        /// </summary>
        private void CheckResultingNetwork(BayesianNetwork network)
        {
            const string message = "You may consider including DCCA results or using other learning algorithms.";
            foreach (var criticalSet in _mcsVars)
            {
                foreach (var includedFault in criticalSet.FaultVariables)
                {
                    if (!network.Dag.IsDirectedEdge(includedFault, criticalSet))
                        Console.Out.WriteLine($"WARNING: There was no edge from {includedFault} to {criticalSet}! {message}");
                }
                if (!network.Dag.IsDirectedEdge(criticalSet, _hazardVar))
                    Console.Out.WriteLine($"WARNING: There was no edge from {criticalSet} to {_hazardVar}! {message}");
            }
        }

        /// <summary>
        /// Store the given BayesianNetwork in the file path of the config, if it is defined there
        /// </summary>
        private void StoreBayesianNetwork(BayesianNetwork bayesianNetwork)
        {
            if (!string.IsNullOrWhiteSpace(Config.BayesianNetworkSerializationPath))
            {
                File.WriteAllText(Config.BayesianNetworkSerializationPath, JsonConvert.SerializeObject(bayesianNetwork, new BayesianNetworkConverter(bayesianNetwork.RandomVariables)));
            }
        }

        /// <summary>
        /// Create a BayesianNetwork from a JSON file.
        /// </summary>
        /// <param name="file">Path to the JSON file</param>
        /// <param name="hazard">Hazard that was used in the bayesian network</param>
        /// <param name="states">Boolen states that were used in the bayesian network</param>
        /// <param name="faults">Faults that were used in the bayesian network</param>
        /// <returns>a BayesianNetwork instance created from the given JSON file</returns>
        public BayesianNetwork FromJson(string file, Func<bool> hazard, Dictionary<string, Func<bool>> states = null, IList<Fault> faults = null)
        {
            CreateRandomVariables(hazard, states, faults);
            var allVars = AllRandomVariables();
            var network = JsonConvert.DeserializeObject<BayesianNetwork>(File.ReadAllText(file),
                new BayesianNetworkConverter(allVars));
            PrintBayesianNetwork(network);
            return network;
        }

    }
}