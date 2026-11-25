namespace HarmReduction
{
    public class Node
    {
        public readonly double Latitude;
        public readonly double Longitude;
        public readonly double Demand;
        public readonly string County;
        public readonly string ZipCode;
        public readonly double AnnualizedCost;
        public readonly double Population;
        public readonly double Referrals;
        public Node(string county, string zipCode, double latitude, double longitude, double demand, double buildCost, double population)
        {
            County = county;
            ZipCode = zipCode;
            Latitude = latitude * Math.PI / 180;
            Longitude = longitude * Math.PI / 180;
            Demand = demand;
            AnnualizedCost = buildCost;
            Population = population;
            
            double referralRate = county switch
            {
                "Bucks" => .04383,
                "Chester" => 0.03918,
                "Delaware" => .05108,
                "Montgomery" => .04223,
                _ => 0.0795, // Philadelphia
            };
            Referrals = population * referralRate;
        }   

        /// <summary>
        /// This returns the distance in miles between the two nodes
        /// </summary>
        /// <param name="node">Node to calculate the distance with</param>
        /// <returns>Distance in miles using the Haversine Formula</returns>
        public double CalculateDistance(Node node)
        {
            double earthMeanRadius = 3958.7613160; //Miles
            double a = Math.Pow(Math.Sin((node.Latitude - this.Latitude) / 2), 2) +
                Math.Cos(this.Latitude) * Math.Cos(node.Latitude) * Math.Pow(Math.Sin((node.Longitude - this.Longitude) / 2), 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return c * earthMeanRadius;
        }

    }
}
