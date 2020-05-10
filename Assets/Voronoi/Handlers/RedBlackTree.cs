// ReSharper disable CheckNamespace
using Unity.Collections;

namespace Voronoi
{
	internal static class RedBlackTree
	{
		public static int InsertTreeNode(
			int node,
			int arc,
			ref NativeArray<int> treeValue,
			ref NativeArray<int> treeLeft,
			ref NativeArray<int> treeRight,
			ref NativeArray<int> treeParent,
			ref NativeArray<int> treePrevious,
			ref NativeArray<int> treeNext,
			ref NativeArray<bool> treeColor,
			ref int treeCount,
			ref int root)
		{
			var successor = treeCount;
			treeValue[successor] = arc;
			treeCount++;

            int parent;

            if (node > -1)
            {
                //insert new node between node and its successor
                treePrevious[successor] = node;
                treeNext[successor] = treeNext[node];
                if (treeNext[node] > -1)
	                treePrevious[treeNext[node]] = successor;
                treeNext[node] = successor;

                //insert successor into the tree
                if (treeRight[node] > -1)
                {
	                node = GetFirst(treeRight[node], ref treeLeft);
	                treeLeft[node] = successor;
                }
                else
                {
	                treeRight[node] = successor;
                }
                parent = node;
            }
            else if (root > -1)
            {
                //if the node is null, successor must be inserted
                //into the left most part of the tree
                node = GetFirst(root, ref treeLeft);
                //successor.Previous = null;
                treeNext[successor] = node;
                treePrevious[node] = successor;
                treeLeft[node] = successor;
                parent = node;
            }
            else
            {
                //first insert
                //successor.Previous = successor.Next = null;
                root = successor;
                parent = -1;
            }

            //successor.Left = successor.Right = null;
            treeParent[successor] = parent;
            treeColor[successor] = true;

            //the magic of the red black tree
            int grandma;
            int aunt;
            node = successor;
            while (parent > -1 && treeColor[parent])
            {
	            grandma = treeParent[parent];
                if (parent == treeLeft[grandma])
                {
                    aunt = treeRight[grandma];
                    if (aunt > -1 && treeColor[aunt])
                    {
	                    treeColor[parent] = false;
	                    treeColor[aunt] = false;
	                    treeColor[grandma] = true;
                        node = grandma;
                    }
                    else
                    {
                        if (node == treeRight[parent])
                        {
                            RotateLeft(parent, ref treeLeft, ref treeRight, ref treeParent, ref root);
                            node = parent;
                            parent = treeParent[node];
                        }
                        treeColor[parent] = false;
                        treeColor[grandma] = true;
                        RotateRight(grandma, ref treeLeft, ref treeRight, ref treeParent, ref root);
                    }
                }
                else
                {
                    aunt = treeLeft[grandma];
                    if (aunt > -1 && treeColor[aunt])
                    {
	                    treeColor[parent] = false;
	                    treeColor[aunt] = false;
	                    treeColor[grandma] = true;
	                    node = grandma;
                    }
                    else
                    {
                        if (node == treeLeft[parent])
                        {
                            RotateRight(parent, ref treeLeft, ref treeRight, ref treeParent, ref root);
                            node = parent;
                            parent = treeParent[node];
                        }
                        
                        treeColor[parent] = false;
                        treeColor[grandma] = true;
                        RotateLeft(grandma, ref treeLeft, ref treeRight, ref treeParent, ref root);
                    }
                }
                parent = treeParent[node];
            }
            treeColor[root] = false;
            return successor;
		}

		public static void RemoveTreeNode(
			int node,
			ref NativeArray<int> treeLeft, 
			ref NativeArray<int> treeRight, 
			ref NativeArray<int> treeParent, 
			ref NativeArray<int> treePrevious, 
			ref NativeArray<int> treeNext, 
			ref NativeArray<bool> treeColor,
			ref int root)
		{
			//fix up linked list structure
			if (treeNext[node] > -1) 
				treePrevious[treeNext[node]] = treePrevious[node];
			if (treePrevious[node] > -1)
				treeNext[treePrevious[node]] = treeNext[node];

            //replace the node
            var parent = treeParent[node];
            var left = treeLeft[node];
            var right = treeRight[node];

            int next;
            //figure out what to replace this node with
            if (left < 0)
                next = right;
            else if (right < 0)
                next = left;
            else
                next = GetFirst(right, ref treeLeft);

            //fix up the parent relation
            if (parent > -1)
            {
	            if (treeLeft[parent] == node)
		            treeLeft[parent] = next;
                else
					treeRight[parent] = next;
            }
            else
            {
                root = next;
            }

            bool red;
            if (left > -1 && right > -1)
            {
                red = treeColor[next];
                treeColor[next] = treeColor[node];
                treeLeft[next] = left;
                treeParent[left] = next;

                // if we reached down the tree
                if (next != right)
                {
                    parent = treeParent[next];
                    treeParent[next] = treeParent[node];

                    node = treeRight[next];
                    treeLeft[parent] = node;

                    treeRight[next] = right;
                    treeParent[right] = next;
                }
                else
                {
                    // the direct right will replace the node
                    treeParent[next] = parent;
                    parent = next;
                    node = treeRight[next];
                }
            }
            else
            {
                red = treeColor[node];
                node = next;
            }

            if (node > -1)
            {
	            treeParent[node] = parent;
            }

            if (red)
            {
                return;
            }

            if (node > -1 && treeColor[node])
            {
	            treeColor[node] = false;
                return;
            }

            //node is null or black

            // fair warning this code gets nasty

            //how do we guarantee sibling is not null
            var sibling = -1;
            do
            {
                if (node == root)
                    break;
                if (node == treeLeft[parent])
                {
                    sibling = treeRight[parent];
                    if (treeColor[sibling])
                    {
	                    treeColor[sibling] = false;
	                    treeColor[parent] = true;
                        RotateLeft(parent, ref treeLeft, ref treeRight, ref treeParent, ref root);
                        sibling = treeRight[parent];
                    }
                    if (treeLeft[sibling] > -1 && treeColor[treeLeft[sibling]] || 
                        treeRight[sibling] > -1 && treeColor[treeRight[sibling]])
                    {
                        //pretty sure this can be sibling.Left!= null && sibling.Left.Red
                        if (treeRight[sibling] < 0 || !treeColor[treeRight[sibling]])
                        {
	                        treeColor[treeLeft[sibling]] = false;
	                        treeColor[sibling] = true;
	                        RotateRight(sibling, ref treeLeft, ref treeRight, ref treeParent, ref root);
                            sibling = treeRight[parent];
                        }
                        treeColor[sibling] = treeColor[parent];
                        treeColor[parent] = treeColor[treeRight[sibling]] = false;
                        RotateLeft(parent, ref treeLeft, ref treeRight, ref treeParent, ref root);
                        node = root;
                        break;
                    }
                }
                else
                {
                    sibling = treeLeft[parent];
                    if (treeColor[sibling])
                    {
	                    treeColor[sibling] = false;
	                    treeColor[parent] = true;
                        RotateRight(parent, ref treeLeft, ref treeRight, ref treeParent, ref root);
                        sibling = treeLeft[parent];
                    }
                    if (treeLeft[sibling] > -1 && treeColor[treeLeft[sibling]] ||
                        treeRight[sibling] > -1 && treeColor[treeRight[sibling]])
                    {
                        if (treeLeft[sibling] < 0 || !treeColor[treeLeft[sibling]])
                        {
	                        treeColor[treeRight[sibling]] = false;
	                        treeColor[sibling] = true;
	                        RotateLeft(sibling, ref treeLeft, ref treeRight, ref treeParent, ref root);
                            sibling = treeLeft[parent];
                        }
                        treeColor[sibling] = treeColor[parent];
                        treeColor[parent] = treeColor[treeLeft[sibling]] = false;
                        RotateRight(parent, ref treeLeft, ref treeRight, ref treeParent, ref root);
                        node = root;
                        break;
                    }
                }
                treeColor[sibling] = true;
                node = parent;
                parent = treeParent[parent];
            } while (!treeColor[node]);

            if (node > -1)
	            treeColor[node] = false;
		}

		private static int GetFirst(int node, ref NativeArray<int> treeLeft)
		{
			if (node < 0) return -1;
			while (treeLeft[node] > -1)
				node = treeLeft[node];
			return node;
		}

		private static int GetLast(int node, ref NativeArray<int> treeRight)
		{
			if (node < 0) return -1;
			while (treeRight[node] > -1) node = treeRight[node];
			return node;
		}

		private static void RotateLeft(
			int node, 
			ref NativeArray<int> treeLeft,
			ref NativeArray<int> treeRight,
			ref NativeArray<int> treeParent,
			ref int root)
		{
			var p = node;
			var q = treeRight[node];
			var parent = treeParent[p];

			if (parent > -1)
			{
				if (treeLeft[parent] == p)
					treeLeft[parent] = q;
				else
					treeRight[parent] = q;
			}
			else
				root = q;
			treeParent[q] = parent;
			treeParent[p] = q;
			treeRight[p] = treeLeft[q];
			if (treeRight[p] > -1)
				treeParent[treeRight[p]] = p;
			treeLeft[q] = p;
		}

		private static void RotateRight(
			int node,
			ref NativeArray<int> treeLeft,
			ref NativeArray<int> treeRight,
			ref NativeArray<int> treeParent,
			ref int root)
		{
			var p = node;
			var q = treeLeft[node];
			var parent = treeParent[p];

			if (parent > -1)
			{
				if (treeLeft[parent] == p) 
					treeLeft[parent] = q;
				else
					treeRight[parent] = q;
			}
			else
				root = q;

			treeParent[q] = parent;
			treeParent[p] = q;
			treeLeft[p] = treeRight[q];
			if (treeLeft[p] > -1)
				treeParent[treeLeft[p]] = p;
			treeRight[q] = p;
		}
	}
}