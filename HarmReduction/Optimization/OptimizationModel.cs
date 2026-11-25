using Gurobi;

namespace HarmReduction.Optimization
{
    public class OptimizationModel
    {
        private readonly List<Node> _nodes;
        private readonly GRBModel _gModel;
        private readonly List<ConstraintBase> _modelConstrs;
                
        public OptimizationModel(List<Node> nodes)
        {
            _nodes = nodes;
            _gModel = new(new GRBEnv());
            _modelConstrs = [];
        }

        public void SolveModel()
        {
            int numSamples = 500;
            bool useRandomDraws = numSamples > 1;
            double objWeightNaloxone = 0.75;
            double objWeightSample = 1.0 / numSamples;
            string year1 = "Year1";
            string year4 = "Year4";
            BuildVars buildVarsYear1 = new(_nodes, year1);
            BuildVars buildVarsYear4 = new(_nodes, year4, buildVarsYear1);
            AssignmentVars assignmentVarsYear1 = new(_nodes, buildVarsYear1, year1, 10.0);
            AssignmentVars assignmentVarsYear4 = new(_nodes, buildVarsYear4, year4, 5.0);
            _modelConstrs.Add(buildVarsYear1);
            _modelConstrs.Add(buildVarsYear4);
            _modelConstrs.Add(assignmentVarsYear1);
            _modelConstrs.Add(assignmentVarsYear4);
            List<PeopleServed> naloxoneConstrsYear1 = [];
            List<PeopleServed> naloxoneConstrsYear4 = [];
            List<PeopleServed> referralConstrsYear1 = [];
            List<PeopleServed> referralConstrsYear4 = [];

            for (int sample = 1; sample <= numSamples; sample++)
            {
                for (int i = 1; i <= 2; i++)
                {
                    double budget = i == 1 ? 2000000 : 3400000;
                    string description = i == 1 ? year1 : year4;
                    if (useRandomDraws)
                    {
                        description = $"{description}_Sample{sample}";
                    }
                    double objWeightYear = i == 1 ? 1.0 / 11.0 : 10.0 / 11.0;
                    BuildVars bVars = i == 1 ? buildVarsYear1 : buildVarsYear4;
                    AssignmentVars aVars = i == 1 ? assignmentVarsYear1 : assignmentVarsYear4;
                    List<PeopleServed> naloxoneConstrs = i == 1 ? naloxoneConstrsYear1 : naloxoneConstrsYear4;
                    List<PeopleServed> referralConstrs = i == 1 ? referralConstrsYear1 : referralConstrsYear4; 

                    PeopleServed naloxoneServed = new(_nodes, bVars, aVars, $"Naloxone_{description}", objWeightYear * objWeightNaloxone * objWeightSample, true, useRandomDraws, sample, 0.2 * i, i == 2);
                    _modelConstrs.Add(naloxoneServed);
                    naloxoneConstrs.Add(naloxoneServed);

                    PeopleServed referralConstr = new(_nodes, bVars, aVars, $"Referrals_{description}", objWeightYear * (1.0 - objWeightNaloxone) * objWeightSample, false, useRandomDraws, -sample, 0.2 * i, false);
                    _modelConstrs.Add(referralConstr);
                    referralConstrs.Add(referralConstr);

                    _modelConstrs.Add(new BudgetConstrs(_nodes, buildVarsYear1, naloxoneServed, budget, description));
                }
            }
            foreach (ConstraintBase constr in _modelConstrs)
            {
                constr.Setup(_gModel);
            }

            foreach (ConstraintBase constr in _modelConstrs)
            {
                constr.PutVals(_gModel);
            }

            _gModel.ModelSense = GRB.MAXIMIZE;
            _gModel.Set(GRB.DoubleParam.MIPGap, 0.01);
            _gModel.Update();
            _gModel.Write("C:\\Users\\morga\\Dropbox\\Morgan Documents\\Penn State\\Courses\\IE 570\\Group Project\\Model.lp");
            _gModel.Optimize();
            if (_gModel.Status != GRB.Status.OPTIMAL)
            {
                _gModel.ComputeIIS();
                _gModel.Write("C:\\Users\\morga\\Dropbox\\Morgan Documents\\Penn State\\Courses\\IE 570\\Group Project\\Infeasible.ilp");
            }
            else
            {
                _gModel.Write("C:\\Users\\morga\\Dropbox\\Morgan Documents\\Penn State\\Courses\\IE 570\\Group Project\\Solution.sol");

                // Report the solution
                for (int i = 1; i <= 2; i++)
                {
                    BuildVars bVars = i == 1 ? buildVarsYear1 : buildVarsYear4;
                    AssignmentVars aVars = i == 1 ? assignmentVarsYear1 : assignmentVarsYear4;
                    List<PeopleServed> naloxoneConstrs = i == 1 ? naloxoneConstrsYear1 : naloxoneConstrsYear4;
                    List<PeopleServed> referralConstrs = i == 1 ? referralConstrsYear1 : referralConstrsYear4;

                    int count = 0;
                    double totalNaloxone = 0.0;
                    double totalReferrals = 0.0;
                    foreach (Node thisNode in _nodes)
                    {
                        if (bVars.GetBuildVar(thisNode).X > 0.5)
                        {
                            count++;
                            double naloxoneAmt = 0.0;
                            foreach (PeopleServed nConstr in naloxoneConstrs)
                            {
                                naloxoneAmt += nConstr.GetVar(thisNode).X;
                            }
                            naloxoneAmt /= numSamples;

                            double referralAmt = 0.0;
                            foreach (PeopleServed rConstr in referralConstrs)
                            {
                                referralAmt += rConstr.GetVar(thisNode).X;
                            }
                            referralAmt /= numSamples;

                            Console.WriteLine($"{thisNode.ZipCode}_{thisNode.County}_{thisNode.Population} (Population)_{Math.Round(naloxoneAmt, 0)} (Naloxone)_{Math.Round(referralAmt, 0)} (Referrals)");
                            totalNaloxone += naloxoneAmt;
                            totalReferrals += referralAmt;
                        }
                    }

                    Console.WriteLine($"Year {(i == 1 ? "1" : "4")} Summary: {count} built, {Math.Round(totalNaloxone, 0)} Naloxone doses served, {Math.Round(totalReferrals, 0)} referrals given");
                                                        
                    Console.WriteLine();
                }            
            }            
        }       
    }
}
