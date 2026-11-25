using Gurobi;

namespace HarmReduction.Optimization
{
    public class PeopleServed : ConstraintBase
    {
        private readonly Dictionary<Node, GRBVar> _peopleServedVars;
        private readonly Dictionary<Node, GRBConstr> _peopleDefConstrs;
        private readonly Dictionary<Node, GRBConstr> _peopleMinConstrs;
        private readonly Dictionary<Node, GRBVar> _peopleMinRelaxation;

        private readonly AssignmentVars _assignmentVars;
        private readonly BuildVars _buildVars;
        private readonly string _description;
        private readonly double _objCoeff, _stDev;
        private readonly bool _useDemand, _useRandomDraws, _increaseNaloxoneDemand;
        private readonly Random _random;
        
        public PeopleServed(List<Node> nodes, BuildVars bVars, AssignmentVars aVars, string description, 
            double objCoeff, bool useDemand, bool useRandomDraws, int randSeed, double stDev, bool increaseNaloxoneDemand) : base(nodes)
        {
            _peopleServedVars = [];
            _peopleDefConstrs = [];
            _peopleMinConstrs = [];
            _peopleMinRelaxation = [];
            _buildVars = bVars;
            _assignmentVars = aVars;
            _description = description;
            _objCoeff = objCoeff;
            _useDemand = useDemand;
            _useRandomDraws = useRandomDraws;
            _stDev = stDev;
            _increaseNaloxoneDemand = increaseNaloxoneDemand;
            _random = new Random(randSeed * 100);
        }

        public override void Setup(GRBModel gModel)
        {
            GRBVar[] gVars = gModel.AddVars(Nodes.Count * 2, GRB.CONTINUOUS);
            GRBConstr[] gConstrs = gModel.AddConstrs(Nodes.Count * 2);
            int vIndex = 0, cIndex = 0;
            foreach (Node thisNode in Nodes)
            {
                _peopleServedVars[thisNode] = gVars[vIndex++];
                _peopleServedVars[thisNode].VarName = $"{_description}_At_{thisNode.ZipCode}";
                _peopleMinRelaxation[thisNode] = gVars[vIndex++];
                _peopleMinRelaxation[thisNode].VarName = $"MinRelax_{_description}_{thisNode.ZipCode}";
                _peopleDefConstrs[thisNode] = gConstrs[cIndex++];
                _peopleDefConstrs[thisNode].ConstrName = $"Def_{_description}_{thisNode.ZipCode}";
                _peopleMinConstrs[thisNode] = gConstrs[cIndex++];
                _peopleMinConstrs[thisNode].ConstrName = $"Min_{_description}_{thisNode.ZipCode}";              
            }
        }
        public override void PutVals(GRBModel gModel)
        {
            foreach (Node thisNode in Nodes)
            {
                _peopleServedVars[thisNode].LB = 0.0;
                _peopleServedVars[thisNode].UB = GRB.INFINITY; 
                _peopleServedVars[thisNode].Obj = _objCoeff;
                _peopleMinRelaxation[thisNode].LB = 0.0;
                _peopleMinRelaxation[thisNode].UB = GRB.INFINITY;
                _peopleMinRelaxation[thisNode].Obj = -100.0 * _objCoeff;
                _peopleDefConstrs[thisNode].RHS = 0.0;
                _peopleDefConstrs[thisNode].Sense = GRB.GREATER_EQUAL;
                _peopleMinConstrs[thisNode].RHS = 0.0;
                _peopleMinConstrs[thisNode].Sense = GRB.GREATER_EQUAL;

                // Definition Constraints
                gModel.ChgCoeff(_peopleDefConstrs[thisNode], _peopleServedVars[thisNode], -1.0);

                double demandVal = _useDemand ? thisNode.Demand : thisNode.Referrals;
                if (_increaseNaloxoneDemand)
                {
                    demandVal *= thisNode.County switch
                    {
                        "Bucks" => 1.2581,
                        "Chester" => 1.2927,
                        "Delaware" => 1.2105,
                        "Montgomery" => 1.6509,                        
                        _ => 1.5969, // Philadelphia
                    };
                }
                double demandValToUse = _useRandomDraws ? Math.Max(GetRandomNormalDraw(demandVal, demandVal * _stDev), 0.0) : demandVal;

                foreach (var item in _assignmentVars.GetAssignmentVars(thisNode))
                {
                    Node assignmentNode = item.Key;
                    double distance = assignmentNode.CalculateDistance(thisNode);
                    double distanceFraction = Math.Max(0.0, (5.0 - distance) / 5.0);
                                                            
                    gModel.ChgCoeff(_peopleDefConstrs[assignmentNode], item.Value, distanceFraction * demandValToUse);
                }

                // Min Constraints (if built, it must serve at least 100 people)
                gModel.ChgCoeff(_peopleMinConstrs[thisNode], _peopleServedVars[thisNode], 1.0);
                gModel.ChgCoeff(_peopleMinConstrs[thisNode], _buildVars.GetBuildVar(thisNode), -100.0);
                gModel.ChgCoeff(_peopleMinConstrs[thisNode], _peopleMinRelaxation[thisNode], 1.0);
            }
        }    
        
        private double GetRandomNormalDraw(double mean, double stDev)
        {
            // Uniform numbers between 0 and 1 exclusive
            double u1 = 1.0 - _random.NextDouble();
            double u2 = 1.0 - _random.NextDouble();

            // Box-Muller transform calculation
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            // Scale and shift to match desired mean and standard deviation
            return mean + stDev * randStdNormal;
        }

        public GRBVar GetVar(Node thisNode)
        {
            return _peopleServedVars[thisNode];
        }
    }
}
