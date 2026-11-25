using ClosedXML.Excel;

namespace HarmReduction
{
    class DataReader
    {
        private readonly string _fileLocation;
        
        public DataReader(string fileLocation)
        {
            _fileLocation = fileLocation;
            if (!File.Exists(fileLocation))
            {
                throw new Exception($"File {fileLocation} not found!");
            }
        }

        public List<Node> GetNodes()
        {
            List<Node> nodes = [];
            using (var workbook = new XLWorkbook(_fileLocation))
            {
                var worksheet = workbook.Worksheet("Optimization Data");
                
                foreach (var row in worksheet.RowsUsed())
                {
                    string county = row.Cell(1).GetValue<string>();
                    string zipCode = row.Cell(2).GetValue<string>();
                    string latitudeStr = row.Cell(3).GetValue<string>();
                    string longitudeStr = row.Cell(4).GetValue<string>();
                    string demandStr = row.Cell(5).GetValue<string>();
                    string buildCostStr = row.Cell(6).GetValue<string>();
                    string populationStr = row.Cell(7).GetValue<string>();

                    if (double.TryParse(demandStr, out double demand) && double.TryParse(longitudeStr, out double longitude) && 
                        double.TryParse(latitudeStr, out double latitude) && double.TryParse(buildCostStr, out double buildCost) &&
                        double.TryParse(populationStr, out double population))
                    {
                        if (demand > 0.0)
                        {
                            nodes.Add(new Node(county, zipCode, latitude, longitude, demand, buildCost, population));
                        }                        
                    }
                }
            }
            return nodes;
        }
    }
}
