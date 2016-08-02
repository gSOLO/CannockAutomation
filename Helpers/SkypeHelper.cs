using System;
using System.Collections.Generic;
using System.Security.Policy;
using CannockAutomation.Actions;
using CannockAutomation.Devices;
using CannockAutomation.Extensions;
using SKYPE4COMLib;

namespace CannockAutomation.Helpers
{
    public static class SkypeHelper
    {

        public static readonly SKYPE4COMLib.Skype SkypeClient = new SKYPE4COMLib.Skype();

        static SkypeHelper()
        {
            
        }

        public static void AttachClient()
        {
            
        }

        public static SwitchActions GetActions(String query)
        {
            return SwitchActions.None;
        }

        public static SwitchActions GetActions(String query, out String response)
        {
            response = String.Empty;
            return SwitchActions.None;
        }

    }
}