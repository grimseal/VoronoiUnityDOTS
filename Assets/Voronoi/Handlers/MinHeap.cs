using System;
using Unity.Collections;
using Voronoi.Structures;

namespace Voronoi.Handlers
{
	public struct MinHeap
	{
		public static bool EventInsert(FortuneEvent fortuneEvent, NativeArray<FortuneEvent> events, ref int count)
		{
			if (count == events.Length) throw new InvalidOperationException("Min heap capacity reached");
			events[count] = fortuneEvent;
			count++;
			PercolateUp(count - 1, events, count);
			return true;
		}

		public static FortuneEvent EventPop(NativeArray<FortuneEvent> events, ref int count)
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
			PercolateDown(0, events, count);
			return min;
		}

		public static FortuneEvent EventPeek(NativeArray<FortuneEvent> events, int count)
		{
			if (count == 0) throw new InvalidOperationException("Min heap is empty");
			return events[0];
		}

		public static bool EventRemove(ref FortuneEvent fortuneEvent, NativeArray<FortuneEvent> events, ref int count)
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
			Swap(index, count, events);
			if (LeftLessThanRight(index, (index - 1)/2, events))
				PercolateUp(index, events, count);
			else
				PercolateDown(index, events, count);
			return true;
		}

		private static void PercolateDown(int index, NativeArray<FortuneEvent> events, int count)
		{
			while (true)
			{
				var left = 2*index + 1;
				var right = 2*index + 2;
				var largest = index;
				if (left < count && LeftLessThanRight(left, largest, events)) largest = left;
				if (right < count && LeftLessThanRight(right, largest, events)) largest = right;
				if (largest == index) return;
				Swap(index, largest, events);
				index = largest;
			}
		}

		private static void PercolateUp(int index, NativeArray<FortuneEvent> events, int count)
		{
			while (true)
			{
				if (index >= count || index <= 0) return;
				var parent = (index - 1)/2;
				if (LeftLessThanRight(parent, index, events)) return;
				Swap(index, parent, events);
				index = parent;
			}
		}

		private static bool LeftLessThanRight(int left, int right, NativeArray<FortuneEvent> events)
		{
			// 	var other = (FortuneEvent) obj;
			// 	var c = Y.CompareTo(other.Y);
			// 	return c == 0 ? X.CompareTo(other.X) : c;
			var a = events[left];
			var b = events[right];
			var c = a.Y.CompareTo(b.Y);
			return (c == 0 ? a.X.CompareTo(b.X) : c) < 0;
		}

		private static void Swap(int left, int right, NativeArray<FortuneEvent> events)
		{
			var temp = events[left];
			events[left] = events[right];
			events[right] = temp;
		}
	}
}