using System.ComponentModel.DataAnnotations;

namespace Silapa.Models
{
    public class VisitorCounts
    {
        [Key]
        public int Id { get; set; }
        public DateTime VisitDate{ get; set; }
        public int Week { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public int VisitCount{ get; set; }
    }
}