using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class Competitionlist
    {
        [Key]
        public int Id { get; set; }
        public int? c_id { get; set; }
        public string? Name { get; set; }
        public string? type { get; set; }
        public int teacher { get; set; }
        public int student{ get; set; }
        public int director{ get; set; }
        public string? details   { get; set; }
        public string? status { get; set; }
        public DateTime? lastupdate  { get; set; }
        public ICollection<Registerhead> registerheads { get; set; }
        public ICollection<racedetails>? racedetails { get; set; }
        public ICollection<dCompetitionlist> dCompetitionlists { get; set; }
        public ICollection<registerdirector> registerdirectors { get; set; }
        public ICollection<referee> referees { get; set;}
        [ForeignKey("c_id")]
		public virtual category? Category { get; set; }
    }
}