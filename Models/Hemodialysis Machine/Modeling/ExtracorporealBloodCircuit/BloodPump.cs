// The MIT License (MIT)
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

namespace SafetySharp.CaseStudies.HemodialysisMachine.Modeling.ExtracorporealBloodCircuit
{
	using ISSE.SafetyChecking.Modeling;
	using SafetySharp.Modeling;

	public class BloodPump : Component
	{
		public readonly BloodFlowInToOut MainFlow = new BloodFlowInToOut();

		[Range(0, 8, OverflowBehavior.Error)]
		public int PumpSpeed = 0;

		[Provided]
		public Blood SetMainFlow(Blood fromPredecessor)
		{
			return fromPredecessor;
		}

		[Provided]
		public virtual Suction SetMainFlowSuction(Suction fromSuccessor)
		{
			Suction toPredecessor;
			toPredecessor.CustomSuctionValue = PumpSpeed; //Force suction set by pump
			toPredecessor.SuctionType=SuctionType.CustomSuction;
			return toPredecessor;
		}

		public BloodPump()
		{
			MainFlow.UpdateBackward=SetMainFlowSuction;
			MainFlow.UpdateForward=SetMainFlow;
			BloodPumpDefect.HasCustomDemand = () => PumpSpeed != 0;
		}

		public readonly Fault BloodPumpDefect = new PermanentFault { DemandType = Fault.DemandTypes.OnStartOfStep };

		[FaultEffect(Fault = nameof(BloodPumpDefect))]
		public class BloodPumpDefectEffect : BloodPump
		{
			[Provided]
			public override Suction SetMainFlowSuction(Suction fromSuccessor)
			{
				Suction toPredecessor;
				toPredecessor.CustomSuctionValue = 0; //Force suction set by motor
				toPredecessor.SuctionType = SuctionType.CustomSuction;
				return toPredecessor;
			}
		}
	}
}