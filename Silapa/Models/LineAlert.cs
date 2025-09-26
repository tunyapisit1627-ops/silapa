using System.Net;
using System.Text;

namespace Silapa.Models
{
    public class LineAlert
    {
        
        public async Task lineNotify(string msg, string token)
        {
            //09KhTjFWhoUeV7e1GAdui1kOV0fAIzYRJmY1Fh9ZmS6
            try
            {
                var request =  (HttpWebRequest)WebRequest.Create("https://notify-api.line.me/api/notify");
                var postData = string.Format("message={0}", msg);
                var data = Encoding.UTF8.GetBytes(postData);

                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = data.Length;
                request.Headers.Add("Authorization", "Bearer " + token);

                using (var stream = request.GetRequestStream()) stream.Write(data, 0, data.Length);
                var response = (HttpWebResponse)request.GetResponse();
                var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());       
            }
           
        }
    }
}