using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using SystemColor = System.Drawing.Color;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using CannockAutomation.Actions;
using CannockAutomation.Color;
using CannockAutomation.Devices;
using CannockAutomation.Events;
using CannockAutomation.Extensions;
using CannockAutomation.Helpers;
using CannockAutomation.Net;
using CannockAutomation.Notifications;
using CannockAutomation.Properties;
using CannockAutomation.Timers;
using CannockAutomation.Util;
using iTunesLib;
using SharpHue;
using SystemTimer = System.Timers.Timer;

namespace CannockAutomation
{
    internal static class Program
    {
        public static NotifyIcon NotificationIcon;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(String[] mainArgs)
        {
            ProgramRecovery.RegisterForRestart();
            ProgramRecovery.RegisterForRecovery();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var listeningIcon = new Icon(Configuration.ListeningIconFilePath);
            var stoppedListeningIcon = new Icon(Configuration.StoppedListeningIconFilePath);

            NotificationIcon = new NotifyIcon {Icon = stoppedListeningIcon, Text = Configuration.NotifyIconText, Visible = true, ContextMenuStrip = new ContextMenuStrip()};

            NotificationIcon.DoubleClick += (sender, args) => Lilly.ToggleListening();

            NotificationIcon.ContextMenuStrip.Items.Add("&Exit", null, (sender, args) =>
            {
                Lilly.Save();
                ProgramRecovery.UnregisterRestart();
                ProgramRecovery.UnregisterRcovery();
                Application.Exit();
            });
            NotificationIcon.ContextMenuStrip.Items.Add("Start Listening", null, (sender, args) => Lilly.StartListening());
            NotificationIcon.ContextMenuStrip.Items.Add("Stop Listening", null, (sender, args) => Lilly.StopListening());
            NotificationIcon.ContextMenuStrip.Items.Add("&Restart", null, (sender, args) =>
            {
                Restart();
            });

            NotificationIcon.ContextMenuStrip.Items[1].Visible = !Lilly.IsListening;
            NotificationIcon.ContextMenuStrip.Items[2].Visible = Lilly.IsListening;

            try
            {
                InternalizeTimers();

                Lilly.Started += (sender, args) =>
                {
                    NotificationIcon.Icon = listeningIcon;
                    NotificationIcon.ContextMenuStrip.Items[1].Visible = false;
                    NotificationIcon.ContextMenuStrip.Items[2].Visible = true;
                    Pushover.Send("Started Listening");
                    NotificationIcon.Text = "Lilly is Listening";
                };
                Lilly.Stopped += (sender, args) =>
                {
                    NotificationIcon.Icon = stoppedListeningIcon;
                    NotificationIcon.ContextMenuStrip.Items[1].Visible = true;
                    NotificationIcon.ContextMenuStrip.Items[2].Visible = false;
                    Pushover.Send("Stopped Listening");
                    NotificationIcon.Text = "Lilly is not Listening";
                };
                Lilly.ListeningStateChanged += (sender, args) =>
                {
                    ProgramRecovery.ReregisterForRecovery();
                };

                Lilly.Request += RequestHandler;
                Lilly.RequestComplete += RequestCompleteHandler;

                Lilly.Light += LightHandler;
                Lilly.Music += MusicHandler;
                Lilly.Motion += MotionHandler;
                Lilly.LightSwitch += LightSwitchHandler;
                Lilly.PowerSwitch += PowerSwitchHandler;

                var recoveredLastSession = false;
                if (mainArgs.Length > 0 && mainArgs[0] == "/restart")
                {
                    recoveredLastSession = ProgramRecovery.RecoverLastSession(mainArgs[0]);
                }
                if (!recoveredLastSession) { Lilly.StartListening(); }

                Application.ApplicationExit += (sender, args) =>
                {
                    NotificationIcon.Visible = false;
                    NotificationIcon.Dispose();
                    Lilly.StopListening(false);
                };
            }
            catch (Exception e)
            {
                Pushover.Push(e.Message, $"Lilly Main {e.GetType()}", Priority.High);
                throw;
            }

            Application.Run(new ProgramContext());

            AppDomain.CurrentDomain.UnhandledException += (sender, args) => Pushover.Alert(args.ExceptionObject.ToString());
        }

        private static void Restart()
        {
            Lilly.Save();
            ProgramRecovery.WriteRecoveryFile();

            Process.Start(Path.Combine(Environment.CurrentDirectory, "CannockAutomationRestart.exe"));
            Application.Exit();
        }

        private static void InternalizeTimers()
        {
            Lights.Entryway.SetTimer(minutes: 1);
            Lights.SecondFloorLanding.SetTimer(seconds: 30);
            Lights.LivingRoom.SetTimer(hours: 8);
            Lights.DiningRoom.SetTimer(hours: 1);
            Lights.Kitchen.SetTimer(minutes: 2);
            Lights.QuarterLanding.SetTimer(seconds: 45);
            Lights.ThirdFloorLanding.SetTimer(seconds: 45);
            Lights.Bedroom.SetTimer(minutes: 5);
            Lights.Hallway.SetTimer(minutes: 1);

            LightOffTimer.Exception += LightOffTimerExceptionHandler;

            LightHelper.LightStateChange += LightStateChangeHandler;

            //LightSwitches.OutsideGarage.SetTimer(hours: 1);
            //LightSwitches.Garage.SetTimer(minutes: 10);
            //LightSwitches.LaundryRoom.SetTimer(minutes: 10);
            //LightSwitches.FrontDoor.SetTimer(minutes: 10);
            //LightSwitches.OutsideLivingRoom.SetTimer(hours: 1);
            //LightSwitches.Closet.SetTimer(minutes: 10);

            PowerSwitches.Toaster.SetTimer(hours: 1);

            SwitchOffTimer.Exception += SwitchOffTimerExceptionHandler;

            SwitchHelper.SwitchStateChange += SwitchStateChangeHandler;

            TimedWebClient.Exception += TimedWebClientExceptionHandler;
        }

        private static void LightOffTimerExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var e = args.ExceptionObject as Exception;
            if (e != null) { Pushover.Push(e.Message, $"Lilly LightOffTimer {e.GetType()}"); }
        }

        private static void LightStateChangeHandler(object sender, EventArgs args)
        {
            if (DeviceHelper.HueLights.AreOn())
            {
                try
                {
                    //LightSwitches.OutsideGarage.TurnOn();
                    //LightSwitches.Garage.TurnOn();
                    //LightSwitches.FrontDoor.TurnOn();
                    //LightSwitches.OutsideLivingRoom.TurnOn();

                    var geofencingLights = new List<Light>
                    {
                        Lights.DiningRoom.GetHueLight(),
                        Lights.Kitchen.GetHueLight(),
                        Lights.Bedroom.GetHueLight(),
                    };

                    if (geofencingLights.All(light => light != null && light.State.Brightness < 10))
                    {
                        Lights.ThirdFloor.SetTimer(minutes: 5);
                    }
                }
                catch (Exception e)
                {
                    Pushover.Push(e.Message, $"Lilly LightSwitches LightStateChange {e.GetType()}", Priority.High);
                }
            }
            foreach (var light in DeviceHelper.HueLights.ToList())
            {
                try
                {
                    if (light.GetStateChanged()) { light.Switched(light.State.IsOn); }
                }

                catch (Exception e)
                {
                    Pushover.Alert(e.Message, $"Lilly {light.Name} LightStateChange {e.GetType()}");
                }
            }
        }

        private static void SwitchOffTimerExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var e = args.ExceptionObject as Exception;
            if (e != null) { Pushover.Push(e.Message, $"Lilly SwitchOffTimer {e.GetType()}"); }
        }

        private static void SwitchStateChangeHandler(object sender, EventArgs args)
        {
            try
            {
                Pushover.Debug($"{sender} SwitchStateChange");
            }
            catch (Exception e)
            {
                Pushover.Alert(e.Message, $"Lilly {sender} SwitchStateChange {e.GetType()}");
            }
        }

        private static void RequestHandler(Object sender, RequestArgs args)
        {
            try
            {
                Pushover.Write(args.Query);
            }
            catch (Exception e)
            {
                Pushover.Alert(e.Message, $"Lilly RequestHandler {e.GetType()}");
            }
        }

        private static void RequestCompleteHandler(Object sender, RequestArgs args)
        {
            try
            {
                if (!String.IsNullOrWhiteSpace(args.Response))
                {
                    Pushover.Log(args.ToString());
                    Pushover.Push(args.Response, priority: args.Priority, url: args.Url, urlTitle: "Repeat Request");
                }
                else
                {
                    Pushover.Push(args.ToString(), priority: args.Priority, url: args.Url, urlTitle: "Repeat Request");
                }
                
                if (args.Query == "restart" || args.Url.EndsWith("&restart") || args.Url.EndsWith("/restart") || args.Results.Contains("Lilly.Restart"))
                {
                    Restart();
                }
            }
            catch (Exception e)
            {
                Pushover.Alert(e.Message, $"Lilly RequestCompleteHandler {e.GetType()}");
            }
        }

        private static void LightHandler(Object sender, LightArgs args)
        {
            try
            {
                var action = args.Action;
                if (args.Light != Lights.All)
                {
                    var lights = args.Light.ToList<Lights>();
                    if (lights.Count() > 1)
                    {
                        if (action.HasFlag(LightActions.Toggle))
                        {
                            action &= ~LightActions.Toggle;
                            DeviceHelper.RefreshHueLights();
                            action |= args.Light.AreOn() ? LightActions.TurnOff : LightActions.TurnOn;
                        }
                        foreach (var singleLight in lights)
                        {
                            LightHandler(sender, new LightArgs(args) {Action = action, Color = args.Color, Retry = args.Retry, TransitionTime = args.TransitionTime, Light = singleLight});
                        }
                        return;
                    }
                }
                else if (action == LightActions.Color)
                {
                    DeviceHelper.RefreshHueLights();
                    var allLightsOn = Lights.All.AreOn();
                    var allLightsOff = Lights.All.AreOff();
                    if (!Lights.All.AreOn() && !Lights.All.AreOff())
                    {
                        var lights = Lights.All.ToList<Lights>().Where(aLight => aLight.IsOn());
                        foreach (var singleLight in lights)
                        {
                            LightHandler(sender, new LightArgs(args) {Action = action, Color = args.Color, Retry = args.Retry, TransitionTime = args.TransitionTime, Light = singleLight});
                        }
                        return;
                    }
                }

                var light = args.Light.GetHueLight();

                if (light == null && !args.Light.HasFlag(Lights.All) && (!args.Retry.HasValue || args.Retry.Value))
                {
                    DeviceHelper.RefreshHueLights();
                    args.Retry = false;
                    LightHandler(sender, args);
                    return;
                }

                var transitionTime = args.TransitionTime;

                var state = light != null ? new LightStateBuilder().For(light) : new LightStateBuilder().ForAll();
                state.TransitionTime(transitionTime);

                if (transitionTime == 0) { transitionTime = 40; }

                // Turn On or Turn Off
                if (action.HasFlag(LightActions.Toggle))
                {
                    action &= ~LightActions.Toggle;
                    if (light != null)
                    {
                        DeviceHelper.HueLights.RefreshState(light);
                        action |= light.State.IsOn ? LightActions.TurnOff : LightActions.TurnOn;
                    }
                    else
                    {
                        DeviceHelper.HueLights.RefreshState();
                        action |= DeviceHelper.HueLights.AreOn() ? LightActions.TurnOff : LightActions.TurnOn;
                    }
                }

                if (action.HasFlag(LightActions.TurnOn))
                {
                    state.TurnOn();
                    state.Brightness(255);
                }
                else if (action.HasFlag(LightActions.TurnOff))
                {
                    state.TurnOff();
                }

                LightBrightnessHandler(action, light, args.Brightness, transitionTime, ref state);

                // Color
                if (action.HasFlag(LightActions.Color))
                {
                    if (light == null || light.State.IsOn || action.HasFlag(LightActions.TurnOn))
                    {
                        state.TurnOn();
                        if (args.Color != SystemColor.Empty)
                        {
                            state.Color(args.Color);
                        }
                        else
                        {
                            state.RandomColor();
                        }
                        state.TransitionTime(transitionTime);
                    }
                }

                state.ApplyInQueue();
            }
            catch (Exception e)
            {
                if (!args.Retry.HasValue || args.Retry.Value)
                {
                    args.Retry = false;
                    LightHandler(sender, args);
                    return;
                }
                Pushover.Alert(e.ToString(), $"Lilly {args.Light} {args.Action} LightHandler {e.GetType()}");
            }
        }

        private static void LightBrightnessHandler(LightActions action, Light light, byte? brightness, ushort transitionTime, ref LightStateBuilder state)
        {
            if (action.HasFlag(LightActions.Dim))
            {
                state.TurnOn();
                if (!brightness.HasValue)
                {
                    var newBrightness = light != null ? light.State.Brightness : DeviceHelper.HueLights.Min(aLight => aLight.State.Brightness);
                    if (newBrightness == 0) { state.TurnOff(); }
                    brightness = (byte) Math.Max(0, newBrightness - 85);
                }
                state.Brightness(brightness.Value);
                state.TransitionTime(transitionTime);
            }
            if (action.HasFlag(LightActions.Brighten))
            {
                state.TurnOn();
                if (!brightness.HasValue)
                {
                    var newBrightness = light != null ? light.State.Brightness : DeviceHelper.HueLights.Max(aLight => aLight.State.Brightness);
                    brightness = (byte) Math.Min(255, newBrightness + 85);
                }
                state.Brightness(brightness.Value);
                state.TransitionTime(transitionTime);
            }
        }

        private static void MusicHandler(Object sender, MusicArgs args)
        {
            try
            {
                var iTunes = new iTunesApp();

                var action = args.Action;

                var name = args.Name;
                IITPlaylist playlist = null;
                if (MusicActions.Playlist.HasFlag(action) && !String.IsNullOrWhiteSpace(name))
                {
                    var sortedPlaylists = new List<IITPlaylist>();
                    var playlists = iTunes.LibrarySource.Playlists;
                    for (var playlistIndex = 1; playlistIndex < playlists.Count + 1; playlistIndex++)
                    {
                        var thisPlaylist = playlists[playlistIndex];
                        if (thisPlaylist.Name == "Music" && name != "music") { continue; }
                        if (thisPlaylist.Kind != ITPlaylistKind.ITPlaylistKindUser || thisPlaylist.Tracks.Count == 0) { continue; }
                        sortedPlaylists.Add(thisPlaylist);
                    }
                    playlist = sortedPlaylists.OrderBy(playlistInfo => LevenshteinDistance.Compute(playlistInfo.Name.ToLower(), name)).FirstOrDefault();
                }

                switch (action)
                {
                    case MusicActions.Play:
                        if (playlist != null && playlist != iTunes.CurrentPlaylist)
                        {
                            playlist.PlayFirstTrack();
                        }
                        else
                        {
                            iTunes.Play();
                        }
                        break;
                    case MusicActions.Shuffle:
                        if (playlist != null && playlist != iTunes.CurrentPlaylist)
                        {
                            playlist.Shuffle = true;
                            playlist.PlayFirstTrack();
                        }
                        else
                        {
                            iTunes.CurrentPlaylist.Shuffle = true;
                            iTunes.Play();
                        }
                        break;
                    case MusicActions.Pause:
                        iTunes.Pause();
                        break;
                    case MusicActions.PlayPause:
                        iTunes.PlayPause();
                        break;
                    case MusicActions.Stop:
                        iTunes.Stop();
                        break;
                    case MusicActions.Resume:
                        iTunes.Resume();
                        break;
                    case MusicActions.Back:
                        iTunes.BackTrack();
                        break;
                    case MusicActions.Previous:
                        iTunes.PreviousTrack();
                        break;
                    case MusicActions.Next:
                        iTunes.NextTrack();
                        break;
                    case MusicActions.Skip:
                        iTunes.NextTrack();
                        break;
                    case MusicActions.VolumeUp:
                        //var upSpeakerName = args.Name;
                        var highVolume = iTunes.SoundVolume += 10;
                        iTunes.SoundVolume = Math.Min(100, highVolume);
                        break;
                    case MusicActions.VolumeDown:
                        //var downSpeakerName = args.Name;
                        var lowVolume = iTunes.SoundVolume -= 10;
                        iTunes.SoundVolume = Math.Max(0, lowVolume);
                        break;
                    case MusicActions.Mute:
                        //var muteSpeakerName = args.Name;
                        iTunes.Mute = !iTunes.Mute;
                        break;
                    case MusicActions.Enable:
                        //var enableSpeakerName = args.Name;
                        break;
                    case MusicActions.Disable:
                        //var disableSpeakerName = args.Name;
                        break;
                }

                Marshal.ReleaseComObject(iTunes);
            }
            catch (Exception e)
            {
                if (!args.Retry.HasValue || args.Retry.Value)
                {
                    args.Retry = false;
                    MusicHandler(sender, args);
                    return;
                }
                Pushover.Alert(e.Message, $"Lilly {args.Action} MusicHandler {e.GetType()}");
            }
        }

        private static void MotionLightsOn(Object sender, RequestArgs args, Lights lights)
        {
            LightHandler(sender, new LightArgs(args)
            {
                Action = LightActions.TurnOn | LightActions.Color,
                Color = LightColor.Concentrate,
                Light = lights,
                TransitionTime = 10,
            });
        }

        private static void MotionHandler(Object sender, MotionArgs args)
        {
            try
            {
                var action = args.Action;
                var haveMotion = action.HasFlag(MotionActions.Motion);
                var goingDown = action.HasFlag(MotionActions.GoingDown);
                var goingUp = action.HasFlag(MotionActions.GoingUp);
                var goingSomewhere = !action.HasFlag(MotionActions.GoingNowhere);
                var sensors = args.Sensor;

                if (haveMotion)
                {
                    if (sensors.HasFlag(Sensors.FrontDoor))
                    {
                        MotionLightsOn(sender, args, Lights.Entryway);
                    }
                    if (sensors.HasFlag(Sensors.Garage))
                    {
                        LightSwitches.Garage.TurnOn();

                        LightSwitches.OutsideGarage.ResetTimer();
                        LightSwitches.Garage.ResetTimer();
                    }
                    if (sensors.HasFlag(Sensors.Basement))
                    {
                        MotionLightsOn(sender, args, Lights.Entryway);

                        //LightSwitches.FrontDoor.ResetTimer();
                        //LightSwitches.OutsideGarage.ResetTimer();
                    }
                    if (sensors.HasFlag(Sensors.Entryway))
                    {
                        MotionLightsOn(sender, args, Lights.Entryway);

                        if (goingUp || goingSomewhere) { MotionLightsOn(sender, args, Lights.SecondFloorLanding); }
                    }
                    if (sensors.HasFlag(Sensors.SecondFloorLanding))
                    {
                        MotionLightsOn(sender, args, Lights.SecondFloorLanding);

                        if (goingUp)
                        {
                            MotionLightsOn(sender, args, Lights.LivingRoom);
                        }
                        else if (goingDown || goingSomewhere)
                        {
                            MotionLightsOn(sender, args, Lights.Entryway);
                        }
                    }
                    if (sensors.HasFlag(Sensors.LoveSeat))
                    {
                        Lights.DiningRoom.RestartTimer();
                    }
                    if (sensors.HasFlag(Sensors.Sofa))
                    {
                        Lights.LivingRoom.RestartTimer();
                    }
                    if (sensors.HasFlag(Sensors.Kitchen))
                    {
                        Lights.Kitchen.RestartTimer();
                    }
                    if (sensors.HasFlag(Sensors.KitchenSink))
                    {
                        MotionLightsOn(sender, args, Lights.Kitchen);

                        Lights.Kitchen.RestartTimer();
                        Lights.DiningRoom.RestartTimer();
                    }
                    if (sensors.HasFlag(Sensors.QuarterLanding))
                    {
                        MotionLightsOn(sender, args, Lights.QuarterLanding);

                        if (goingUp || goingSomewhere)
                        {
                            MotionLightsOn(sender, args, Lights.ThirdFloorLanding);
                        }

                        Lights.Kitchen.RestartTimer();
                    }
                    if (sensors.HasFlag(Sensors.ThirdFloorLanding))
                    {
                        MotionLightsOn(sender, args, Lights.ThirdFloorLanding);
                    }
                    if (sensors.HasFlag(Sensors.Bedroom))
                    {
                        Lights.Hallway.RestartTimer();
                        Lights.Bedroom.RestartTimer();
                    }
                    if (sensors.HasFlag(Sensors.Bed))
                    {
                        Lights.Bedroom.RestartTimer();
                    }
                    if (sensors.HasFlag(Sensors.Hallway))
                    {
                        MotionLightsOn(sender, args, Lights.Hallway);

                        Lights.Hallway.RestartTimer();
                    }

                    Lights.Entryway.RestartTimer();
                    Lights.SecondFloorLanding.RestartTimer();
                    Lights.QuarterLanding.RestartTimer();
                    Lights.ThirdFloorLanding.RestartTimer();

                    // Text Alerts
                    if (args.Alert)
                    {
                        Ifttt.Text($"Detected Motion: {args.Sensor.GetName()}");
                        return;
                    }

                    // Notifications
                    if (sensors.Any(Sensors.WeMo.HasFlag))
                    {
                        var title = $"Lilly: {sensors.GetName()}";
                        if (action.HasFlag(MotionActions.SuspiciousMotion))
                        {
                            Pushover.Alert("Detected Suspicious Motion", title);
                        }
                        else if (!args.AtHome && !sensors.HasFlag(Sensors.FrontDoor))
                        {
                            var priority = action.HasFlag(MotionActions.InactiveMotion) ? Priority.Alert : Priority.Normal;
                            Pushover.Push("Detected Motion", title, priority);
                        }
                        else if (sensors.HasFlag(Sensors.FrontDoor) && action.HasFlag(MotionActions.IdleMotion))
                        {
                            var priority = action.HasFlag(MotionActions.InactiveMotion) ? Priority.Normal : Priority.Quiet;
                            Pushover.Push("Detected Motion", title, priority);
                        }
                    }
                    
                }
                
            }
            catch (Exception e)
            {
                if (!args.Retry.HasValue || args.Retry.Value)
                {
                    args.Retry = false;
                    MotionHandler(sender, args);
                    return;
                }
                Pushover.Alert(e.Message, $"Lilly {args.Sensor} {args.Action} MotionHandler {e.GetType()}");
            }
        }

        private static void LightSwitchHandler(Object sender, SwitchArgs args)
        {
            try
            {
                var action = args.Action;
                var lightSwitch = (LightSwitches) args.Switch;
                var switchedOn = action.HasFlag(SwitchActions.On);
                var switchedOff = action.HasFlag(SwitchActions.Off);
                var turnOn = action.HasFlag(SwitchActions.TurnOn);
                var turndOff = action.HasFlag(SwitchActions.TurnOff);

                if (lightSwitch.HasFlag(LightSwitches.Garage) && switchedOn)
                {
                    Lights.Entryway.StopTimer();
                }
                if (lightSwitch.HasFlag(LightSwitches.KitchenSink))
                {
                    Lights.Kitchen.StopTimer();
                }
                if (lightSwitch.HasFlag(LightSwitches.SecondFloorBathroom) && switchedOn)
                {
                    Lights.QuarterLanding.StopTimer();
                }
                if (lightSwitch.HasFlag(LightSwitches.ThirdFloorBathroom) && switchedOn)
                {
                    Lights.Hallway.StopTimer();
                }


                if (switchedOn)
                {
                    lightSwitch.GetDevice().IsOn = true;
                }
                else if (switchedOff)
                {
                    lightSwitch.GetDevice().IsOn = false;
                }

                // Only the ThirdFloorBathroom is hooked up at the moment.
                if (lightSwitch != LightSwitches.ThirdFloorBathroom)
                {
                    return;
                }

                if (turnOn)
                {
                    lightSwitch.TurnOn();
                }
                else if (turndOff)
                {
                    lightSwitch.TurnOff();
                }
            }
            catch (Exception e)
            {
                if (!args.Retry.HasValue || args.Retry.Value)
                {
                    args.Retry = false;
                    LightSwitchHandler(sender, args);
                    return;
                }
                Pushover.Alert(e.Message, $"Lilly {args.Switch} {args.Action} LightSwitchHandler {e.GetType()}");
            }
        }

        private static void PowerSwitchHandler(Object sender, SwitchArgs args)
        {
            try
            {
                var action = args.Action;
                var powerSwitch = (PowerSwitches) args.Switch;
                var switchedOn = action.HasFlag(SwitchActions.On);
                var switchedOff = action.HasFlag(SwitchActions.Off);
                var turnOn = action.HasFlag(SwitchActions.TurnOn);
                var turndOff = action.HasFlag(SwitchActions.TurnOff);

                if (switchedOn)
                {
                    powerSwitch.GetDevice().IsOn = true;
                }
                else if (switchedOff)
                {
                    powerSwitch.GetDevice().IsOn = false;
                }

                if (turnOn)
                {
                    powerSwitch.TurnOn();
                }
                else if (turndOff)
                {
                    powerSwitch.TurnOff();
                }
            }
            catch (Exception e)
            {
                if (!args.Retry.HasValue || args.Retry.Value)
                {
                    args.Retry = false;
                    PowerSwitchHandler(sender, args);
                    return;
                }
                Pushover.Alert(e.Message, $"Lilly {args.Switch} {args.Action} PowerSwitchHandler {e.GetType()}");
            }
        }

        private static void TimedWebClientExceptionHandler(object sender, UnhandledExceptionEventArgs args)
        {
            var e = args.ExceptionObject as Exception;
            if (e != null) { Pushover.Push(e.Message, $"Lilly TimedWebClient {e.GetType()}"); }
        }
    }
}