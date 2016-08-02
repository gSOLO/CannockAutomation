using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using SKYPE4COMLib;

namespace CannockAutomation.Chat
{
    public class SkypeClient : IDisposable
    {
        public readonly Skype Skype = new SKYPE4COMLib.Skype();

        public SkypeClient()
        {
            
        }

        private TAttachmentStatus AttachmentStatus => ((ISkype) Skype).AttachmentStatus;

        public Boolean Attach()
        {
            if (!Skype.Client.IsRunning) { return false; }

            if (AttachmentStatus == TAttachmentStatus.apiAttachAvailable)
            {
                Skype.Attach(7);
            }

            if (AttachmentStatus != TAttachmentStatus.apiAttachSuccess)
            {
                return false;
            }

            Skype.MessageStatus -= SkypeOnMessageStatus;
            Skype.MessageStatus += SkypeOnMessageStatus;

            return true;
        }

        public Boolean Detach()
        {
            Skype.MessageStatus -= SkypeOnMessageStatus;

            return true;
        }

        public readonly string Padding = Environment.NewLine + Environment.NewLine;

        public Boolean TaggingEnabled { get; set; } = true;

        private void SkypeOnMessageStatus(ChatMessage message, TChatMessageStatus status)
        {
            if (message.FromHandle != Skype.CurrentUserHandle) { return; }

            if (TaggingEnabled)
            {
                if (message.IsEditable && message.Body.StartsWith("#") && !message.Body.EndsWith("#end"))
                {
                    var body = message.Body.Substring(1).Trim();
                    if (body.Contains("university"))
                    {
                        body = $"#school{Padding}{body}{Padding}#end";
                    }
                    message.Body = body;
                }
            }
        }

        public void Dispose()
        {
            Detach();
            Marshal.ReleaseComObject(Skype);
        }
    }
}
