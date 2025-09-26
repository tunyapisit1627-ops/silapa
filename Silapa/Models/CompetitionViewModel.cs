namespace Silapa.Models
{
    public class CompetitionViewModel
    {
        public Competition Competition { get; set; }
        public List<DCompetition> DCompetition { get; set; }
    }
    public class Competition
    {
        public int Id { get; set; }
        public int c_id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Teacher { get; set; }
        public int Student { get; set; }
        public int Director { get; set; }
        public string Details { get; set; }
        public string Status { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public class DCompetition
    {
        public string Name { get; set; }
        public int Scrol { get; set; }
        public string H_Id { get; set; }
        public int Id { get; set; }
        public string U_Id { get; set; }
        public DateTime LastUpdate { get; set; }
    }
}