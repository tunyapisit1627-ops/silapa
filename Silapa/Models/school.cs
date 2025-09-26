using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class school
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        public string? titlename { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? tel  { get; set; }
        public int g_id { get;set;}
        public int? a_id { get; set; }
        
        public string? status { get; set;}
        public DateTime? lastupdate{ get; set; }

        [ForeignKey("g_id")]
		public virtual grouplist? grouplist { get; set; }
        [ForeignKey("a_id")]
        public virtual Affiliation? Affiliation{ get; set; }
         public ICollection<Registerhead> registerheads { get; set; }
    }
}