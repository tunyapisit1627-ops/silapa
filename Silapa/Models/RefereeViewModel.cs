namespace Silapa.Models
{
    public class RefereeViewModel
    {
        public int id { get; set; } // Nullable to support both Create and Edit
        public int c_id { get; set; } // Category ID (foreign key)
        public string r_name { get; set;}
        public string duty { get; set;}
         public int g_id { get; set; } // Category ID (foreign key)
        public string name { get; set; } // Group name
        public string position { get; set; } // Role or responsibility
        public string role { get; set; }
        public string ImageUrl { get; set; } // Base64 string for the image
        public List<int> Categories { get; set; } // เก็บค่าของ Checkbox ที่เลือก
    }
}