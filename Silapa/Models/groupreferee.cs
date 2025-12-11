using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class groupreferee
    {
        [Key]
        public int id { get; set; }
        public int c_id { get; set; }
        public int SettingID { get; set; }
        public string name { get; set; }
        [Column(TypeName = "TEXT")]
        public string duty { get; set; }
        public int total { get; set; }
        public string type { get; set; }
        [ForeignKey("c_id")]
        public virtual category? Category { get; set; }
        public ICollection<referee>? referees { get; set; }

    }
}