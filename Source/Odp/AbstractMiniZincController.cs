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
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using Modeling;
	using System.Diagnostics;

	public abstract class AbstractMiniZincController<TAgent, TTask, TResource> : AbstractController<TAgent, TTask, TResource>
		where TAgent : BaseAgent<TAgent, TTask, TResource>
		where TTask : class, ITask
	{
		private readonly string _constraintsModel;
		[Hidden]
		private string _inputFile;
		[Hidden]
		private string _outputFile;

		public static string MiniZinc = "minizinc.exe";
		private static int _counter = 0;

		public AbstractMiniZincController(string constraintsModel, TAgent[] agents) : base(agents)
		{
			_constraintsModel = constraintsModel;
		}

		public override Dictionary<TAgent, IEnumerable<Role<TAgent, TTask, TResource>>> CalculateConfigurations(params TTask[] tasks)
		{
			var configs = new Dictionary<TAgent, IEnumerable<Role<TAgent, TTask, TResource>>>();
			foreach (var task in tasks)
			{
				lock(MiniZinc)
				{
					CreateDataFile(task);
					ExecuteMiniZinc();
					ParseConfigurations(configs, task);
				}
			}
			return configs;
		}

		private void CreateDataFile(TTask task)
		{
			_inputFile = $"data{++_counter}.dzn";
			using (var writer = new StreamWriter(_inputFile))
			{
				WriteInputData(writer);
			}
		}

		protected abstract void WriteInputData(StreamWriter writer);

		private void ExecuteMiniZinc()
		{
			_outputFile = $"output{_counter}.dzn";
			var startInfo = new ProcessStartInfo
			{
				FileName = MiniZinc,
				Arguments = $"-o {_outputFile} {_constraintsModel} {_inputFile}",
				RedirectStandardError = true,
				RedirectStandardOutput = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using (var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true })
			{
				process.Start();

				process.BeginErrorReadLine();
				process.BeginOutputReadLine();

				process.OutputDataReceived += (o, e) => Console.WriteLine(e.Data);
				process.ErrorDataReceived += (o, e) => Console.WriteLine(e.Data);

				process.WaitForExit();
			}
		}

		private void ParseConfigurations(Dictionary<TAgent, IEnumerable<Role<TAgent, TTask, TResource>>> configs, TTask task)
		{
			var lines = File.ReadAllLines(_outputFile);
			if (lines[0].Contains("UNSATISFIABLE"))
			{
				ReconfigurationFailure = true;
				return;
			}

			var agentIds = ParseList(lines[0]);
			var capabilityIds = ParseList(lines[1]);

			var role = default(Role<TAgent, TTask, TResource>);
			TAgent lastAgent = null;

			for (int i = 0; i < agentIds.Length; ++i)
			{
				var agent = GetAgent(agentIds[i]);
				// connect to previous role
				role.PostCondition.Port = agent;
				// get new role
				role = GetRole(task, lastAgent, lastAgent == null ? null : (Condition<TAgent, TTask>?)role.PostCondition);

				// collect capabilities for the current agent into one role
				for (var current = agentIds[i]; current == agentIds[i]; ++i)
				{
					if (capabilityIds[i] >= 0)
						role.AddCapability(task.RequiredCapabilities[capabilityIds[i]]);
				}

				if (!configs.ContainsKey(agent))
					configs.Add(agent, new HashSet<Role<TAgent, TTask, TResource>>());
				(configs[agent] as HashSet<Role<TAgent, TTask, TResource>>).Add(role);
			}
		}

		private static int[] ParseList(string line)
		{
			var openBrace = line.IndexOf("[", StringComparison.Ordinal);
			var closeBrace = line.IndexOf("]", StringComparison.Ordinal);

			return line.Substring(openBrace + 1, closeBrace - openBrace - 1).Split(',')
				.Select(n => int.Parse(n.Trim()) - 1).ToArray();
		}

		protected virtual TAgent GetAgent(int index)
		{
			return Agents[index];
		}
	}
}