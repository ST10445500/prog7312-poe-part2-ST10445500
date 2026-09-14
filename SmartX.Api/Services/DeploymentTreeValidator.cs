using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//DeploymentTreeValidator

//.....................................o0oSTART OF FILEo0o........................................//

// The service keeps business rules and validation away from the controller.

namespace SmartX.Api.Services
{
    //walks a deployment hierarchy and reports anything wrong with it
    //any depth, so each level calls the same method on its children
    public class DeploymentTreeValidator
    {
        //how deep the walk is allowed to go before the tree is treated as malformed
        public const int MaxDepth = 32;

        private readonly SensorStore _store;

        //..............................................................................//

        public DeploymentTreeValidator(SensorStore store)
        {
            _store = store;
        }

        //..............................................................................//

        //validates a whole deployment tree starting from its root
        public DeploymentValidationResult Validate(DeploymentNode root)
        {
            var result = new DeploymentValidationResult();

            if (root == null)
            {
                result.Errors.Add("No deployment tree was supplied.");
                return result;
            }

            ValidateNode(root, root.Name, 1, result);
            return result;
        }

        //..............................................................................//

        //checks one node and then walks its children
        private void ValidateNode(DeploymentNode node, string path, int depth, DeploymentValidationResult result)
        {
            result.NodesChecked++;

            if (depth > result.DeepestLevel)
            {
                result.DeepestLevel = depth;
            }

            // Nothing real is this deep. Without the limit the recursion overflows the stack.
            if (depth > MaxDepth)
            {
                result.Errors.Add($"'{path}' is nested deeper than {MaxDepth} levels, which is not a valid deployment.");
                return;
            }

            if (string.IsNullOrWhiteSpace(node.Name))
            {
                result.Errors.Add($"A node under '{path}' has no name.");
            }

            if (node.IsSensor)
            {
                result.SensorsFound++;

                if (node.Children.Count > 0)
                {
                    result.Errors.Add($"'{path}' has a mac address so it is a sensor, but it also has children.");
                }

                if (!_store.IsRegistered(node.SensorMacAddress!))
                {
                    result.Errors.Add($"'{path}' points at sensor {node.SensorMacAddress}, which is not registered.");
                }
            }

            // A node with no children ends this branch of the walk.
            if (node.Children.Count == 0)
            {
                return;
            }

            // Two sibling nodes sharing a name would make a path ambiguous.
            var namesUsedHere = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var child in node.Children)
            {
                if (!namesUsedHere.Add(child.Name))
                {
                    result.Errors.Add($"'{path}' has more than one child called '{child.Name}'.");
                }

                ValidateNode(child, $"{path} / {child.Name}", depth + 1, result);
            }
        }
    }
}

//.....................................o0oEND OF FILEo0o..........................................//
