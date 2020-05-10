using System;
using Unity.Mathematics;

namespace Voronoi.Structures
{
	public struct VSite : IComparable<VSite>, IEquatable<VSite>
	{
		public readonly int Id;

		public readonly float X;

		public readonly float Y;
		
		public VSite(int index, float2 point)
		{
			Id = index;
			X = point.x;
			Y = point.y;
		}

		public float2 Point => new float2(X,Y);

		public static bool operator ==(VSite a, VSite b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(VSite a, VSite b)
		{
			return !a.Equals(b);
		}
		
		public int CompareTo(VSite other)
		{
			return Id.CompareTo(other.Id);
		}

		public bool Equals(VSite other)
		{
			return Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			return obj is VSite other && Equals(other);
		}

		public override int GetHashCode()
		{
			return Id;
		}
	}
}