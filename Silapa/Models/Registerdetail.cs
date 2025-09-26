using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Silapa.Models
{
    public class Registerdetail
    {
        public int no { get; set; }
        public int h_id { get; set; }
        public string Prefix { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Type { get; set; }
        public string ?ImageUrl{ get; set; }
        public int c_id { get; set; }
        public string u_id { get; set; }
        public DateTime lastupdate { get; set; }

        [ForeignKey("h_id")]
        public virtual Registerhead Registerhead { get; set; }
    }
}