using System;
using UnityEngine;

namespace PingPong.Gameplay
{
    public sealed class SettingsService
    {
        private readonly IKeyedStore<GameSettings> _store;

        public SettingsService(IKeyedStore<GameSettings> store)
        {
            _store = store;
            Current = Sanitised(store.Load() ?? new GameSettings());
        }

        public GameSettings Current { get; private set; }

        public event Action<GameSettings> Changed;

        public void Update(Action<GameSettings> change)
        {
            var next = Sanitised(Copy(Current));
            change(next);
            Current = Sanitised(next);
            _store.Save(Current);
            Changed?.Invoke(Current);
        }

        private static GameSettings Copy(GameSettings source) => new GameSettings
        {
            EffectsVolume = source.EffectsVolume,
            DominantHand = source.DominantHand
        };

        private static GameSettings Sanitised(GameSettings settings)
        {
            settings.EffectsVolume = Mathf.Clamp01(settings.EffectsVolume);
            if (settings.DominantHand == Hand.None)
            {
                settings.DominantHand = Hand.Right;
            }

            return settings;
        }
    }
}
