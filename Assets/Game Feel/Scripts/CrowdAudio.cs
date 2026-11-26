using System.Collections;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Stadium crowd sound, generated in code (the project has no crowd audio files):
    /// a looping background murmur, a roar on points and a big cheer + applause at the start and end.
    /// Swap in real recordings by assigning the clip fields; generated clips are only used when they're empty.
    /// Added automatically by GameFeelBootstrap.
    /// </summary>
    public class CrowdAudio : MonoBehaviour
    {
        [Header("Optional: real recordings (leave empty to use generated sound)")]
        public AudioClip ambienceClip;
        public AudioClip roarClip;
        public AudioClip applauseClip;
        public AudioClip oohClip;

        [Header("Volumes")]
        [Range(0f, 1f)] public float ambienceVolume = 0.18f;
        [Range(0f, 1f)] public float roarVolume = 0.7f;
        [Range(0f, 1f)] public float applauseVolume = 0.5f;
        [Range(0f, 1f)] public float oohVolume = 0.6f;

        const int SampleRate = 22050;

        AudioSource _ambience, _roar, _applause, _ooh;
        MatchFeed _match;

        void Start()
        {
            var rng = new System.Random(1234);
            if (ambienceClip == null) ambienceClip = MakeAmbience(rng, 8f);
            if (roarClip == null) roarClip = MakeRoar(rng, 3.5f);
            if (applauseClip == null) applauseClip = MakeApplause(rng, 5f);
            if (oohClip == null) oohClip = MakeOoh(rng, 1.7f);

            _ambience = MakeSource("Crowd Ambience", ambienceClip, true);
            _ambience.volume = ambienceVolume;
            _ambience.Play();
            _roar = MakeSource("Crowd Roar", roarClip, false);
            _applause = MakeSource("Crowd Applause", applauseClip, false);
            _ooh = MakeSource("Crowd Ooh", oohClip, false);

            _match = GetComponent<MatchFeed>();
            if (_match != null)
            {
                _match.GameStarted += OnGameStarted;
                _match.PointScored += OnPointScored;
                _match.GameWon += OnGameWon;
            }
        }

        void OnDestroy()
        {
            if (_match != null)
            {
                _match.GameStarted -= OnGameStarted;
                _match.PointScored -= OnPointScored;
                _match.GameWon -= OnGameWon;
            }
        }

        AudioSource MakeSource(string name, AudioClip clip, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = loop;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // the crowd surrounds you
            return src;
        }

        void OnGameStarted() { Roar(0.6f); Applause(0.6f); }
        void OnPointScored(int winner, int s, int n) { Roar(0.8f); }
        void OnGameWon(int winner) { Roar(1f); Applause(1f); StartCoroutine(SecondWave()); }

        IEnumerator SecondWave()
        {
            yield return new WaitForSeconds(2.2f);
            Roar(0.85f);
        }

        /// <summary>Play a crowd roar (0..1).</summary>
        public void Roar(float intensity)
        {
            if (_roar == null) return;
            _roar.pitch = Random.Range(0.94f, 1.06f);
            _roar.PlayOneShot(_roar.clip, roarVolume * Mathf.Clamp01(intensity));
            StartCoroutine(Duck());
        }

        public void Applause(float intensity)
        {
            if (_applause == null) return;
            _applause.PlayOneShot(_applause.clip, applauseVolume * Mathf.Clamp01(intensity));
        }

        /// <summary>The crowd's "ooooh" for close calls (net, edge, near misses).</summary>
        public void Ooh(float intensity)
        {
            if (_ooh == null) return;
            _ooh.pitch = Random.Range(0.95f, 1.05f);
            _ooh.PlayOneShot(_ooh.clip, oohVolume * Mathf.Clamp01(intensity));
        }

        // Swell the background murmur up under a roar, then settle.
        IEnumerator Duck()
        {
            float t = 0f;
            while (t < 3f)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / 3f) * Mathf.PI);
                _ambience.volume = ambienceVolume * (1f + 0.8f * k);
                yield return null;
            }
            _ambience.volume = ambienceVolume;
        }

        // ---------------- Sound generation ----------------

        // One-pole filters (cheap, fine for noise shaping).
        struct OnePole
        {
            float _a, _y;
            public OnePole(float cutoffHz) { _a = Mathf.Exp(-2f * Mathf.PI * cutoffHz / SampleRate); _y = 0f; }
            public float LowPass(float x) { _y = x + _a * (_y - x); return _y; }
            public float HighPass(float x) { return x - LowPass(x); }
        }

        static float Noise(System.Random r) => (float)(r.NextDouble() * 2.0 - 1.0);

        /// <summary>Background murmur: band-limited noise with many overlapping "voices" fading in and out.</summary>
        static AudioClip MakeAmbience(System.Random rng, float seconds)
        {
            int n = Mathf.RoundToInt(seconds * SampleRate);
            var data = new float[n];
            var lp = new OnePole(900f);
            var hp = new OnePole(180f);
            const int voices = 24;
            var phase = new float[voices];
            var rate = new float[voices];
            for (int v = 0; v < voices; v++) { phase[v] = (float)rng.NextDouble(); rate[v] = 0.4f + (float)rng.NextDouble() * 1.6f; }
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float env = 0f;
                for (int v = 0; v < voices; v++)
                {
                    float s = Mathf.Sin((t * rate[v] + phase[v]) * Mathf.PI * 2f);
                    env += s > 0f ? s : 0f;
                }
                env = 0.35f + env / voices;
                float x = Noise(rng);
                x = hp.HighPass(lp.LowPass(x));
                data[i] = x * env;
            }
            data = LoopCrossfade(data, Mathf.RoundToInt(0.5f * SampleRate));
            Normalize(data, 0.7f);
            var clip = AudioClip.Create("Generated Crowd Ambience", data.Length, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Roar: brighter, louder noise with a fast swell, cheering flutter and a long tail.</summary>
        static AudioClip MakeRoar(System.Random rng, float seconds)
        {
            int n = Mathf.RoundToInt(seconds * SampleRate);
            var data = new float[n];
            var lp = new OnePole(2600f);
            var hp = new OnePole(250f);
            var lp2 = new OnePole(1200f);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                float attack = Mathf.Clamp01(t / 0.35f);
                float decay = Mathf.Exp(-Mathf.Max(0f, t - 0.8f) * 1.1f);
                float flutter = 0.8f + 0.2f * Mathf.Sin(t * 2f * Mathf.PI * 7.3f) * Mathf.Sin(t * 2f * Mathf.PI * 3.1f);
                float x = Noise(rng);
                float bright = hp.HighPass(lp.LowPass(x));
                float body = lp2.LowPass(x) * 0.8f;
                data[i] = (bright + body) * attack * decay * flutter;
            }
            Normalize(data, 0.9f);
            var clip = AudioClip.Create("Generated Crowd Roar", n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Applause: thousands of short noise "claps" at random times, thinning out at the end.</summary>
        static AudioClip MakeApplause(System.Random rng, float seconds)
        {
            int n = Mathf.RoundToInt(seconds * SampleRate);
            var data = new float[n];
            int clapLen = Mathf.RoundToInt(0.018f * SampleRate);
            float claps = 140f * seconds;
            for (int c = 0; c < claps; c++)
            {
                // more claps early, fewer towards the end
                float u = (float)rng.NextDouble();
                float tStart = Mathf.Pow(u, 1.6f) * (seconds - 0.05f);
                int start = Mathf.RoundToInt(tStart * SampleRate);
                float amp = 0.3f + (float)rng.NextDouble() * 0.7f;
                var hp = new OnePole(900f + (float)rng.NextDouble() * 1500f);
                for (int k = 0; k < clapLen && start + k < n; k++)
                {
                    float e = Mathf.Exp(-k / (clapLen * 0.25f));
                    data[start + k] += hp.HighPass(Noise(rng)) * e * amp;
                }
            }
            // fade the tail
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                data[i] *= Mathf.Clamp01((1f - t) * 3f);
            }
            Normalize(data, 0.85f);
            var clip = AudioClip.Create("Generated Crowd Applause", n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>Makes a seamless loop: the tail is blended into the head and dropped.</summary>
        static float[] LoopCrossfade(float[] d, int len)
        {
            var o = new float[d.Length - len];
            for (int i = 0; i < o.Length; i++)
            {
                if (i < len)
                {
                    float k = i / (float)len;
                    o[i] = d[i] * k + d[o.Length + i] * (1f - k);
                }
                else o[i] = d[i];
            }
            return o;
        }

        /// <summary>"Ooh": many detuned voices sliding down a little, through a soft vowel-like filter.</summary>
        static AudioClip MakeOoh(System.Random rng, float seconds)
        {
            int n = Mathf.RoundToInt(seconds * SampleRate);
            var data = new float[n];
            const int voices = 36;
            for (int v = 0; v < voices; v++)
            {
                float f0 = 150f + (float)rng.NextDouble() * 180f;
                float phase = (float)rng.NextDouble();
                float vib = 4f + (float)rng.NextDouble() * 2f;
                float start = (float)rng.NextDouble() * 0.12f;
                float amp = 0.5f + (float)rng.NextDouble() * 0.5f;
                for (int i = 0; i < n; i++)
                {
                    float t = i / (float)SampleRate - start;
                    if (t < 0f) continue;
                    float glide = 1f - 0.09f * Mathf.Clamp01(t / seconds);
                    float f = f0 * glide * (1f + 0.012f * Mathf.Sin(t * vib * 2f * Mathf.PI));
                    phase += f / SampleRate;
                    // rounded "oo" timbre: fundamental + a little second harmonic
                    float w = Mathf.Sin(phase * 2f * Mathf.PI) + 0.25f * Mathf.Sin(phase * 4f * Mathf.PI);
                    float env = Mathf.Clamp01(t / 0.18f) * Mathf.Exp(-Mathf.Max(0f, t - 0.35f) * 2.2f);
                    data[i] += w * env * amp;
                }
            }
            var lp = new OnePole(900f);
            for (int i = 0; i < n; i++) data[i] = lp.LowPass(data[i] + Noise(rng) * 0.08f * Mathf.Abs(data[i]));
            Normalize(data, 0.85f);
            var clip = AudioClip.Create("Generated Crowd Ooh", n, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static void Normalize(float[] d, float peak)
        {
            float max = 0.0001f;
            for (int i = 0; i < d.Length; i++) max = Mathf.Max(max, Mathf.Abs(d[i]));
            float g = peak / max;
            for (int i = 0; i < d.Length; i++) d[i] *= g;
        }
    }
}
