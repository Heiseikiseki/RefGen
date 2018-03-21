using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;

namespace RefGen {
    class Program {

        static string author, title, conf, vol, num, pages, month, year, jour, name, txt;
        static int andtimes;
        static List<(string refresault, string name)> RefList = new List<(string, string)>();
        static void Main(string[] args) {
            Console.WriteLine("Press any key to start");
            Console.ReadLine();
            foreach (var reftxt in Directory.EnumerateFiles("ref", "*.txt", SearchOption.AllDirectories)) {
                Console.WriteLine($"processing {reftxt}");
                txt = File.ReadAllText(reftxt, Encoding.UTF8);
                author = Getvalue(@"author={(.*?)}");
                string pattern = Regex.Escape(" and ");
                Regex rgx = new Regex(pattern);
                andtimes = Regex.Matches(author, pattern).Count;
                name = (andtimes == 0) ?
                    Regex.Match(author, @"\. ([^.]{2,})").Groups[1].Value.Trim() :
                    Regex.Match(author, @"\. ([^.]*)(?= and)").Groups[1].Value.Trim();

                author = rgx.Replace(author, ", ", andtimes - 1);
                title = ToTitleCase(Getvalue(@"\ntitle={(.*?)}"));
                conf = Getvalue(@"\nbooktitle={(.*?)}");
                jour = Getvalue(@"\njournal={(.*?)}");
                vol = Getvalue(@"\nvolume={(.*?)}");
                num = Getvalue(@"\nnumber={(.*?)}");
                pages = Getvalue(@"\npages={(.*?)}");
                month = Getvalue(@"\nmonth={(.*?)}").Substring(0, 3);
                year = Getvalue(@"\nyear={(.*?)}");

                Combine();
            };
            RefList = RefList.OrderBy(x => x.name).ToList();

            StringBuilder sb = new StringBuilder();

            foreach (var item in RefList) {
                sb.AppendLine($"<p>{item.refresault}</p>");
            }
            StringWriter sw = new StringWriter();

            using (HtmlTextWriter writer = new HtmlTextWriter(sw)) {
                writer.RenderBeginTag("!DOCTYPE html");
                writer.RenderBeginTag(HtmlTextWriterTag.Html);
                writer.RenderBeginTag(HtmlTextWriterTag.Head);
                writer.RenderBeginTag("meta charset=\"UTF-8\"");
                writer.RenderEndTag();
                writer.RenderBeginTag(HtmlTextWriterTag.Body);
                writer.Write($"{sb.ToString()}");
                writer.RenderEndTag();
                writer.RenderEndTag();

                File.WriteAllText("Kiseki.html", sw.ToString(), Encoding.UTF8);
                Process.Start("Kiseki.html");
                Console.ReadLine();
            }
        }

        static void Combine() {
            bool IsJournal = (jour != "none");
            string reference = $"{author}, “{title},” ";
            reference += (jour == "none") ? $"in Proceedings of <i>the {conf}</i>, " : $"<i>{jour}</i>, ";
            if (vol != "none") reference += $"vol. {vol}, ";
            if (num != "none") reference += $"no. {num}, ";
            reference += $"pp. {pages}, {month}. {year}.";
            RefList.Add((reference, name));
        }

        static string Getvalue(string pattern) {
            string temp = Regex.Match(txt, pattern).Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(temp)) return "none";
            else return temp;
        }

        public static string ToTitleCase(string title) {
            string WorkingTitle = title;

            if (!string.IsNullOrEmpty(WorkingTitle)) {
                char[] space = new char[] { ' ' };

                List<string> artsAndPreps = new List<string>() { "and", "at", "from", "into", "of", "on", "or", "the", "to", "for", "with", };
                TextInfo textInfo = new CultureInfo("en-US", false).TextInfo;

                //Convert to title case.
                WorkingTitle = textInfo.ToTitleCase(title);

                List<string> tokens = WorkingTitle.Split(space, StringSplitOptions.RemoveEmptyEntries).ToList();

                WorkingTitle = tokens[0];

                tokens.RemoveAt(0);

                WorkingTitle += tokens.Aggregate(String.Empty, (String prev, String input)
                                       => prev +
                                           (artsAndPreps.Contains(input.ToLower()) // If True
                                               ? $" {input.ToLower()}"               // Return the prep/art lowercase
                                               : $" {input}"));                   // Otherwise return the valid word.


            }

            return WorkingTitle;

        }
    }
}
