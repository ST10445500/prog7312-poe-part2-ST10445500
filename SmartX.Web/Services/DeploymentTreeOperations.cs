using SmartX.Shared.Models;

//ST10445500 - PROG7312 - SmartX POE
//DeploymentTreeOperations

//.....................................o0oSTART OF FILEo0o........................................//

// The helper keeps shared client wording and settings out of the pages that use them.

namespace SmartX.Web.Services
{
    //the walks the deployment editor needs over a tree of any depth
    //each one recurses, because a tree has no fixed number of levels
    public static class DeploymentTreeOperations
    {
        //..............................................................................//

        //builds a copy of a node and everything underneath it
        public static DeploymentNode Clone(DeploymentNode node)
        {
            return new DeploymentNode
            {
                Name = node.Name,
                SensorMacAddress = node.SensorMacAddress,
                Children = node.Children.Select(Clone).ToList()
            };
        }

        //..............................................................................//

        //removes a node from whichever parent holds it, returning false if it was not found
        public static bool Detach(DeploymentNode root, DeploymentNode node)
        {
            if (root.Children.Remove(node))
            {
                return true;
            }

            return root.Children.Any(child => Detach(child, node));
        }

        //..............................................................................//

        //moves a node inside a zone, at the end of whatever is already there
        public static bool MoveInto(DeploymentNode root, DeploymentNode node, DeploymentNode zone)
        {
            if (!CanMove(node, zone))
            {
                return false;
            }

            // A device dragged in from the unplaced list was never in the tree.
            Detach(root, node);

            zone.Children.Add(node);
            return true;
        }

        //..............................................................................//

        //moves a node in among a zone's children, just before the one given
        public static bool MoveBefore(DeploymentNode root, DeploymentNode node, DeploymentNode zone, DeploymentNode? before)
        {
            if (!CanMove(node, zone))
            {
                return false;
            }

            Detach(root, node);

            // Detaching first shifts the list, so the index has to be read after it.
            var index = before == null ? -1 : zone.Children.IndexOf(before);

            if (index < 0)
            {
                zone.Children.Add(node);
            }
            else
            {
                zone.Children.Insert(index, node);
            }

            return true;
        }

        //..............................................................................//

        //checks whether a node is allowed to end up under this zone
        private static bool CanMove(DeploymentNode node, DeploymentNode zone)
        {
            // A sensor is a leaf, and a branch cannot be dropped inside itself.
            return !zone.IsSensor && !Contains(node, zone);
        }

        //..............................................................................//

        //checks whether a node is this node or sits somewhere underneath it
        public static bool Contains(DeploymentNode node, DeploymentNode candidate)
        {
            if (ReferenceEquals(node, candidate))
            {
                return true;
            }

            return node.Children.Any(child => Contains(child, candidate));
        }

        //..............................................................................//

        //retrieves every mac address the tree currently places somewhere
        public static HashSet<string> MacAddressesIn(DeploymentNode root)
        {
            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectMacAddresses(root, found);

            return found;
        }

        //..............................................................................//

        //adds this node's mac address if it has one, then asks its children
        private static void CollectMacAddresses(DeploymentNode node, HashSet<string> found)
        {
            if (node.IsSensor)
            {
                found.Add(node.SensorMacAddress!);
            }

            foreach (var child in node.Children)
            {
                CollectMacAddresses(child, found);
            }
        }

    }

    //..............................................................................//

    //holds where a node was dropped in among a zone's children
    public class DeploymentDrop
    {
        //the zone whose children the node is landing in
        public DeploymentNode Zone { get; set; } = null!;

        //the child it should sit just before, or null to land at the end
        public DeploymentNode? Before { get; set; }
    }

}

//.....................................o0oEND OF FILEo0o..........................................//
