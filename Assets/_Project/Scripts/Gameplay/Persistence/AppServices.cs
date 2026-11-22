using UnityEngine;

namespace PingPong.Gameplay
{
    public static class AppServices
    {
        private static ProfileService _profile;
        private static SettingsService _settings;

        public static ProfileService Profile => _profile ?? (_profile = new ProfileService(new JsonFileStore<PlayerProfile>(Application.persistentDataPath, "profile.json")));

        public static SettingsService Settings => _settings ?? (_settings = new SettingsService(new JsonFileStore<GameSettings>(Application.persistentDataPath, "settings.json")));

        public static TeamColour LocalTeam => Profile.Current != null ? Profile.Current.Team : TeamColour.Red;

        public static MatchRequest? PendingMatch { get; set; }

        public static PingPong.Core.MenuScreen? MenuEntry { get; set; }

        public static void Replace(ProfileService profile, SettingsService settings)
        {
            _profile = profile;
            _settings = settings;
            PendingMatch = null;
            MenuEntry = null;
        }
    }
}
