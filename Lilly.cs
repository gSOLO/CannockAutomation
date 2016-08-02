using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using SystemColor = System.Drawing.Color;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using CannockAutomation.Actions;
using CannockAutomation.Devices;
using CannockAutomation.Events;
using CannockAutomation.Extensions;
using CannockAutomation.Helpers;
using CannockAutomation.Net;
using CannockAutomation.Notifications;
using CannockAutomation.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CannockAutomation
{
    public static class Lilly
    {
        public static event EventHandler<LightArgs> Light;
        public static event EventHandler<MusicArgs> Music;
        public static event EventHandler<MotionArgs> Motion;
        public static event EventHandler<SwitchArgs> LightSwitch;
        public static event EventHandler<SwitchArgs> PowerSwitch;
        public static event EventHandler<RequestArgs> Request;
        public static event EventHandler<RequestArgs> RequestComplete;
        public static event EventHandler<DataArgs> Data;
        public static event EventHandler<EventArgs> Started;
        public static event EventHandler<EventArgs> Stopped;
        public static event EventHandler<EventArgs> ListeningStateChanged;

        private static readonly HttpListener Listener;

        private static Boolean _isListening;

        public static Boolean IsListening
        {
            get { return _isListening && Listener.IsListening; }
        }

        static Lilly()
        {
            Listener = new HttpListener();
        }

        public static Boolean ToggleListening()
        {
            return IsListening ? StopListening() : StartListening();
        }

        public static Boolean StartListening()
        {
            var wasListening = StopListening(false);

            var username = Environment.GetEnvironmentVariable("USERNAME");
            var userdomain = Environment.GetEnvironmentVariable("USERDOMAIN");

            Listener.Prefixes.Clear();
            Listener.Prefixes.Add(Configuration.ListenerPrefix);
            try
            {
                if (!Listener.IsListening) { Listener.Start(); }
            }
            catch (HttpListenerException e)
            {
                if (e.ErrorCode == 5)
                {
                    // ReSharper disable once StringLiteralTypo
                    throw new Exception($"netsh http add urlacl url={Configuration.ListenerPrefix} user={userdomain}\\{username} listen=yes", e);
                }
                throw;
            }

            _isListening = true;
            SetData("Listening", "True");

            WaitForAsyncRequest();

            Started?.Invoke(null, EventArgs.Empty);

            if (!wasListening)
            {
                ListeningStateChanged?.Invoke(null, EventArgs.Empty);
            }

            return true;
        }

        public static Boolean StopListening(Boolean notify = true)
        {
            var wasListening = IsListening;

            _isListening = false;
            SetData("Listening", "False");

            if (notify)
            {
                Stopped?.Invoke(null, EventArgs.Empty);
            }

            if (wasListening)
            {
                ListeningStateChanged?.Invoke(null, EventArgs.Empty);
            }

            return wasListening;
        }

        private static void WaitForAsyncRequest()
        {
            Listener.BeginGetContext(AsyncRequestHandler, Listener);
        }

        private static void AsyncRequestHandler(IAsyncResult result)
        {
            try
            {
                if (!IsListening) { return; }

                var context = Listener.EndGetContext(result);

                WaitForAsyncRequest();

                var request = context.Request;
                var response = context.Response;
                response.StatusCode = (int) HttpStatusCode.OK;
                response.StatusDescription = "OK";

                var query = request.Url.Query;

                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    var requestString = reader.ReadToEnd();
                    if (request.ContentType == "application/json")
                    {
                        var json = JObject.Parse(requestString);
                    }
                    else
                    {
                        query = $"{query}&{requestString}";
                    }
                }

                var queryInfo = HttpUtility.ParseQueryString(query);

                Boolean handleRequest;
                var responseString = GetResponse(request.Url.AbsolutePath, queryInfo, out handleRequest);

                if (handleRequest && ShouldHandleRequest(query))
                {
                    var requestArgs = HandleRequest(query, request.Url.AbsoluteUri);
                    if (String.IsNullOrEmpty(responseString))
                    {
                        responseString = GetResponse(requestArgs);
                        if (requestArgs.Source.HasFlag(RecognitionDevice.Text))
                        {
                            Ifttt.Text(responseString);
                        }
                    }
                }

                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);

                response.Close();
            }
            catch (Exception e)
            {
                Pushover.Alert(e.Message, $"Lilly AsyncRequestHandler {e.GetType()}");
            }
        }

        public static String GetResponse(String path, NameValueCollection query, out Boolean handleRequest)
        {
            var response = String.Empty;
            var requestRegex = new Regex(@"^.*/favicon.ico$|^/ui.*|^/status.*|^/device.*|^/info.*|^/refresh.*");
            handleRequest = !requestRegex.IsMatch(path);
            try
            {
                path = Uri.UnescapeDataString(path).Replace('+', ' ');

                if (path.EndsWith("/favicon.ico")) { return GetFaviconString(); }
                if (path.StartsWith("/ui") || path.StartsWith("/lilly/ui")) { return GetView(path, query); }
                if (path.StartsWith("/data") || path.StartsWith("/lilly/data") || path.StartsWith("/if") || path.StartsWith("/lilly/if"))
                {
                    response = GetData(path, query, out handleRequest);
                }

                if (!handleRequest && String.IsNullOrWhiteSpace(response))
                {
                    ThreadPool.QueueUserWorkItem(o => TimedWebClient.Ping($"{Configuration.WemoServiceBaseUri}{query}", false));

                    Boolean haveDecviceInfo;
                    var deviceInfo = DeviceHelper.GetDeviceInfo(query, out haveDecviceInfo);
                    if (haveDecviceInfo)
                    {
                        return deviceInfo;
                    }
                    var info = new
                    {
                        IsListenineg = IsListening,
                        Query = query,
                        Path = path,
                        Devices = DeviceHelper.Devices.Select(device => new { Type = device.Id.GetType().Name, device.Name, device.IsOn, device.Udn, device.LastUpdate, }).ToArray(),
                    };
                    return JsonConvert.SerializeObject(info, Formatting.Indented);
                }
            }
            catch (Exception e)
            {
                Pushover.Push(e.Message, e.GetType().ToString());
            }
            return response;
        }

        public static String GetResponse(RequestArgs requestArgs)
        {
            if (!requestArgs.Results.Any() && requestArgs.Response == null)
            {
                return "I'm not sure what you want me to do";
            }

            return requestArgs.Response ?? "Okay";
        }

        public static String GetFaviconString()
        {
            try
            {
                using (var streamReader = new StreamReader(IsListening ? Configuration.ListeningIconFilePath : Configuration.StoppedListeningIconFilePath))
                {
                    return streamReader.ReadToEnd();
                }
            }
            catch (Exception e)
            {
                Pushover.Push(e.Message, e.GetType().ToString());
            }
            return String.Empty;
        }

        private static String GetView(String path, NameValueCollection query)
        {
            return $"{path} {query}";
        }

        private static readonly Object DataLocker = new Object();

        public static String GetData()
        {
            Boolean handleRequest;
            return GetData("/data", null, out handleRequest);
        }

        public static String GetJson()
        {
            Boolean handleRequest;
            return GetData("/json", null, out handleRequest);
        }

        public static String GetData(String name)
        {
            Boolean handleRequest;
            return GetData("/get", new NameValueCollection {{name, String.Empty}}, out handleRequest);
        }

        public static Boolean Is(String name)
        {
            Boolean boolean;
            Boolean.TryParse(GetData(name), out boolean);
            return boolean;
        }

        public static Boolean IsNot(String name)
        {
            return !Is(name);
        }

        public static Boolean If(String name, String value, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
        {
            return String.Equals(GetData(name), value, comparisonType);
        }

        public static Boolean If(String name)
        {
            return Is(name);
        }

        public static void SetData(String name, Object value)
        {
            ThreadPool.QueueUserWorkItem(o =>
            {
                Boolean handleRequest;
                GetData("/set", new NameValueCollection {{name, value.ToString()}}, out handleRequest);
            });
        }

        public static void Save()
        {
            Boolean handleRequest;
            GetData("/set", new NameValueCollection(), out handleRequest);
        }

        private static readonly String DataFile = Configuration.DataFilePath;
        private static readonly String JsonFile = Configuration.JsonFilePath;

        public static String GetData(String path, NameValueCollection query, out Boolean handleRequest)
        {
            lock (DataLocker)
            {
                handleRequest = false;
                NameValueCollection data = null;

                var haveDataFile = File.Exists(DataFile) && new FileInfo(DataFile).Length > 0;
                var haveJsonFile = File.Exists(JsonFile) && new FileInfo(JsonFile).Length > 0;

                // Recover Data
                if (haveJsonFile && !haveDataFile)
                {
                    try
                    {

                        var jsonString = File.ReadAllText(JsonFile);
                        var json = JObject.Parse(jsonString);
                        data = new NameValueCollection();
                        foreach (var property in json.OfType<JProperty>().Where(property => property.Name != "Devices" && property.Name != "Version"))
                        {
                            data.Set(property.Name, property.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        Pushover.Alert(e.Message, $"Lilly GetData {path} {e.GetType()}");
                    }
                }

                // Clear Data
                if (!haveDataFile || path.Contains("/clear"))
                {
                    var dataArgs = new DataArgs { Name = DataFile, Action = DataActions.Clear, };

                    File.Create(DataFile).Close();

                    OnData(dataArgs);
                }
                if (!haveJsonFile || path.Contains("/clear"))
                {
                    var dataArgs = new DataArgs { Name = JsonFile, Action = DataActions.Clear, };

                    File.Create(JsonFile).Close();

                    OnData(dataArgs);
                }

                if (data == null)
                {
                    var dataString = File.ReadAllText(DataFile);
                    data = HttpUtility.ParseQueryString(dataString);
                }

                // Set Data
                if (path.Contains("/set"))
                {
                    try
                    {
                        foreach (var key in query.AllKeys)
                        {
                            if (String.IsNullOrWhiteSpace(key)) { continue; }

                            var value = query.Get(key).Trim();
                            var oldValue = data.Get(key);
                            
                            var dataArgs = new DataArgs { Name = key, Action = DataActions.Set, OldValue = oldValue, NewValue = value, };
                            if (oldValue != value)
                            {
                                dataArgs.Action |= DataActions.Change;
                            }
							
							switch (key) {
								case "Action":
							        switch (value)
							        {
							            case "sunrise":
                                            data.Set("Sunrise", "True");
                                            data.Set("Sunset", "False");
                                            data.Set("Night", "False");
                                            data.Set("Day", "True");
							                break;
                                        case "sunset":
                                            data.Set("Sunrise", "False");
                                            data.Set("Sunset", "True");
                                            data.Set("Night", "True");
                                            data.Set("Day", "False");
							                break;
                                        default:
                                            if (value.StartsWith("entered"))
                                            {
                                                value = value.Replace("entered", String.Empty);

                                                data.Set("LastLocation", data.Get("Location"));
                                                data.Set("Location", value);
                                                data.Set("Home", value == "Home" ? "True" : "False");
                                                data.Set("Work", value == "Work" ? "True" : "False");

                                            }
                                            else if (value.StartsWith("exited"))
                                            {
                                                value = value.Replace("exited", String.Empty);

                                                data.Set("LastLocation", value);
                                                data.Set("LastKnownLocation", value);
                                                data.Set("Location", "Unknown");
                                                data.Set("Home", "False");
                                                data.Set("Work", "False");
                                            }
							                break;
							        }
							        break;

                                case "source":
							        break;
									
								default:
							        if (String.IsNullOrWhiteSpace(value))
							        {
							            data.Remove(key);
							        }
							        else
							        {
                                        data.Set(key, value);
							        }
							        break;
							}
                            
                            OnData(dataArgs);
                        }

                        data.Set("Date", DateTime.Now.ToString(CultureInfo.InvariantCulture));
                        File.WriteAllText(DataFile, data.ToString());

                        var json = data.Keys.Cast<String>().Where(key => key != null).ToDictionary(key => key, key => (Object)data.Get(key));
                        json.Add("Version", Assembly.GetExecutingAssembly().GetName().Version);
                        var devices = DeviceHelper.Devices.Select(device => new {Type = device.Id.GetType().Name, device.Name, device.IsOn, device.Udn, device.LastUpdate,}).ToArray();
                        json.Add("Devices", devices);

                        File.WriteAllText(JsonFile, JsonConvert.SerializeObject(json, Formatting.Indented));
                    }
                    catch (Exception e)
                    {
                        Pushover.Alert(e.Message, $"Lilly GetData {path} {e.GetType()}");
                    }
                }

                // Check Data
                if (path.Contains("/if"))
                {
                    foreach (var key in query.AllKeys)
                    {
                        var queryValue = query.Get(key);
                        if (!String.IsNullOrWhiteSpace(key) && !String.IsNullOrWhiteSpace(queryValue))
                        {
                            var value = data.Get(key);
                            handleRequest = String.Equals(queryValue, value, StringComparison.OrdinalIgnoreCase);

                            var dataArgs = new DataArgs { Name = key, Action = DataActions.Check, Value = value, };
                            OnData(dataArgs);
                        }
                    }
                }

                // Get Data (Key)
                if (path.Contains("/get") && query.AllKeys.Count() == 1)
                {
                    handleRequest = false;
                    var key = query.AllKeys.Single();
                    var value = data.Get(key);

                    var dataArgs = new DataArgs { Name = key, Action = DataActions.Get, Value = value, };
                    OnData(dataArgs);

                    return value;
                }

                // Get Data (JSON)
                if (path.Contains("/json"))
                {
                    return File.ReadAllText(JsonFile);
                }

                // Get Data (Query)
                OnData(new DataArgs { Name = JsonFile, Action = DataActions.Get, });

                return data.ToString();
            }
        }

        private static Boolean ShouldHandleRequest(String query)
        {
            return !String.IsNullOrWhiteSpace(query) && (IsListening || query.EndsWith("start listening") || query.EndsWith("stop listening") || query.EndsWith("restart") || query.EndsWith("save"));
        }

        public static RequestArgs HandleRequest(String request, String url = null, Priority priority = Priority.Silent, RecognitionDevice source = RecognitionDevice.None)
        {
            var requestArgs = new RequestArgs
            {
                Url = url,
                Query = request,
                Priority = priority,
                Source = source,
            };
            try
            {
                request = Uri.UnescapeDataString(request).ToLower().Replace('+', ' ');
                var queryStrings = new List<String>();

                if (!request.Contains("split=false"))
                {
                    request = Regex.Replace(request, @"\?q=|\?request=|\?status|\?devices|\?info|\?", String.Empty);
                    queryStrings = request.Split(new[] {"then", "&"}, StringSplitOptions.RemoveEmptyEntries).Where(q => !String.IsNullOrWhiteSpace(q)).ToList();
                }
                else
                {
                    queryStrings.Add(request);
                }

                OnRequest(requestArgs);

                HandleQueries(queryStrings, ref requestArgs);

                OnRequest(requestArgs, true);
            }
            catch (Exception e)
            {
                Pushover.Alert(e.Message, $"Lilly HandleRequest {e.GetType()}");
            }

            return requestArgs;
        }

        private static RecognitionDevice GetRecognitionDevice (String query)
        {
            if (query.Contains("source=alexa"))
            {
                return RecognitionDevice.Alexa;
            }

            if (query.Contains("source=ifttt"))
            {
                var device = RecognitionDevice.Ifttt;
                if (query.Contains("ifttt/sms"))
                {
                    device |= RecognitionDevice.Sms;
                }
                if (query.Contains("ifttt/phone"))
                {
                    device |= RecognitionDevice.Phone;
                }
                if (query.Contains("ifttt/wemo"))
                {
                    device |= RecognitionDevice.WeMo;
                }
                return device;
            }

            if (query.Contains("source=amazon"))
            {
                return RecognitionDevice.Amazon;
            }

            if (query.Contains("axius=") || query.Contains("source=axius"))
            {
                return RecognitionDevice.Axius;
            }

            return RecognitionDevice.Lilly;
        }

        private static void OnRequest(RequestArgs args, bool complete = false)
        {
            if (!complete && args.Source == RecognitionDevice.None)
            {
                args.Source = GetRecognitionDevice(args.Query);
            }
            
            var handler = complete ? RequestComplete : Request;
            if (handler != null)
            {
                ThreadPool.QueueUserWorkItem(o => handler(null, args));
            }
        }

        private static void OnLight(LightArgs args)
        {
            var handler = Light;
            if (handler != null)
            {
                ThreadPool.QueueUserWorkItem(o => handler(null, args));
            }
        }

        private static void OnMusic(MusicArgs args)
        {
            var handler = Music;
            if (handler != null)
            {
                ThreadPool.QueueUserWorkItem(o => handler(null, args));
            }
        }

        private static void OnMotion(MotionArgs args)
        {
            var handler = Motion;
            if (handler != null)
            {
                ThreadPool.QueueUserWorkItem(o => handler(null, args));
            }
        }

        private static void OnSwitch(SwitchArgs args)
        {
            var handler = args.Switch is LightSwitches ? LightSwitch : PowerSwitch;
            if (handler != null)
            {
                ThreadPool.QueueUserWorkItem(o => handler(null, args));
            }
        }

        private static void OnData(DataArgs args)
        {
            var handler = Data;
            if (handler != null)
            {
                ThreadPool.QueueUserWorkItem(o => handler(null, args));
            }
        }

        private static void HandleQueries(IEnumerable<String> queryStrings, ref RequestArgs requestArgs)
        {
            var affectedLights = Lights.None;
            var affectedSpeakers = Speakers.None;

            foreach (var query in queryStrings)
            {
                if (HandleQuery(query, ref requestArgs)) { continue; }

                if (HandleSensorQuery(query, ref requestArgs)) { continue; }

                if (HandleSwitchQuery(query, ref requestArgs)) { continue; }

                if (HandleLightQuery(query, ref requestArgs, ref affectedLights)) { continue; }

                if (HandleMusicQuery(query, ref requestArgs, ref affectedSpeakers)) { continue; }

                if (HandleTelevisionQuery(query, ref requestArgs)) { continue; }

                if (HandleMoodQuery(query, ref requestArgs)) { continue; }

                HandleUnhandledQuery(query, ref requestArgs);
            }
        }

        private static Boolean HandleQuery(String query, ref RequestArgs requestArgs)
        {
            switch (query)
            {
                case "stop listening":
                    StopListening();
                    requestArgs.Response = "Okay, I've stopped listening";
                    requestArgs.AddResult("Lilly.StopListening");
                    break;
                case "start listening":
                    StartListening();
                    requestArgs.Response = "Okay, I'm listening";
                    requestArgs.AddResult("Lilly.StartListening");
                    break;
                case "restart":
                    requestArgs.Response = "Okay, I'm restarting";
                    requestArgs.AddResult("Lilly.Restart");
                    break;
                case "save":
                    Save();
                    requestArgs.Response = GetJson();
                    requestArgs.AddResult("Lilly.Save");
                    break;
                case "test":
                    requestArgs.Response = "Success";
                    requestArgs.AddResult("Lilly.Test");
                    break;
                case "ping":
                    requestArgs.Response = "Pong";
                    requestArgs.AddResult("Lilly.Ping");
                    break;
                default:
                    var isInfoQuery = query.StartsWith("axius=") || query.StartsWith("source=") || query.StartsWith("_");
                    return isInfoQuery;
            }
            return true;
        }

        private static Boolean HandleSensorQuery(String query, ref RequestArgs requestArgs)
        {
            Sensors sensors;
            var actions = MotionHelper.GetActions(query, out sensors);

            if (!actions.Any() || !sensors.Any()) { return false; }

            requestArgs.Priority = actions.HasFlag(MotionActions.InactiveMotion) ? Priority.Log : Priority.Local;

            var args = new MotionArgs(requestArgs)
            {
                Action = actions,
                Sensor = sensors,
                AtHome = Is("Home"),
            };

            if (sensors.HasFlag(Sensors.FrontDoor) && If("AlertOnFrontDoorMotion"))
            {
                args.Alert = true;
                SetData("AlertOnFrontDoorMotion", false);
            }

            OnMotion(args);
            requestArgs.AddResult(args);

            return true;
        }

        private static Boolean HandleSwitchQuery(String query, ref RequestArgs requestArgs)
        {
            LightSwitches lightSwitch;
            PowerSwitches powerSwitch;
            var actions = SwitchHelper.GetActions(query, out lightSwitch, out powerSwitch);
            if (!actions.Any() || (!lightSwitch.Any() && !powerSwitch.Any())) { return false; }

            var changeingOnOffState = actions == SwitchActions.TurnOn && actions == SwitchActions.TurnOff;

            if (!changeingOnOffState) { requestArgs.Priority = Priority.Log; }

            SwitchArgs args;
            if (lightSwitch != LightSwitches.None)
            {
                args = new SwitchArgs(requestArgs)
                {
                    Action = actions,
                    Switch = lightSwitch,
                };
                OnSwitch(args);
                requestArgs.AddResult(args);
                requestArgs.Response = $"{lightSwitch.GetName()} {actions.GetName()}";

                return !changeingOnOffState;
            }

            if (powerSwitch != PowerSwitches.None)
            {
                args = new SwitchArgs(requestArgs)
                {
                    Action = actions,
                    Switch = powerSwitch,
                };
                OnSwitch(args);
                requestArgs.AddResult(args);
                requestArgs.Response = $"{powerSwitch.GetName()} {actions.GetName()}";

                return true;
            }

            return true;
        }

        private static Boolean HandleLightQuery(String query, ref RequestArgs requestArgs, ref Lights affectedLights)
        {
            var lights = LightHelper.GetLights(query, affectedLights);

            SystemColor color;
            byte? brightness;
            LightActions lightActions;

            if (!lights.Any())
            {
                color = LightHelper.GetColor(query, true);
                if (color == SystemColor.Empty && query != "random")
                {
                    return requestArgs.Results.Any(result => result is SwitchArgs);
                }
                lights = Lights.All;
                lightActions = LightActions.Color;
                brightness = null;
            }
            else
            {
                lightActions = LightHelper.GetActions(query, out color, out brightness);
                if (requestArgs.Source == RecognitionDevice.Amazon && affectedLights == Lights.None)
                {
                    lightActions &= ~LightActions.TurnOn;
                    lightActions &= ~LightActions.TurnOff;
                }
            }

            var args = new LightArgs(requestArgs)
            {
                Action = lightActions,
                Light = lights,
                Color = color,
                Brightness = brightness,
            };

            OnLight(args);
            requestArgs.AddResult(args);

            affectedLights |= lights;

            return true;
        }

        private static Boolean HandleMusicQuery(String query, ref RequestArgs requestArgs, ref Speakers affectedSpeakers)
        {
            if (requestArgs.Source == RecognitionDevice.Amazon) { return true; }

            var musicActions = MusicHelper.GetActions(query).ToList<MusicActions>();

            if (!musicActions.Any()) { return false; }

            var musicName = String.Join(String.Empty, query.Split(new[] {"playlist", "play", "shuffle", " "}, StringSplitOptions.RemoveEmptyEntries));

            var speakers = MusicHelper.GetSpeakers(query, affectedSpeakers);

            foreach (var musicAction in musicActions)
            {
                MusicArgs args;
                if (MusicActions.Speaker.HasFlag(musicAction))
                {
                    if (!speakers.Any()) { continue; }
                    args = new MusicArgs(requestArgs)
                    {
                        Action = musicAction,
                        Speaker = speakers,
                    };
                    OnMusic(args);
                    requestArgs.AddResult(args);

                    affectedSpeakers |= speakers;
                }
                else
                {
                    args = new MusicArgs(requestArgs)
                    {
                        Action = musicAction,
                        Name = musicName,
                    };
                    OnMusic(args);
                    requestArgs.AddResult(args);
                }
            }

            return true;
        }

        private static Boolean HandleTelevisionQuery(String query, ref RequestArgs requestArgs)
        {
            return false;
        }

        private static readonly Regex MoodRegex = new Regex(@"i'm (?<mood>((?!home).)+)|feel (?<mood>.+)|felt (?<mood>.+)|feeling (?<mood>.+)");
        private static Boolean HandleMoodQuery(String query, ref RequestArgs requestArgs)
        {
            query = HttpUtility.HtmlDecode(query);
            var match = MoodRegex.Match(query).Groups["mood"];
            if (!match.Success) { return false; }
            var mood = match.Value;

            SetData("Mood", CultureInfo.InvariantCulture.TextInfo.ToTitleCase(mood));
            Ifttt.Trigger("add_row", "Lilly", "Moods", $"{DateTime.Now}|||{mood}");

            requestArgs.Response = $"Okay, thanks for letting me know you're {mood}";
            requestArgs.AddResult("Lilly.Mood");
            return true;
        }

        private static void HandleUnhandledQuery(String query, ref RequestArgs requestArgs)
        {
            query = HttpUtility.HtmlDecode(query);
            switch (query)
            {
                case "i'm home":
                    SetData("Action", "enteredHome");
                    requestArgs.Response = "Welcome home";
                    requestArgs.AddResult("Lilly.Home");
                    break;
                case "goodnight":
                case "good night":
                    SetData("Sleep", true);
                    requestArgs.Response = "Goodnight";
                    requestArgs.AddResult("Lilly.Sleep");
                    break;
                case "good morning":
                case "i'm awake":
                    SetData("Awake", true);
                    requestArgs.Response = "Good morning";
                    requestArgs.AddResult("Lilly.Awake");
                    break;
                case "watch the front door":
                case "i'm expecting someone":
                    SetData("AlertOnFrontDoorMotion", true);
                    requestArgs.Response = "Okay, I'll watch the front door";
                    requestArgs.AddResult("Lilly.AlertOnFrontDoorMotion");
                    break;
                default:
                    if (requestArgs.Priority >= Priority.Silent) { requestArgs.Priority = Priority.Log; }
                    break;
            }
        }
    }
}