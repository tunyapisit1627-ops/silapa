namespace Silapa.Models
{
    public class StatsViewModel
    {
        // สถิติรวม
        public int TotalCompetitions { get; set; }
        public int TotalSchools { get; set; }
        public int TotalStudents { get; set; }
        public int TotalTeachers { get; set; }

        // สถิติเหรียญรางวัล (นับจากผลการแข่งขัน)
        public int GoldMedals { get; set; }
        public int SilverMedals { get; set; }
        public int BronzeMedals { get; set; }
        public int Participation { get; set; } // เข้าร่วม

        // ข้อมูลสำหรับตารางจัดอันดับ
        public List<SchoolRankViewModel> SchoolRankings { get; set; }
    }

    public class SchoolRankViewModel
    {
        public string SchoolName { get; set; }
        public int Gold { get; set; }
        public int Silver { get; set; }
        public int Bronze { get; set; }
        public int TotalMedals { get; set; }
        public double TotalScore { get; set; }

        public int WinnerCount { get; set; }      // จำนวนชนะเลิศ (ที่ 1)
        public int RunnerUp1Count { get; set; }   // รองชนะเลิศอันดับ 1 (ที่ 2)
        public int RunnerUp2Count { get; set; }   // รองชนะเลิศอันดับ 2 (ที่ 3)
    }
}