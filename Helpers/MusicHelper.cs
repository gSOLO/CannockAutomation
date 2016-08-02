using System;
using System.Collections.Generic;
using System.Linq;
using CannockAutomation.Actions;
using CannockAutomation.Devices;
using CannockAutomation.Extensions;

namespace CannockAutomation.Helpers
{
    public static class MusicHelper
    {
        private static readonly List<QueryHelper> SpeakerHelpers;
        private static readonly List<QueryHelper> PlayHelpers;
        private static readonly List<QueryHelper> SkipHelpers;
        private static readonly List<QueryHelper> EnableHelpers;
        private static readonly List<QueryHelper> VolumeUpHelpers;
        private static readonly List<QueryHelper> VolumeDownHelpers;

        static MusicHelper()
        {
            SpeakerHelpers = new List<QueryHelper>
            {
                Speakers.PC.GetQuery(),
                Speakers.PC.GetQuery("computer"),
                Speakers.PC.GetQuery("desktop"),
                Speakers.AppleTV.GetQuery(),
                Speakers.TV.GetQuery("tv", "apple tv"),
                Speakers.TV.GetQuery("television"),
                Speakers.AirPlay4S.GetQuery("kitchen"),
                Speakers.LivingRoom.GetQuery("living"),
                Speakers.SecondFloor.GetQuery("on the second floor"),
                Speakers.SecondFloor.GetQuery("downstairs"),
                Speakers.SecondFloor.GetQuery("on the first floor"),
                Speakers.AirPlay3GS.GetQuery("on the third floor"),
                Speakers.AirPlay3GS.GetQuery("upstairs"),
            };

            PlayHelpers = new List<QueryHelper>
            {
                MusicActions.Play.GetQuery(),
                MusicActions.Pause.GetQuery(),
                MusicActions.Stop.GetQuery(),
                MusicActions.Resume.GetQuery(),
                MusicActions.Shuffle.GetQuery(),
                MusicActions.PlayPause.GetQuery("music"),
            };

            SkipHelpers = new List<QueryHelper>
            {
                MusicActions.Skip.GetQuery(),
                MusicActions.Back.GetQuery(),
                MusicActions.Next.GetQuery(),
                MusicActions.Previous.GetQuery(),
            };

            EnableHelpers = new List<QueryHelper>
            {
                MusicActions.Enable.GetQuery(),
                MusicActions.Disable.GetQuery(),
            };

            VolumeUpHelpers = new List<QueryHelper>
            {
                MusicActions.VolumeUp.GetQuery(),
            };

            VolumeDownHelpers = new List<QueryHelper>
            {
                MusicActions.VolumeDown.GetQuery(),
            };
        }

        public static Speakers GetSpeakers(String query, Speakers rest = Speakers.None)
        {
            var speakers = Speakers.None;

            foreach (var queryHelper in SpeakerHelpers)
            {
                queryHelper.Run(query, ref speakers);
            }

            if (speakers == Speakers.None && (query.Contains("speakers") || query.Contains("music") || query.Contains("all")))
            {
                speakers = Speakers.All;
            }

            if (query.Contains("except"))
            {
                speakers = Speakers.All & ~speakers;
            }

            if (rest != Speakers.None && query.Contains("rest"))
            {
                speakers &= ~rest;
            }

            return speakers;
        }

        public static Speakers GetSpeakers(String query, List<Speakers> restList)
        {
            var rest = restList.Aggregate(Speakers.None, (current, speaker) => current | speaker);
            return GetSpeakers(query, rest);
        }

        public static MusicActions GetActions(String query)
        {
            var actions = MusicActions.None;

            if (String.IsNullOrWhiteSpace(query)) return actions;

            foreach (var queryHelper in PlayHelpers.Where(queryHelper => queryHelper.HaveMatch(query)))
            {
                queryHelper.Run(query, ref actions);
                break;
            }

            foreach (var queryHelper in SkipHelpers.Where(queryHelper => queryHelper.HaveMatch(query)))
            {
                queryHelper.Run(query, ref actions);
                break;
            }

            if (query.Contains("mute"))
            {
                actions |= MusicActions.Mute;
            }
            else if (query.Contains(" up ") || query.EndsWith("up") || query.Contains("louder") || query.Contains("quiet") || (query.Contains("soft") && !query.Contains("softer")))
            {
                actions |= MusicActions.VolumeUp;
            }
            else if (query.Contains(" down ") || query.EndsWith("down") || query.Contains("softer") || (query.Contains("loud") && !query.Contains("louder")))
            {
                actions |= MusicActions.VolumeDown;
            }

            foreach (var queryHelper in EnableHelpers.Where(queryHelper => queryHelper.HaveMatch(query)))
            {
                queryHelper.Run(query, ref actions);
                break;
            }
            return actions;
        }

        public static MusicActions GetActions(String query, out List<MusicActions> list)
        {
            var actions = GetActions(query);
            var invalidActions = new[] { MusicActions.Speaker, MusicActions.Player, MusicActions.Playlist, MusicActions.None };
            list = new List<MusicActions>();
            foreach (MusicActions action in Enum.GetValues(typeof(MusicActions)))
            {
                if (actions.HasFlag(action) && !invalidActions.Contains(action)) list.Add(action);
            }
            return actions;
        }

    }
}
