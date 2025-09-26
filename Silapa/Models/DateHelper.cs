using System;
using System.Globalization;
namespace Silapa.Models
{
    public class DateHelper
    {
        public static string ConvertToBuddhistDateRange(string daterace, CultureInfo thaiCulture)
        {
            if (string.IsNullOrEmpty(daterace))
            {
                return "ไม่ระบุวันที่";
            }
            string[] dateParts = daterace.Split('-');
            if (dateParts.Length != 2)
            {
                return "รูปแบบวันที่ไม่ถูกต้อง";
            }



            string[] ddsub = daterace.Split('-');
            string[] startdate = ddsub[0].Trim().Split('/');
            string[] enddate = ddsub[1].Trim().Split('/');

            DateTime startDateNow = new DateTime(Convert.ToInt32(startdate[2]), Convert.ToInt32(startdate[0]), Convert.ToInt32(startdate[1]));
            DateTime endDateNow = new DateTime(Convert.ToInt32(enddate[2]), Convert.ToInt32(enddate[0]), Convert.ToInt32(enddate[1]));

            // แปลงเป็น พ.ศ.
            int buddhistYearS = startDateNow.Year + 543;
            int buddhistYearN = endDateNow.Year + 543;
            string dsnow = $"{startDateNow.ToString("dd MMMM", thaiCulture)} {buddhistYearS}";
            string denow = $"{endDateNow.ToString("dd MMMM", thaiCulture)} {buddhistYearN}";

            return $"{dsnow} - {denow}";
        }
        public static bool IsCurrentDateInRange(DateTime startDate, DateTime endDate)
        {
            // วันที่ปัจจุบัน
            DateTime currentDate = DateTime.Now;

            // แปลงวันที่จากปี พ.ศ. เป็น ค.ศ. (ถ้าจำเป็น)
            if (startDate.Year > 2500)
            {
                startDate = startDate.AddYears(-543);
            }
            if (endDate.Year > 2500)
            {
                endDate = endDate.AddYears(-543);
            }

            // ตรวจสอบว่าปัจจุบันอยู่ในช่วงหรือไม่
            return currentDate >= startDate && currentDate <= endDate;
        }
    }
}