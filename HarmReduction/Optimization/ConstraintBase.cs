using Gurobi;

namespace HarmReduction.Optimization
{
    public abstract class ConstraintBase(List<Node> nodes)
    {
        protected readonly List<Node> Nodes = nodes;

        /// <summary>
        /// This creates the indices for the variables and constraints that will be needed
        /// </summary>
        public abstract void Setup(GRBModel gModel);

        /// <summary>
        /// This inputs the data values for the optimization problem  
        /// </summary>
        public abstract void PutVals(GRBModel gModel);
    }
}
