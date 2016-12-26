using System;
using System.IO;
using System.Net;
using System.Xml;
using System.Text;

namespace W2W
{
    public static class Globals
    {
        public static string version = "What2Wear v0.2";
        public static string location = "NOT SET";
        public static string currentLocation = "NOT SET";
       // public static string file
    }
    class Program
    {
        // IP API key: 75858599f7d77949e9aea959fc579c36609c53bfd45ac4f1da5e64efae1bf876

        // IDEA HEADER //
        //
        // Try creating unit tests
        // DressYourKid version. 
        // What2Wear Cycling
        // Primært til/fra jobb. 25 feb var det -1 grad som var for kaldt for de tykkeste handskene. Så å si vindstille.
        //     
        // OPTIMIZE: Write to a buffer and commit it to a file instaed of writing multiple times.

        // Can you use this?
        DateTime Time = DateTime.Now;
        

        static void Main(string[] args)
        {
            //InitialUpdate();
            
            DateTime _now = DateTime.Now;
            WriteToLogFile("* * * * * * * * * * * * * * * * * * * * * * * * * * * * * *");
            WriteToLogFile("[" + _now + "] Commandline: "+Globals.version);
            WriteToLogFile("[" + _now + "] Commandline: Application initiated.");
            
            int updateInterval = 30 * 60 * 1000;
            System.Timers.Timer _updater = new System.Timers.Timer(updateInterval);
            _updater.Elapsed += Updater_Elapsed;
            _updater.Start();
            
            WriteToLogFile("[" + _now + "] Commandline: Updating interval set to " + updateInterval/60000+" minutes");
            WriteToLogFile("[" + _now + "] Commandline: Press any key to terminate application.");
            
            InitialUpdate();
            Console.Read();
            WriteToLogFile("[" + _now +"] Commandline: Application terminated by user.");
            /* CHANGING LOCATION IN SETTINGS. 
            
    ******************* NOT IMPLEMENTED. **************************

            WriteToFile("Type: \'cl\' to change location or any key to quit.");
           
            string ans = Console.ReadLine();
            if (ans == "cl"){
                Console.Write("New location: ");
                string loc = Console.ReadLine();
                // prop = loc
                WriteToFile("New location set.");
                //LocationSettings.Default.Location = "Haugesund";
                LocationSettings.Default.Save();
            }
            */
        }
        private static void GetLocation(){
            WebRequest wr = WebRequest.Create("http://api.ipinfodb.com/v3/ip-city/?key=75858599f7d77949e9aea959fc579c36609c53bfd45ac4f1da5e64efae1bf876");
            WebResponse resp = wr.GetResponse();
            string pageSource;

			using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
			{
			    pageSource = sr.ReadToEnd();
			}

			string[] splitted = pageSource.Split(';');
			if (splitted[7].Length == 4)
            {
                Globals.location = splitted[7];
            }
        }
        private static void InitialUpdate()
        {
        	GetLocation();
            Update();
        }
        private static void Updater_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // try to reduce the amount of work by checking if the forecast is updated before doing the Update();
            var xml = GetXMLdocument();
            if (xml == null)
                return;
            var time = DateTime.Parse(xml.SelectSingleNode("/weatherdata/forecast/tabular/time").Attributes["from"].Value);
            //check if the forecast is updated here.
            Update();
            
        }
        private static void Update()
        {
            // does this need to be in the update or just in the startup of the program. If the program is started
            // from an app f ex. If it is continously running and you move the computer to a different network (try this,
            // when going to Haugis). 
            //
            // Perform the check each time.
            //
            //declarations
            string curFile = "C:\\inetpub\\wwwroot\\data.htm";
            //string curFile = "C:\\Users\\andreas.hove\\Desktop\\data.html";

            string[] intensities = { "~ LONG RUN - LOW INTENSITY ~", "~ TEMPO RUN - MEDIUM INTENSITY ~", "~ INTERVAL RUN - HIGH INTENSITY ~" };
            var _currentTime = DateTime.Now;
            var _timestamp = _currentTime.ToShortTimeString();

            // the alpha parameter is set by the temperature
            // 
            double _alpha = 0;
            double _incr = 5;
            double _temp = 0;
            double _rain = 0;
            double _wind = 0;
            double _feelLikeTemp = 0;
            string _desc = "";
            string _name = "NOT SET";
            DateTime _sunrise = _currentTime;
            DateTime _sunset = _currentTime;
            DateTime _time = _currentTime;
            TimeSpan _morning = new TimeSpan(7, 0, 0);
            TimeSpan _evening = new TimeSpan(23, 0, 0);
            TimeSpan _now = DateTime.Now.TimeOfDay;


            // only update between 07-23 o'clock
            if ((_now > _evening) || (_now < _morning))
                return;
            
            var xmlDoc = GetXMLdocument();
            if (xmlDoc == null)
                return;

            try
            {
                _temp = Double.Parse(xmlDoc.SelectSingleNode("/weatherdata/forecast/tabular/time/temperature").Attributes["value"].Value, System.Globalization.CultureInfo.InvariantCulture);
                _wind = Double.Parse(xmlDoc.SelectSingleNode("/weatherdata/forecast/tabular/time/windSpeed").Attributes["mps"].Value, System.Globalization.CultureInfo.InvariantCulture);
                _rain = Double.Parse(xmlDoc.SelectSingleNode("/weatherdata/forecast/tabular/time/precipitation").Attributes["value"].Value, System.Globalization.CultureInfo.InvariantCulture);
                _desc = xmlDoc.SelectSingleNode("/weatherdata/forecast/tabular/time/symbol").Attributes["name"].Value.ToLower();
                _time = DateTime.Parse(xmlDoc.SelectSingleNode("/weatherdata/forecast/tabular/time").Attributes["from"].Value);
                _name = xmlDoc.SelectSingleNode("/weatherdata/location/name").InnerText;
                _sunrise = DateTime.Parse(xmlDoc.SelectSingleNode("/weatherdata/sun").Attributes["rise"].Value);
                _sunset = DateTime.Parse(xmlDoc.SelectSingleNode("/weatherdata/sun").Attributes["set"].Value);
            }
            catch
            {
                WriteToLogFile("["+_currentTime+ "] TimerThread: ERROR: There was an error parsing xml values. Aborting.");
                return;
            }

            // setting _alpha
            if (_temp < 5)
                _alpha = 1;
            else if (_temp >= 5 && _temp < 10)
                _alpha = 0.8;
            else if (_temp >= 10 && _temp < 15)
                _alpha = 0.6;
            else
                _alpha = 0.4;

            // Values retrieved and parsed OK. Start writing to file.
            if (File.Exists(curFile))
                File.Delete(curFile);

            _feelLikeTemp = _temp - (_wind / 2);
            if (_rain > 0)
                _feelLikeTemp -= _rain / 3;
            _feelLikeTemp = Math.Floor(_feelLikeTemp);
            
            double _unaffectedTemp = 0.0;
			_unaffectedTemp =  _feelLikeTemp;
			
            WriteToFile("<font size=\"50\">What2Wear v0.2");
            WriteToFile("Forecast from " + _time.ToShortTimeString()+" to "+_time.AddHours(1).ToShortTimeString() +" for "+_name);
            WriteToFile("Weather is " + _desc.ToLower());
            WriteToFile("Temp: " + _temp + " (feels like: " + _feelLikeTemp + ")<br>Rain: " + _rain + " mm/h<br>Wind: " + _wind + " m/s");

            for (int i = 0; i < intensities.Length; i++)
            {
                WriteToFile("");
                WriteToFile(intensities[i]);

                // sokker: temp
                if (_feelLikeTemp < -5)
                    WriteToFile("Ullsokker");

                // ullundertøy nededel: temp
                if (_feelLikeTemp < -5)
                    WriteToFile("Ullundertøy: underdel");

                // ullundertøy overdel: temp+intensity
                if (_feelLikeTemp < 5)
                    //string.Concat(res, "Ullundertøy: overdel");
                    WriteToFile("Ullundertøy: overdel");

                // bukse/shorts: temp+intensity
                if (_feelLikeTemp > 15)
                    WriteToFile("Shorts");
                else if (_feelLikeTemp <= 15 && _feelLikeTemp > -5)
                    WriteToFile("Tights");
                else
                    WriteToFile("Joggebukse");

                // overdel/tskjorte/langermet
                if (_feelLikeTemp < 15 && _feelLikeTemp >= 5)
                    WriteToFile("Langermet trøye");
                // between 5 and -5 we use ullundertøy
                else if (_feelLikeTemp < -3 && _feelLikeTemp > -6)
                    WriteToFile("Kortermet trøye");
                else if (_feelLikeTemp <= -6)
                    WriteToFile("Langermet trøye");

                // regnjakke
                if (_feelLikeTemp < 15 && _rain < 5)
                    WriteToFile("Blå jakke");
                else if (_rain >= 5)
                    WriteToFile("Gul jakke");

                // buff
                if (_feelLikeTemp <= 5 && _feelLikeTemp > 0)
                    WriteToFile("Buff (tynn)");
                else if (_feelLikeTemp <= 0)
                    WriteToFile("Buff (tykk)");
                // hansker
                if (_rain > 2 && _unaffectedTemp < 8)
                    WriteToFile("Hansker (tykk)");
                else if (_unaffectedTemp <= 10 && _unaffectedTemp > 0)
                    WriteToFile("Hansker (tynn)");
                else if (_unaffectedTemp <= 0)
                    WriteToFile("Hansker (tykk)");

                // solbriller
                if (_desc == "clear sky" || _desc == "partly cloudy")
                {
                    if (_currentTime > _sunrise && _currentTime < _sunset)
                        WriteToFile("Solbriller");
                }

                // lue/pannebånd
                if (_feelLikeTemp < 0)
                    WriteToFile("Lue");
                else
                    //string.Concat(res, "Pannebånd<br>");
                    WriteToFile("Pannebånd");

                // increasing temperature to compensate for increased intensity
                _feelLikeTemp += _incr * _alpha;
                //Console.WriteLine("Feelliketemp: " + _feelLikeTemp);

            }
            WriteToFile("</font>");          
              
        }

        private static XmlDocument GetXMLdocument()
        {
            var retries = 3;
            var _yrQuery = string.Format("http://www.yr.no/place/Norway/Postnummer/" + Globals.location+"/forecast_hour_by_hour.xml");
            
            //var _yrQuery = string.Format("http://www.yr.no/place/Norway/Rogaland/Stavanger/Stavanger/forecast_hour_by_hour.xml");
            //var _yrQuery = string.Format("http://www.yr.no/place/Norway/Rogaland/" + LocationSettings.Default.Location + "/" + LocationSettings.Default.Location + "/forecast.xml");

            XmlDocument _xmlDoc = new XmlDocument();
            bool proceed = true; 
            while (proceed)
            {

                try
                {
                    _xmlDoc.Load(_yrQuery);
                    return _xmlDoc;

                }
                catch (WebException)
                {
                    retries -= 1;
                    if (retries <= 0)
                    {
                        WriteToLogFile("["+DateTime.Now+ "] RetrieveXML: ERROR: Could not retrieve weather(3 retries). Aborting!");
                        proceed = false;
                    }
                }
            }
            return null;
        }
        
        private static void WriteToFile(string s)
        {
            //try catch these fuckers and logfile method
            
            try
            {
                using (FileStream fs = new FileStream("C:\\inetpub\\wwwroot\\data.htm", FileMode.Append))
                //using (FileStream fs = new FileStream("C:\\Users\\andreas.hove\\Desktop\\data.html", FileMode.Append))
                {
                    using (StreamWriter w = new StreamWriter(fs, Encoding.UTF8))
                    {
                        w.WriteLine(s + "<br>");
                    }
                }
            }
            catch
            {
                Console.WriteLine("[" + DateTime.Now + "] I/O: Could not open log file for writing.");
            }
            
        }
        private static void WriteToLogFile(string s)
        {
            try
            {
                using (FileStream fs = new FileStream("C:\\inetpub\\wwwroot\\log.txt", FileMode.Append))
                //using (FileStream fs = new FileStream("C:\\Users\\andreas.hove\\Desktop\\log.txt", FileMode.Append))
                {
                    using (StreamWriter w = new StreamWriter(fs, Encoding.UTF8))
                    {
                        w.WriteLine(s);
                    }
                }

                Console.WriteLine(s);
            }
            catch
            {
                Console.WriteLine("[" + DateTime.Now + "] I/O: Could not open htm file for writing.");
            }
        }
    }
}
