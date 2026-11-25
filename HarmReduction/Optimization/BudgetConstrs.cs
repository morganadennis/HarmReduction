using Gurobi;

namespace HarmReduction.Optimization
{
    class BudgetConstrs : ConstraintBase
    {
        private readonly BuildVars _buildVars;
        private readonly PeopleServed _naloxoneVars;
        private readonly GRBConstr[] _budgetConstr;
        private readonly double _budget;
        private readonly string _year;

        public BudgetConstrs(List<Node> nodes, BuildVars buildVars, PeopleServed naloxoneVars, double budget, string year) : base(nodes) 
        {
            _buildVars = buildVars;
            _naloxoneVars = naloxoneVars;
            _budgetConstr = new GRBConstr[1];
            _budget = budget;
            _year = year;
        }

        public override void Setup(GRBModel gModel)
        {
            _budgetConstr[0] = gModel.AddConstrs(1)[0];
            _budgetConstr[0].ConstrName = $"Budget_{_year}";
        }

        public override void PutVals(GRBModel gModel)
        {
            _budgetConstr[0].RHS = _budget;
            _budgetConstr[0].Sense = GRB.LESS_EQUAL;

            double incrCost = 23.35;
            foreach (Node thisNode in Nodes)
            {
                gModel.ChgCoeff(_budgetConstr[0], _buildVars.GetBuildVar(thisNode), thisNode.AnnualizedCost);
                gModel.ChgCoeff(_budgetConstr[0], _naloxoneVars.GetVar(thisNode), incrCost);
            }
        }        
    }
}
