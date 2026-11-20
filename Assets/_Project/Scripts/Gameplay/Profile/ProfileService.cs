using System;
using PingPong.Core;

namespace PingPong.Gameplay
{
    public sealed class ProfileService
    {
        private readonly IKeyedStore<PlayerProfile> _store;
        private PlayerProfile _current;

        public ProfileService(IKeyedStore<PlayerProfile> store)
        {
            _store = store;
            var loaded = store.Load();
            _current = loaded != null && loaded.IsComplete ? loaded : null;
        }

        public bool HasProfile => _current != null;

        public PlayerProfile Current => _current;

        public event Action<PlayerProfile> Changed;

        public bool TrySave(string nickname, TeamColour team)
        {
            if (!NicknameRules.IsValid(nickname))
            {
                return false;
            }

            _current = new PlayerProfile(nickname, team);
            _store.Save(_current);
            Changed?.Invoke(_current);
            return true;
        }
    }
}
