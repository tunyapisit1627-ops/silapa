using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class dCompetitionlist
    {    
        public int id { get; set; }
        public int h_id{ get; set; }
        public string? name{ get; set;}
        public int? scrol{ get; set; }
        public string? u_id{ get; set; }
        public DateTime? lastupdate{ get; set; }
        [ForeignKey("h_id")]
         public virtual Competitionlist? Competitionlist { get; set; }
    }
}