using Unity.Collections;

namespace Voronoi.Structures
{
    public struct RedBlackTree
    {
        public NativeArray<int> value;
        public NativeArray<int> left;
        public NativeArray<int> right;
        public NativeArray<int> parent;
        public NativeArray<int> previous;
        public NativeArray<int> next;
        public NativeArray<bool> color;
        public int count;
        public int root;

        public RedBlackTree(int capacity)
        {
            value = new NativeArray<int>(capacity, Allocator.Temp);
            left = new NativeArray<int>(capacity, Allocator.Temp);
            right = new NativeArray<int>(capacity, Allocator.Temp);
            parent = new NativeArray<int>(capacity, Allocator.Temp);
            previous = new NativeArray<int>(capacity, Allocator.Temp);
            next = new NativeArray<int>(capacity, Allocator.Temp);
            color = new NativeArray<bool>(capacity, Allocator.Temp);
            count = 0;
            root = -1;
        }

        public void Reset()
        {
            for (var i = 0; i < value.Length; i++)
            {
                value[i] = -1;
                left[i] = -1;
                right[i] = -1;
                parent[i] = -1;
                previous[i] = -1;
                next[i] = -1;
            }
            count = 0;
            root = -1;
        }

        public int InsertNode(int node, int val)
		{
			var successor = count;
			value[successor] = val;
			count++;

            int parent;

            if (node > -1)
            {
                //insert new node between node and its successor
                previous[successor] = node;
                next[successor] = next[node];
                if (next[node] > -1)
	                previous[next[node]] = successor;
                next[node] = successor;

                //insert successor into the tree
                if (right[node] > -1)
                {
	                node = GetFirst(right[node]);
	                left[node] = successor;
                }
                else
                {
	                right[node] = successor;
                }
                parent = node;
            }
            else if (root > -1)
            {
                //if the node is null, successor must be inserted
                //into the left most part of the tree
                node = GetFirst(root);
                //successor.Previous = null;
                next[successor] = node;
                previous[node] = successor;
                left[node] = successor;
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
            this.parent[successor] = parent;
            color[successor] = true;

            //the magic of the red black tree
            int grandma;
            int aunt;
            node = successor;
            while (parent > -1 && color[parent])
            {
	            grandma = this.parent[parent];
                if (parent == left[grandma])
                {
                    aunt = right[grandma];
                    if (aunt > -1 && color[aunt])
                    {
	                    color[parent] = false;
	                    color[aunt] = false;
	                    color[grandma] = true;
                        node = grandma;
                    }
                    else
                    {
                        if (node == right[parent])
                        {
                            RotateLeft(parent);
                            node = parent;
                            parent = this.parent[node];
                        }
                        color[parent] = false;
                        color[grandma] = true;
                        RotateRight(grandma);
                    }
                }
                else
                {
                    aunt = left[grandma];
                    if (aunt > -1 && color[aunt])
                    {
	                    color[parent] = false;
	                    color[aunt] = false;
	                    color[grandma] = true;
	                    node = grandma;
                    }
                    else
                    {
                        if (node == left[parent])
                        {
                            RotateRight(parent);
                            node = parent;
                            parent = this.parent[node];
                        }
                        
                        color[parent] = false;
                        color[grandma] = true;
                        RotateLeft(grandma);
                    }
                }
                parent = this.parent[node];
            }
            color[root] = false;
            return successor;
		}
        
        public void RemoveNode(int node)
		{
			//fix up linked list structure
			if (this.next[node] > -1) 
				previous[this.next[node]] = previous[node];
			if (previous[node] > -1)
				this.next[previous[node]] = this.next[node];

            //replace the node
            var parent = this.parent[node];
            var left = this.left[node];
            var right = this.right[node];

            int next;
            //figure out what to replace this node with
            if (left < 0)
                next = right;
            else if (right < 0)
                next = left;
            else
                next = GetFirst(right);

            //fix up the parent relation
            if (parent > -1)
            {
	            if (this.left[parent] == node)
		            this.left[parent] = next;
                else
					this.right[parent] = next;
            }
            else
            {
                root = next;
            }

            bool red;
            if (left > -1 && right > -1)
            {
                red = color[next];
                color[next] = color[node];
                this.left[next] = left;
                this.parent[left] = next;

                // if we reached down the tree
                if (next != right)
                {
                    parent = this.parent[next];
                    this.parent[next] = this.parent[node];

                    node = this.right[next];
                    this.left[parent] = node;

                    this.right[next] = right;
                    this.parent[right] = next;
                }
                else
                {
                    // the direct right will replace the node
                    this.parent[next] = parent;
                    parent = next;
                    node = this.right[next];
                }
            }
            else
            {
                red = color[node];
                node = next;
            }

            if (node > -1)
            {
	            this.parent[node] = parent;
            }

            if (red)
            {
                return;
            }

            if (node > -1 && color[node])
            {
	            color[node] = false;
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
                if (node == this.left[parent])
                {
                    sibling = this.right[parent];
                    if (color[sibling])
                    {
	                    color[sibling] = false;
	                    color[parent] = true;
                        RotateLeft(parent);
                        sibling = this.right[parent];
                    }
                    if (this.left[sibling] > -1 && color[this.left[sibling]] || 
                        this.right[sibling] > -1 && color[this.right[sibling]])
                    {
                        //pretty sure this can be sibling.Left!= null && sibling.Left.Red
                        if (this.right[sibling] < 0 || !color[this.right[sibling]])
                        {
	                        color[this.left[sibling]] = false;
	                        color[sibling] = true;
	                        RotateRight(sibling);
                            sibling = this.right[parent];
                        }
                        color[sibling] = color[parent];
                        color[parent] = color[this.right[sibling]] = false;
                        RotateLeft(parent);
                        node = root;
                        break;
                    }
                }
                else
                {
                    sibling = this.left[parent];
                    if (color[sibling])
                    {
	                    color[sibling] = false;
	                    color[parent] = true;
                        RotateRight(parent);
                        sibling = this.left[parent];
                    }
                    if (this.left[sibling] > -1 && color[this.left[sibling]] ||
                        this.right[sibling] > -1 && color[this.right[sibling]])
                    {
                        if (this.left[sibling] < 0 || !color[this.left[sibling]])
                        {
	                        color[this.right[sibling]] = false;
	                        color[sibling] = true;
	                        RotateLeft(sibling);
                            sibling = this.left[parent];
                        }
                        color[sibling] = color[parent];
                        color[parent] = color[this.left[sibling]] = false;
                        RotateRight(parent);
                        node = root;
                        break;
                    }
                }
                color[sibling] = true;
                node = parent;
                parent = this.parent[parent];
            } while (!color[node]);

            if (node > -1)
	            color[node] = false;
		}
        
        private int GetFirst(int node)
        {
            if (node < 0) return -1;
            while (left[node] > -1)
                node = left[node];
            return node;
        }

        private int GetLast(int node)
        {
            if (node < 0) return -1;
            while (right[node] > -1) node = right[node];
            return node;
        }

        private void RotateLeft(int node)
        {
            var p = node;
            var q = right[node];
            var parent = this.parent[p];

            if (parent > -1)
            {
                if (left[parent] == p)
                    left[parent] = q;
                else
                    right[parent] = q;
            }
            else
                root = q;
            this.parent[q] = parent;
            this.parent[p] = q;
            right[p] = left[q];
            if (right[p] > -1)
                this.parent[right[p]] = p;
            left[q] = p;
        }

        private void RotateRight(int node)
        {
            var p = node;
            var q = left[node];
            var parent = this.parent[p];

            if (parent > -1)
            {
                if (left[parent] == p) 
                    left[parent] = q;
                else
                    right[parent] = q;
            }
            else
                root = q;

            this.parent[q] = parent;
            this.parent[p] = q;
            left[p] = right[q];
            if (left[p] > -1)
                this.parent[left[p]] = p;
            right[q] = p;
        }

    }
}
