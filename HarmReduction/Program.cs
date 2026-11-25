namespace HarmReduction
{
    using Gurobi;
    using HarmReduction.Optimization;

    internal class Program
    {
        static void Main(string[] args)
        {
            string dataFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Optimization\\Model and Data", "Input Data.xlsx");
            DataReader reader = new(dataFile);
            List<Node> nodes = reader.GetNodes();
            Console.WriteLine($"{nodes.Count} nodes read successfully");

            OptimizationModel opModel = new(nodes);
            opModel.SolveModel();
        }
    }
}
