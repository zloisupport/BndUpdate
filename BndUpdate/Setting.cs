using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Formatting = Newtonsoft.Json.Formatting;

namespace BndUpdate
{
   public class Setting
    {
        private string Language { get; set; }
        private string LastUpDate { get; set; }


        public Setting(string x , string y)
        {
            this.Language = x;
            this.LastUpDate = y;
        }
        private string ReadSetting()
        {
            var languages = new StreamReader("setting.json").ReadToEnd();
            JObject json = JObject.Parse(languages);
            string data = (string)json["languages"];
            return data;
        }

        public void CreateSetting()
        {

            //using (FileStream fs = new FileStream("setting.json", FileMode.OpenOrCreate))
            //{
            //    Setting st = new Setting("en_GB", DateTime.Now.ToString());
            //    string json = JsonConvert.SerializeObject(st, Formatting.Indented);
            //    byte[] bytes = Encoding.UTF8.GetBytes(json);
            //    fs.Write(bytes, 0, bytes.Length);
            //}
            string lang = Language;
            string date = LastUpDate;
                Setting st = new Setting
                ( "s",
                  "s"
                );
                File.WriteAllText(@"setting.json", JsonConvert.SerializeObject(st,Formatting.Indented));

        }

        public string CurrentLanugage()
        {
            string currentLanguage = null;
            if (File.Exists("Settings.json"))
            {
                ReadSetting();
            }
            else
            {
                CreateSetting();
            }
            return currentLanguage;
        }
    }
}
