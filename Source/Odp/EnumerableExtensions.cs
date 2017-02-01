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
	using System.Linq;

	internal static class EnumerableExtensions
	{
		public static T MinBy<T>(this IEnumerable<T> elements, Func<T, int> costFunction)
		{
			return elements.Select(elem => new { elem, cost = costFunction(elem) })
						   .Aggregate((tuple1, tuple2) => (tuple2.cost <= tuple1.cost) ? tuple2 : tuple1)
						   .elem;
		}

		public static T MaxBy<T>(this IEnumerable<T> elements, Func<T, int> costFunction)
		{
			return elements.Select(elem => new { elem, cost = costFunction(elem) })
						   .Aggregate((tuple1, tuple2) => (tuple2.cost >= tuple1.cost) ? tuple2 : tuple1)
						   .elem;
		}

		public static T MinByOrDefault<T>(this IEnumerable<T> elements, Func<T, int> costFunction)
		{
			var seed = new { elem = default(T), cost = int.MaxValue };
			return elements.Select(elem => new { elem, cost = costFunction(elem) })
						   .Aggregate(seed, (tuple1, tuple2) => (tuple2.cost <= tuple1.cost) ? tuple2 : tuple1)
						   .elem;
		}

		public static T MaxByOrDefault<T>(this IEnumerable<T> elements, Func<T, int> costFunction)
		{
			var seed = new { elem = default(T), cost = int.MaxValue };
			return elements.Select(elem => new { elem, cost = costFunction(elem) })
						   .Aggregate(seed, (tuple1, tuple2) => (tuple2.cost >= tuple1.cost) ? tuple2 : tuple1)
						   .elem;
		}

		public static IEnumerable<T> Slice<T>(this IEnumerable<T> elements, int startIndex, int endIndex)
		{
			if (startIndex > endIndex || startIndex < 0 || endIndex < 0)
				return Enumerable.Empty<T>();

			return elements.Skip(startIndex).Take(endIndex - startIndex + 1);
		}
	}
}
