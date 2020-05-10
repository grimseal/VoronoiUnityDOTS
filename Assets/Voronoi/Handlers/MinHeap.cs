// ReSharper disable CheckNamespace
using System;
using Unity.Collections;

namespace Voronoi
{
	internal static class MinHeap
	{
		public static bool EventInsert(FortuneEvent fortuneEvent, ref NativeArray<FortuneEvent> events, ref int count)
		{
			if (count == events.Length) throw new InvalidOperationException("Min heap capacity reached");
			events[count] = fortuneEvent;
			count++;
			PercolateUp(count - 1, ref events, count);
			return true;
		}

		public static FortuneEvent EventPop(ref NativeArray<FortuneEvent> events, ref int count)
		{
			if (count == 0) throw new InvalidOperationException("Min heap is empty");
			if (count == 1)
			{
				count--;
				return events[count];
			}

			var min = events[0];
			events[0] = events[count - 1];
			count--;
			PercolateDown(0, ref events, count);
			return min;
		}

		public static FortuneEvent EventPeek(ref NativeArray<FortuneEvent> events, int count)
		{
			if (count == 0) throw new InvalidOperationException("Min heap is empty");
			return events[0];
		}

		public static bool EventRemove(ref FortuneEvent fortuneEvent, ref NativeArray<FortuneEvent> events, ref int count)
		{
			var index = -1;
			for (var i = 0; i < count; i++)
			{
				if (events[i].Id != fortuneEvent.Id) continue;
				index = i;
				break;
			}

			if (index == -1) return false;

			count--;
			Swap(index, count, ref events);
			if (LeftLessThanRight(index, (index - 1)/2, ref events))
				PercolateUp(index, ref events, count);
			else
				PercolateDown(index, ref events, count);
			return true;
		}

		private static void PercolateDown(int index, ref NativeArray<FortuneEvent> events, int count)
		{
			while (true)
			{
				var left = 2*index + 1;
				var right = 2*index + 2;
				var largest = index;
				if (left < count && LeftLessThanRight(left, largest, ref events)) largest = left;
				if (right < count && LeftLessThanRight(right, largest, ref events)) largest = right;
				if (largest == index) return;
				Swap(index, largest, ref events);
				index = largest;
			}
		}

		private static void PercolateUp(int index, ref NativeArray<FortuneEvent> events, int count)
		{
			while (true)
			{
				if (index >= count || index <= 0) return;
				var parent = (index - 1)/2;
				if (LeftLessThanRight(parent, index, ref events)) return;
				Swap(index, parent, ref events);
				index = parent;
			}
		}

		private static bool LeftLessThanRight(int left, int right, ref NativeArray<FortuneEvent> events)
		{
			var a = events[left];
			var b = events[right];
			var c = a.Y.CompareTo(b.Y);
			return (c == 0 ? a.X.CompareTo(b.X) : c) < 0;
		}

		private static void Swap(int left, int right, ref NativeArray<FortuneEvent> events)
		{
			var temp = events[left];
			events[left] = events[right];
			events[right] = temp;
		}
	}
}