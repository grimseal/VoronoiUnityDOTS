using Unity.Collections;

namespace Voronoi.Handlers
{
	public struct RedBlackTree
	{
		public static int TreeInsert(
			int node,
			int value,
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
			treeValue[successor] = value;
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
	                int node1 = treeRight[node];
	                if (node1 < 0) node = -1;
	                else
	                {
		                while (treeLeft[node1] > -1)
			                node1 = treeLeft[node1];
		                node = node1;
	                }

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
                int node1 = root;
                if (node1 < 0) node = -1;
                else
                {
	                while (treeLeft[node1] > -1)
		                node1 = treeLeft[node1];
	                node = node1;
                }

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
	                        var p1 = parent;
	                        var q1 = treeRight[parent];
	                        var parent2 = treeParent[p1];

	                        if (parent2 > -1)
	                        {
		                        if (treeLeft[parent2] == p1)
			                        treeLeft[parent2] = q1;
		                        else
			                        treeRight[parent2] = q1;
	                        }
	                        else
		                        root = q1;
	                        treeParent[q1] = parent2;
	                        treeParent[p1] = q1;
	                        treeRight[p1] = treeLeft[q1];
	                        if (treeRight[p1] > -1)
		                        treeParent[treeRight[p1]] = p1;
	                        treeLeft[q1] = p1;
	                        node = parent;
                            parent = treeParent[node];
                        }
                        treeColor[parent] = false;
                        treeColor[grandma] = true;
                        var p = grandma;
                        var q = treeLeft[grandma];
                        var parent1 = treeParent[p];

                        if (parent1 > -1)
                        {
	                        if (treeLeft[parent1] == p) 
		                        treeLeft[parent1] = q;
	                        else
		                        treeRight[parent1] = q;
                        }
                        else
	                        root = q;

                        treeParent[q] = parent1;
                        treeParent[p] = q;
                        treeLeft[p] = treeRight[q];
                        if (treeLeft[p] > -1)
	                        treeParent[treeLeft[p]] = p;
                        treeRight[q] = p;
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
	                        var p = parent;
	                        var q = treeLeft[parent];
	                        var parent1 = treeParent[p];

	                        if (parent1 > -1)
	                        {
		                        if (treeLeft[parent1] == p) 
			                        treeLeft[parent1] = q;
		                        else
			                        treeRight[parent1] = q;
	                        }
	                        else
		                        root = q;

	                        treeParent[q] = parent1;
	                        treeParent[p] = q;
	                        treeLeft[p] = treeRight[q];
	                        if (treeLeft[p] > -1)
		                        treeParent[treeLeft[p]] = p;
	                        treeRight[q] = p;
	                        node = parent;
                            parent = treeParent[node];
                        }
                        
                        treeColor[parent] = false;
                        treeColor[grandma] = true;
                        var p1 = grandma;
                        var q1 = treeRight[grandma];
                        var parent2 = treeParent[p1];

                        if (parent2 > -1)
                        {
	                        if (treeLeft[parent2] == p1)
		                        treeLeft[parent2] = q1;
	                        else
		                        treeRight[parent2] = q1;
                        }
                        else
	                        root = q1;
                        treeParent[q1] = parent2;
                        treeParent[p1] = q1;
                        treeRight[p1] = treeLeft[q1];
                        if (treeRight[p1] > -1)
	                        treeParent[treeRight[p1]] = p1;
                        treeLeft[q1] = p1;
                    }
                }
                parent = treeParent[node];
            }
            treeColor[root] = false;
            return successor;
		}

		public static void TreeRemove(
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
            {
	            int node1 = right;
	            if (node1 < 0) next = -1;
	            else
	            {
		            while (treeLeft[node1] > -1)
			            node1 = treeLeft[node1];
		            next = node1;
	            }
            }

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
	                    var p = parent;
	                    var q = treeRight[parent];
	                    var parent1 = treeParent[p];

	                    if (parent1 > -1)
	                    {
		                    if (treeLeft[parent1] == p)
			                    treeLeft[parent1] = q;
		                    else
			                    treeRight[parent1] = q;
	                    }
	                    else
		                    root = q;
	                    treeParent[q] = parent1;
	                    treeParent[p] = q;
	                    treeRight[p] = treeLeft[q];
	                    if (treeRight[p] > -1)
		                    treeParent[treeRight[p]] = p;
	                    treeLeft[q] = p;
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
	                        var p = sibling;
	                        var q = treeLeft[sibling];
	                        var parent1 = treeParent[p];

	                        if (parent1 > -1)
	                        {
		                        if (treeLeft[parent1] == p) 
			                        treeLeft[parent1] = q;
		                        else
			                        treeRight[parent1] = q;
	                        }
	                        else
		                        root = q;

	                        treeParent[q] = parent1;
	                        treeParent[p] = q;
	                        treeLeft[p] = treeRight[q];
	                        if (treeLeft[p] > -1)
		                        treeParent[treeLeft[p]] = p;
	                        treeRight[q] = p;
	                        sibling = treeRight[parent];
                        }
                        treeColor[sibling] = treeColor[parent];
                        treeColor[parent] = treeColor[treeRight[sibling]] = false;
                        var p1 = parent;
                        var q1 = treeRight[parent];
                        var parent2 = treeParent[p1];

                        if (parent2 > -1)
                        {
	                        if (treeLeft[parent2] == p1)
		                        treeLeft[parent2] = q1;
	                        else
		                        treeRight[parent2] = q1;
                        }
                        else
	                        root = q1;
                        treeParent[q1] = parent2;
                        treeParent[p1] = q1;
                        treeRight[p1] = treeLeft[q1];
                        if (treeRight[p1] > -1)
	                        treeParent[treeRight[p1]] = p1;
                        treeLeft[q1] = p1;
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
	                    var p = parent;
	                    var q = treeLeft[parent];
	                    var parent1 = treeParent[p];

	                    if (parent1 > -1)
	                    {
		                    if (treeLeft[parent1] == p) 
			                    treeLeft[parent1] = q;
		                    else
			                    treeRight[parent1] = q;
	                    }
	                    else
		                    root = q;

	                    treeParent[q] = parent1;
	                    treeParent[p] = q;
	                    treeLeft[p] = treeRight[q];
	                    if (treeLeft[p] > -1)
		                    treeParent[treeLeft[p]] = p;
	                    treeRight[q] = p;
	                    sibling = treeLeft[parent];
                    }
                    if (treeLeft[sibling] > -1 && treeColor[treeLeft[sibling]] ||
                        treeRight[sibling] > -1 && treeColor[treeRight[sibling]])
                    {
                        if (treeLeft[sibling] < 0 || !treeColor[treeLeft[sibling]])
                        {
	                        treeColor[treeRight[sibling]] = false;
	                        treeColor[sibling] = true;
	                        var p1 = sibling;
	                        var q1 = treeRight[sibling];
	                        var parent2 = treeParent[p1];

	                        if (parent2 > -1)
	                        {
		                        if (treeLeft[parent2] == p1)
			                        treeLeft[parent2] = q1;
		                        else
			                        treeRight[parent2] = q1;
	                        }
	                        else
		                        root = q1;
	                        treeParent[q1] = parent2;
	                        treeParent[p1] = q1;
	                        treeRight[p1] = treeLeft[q1];
	                        if (treeRight[p1] > -1)
		                        treeParent[treeRight[p1]] = p1;
	                        treeLeft[q1] = p1;
	                        sibling = treeLeft[parent];
                        }
                        treeColor[sibling] = treeColor[parent];
                        treeColor[parent] = treeColor[treeLeft[sibling]] = false;
                        var p = parent;
                        var q = treeLeft[parent];
                        var parent1 = treeParent[p];

                        if (parent1 > -1)
                        {
	                        if (treeLeft[parent1] == p) 
		                        treeLeft[parent1] = q;
	                        else
		                        treeRight[parent1] = q;
                        }
                        else
	                        root = q;

                        treeParent[q] = parent1;
                        treeParent[p] = q;
                        treeLeft[p] = treeRight[q];
                        if (treeLeft[p] > -1)
	                        treeParent[treeLeft[p]] = p;
                        treeRight[q] = p;
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
	}
}