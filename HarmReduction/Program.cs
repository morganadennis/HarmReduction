namespace HarmReduction
{
    using Gurobi;
    using HarmReduction.Optimization;

    internal class Program
    {
        static void Main(string[] args)
        {
            string dataFile = "C:\\Users\\morga\\Dropbox\\Morgan Documents\\Penn State\\Courses\\IE 570\\Group Project\\Zip Code Data Summary.xlsx";
            DataReader reader = new(dataFile);
            List<Node> nodes = reader.GetNodes();
            Console.WriteLine($"{nodes.Count} nodes read successfully");

            OptimizationModel opModel = new(nodes);
            opModel.SolveModel();
        }
    }
}
