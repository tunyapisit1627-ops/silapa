using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;

namespace Silapa.Models
{
    public class TimelineItem

    {
        // กำหนดวันที่เริ่มต้นและสิ้นสุดเป็นปี 2025 ตามที่คุณระบุ
        // Note: ควรเปลี่ยนเป็นปีปัจจุบันหากไม่ใช่ 2568 (2025)

        [Key] // 1. ระบุว่าเป็น Primary Key
        public int EventID { get; set; }

        public string? Title { get; set; }
        public string? Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int IconNumber { get; set; }

        public int DisplayOrder { get; set; } // 2. เพิ่ม DisplayOrder สำหรับเรียงลำดับ
        public int SetupSystemID { get; set; }

        // 2. เพิ่ม Navigation Property (แนะนำอย่างยิ่งเมื่อใช้ EF Core)
        // เพื่อให้สามารถเข้าถึงข้อมูลของ setupsystem ทั้งหมดได้ง่ายๆ
        [ForeignKey("SetupSystemID")]
        public virtual setupsystem? SetupSystem { get; set; }

        // คุณสมบัติสำหรับแสดงผล (ไม่เก็บลงฐานข้อมูล)
        [NotMapped] // 3. บอก EF Core ว่าไม่ต้องยุ่งกับ Property นี้
        public string? DateRange { get; set; }

        [NotMapped] // 3. บอก EF Core ว่าไม่ต้องยุ่งกับ Property นี้
        public string? StatusClass { get; set; }

        [NotMapped] // 3. บอก EF Core ว่าไม่ต้องยุ่งกับ Property นี้
        public string? StatusText { get; set; }
        public void PrepareForDisplay(DateTime currentDate)
        {
            var thaiCulture = new CultureInfo("th-TH");
            // สร้าง DateRange สำหรับแสดงผล
            // ตัวอย่าง: "11 - 12 ธันวาคม 2568" หรือ "13 ธันวาคม 2568"
            // 2. จัดรูปแบบ DateRange โดยใช้ Thai Culture และแปลงเป็นปี พ.ศ. (+543)
            if (StartDate.Month == EndDate.Month)
            {
                if (StartDate.Day == EndDate.Day)
                {
                    // กรณีวันเดียวกัน: "13 ตุลาคม 2568"
                    DateRange = StartDate.ToString("d MMMM", thaiCulture) + " " + (StartDate.Year + 543);
                }
                else
                {
                    // กรณีหลายวันในเดือนเดียวกัน: "11 - 12 ธันวาคม 2568"
                    DateRange = $"{StartDate.Day} - {EndDate.Day} {StartDate.ToString("MMMM", thaiCulture)} {StartDate.Year + 543}";
                }
            }
            else
            {
                // กรณีคร่อมเดือน: "30 พฤศจิกายน - 5 ธันวาคม 2568"
                DateRange = $"{StartDate.ToString("d MMMM", thaiCulture)} - {EndDate.ToString("d MMMM", thaiCulture)} {StartDate.Year + 543}";
            }

            // คำนวณ Status
            CalculateStatus(currentDate);
        }

        public void CalculateStatus(DateTime currentDate)
        {
            // กำหนดข้อความเริ่มต้น
            StatusText = "⏳ รอประกาศ";

            if (currentDate.Date > EndDate.Date)
            {
                // 1. สิ้นสุดแล้ว
                StatusClass = "status-closed";
                StatusText = "✖ สิ้นสุด/ปิดรับแล้ว";
            }
            else if (currentDate.Date >= StartDate.Date && currentDate.Date <= EndDate.Date)
            {
                // 2. กำลังดำเนินการ
                StatusClass = "status-open";

                // ปรับข้อความตามรายการ
                if (IconNumber == 1) StatusText = "✓ เปิดรับสมัคร";
                else if (IconNumber == 4) StatusText = "📅 วันงาน";
                else StatusText = "⏳ กำลังดำเนินการ";
            }
            else // currentDate.Date < StartDate.Date
            {
                // 3. กำลังจะมาถึง
                StatusClass = "status-upcoming";
                // ใช้ข้อความเริ่มต้นที่ตั้งไว้
            }
        }
    }
}