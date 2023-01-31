using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MHApi.Laser
{

    /// <summary>
    /// Simple class that allows relating analog-out control voltages
    /// to powers at sample via a linear interpolation lookup table
    /// </summary>
    public class PowerAtSampleLookup
    {
        #region Classdefs

        public class TreeSearchException: Exception
        {
            public TreeSearchException(string message) : base(message) { }
        }

        private class TransformNode
        {
            /// <summary>
            /// The key value of the tree node on which the tree will be built
            /// </summary>
            public double Key { get; private set; }

            /// <summary>
            /// The associated measured value with the key
            /// </summary>
            public double Value { get; private set; }

            /// <summary>
            /// The root of the tree half that is greater than Key
            /// </summary>
            public TransformNode Gt { get; private set; }

            /// <summary>
            /// The root of the tree half that is smaller than Key
            /// </summary>
            public TransformNode Lt { get; private set; }

            /// <summary>
            /// Creates a new node
            /// </summary>
            /// <param name="key">The node's key</param>
            /// <param name="value">The node's transform value</param>
            /// <param name="gt">Root of the subtree that is all greater than key</param>
            /// <param name="lt">Root of the subtree that is all smaller than key</param>
            public TransformNode(double key, double value, TransformNode gt, TransformNode lt)
            {
                Key = key;
                Value = value;
                Gt = gt;
                Lt = lt;
            }
        }

        private class TransformTree
        {
            public TransformNode Root { get; private set; }

            private TransformNode BuildSubtree(List<double> keys, List<double> values)
            {
                if (keys.Count == 0)
                    return null;
                if (keys.Count != values.Count)
                    throw new ArgumentException("Keys and values must have same number of elements.");
                int mid_index = keys.Count / 2;
                int start_lt = 0;
                int end_lt = mid_index;
                TransformNode lt_root;
                if ((end_lt - start_lt) <= 0)
                    lt_root = null;
                else
                    lt_root = BuildSubtree(keys.GetRange(start_lt, end_lt - start_lt), 
                        values.GetRange(start_lt, end_lt - start_lt));
                int start_gt = mid_index + 1;
                int end_gt = keys.Count;
                TransformNode gt_root;
                if ((end_gt - end_lt) <= 0)
                    gt_root = null;
                else
                    gt_root = BuildSubtree(keys.GetRange(start_gt, end_gt - start_gt), 
                        values.GetRange(start_gt, end_gt - start_gt));
                return new TransformNode(keys[mid_index], values[mid_index], gt_root, lt_root);
            }

            /// <summary>
            /// Finds the node with the largest key that is smaller than or equal to key
            /// </summary>
            /// <param name="key">The key to find</param>
            /// <param name="root">The subtree to search</param>
            /// <returns></returns>
            private TransformNode Find_Lte(TransformNode root, double key, TransformNode lte)
            {
                if (root == null)
                    return lte;
                double lte_dist = lte != null ? key - lte.Key : double.PositiveInfinity;
                if (key - root.Key >= 0 && key - root.Key < lte_dist)
                    lte = root;
                if (key < root.Key)
                    return Find_Lte(root.Lt, key, lte);
                else
                    return Find_Lte(root.Gt, key, lte);
            }

            private TransformNode Find_Gte(TransformNode root, double key, TransformNode gte)
            {
                if (root == null)
                    return gte;
                double gte_dist = gte != null ? gte.Key - key : double.PositiveInfinity;
                if (root.Key - key >= 0 && root.Key - key < gte_dist)
                    gte = root;
                if (key < root.Key)
                    return Find_Gte(root.Lt, key, gte);
                else
                    return Find_Gte(root.Gt, key, gte);
            }

            private Tuple<TransformNode, TransformNode> Find(double key, TransformNode root)
            {
                return new Tuple<TransformNode, TransformNode>(Find_Lte(root, key, null), Find_Gte(root, key, null));
            }

            public TransformTree(double[] keys, double[] values)
            {
                // Check that keys increase monotonically
                for (int i = 1; i < keys.Length; i++)
                {
                    if (keys[i] <= keys[i - 1])
                        throw new ArgumentException("Keys must increase monotonically");
                }
                Root = BuildSubtree(new List<double>(keys), new List<double>(values));
            }

            /// <summary>
            /// Searches for the nodes that demarcate the closest interval around input
            /// and returns the interpolated value
            /// </summary>
            /// <param name="input">The value to transform</param>
            /// <param name="raise_oob">If true raise exception if input is out of bounds otherwise
            /// return extrem node's value</param>
            /// <returns>The transformed value</returns>
            /// <exception cref="TreeSearchException">If raise_oob is true and input is out of bounds</exception>
            public double Transform(double input, bool raise_oob)
            {
                var search_res = Find(input, this.Root);
                TransformNode lower_bound = search_res.Item1;
                TransformNode upper_bound = search_res.Item2;
                if(lower_bound == null)
                {
                    //This means that our input is lower than the minimum value in the tree
                    if (raise_oob)
                        throw new TreeSearchException("Input provided is smaller than tree minimum");
                    else
                        return upper_bound.Value;
                }
                if(upper_bound == null)
                {
                    if (raise_oob)
                        throw new TreeSearchException("Input provided is larger than tree maximum");
                    else
                        return lower_bound.Value;
                }
                if (upper_bound == lower_bound)
                    return upper_bound.Value;//Exact match
                //Full interval is present so we interpolate
                double interval_size = upper_bound.Key - lower_bound.Key;
                double dist = input - lower_bound.Key;
                double frac = dist / interval_size;
                double value_diff = upper_bound.Value - lower_bound.Value;
                double base_value = lower_bound.Value;
                return base_value + frac * value_diff;
            }
        }

        #endregion

        #region Members

        /// <summary>
        /// Array of measured power at sample values in mW
        /// </summary>
        private double[] _measured_pas_mW;

        /// <summary>
        /// For each measured power at sample the analog out
        /// voltage in V that created it
        /// </summary>
        private double[] _bin_aov_values_V;

        /// <summary>
        /// Determines whether to raise an error when querying out of range value
        /// or if false, return edge value of lookup table
        /// </summary>
        private bool _raise_oor;

        /// <summary>
        /// Lookup tree to transform from power at sample to control voltage
        /// </summary>
        private TransformTree _pas_to_aov;

        /// <summary>
        /// Lookup tree to transform from control voltage to power at sample
        /// </summary>
        private TransformTree _aov_to_pas;

        #endregion

        /// <summary>
        /// Creates a new lookup table
        /// </summary>
        /// <param name="pas">The measured power at sample values</param>
        /// <param name="aov">The analog out voltages at which powers have been measured</param>
        /// <param name="raise_out_of_range">If true, raise error when querying with input outside table
        /// otherwise simply return edge value</param>
        public PowerAtSampleLookup(double[] pas, double[] aov, bool raise_out_of_range)
        {
            if (pas.Length != aov.Length)
                throw new ArgumentException("Both power at sample and analog out voltage need to have same size");
            if (pas.Length == 0)
                throw new ArgumentException("No data provided");
            // Check that values increase monotonically
            for(int i =1; i < pas.Length; i++)
            {
                if (pas[i] <= pas[i - 1] || aov[i] <= aov[i - 1])
                    throw new ArgumentException("Both pas and aov need to increase monotonically");
            }
            _raise_oor = raise_out_of_range;
            _pas_to_aov = new TransformTree(pas, aov);
            _aov_to_pas = new TransformTree(aov, pas);
        }

        #region Methods

        public double GetAOVByPower(double power_at_sample_mW)
        {
            return _pas_to_aov.Transform(power_at_sample_mW, _raise_oor);
        }

        public double GetPowerByAOV(double control_aov)
        {
            return _aov_to_pas.Transform(control_aov, _raise_oor);
        }

        #endregion
    }
}
