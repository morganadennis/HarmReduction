using Gurobi;

namespace HarmReduction.Optimization
{
    public class AssignmentVars : ConstraintBase
    {
        public readonly double _distanceRequirement;
        private readonly BuildVars _buildVars;
        private readonly Dictionary<Node, Dictionary<Node, GRBVar>> _assignmentMap;
        private readonly Dictionary<Node, GRBConstr> _limitOneConstraintMap;
        private readonly Dictionary<Node, GRBConstr> _buildLimitConstraintMap;
        private readonly string _year;

        public AssignmentVars(List<Node> nodes, BuildVars buildVars, string year, double distanceReqt) : base(nodes) 
        {
            _buildVars = buildVars;
            _assignmentMap = [];
            _limitOneConstraintMap = [];
            _buildLimitConstraintMap = [];
            _year = year;
            _distanceRequirement = distanceReqt;
        }

        public override void Setup(GRBModel gModel)
        {
            // For each node, find "close" nodes that are within a given distance
            int index = 0;
            Dictionary<Node, Dictionary<Node, int>> tempMapping = [];
            foreach (Node thisNode in Nodes)
            {
                Dictionary<Node, int> nodeList = [];
                foreach (Node otherNode in Nodes)
                {
                    if (otherNode.CalculateDistance(thisNode) < _distanceRequirement)
                    {
                        nodeList[otherNode] = index++;
                    }
                }
                tempMapping[thisNode] = nodeList;
            }

            // Add the Gurobi variables
            GRBVar[] gVars = gModel.AddVars(index, GRB.BINARY);
            
            // Create the final map with the variables 
            foreach (var thisNode in tempMapping)
            {
                Dictionary<Node, GRBVar> thisMap = [];
                foreach (var closeNode in thisNode.Value)
                {
                    thisMap[closeNode.Key] = gVars[closeNode.Value];
                    thisMap[closeNode.Key].VarName = $"Assignment_{thisNode.Key.ZipCode}_ServedBy_{closeNode.Key.ZipCode}_{_year}";
                }
                _assignmentMap[thisNode.Key] = thisMap;
            }

            // Add one constraint for each node to force exactly one assignment variable for that node to be 1
            index = 0;
            GRBConstr[] gConstrs = gModel.AddConstrs(Nodes.Count);
            foreach (Node thisNode in Nodes)
            {
                _limitOneConstraintMap[thisNode] = gConstrs[index++];
                _limitOneConstraintMap[thisNode].ConstrName = $"LimitOneAssignment_{thisNode.ZipCode}_{_year}";
            }

            // Ensure that assignment variables are 0 if the build variable is 0
            index = 0;
            gConstrs = gModel.AddConstrs(Nodes.Count);
            foreach (Node thisNode in Nodes)
            {
                _buildLimitConstraintMap[thisNode] = gConstrs[index++];
                _buildLimitConstraintMap[thisNode].ConstrName = $"AssignmentForceBuild_{thisNode.ZipCode}_{_year}";
            }
        }

        public override void PutVals(GRBModel gModel)
        {
            Dictionary<Node, int> buildLimitVarCount = [];
            foreach (Node thisNode in Nodes)
            {
                buildLimitVarCount[thisNode] = 0;
            }
            
            foreach (Node thisNode in Nodes)
            {
                _limitOneConstraintMap[thisNode].RHS = 1.0;
                _limitOneConstraintMap[thisNode].Sense = GRB.EQUAL;
                _buildLimitConstraintMap[thisNode].RHS = 0.0;
                _buildLimitConstraintMap[thisNode].Sense = GRB.LESS_EQUAL;
                foreach (var nodeMap in _assignmentMap[thisNode])
                {
                    Node assignedNode = nodeMap.Key;
                    gModel.ChgCoeff(_limitOneConstraintMap[thisNode], nodeMap.Value, 1.0);
                    gModel.ChgCoeff(_buildLimitConstraintMap[assignedNode], nodeMap.Value, 1.0);
                    buildLimitVarCount[assignedNode] += 1;
                }
            }

            foreach (Node thisNode in Nodes)
            {
                gModel.ChgCoeff(_buildLimitConstraintMap[thisNode], _buildVars.GetBuildVar(thisNode), -buildLimitVarCount[thisNode]);
            }
        }    
        
        public Dictionary<Node, GRBVar> GetAssignmentVars(Node thisNode)
        {
            return _assignmentMap[thisNode];
        }
    }
}
