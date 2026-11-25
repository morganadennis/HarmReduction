using Gurobi;

namespace HarmReduction.Optimization
{
    public class BuildVars : ConstraintBase
    {
        private readonly Dictionary<Node, GRBVar> _nodeVars;
        private readonly Dictionary<Node, GRBConstr> _yearConstrs;
        private readonly BuildVars? _firstYearVars;
        private readonly string _year;

        public BuildVars(List<Node> nodes, string year, BuildVars? firstYearVars = null) : base(nodes) 
        {
            _nodeVars = [];
            _yearConstrs = [];
            _year = year;
            _firstYearVars = firstYearVars;
        }
        
        public override void Setup(GRBModel gModel)
        {
            GRBVar[] vars = gModel.AddVars(Nodes.Count, GRB.BINARY);
            int index = 0;
            foreach (Node thisNode in Nodes)
            {
                _nodeVars[thisNode] = vars[index++];
                _nodeVars[thisNode].VarName = $"Build_{thisNode.ZipCode}_{_year}";
            }

            if (_firstYearVars != null)
            {
                index = 0;
                GRBConstr[] constrs = gModel.AddConstrs(Nodes.Count);
                foreach (Node thisNode in Nodes)
                {
                    _yearConstrs[thisNode] = constrs[index++];
                    _yearConstrs[thisNode].ConstrName = $"YearLimit_{thisNode.ZipCode}";
                }
            }
        }

        public override void PutVals(GRBModel gModel)
        {
            foreach (Node thisNode in Nodes)
            {
                GRBVar gVar = _nodeVars[thisNode];
                gVar.LB = 0.0;
                gVar.UB = 1.0;
            }

            if (_firstYearVars != null)
            {
                // [Var Year 4] - [Var Year 1] >= 0
                foreach (Node thisNode in Nodes)
                {
                    GRBConstr gCon = _yearConstrs[thisNode];
                    gCon.RHS = 0.0;
                    gCon.Sense = GRB.GREATER_EQUAL;
                    gModel.ChgCoeff(gCon, _nodeVars[thisNode], 1.0);
                    gModel.ChgCoeff(gCon, _firstYearVars.GetBuildVar(thisNode), -1.0);
                }
            }
        }

        public GRBVar GetBuildVar(Node thisNode)
        {
            return _nodeVars[thisNode];
        }
    }
}
