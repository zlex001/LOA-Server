using System;
using System.Collections.Generic;
using Basic;

namespace Logic.Design
{
    
    public class Multilingual :Ability
    {
        public string label;
        public string chineseSimplified, chineseTraditional, english, japanese, korean;
        public string french, german, spanish, portuguese, russian;
        public string turkish, thai, indonesian, vietnamese, italian;
        public string polish, dutch, swedish, norwegian, danish, finnish, ukrainian;
        
        public override void Init(params object[] args)
        {
            var dict = args[0] as Dictionary<string, object>;
            id = Get<int>(dict, "id");
            cid = Get<string>(dict, "cid");
            label = Get<string>(dict, "label");
            chineseSimplified = Get<string>(dict, "ChineseSimplified");
            chineseTraditional = Get<string>(dict, "ChineseTraditional");
            english = Get<string>(dict, "English");
            japanese = Get<string>(dict, "Japanese");
            korean = Get<string>(dict, "Korean");
            french = Get<string>(dict, "French");
            german = Get<string>(dict, "German");
            spanish = Get<string>(dict, "Spanish");
            portuguese = Get<string>(dict, "Portuguese");
            russian = Get<string>(dict, "Russian");
            turkish = Get<string>(dict, "Turkish");
            thai = Get<string>(dict, "Thai");
            indonesian = Get<string>(dict, "Indonesian");
            vietnamese = Get<string>(dict, "Vietnamese");
            italian = Get<string>(dict, "Italian");
            polish = Get<string>(dict, "Polish");
            dutch = Get<string>(dict, "Dutch");
            swedish = Get<string>(dict, "Swedish");
            norwegian = Get<string>(dict, "Norwegian");
            danish = Get<string>(dict, "Danish");
            finnish = Get<string>(dict, "Finnish");
            ukrainian = Get<string>(dict, "Ukrainian");
        }

        public static void Convert()
        {
            List<Dictionary<string, object>> datas = new List<Dictionary<string, object>>();

            foreach (Multilingual multilingual in Agent.Instance.Content.Gets<Multilingual>())
            {
                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    {"id", multilingual.id},
                    {"cid", multilingual.cid},
                    {"label", multilingual.label},
                    {"ChineseSimplified", multilingual.chineseSimplified},
                    {"ChineseTraditional", multilingual.chineseTraditional},
                    {"English", multilingual.english},
                    {"Japanese", multilingual.japanese},
                    {"Korean", multilingual.korean},
                    {"French", multilingual.french},
                    {"German", multilingual.german},
                    {"Spanish", multilingual.spanish},
                    {"Portuguese", multilingual.portuguese},
                    {"Russian", multilingual.russian},
                    {"Turkish", multilingual.turkish},
                    {"Thai", multilingual.thai},
                    {"Indonesian", multilingual.indonesian},
                    {"Vietnamese", multilingual.vietnamese},
                    {"Italian", multilingual.italian},
                    {"Polish", multilingual.polish},
                    {"Dutch", multilingual.dutch},
                    {"Swedish", multilingual.swedish},
                    {"Norwegian", multilingual.norwegian},
                    {"Danish", multilingual.danish},
                    {"Finnish", multilingual.finnish},
                    {"Ukrainian", multilingual.ukrainian},
                };
                datas.Add(data);
            }

            string path = $"{Utils.Paths.Library}/Config/Multilingual.csv";
            Utils.FileManager.Instance.DeleteFile(path);
            Utils.Csv.SaveByRows(datas, path);
        }
    }
  

}


