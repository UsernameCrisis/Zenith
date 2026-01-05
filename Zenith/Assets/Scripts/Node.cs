using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;

namespace BehaviourTrees
{
    public class BehaviourTree : Node
    {
        public BehaviourTree(string name) : base(name) {}

        public override Status Process()
        {
            while (currentChild < children.Count)
            {
                var status = children[currentChild].Process();
                if (status != Status.Success) return status;
                currentChild++;
            }
            return Status.Success;
        }
    }

    // public class UntilFail : Node {
    //     public UntilFail(string name) : base(name) { }
        
    //     public override Status Process() {
    //         if (children[0].Process() == Status.Failure) {
    //             Reset();
    //             return Status.Failure;
    //         }

    //         return Status.Running;
    //     }
    // }
    
    public class Inverter : Node {
        public Inverter(string name, int priority = 0) : base(name, priority) { }
        
        public override Status Process() {
            switch (children[0].Process()) {
                case Status.Running:
                    return Status.Running;
                case Status.Failure:
                    return Status.Success;
                case Status.Success:
                    return Status.Failure;
                default:
                    return Status.Failure;
            }
        }
    }
    
    public class RandomSelector : PrioritySelector
    {
        private Node current;
        protected override List<Node> SortChildren()  {
            if (current == null) return children.Shuffle().ToList();
            return children;
        }

        public RandomSelector(string name, int priority = 0) : base(name, priority) {}

        public override Status Process()
        {
            if (current != null)
            {
                var status = current.Process();
                if (status != Status.Running) current = null;
                return status;
            }

            var shuffled = SortChildren();
            foreach (var child in shuffled)
            {
                var status = child.Process();
                if (status != Status.Failure)
                {
                    current = (status == Status.Running) ? child : null;
                    return status;
                }
            }

            return Status.Failure;
        }
    }

    public class ProbabilitySelector : PrioritySelector
    {
        private List<float> weights;
        private Node current;

        public ProbabilitySelector(string name, List<float> weights, int priority = 0) : base(name, priority)
        {
            float total = weights.Sum();
            this.weights = weights.Select(w => w / total).ToList();
        }

        public override Status Process()
        {
            if (current != null)
            {
                var status = current.Process();
                if (status != Status.Running)
                    current = null;
                return status;
            }
            
            float roll = Random.value;
            float sum = 0f;

            for (int i = 0; i < children.Count; i++)
            {
                sum += weights[i];
                if (roll <= sum)
                {
                    var status = children[i].Process();
                    if (status == Status.Running)
                        current = children[i];
                    return status;
                }
            }

            return Status.Failure;
        }
    }

    public class PrioritySelector : Selector
    {
        List<Node> sortedChildren;
        List<Node> SortedChildren => sortedChildren ??= SortChildren();

        protected virtual List<Node> SortChildren() => children.OrderByDescending(child => child.priority).ToList();

        public PrioritySelector(string name, int priority = 0) : base(name, priority) {}

        public override void Reset()
        {
            base.Reset();
            sortedChildren = null;
        }

        public override Status Process()
        {
            foreach (var child in SortedChildren)
            {
                switch (child.Process())
                {
                    case Status.Running:
                        return Status.Running;
                    case Status.Success:
                        return Status.Success;
                    default:
                        continue;
                }
            }
            return Status.Failure;
        }
    }

    public class Selector : Node
    {
        public Selector(string name, int priority = 0) : base(name, priority) {}

        public override Status Process()
        {
            if (currentChild < children.Count)
            {
                switch(children[currentChild].Process())
                {
                    case Status.Running:
                        return Status.Running;
                    case Status.Success:
                        Reset();
                        return Status.Success;
                    default:
                        currentChild++;
                        return Status.Running;
                }
            }
            Reset();
            return Status.Failure;
        }
    }

    public class Sequence : Node
    {
        public Sequence(string name, int priority = 0) : base(name, priority) {}
        public override Status Process()
        {
            if (currentChild < children.Count)
            {
                switch(children[currentChild].Process())
                {
                    case Status.Running:
                        return Status.Running;
                    case Status.Failure:
                        Reset();
                        return Status.Failure;
                    default:
                        currentChild++;
                        return currentChild == children.Count ? Status.Success : Status.Running;
                }
            }
            Reset();
            return Status.Success;
        }
    }

    public class Leaf : Node
    {
        readonly IStrategy strategy;

        public Leaf(string name, IStrategy strategy, int priority = 0) : base(name, priority)
        {
            this.strategy = strategy;
        }

        public override Status Process() => strategy.Process();
    }

    public class Node
    {
        public enum Status { Success, Failure, Running }

        public readonly string name;
        public readonly int priority;

        public readonly List<Node> children = new();
        protected int currentChild;
        
        public Node(string name = "Node", int priority = 0)
        {
            this.name = name;
            this.priority = priority;
        }

        public void AddChild(Node child) => children.Add(child);

        public virtual Status Process() => children[currentChild].Process();

        public virtual void Reset()
        {
            currentChild = 0;
            foreach (var child in children)
            {
                child.Reset();
            }
        }
    }
}
