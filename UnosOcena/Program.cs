using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using HtmlAgilityPack;

namespace UnosOcena
{
    // ID = student.ID, br_indeksa = student.br_indeksa, br_poena = grade.br_poena, ocena = grade.ocena
    public class StudentGrade
    {
        public string ID { get; set; }
        public string br_indeksa { get; set; }
        public string br_poena { get; set; }
        public string ocena { get; set; }
    }

    public class Student
    {
        public string ID { get; set; }
        public string br_indeksa { get; set; }
    }

    public class Grade
    {
        public string br_indeksa { get; set; }
        public string br_poena { get; set; }
        public string ocena { get; set; }

        public static Grade FromCSV(string csvLine)
        {
            string[] values = csvLine.Split(';');
            Grade grade = new Grade();
            grade.br_indeksa = values[0];
            grade.br_poena = values[1];
            grade.ocena = values[2];
            return grade;
        }
    }


    public static class Program
    {
        public static CookieContainer Cookies = new CookieContainer();
        public static string auth_cookie = "1gcr8b4i1ps1ek1pb2q0mjakmk";
        public static string ID_Zapisnika = "69962";
        public static string ID_Zaposlenog = "827";
        public static string bg_poena = "90";
        public static string ocena = "10";
        public static string ID_Studenta = "5955";

        public static bool TryAddCookie(this WebRequest webRequest, Cookie cookie)
        {
            HttpWebRequest httpRequest = webRequest as HttpWebRequest;
            if (httpRequest == null)
            {
                return false;
            }

            if (httpRequest.CookieContainer == null)
            {
                httpRequest.CookieContainer = new CookieContainer();
            }

            httpRequest.CookieContainer.Add(cookie);
            return true;
        }


        static List<Student> LoadStudents()
        {
            List<Student> studenti = new List<Student>();

            var htmlWeb = new HtmlWeb();
            var query = $"http://nip.elfak.ni.ac.rs/default/predmet/unos-zapisnika/id/" + ID_Zapisnika;
            htmlWeb.UseCookies = true;
            htmlWeb.PreRequest += request =>
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(new Cookie("PHPSESSID", auth_cookie) { Domain = "nip.elfak.ni.ac.rs" });
                return true;
            };
            var doc = htmlWeb.Load(query);
            var response = doc.DocumentNode.SelectSingleNode("//table/tbody");
            var results = response.SelectNodes("tr");
            foreach (var r in results)
            {
                Student st = new Student();
                st.ID = r.Attributes["id"].Value.Split('_')[1];
                st.br_indeksa = r.ChildNodes[3].InnerText;

                studenti.Add(st);
            }
            return studenti;
        }

        public static List<Grade> LoadGrades()
        {
            List<Grade> grades = File.ReadAllLines("Poeni.csv")
                                 .Select(v => Grade.FromCSV(v))
                                 .ToList();
            return grades;
        }

        static void Main(string[] args)
        {
            var students = LoadStudents();
            var grades = LoadGrades();
            var query = from student in students
                        join grade in grades on student.br_indeksa equals grade.br_indeksa
                        select new StudentGrade() { ID = student.ID, br_indeksa = student.br_indeksa, br_poena = grade.br_poena, ocena = grade.ocena };


            foreach (var s in query)
            {
                try
                {
                    WebRequest request = WebRequest.Create("http://nip.elfak.ni.ac.rs/default/predmet/sacuvaj-ocenu");
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    if (ValidatePointsAndGrade(s) != true)
                        throw new Exception("Points and grade mismatch!");
                    String PostBody = String.Format("poeni5=50&poeni6=60&poeni7=70&poeni8=80&poeni9=90&idraspored={0}&idzapos={1}&br_poena%5B%5D={2}&ocena%5B%5D={3}_{4}",
                        ID_Zapisnika,
                        ID_Zaposlenog,
                        s.br_poena,
                        s.ID,
                        s.ocena
                        );
                    byte[] byteArray = System.Text.Encoding.UTF8.GetBytes(PostBody);
                    request.ContentLength = byteArray.Length;
                    request.TryAddCookie(new Cookie("PHPSESSID", auth_cookie) { Domain = "nip.elfak.ni.ac.rs" });
                    Stream dataStream = request.GetRequestStream();
                    dataStream.Write(byteArray, 0, byteArray.Length);
                    dataStream.Close();

                    WebResponse response = request.GetResponse();
                    if (((HttpWebResponse)response).StatusCode == HttpStatusCode.OK)
                    {
                        StreamReader streamReader = new StreamReader(response.GetResponseStream(), true);
                        Console.WriteLine(streamReader.ReadToEnd());
                        Console.WriteLine("Uneo ocenu za studenta " + s.br_indeksa);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error for " + s.br_indeksa + " error message: " + ex.Message);
                }
            }

        }

        private static bool ValidatePointsAndGrade(StudentGrade s)
        {
            if (int.Parse(s.br_poena) >= 91)
            {
                if (s.ocena != "10")
                    return false;
            }
            else if (int.Parse(s.br_poena) >= 81)
            {
                if (s.ocena != "9")
                    return false;
            }
            else if (int.Parse(s.br_poena) >= 71)
            {
                if (s.ocena != "8")
                    return false;
            }
            else if (int.Parse(s.br_poena) >= 61)
            {
                if (s.ocena != "7")
                    return false;
            }
            else if (int.Parse(s.br_poena) >= 50)
            {
                if (s.ocena != "6")
                    return false;
            }
            return true;
        }
    }
}
