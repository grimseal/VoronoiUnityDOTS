using Unity.Collections;

namespace Voronoi.Handlers
{
	public struct RedBlueTree
	{
		public static int InsertSuccessor(
			int node,
			int arc,
			NativeArray<int> treeArc,
			NativeArray<int> treeLeft,
			NativeArray<int> treeRight,
			NativeArray<int> treeParent,
			NativeArray<int> treePrevious,
			NativeArray<int> treeNext,
			NativeArray<bool> treeRed,
			ref int treeCount,
			ref int root)
		{
			var successor = treeCount;
			treeArc[successor] = arc;
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
	                node = GetFirst(treeRight[node], treeLeft);
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
                node = GetFirst(root, treeLeft);
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
            treeRed[successor] = true;

            //the magic of the red black tree
            int grandma;
            int aunt;
            node = successor;
            while (parent > -1 && treeRed[parent])
            {
	            grandma = treeParent[parent];
                if (parent == treeLeft[grandma])
                {
                    aunt = treeRight[grandma];
                    if (aunt > -1 && treeRed[aunt])
                    {
	                    treeRed[parent] = false;
	                    treeRed[aunt] = false;
	                    treeRed[grandma] = true;
                        node = grandma;
                    }
                    else
                    {
                        if (node == treeRight[parent])
                        {
                            RotateLeft(parent, treeLeft, treeRight, treeParent, ref root);
                            node = parent;
                            parent = treeParent[node];
                        }
                        treeRed[parent] = false;
                        treeRed[grandma] = true;
                        RotateRight(grandma, treeLeft, treeRight, treeParent, ref root);
                    }
                }
                else
                {
                    aunt = treeLeft[grandma];
                    if (aunt > -1 && treeRed[aunt])
                    {
	                    treeRed[parent] = false;
	                    treeRed[aunt] = false;
	                    treeRed[grandma] = true;
	                    node = grandma;
                    }
                    else
                    {
                        if (node == treeLeft[parent])
                        {
                            RotateRight(parent, treeLeft, treeRight, treeParent, ref root);
                            node = parent;
                            parent = treeParent[node];
                        }
                        
                        treeRed[parent] = false;
                        treeRed[grandma] = true;
                        RotateLeft(grandma, treeLeft, treeRight, treeParent, ref root);
                    }
                }
                parent = treeParent[node];
            }
            treeRed[root] = false;
            return successor;
		}

		public static void RemoveNode(
			int node,
			NativeArray<int> treeLeft, 
			NativeArray<int> treeRight, 
			NativeArray<int> treeParent, 
			NativeArray<int> treePrevious, 
			NativeArray<int> treeNext, 
			NativeArray<bool> treeRed,
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
                next = GetFirst(right, treeLeft);

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
                red = treeRed[next];
                treeRed[next] = treeRed[node];
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
                red = treeRed[node];
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

            if (node > -1 && treeRed[node])
            {
	            treeRed[node] = false;
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
                    if (treeRed[sibling])
                    {
	                    treeRed[sibling] = false;
	                    treeRed[parent] = true;
                        RotateLeft(parent, treeLeft, treeRight, treeParent, ref root);
                        sibling = treeRight[parent];
                    }
                    if (treeLeft[sibling] > -1 && treeRed[treeLeft[sibling]] || 
                        treeRight[sibling] > -1 && treeRed[treeRight[sibling]])
                    {
                        //pretty sure this can be sibling.Left!= null && sibling.Left.Red
                        if (treeRight[sibling] < 0 || !treeRed[treeRight[sibling]])
                        {
	                        treeRed[treeLeft[sibling]] = false;
	                        treeRed[sibling] = true;
	                        RotateRight(sibling, treeLeft, treeRight, treeParent, ref root);
                            sibling = treeRight[parent];
                        }
                        treeRed[sibling] = treeRed[parent];
                        treeRed[parent] = treeRed[treeRight[sibling]] = false;
                        RotateLeft(parent, treeLeft, treeRight, treeParent, ref root);
                        node = root;
                        break;
                    }
                }
                else
                {
                    sibling = treeLeft[parent];
                    if (treeRed[sibling])
                    {
	                    treeRed[sibling] = false;
	                    treeRed[parent] = true;
                        RotateRight(parent, treeLeft, treeRight, treeParent, ref root);
                        sibling = treeLeft[parent];
                    }
                    if (treeLeft[sibling] > -1 && treeRed[treeLeft[sibling]] ||
                        treeRight[sibling] > -1 && treeRed[treeRight[sibling]])
                    {
                        if (treeLeft[sibling] < 0 || !treeRed[treeLeft[sibling]])
                        {
	                        treeRed[treeRight[sibling]] = false;
	                        treeRed[sibling] = true;
	                        RotateLeft(sibling, treeLeft, treeRight, treeParent, ref root);
                            sibling = treeLeft[parent];
                        }
                        treeRed[sibling] = treeRed[parent];
                        treeRed[parent] = treeRed[treeLeft[sibling]] = false;
                        RotateRight(parent, treeLeft, treeRight, treeParent, ref root);
                        node = root;
                        break;
                    }
                }
                treeRed[sibling] = true;
                node = parent;
                parent = treeParent[parent];
            } while (!treeRed[node]);

            if (node > -1)
	            treeRed[node] = false;
		}

		private static int GetFirst(int node, NativeArray<int> treeLeft)
		{
			if (node < 0) return -1;
			while (treeLeft[node] > -1)
				node = treeLeft[node];
			return node;
		}

		private static int GetLast(int node, NativeArray<int> treeRight)
		{
			if (node < 0) return -1;
			while (treeRight[node] > -1) node = treeRight[node];
			return node;
		}

		private static void RotateLeft(
			int node, 
			NativeArray<int> treeLeft, 
			NativeArray<int> treeRight, 
			NativeArray<int> treeParent, 
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
			NativeArray<int> treeLeft, 
			NativeArray<int> treeRight, 
			NativeArray<int> treeParent, 
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