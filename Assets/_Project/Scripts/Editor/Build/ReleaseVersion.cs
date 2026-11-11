using System;

namespace PingPong.EditorTools.Build
{
    /// <summary>The kind of build, which decides how the version name moves.</summary>
    public enum ReleaseKind { Develop, Staging, Production }

    /// <summary>
    /// Version name x.y.z: a Develop build bumps z; Staging bumps y and resets z; Production bumps x and
    /// resets y and z. Every build also bumps the bundle (version) code by one.
    /// </summary>
    public readonly struct ReleaseVersion : IEquatable<ReleaseVersion>
    {
        public readonly int Major, Minor, Patch, Code;

        public ReleaseVersion(int major, int minor, int patch, int code)
        {
            Major = major; Minor = minor; Patch = patch; Code = code;
        }

        /// <summary>Reads "x.y.z" (missing parts are 0) and the bundle code.</summary>
        public static ReleaseVersion Parse(string name, int code)
        {
            string[] parts = (name ?? "").Split('.');
            int Part(int i) => i < parts.Length && int.TryParse(parts[i], out int v) && v >= 0 ? v : 0;
            return new ReleaseVersion(Part(0), Part(1), Part(2), code);
        }

        public ReleaseVersion Next(ReleaseKind kind) => kind switch
        {
            ReleaseKind.Develop => new ReleaseVersion(Major, Minor, Patch + 1, Code + 1),
            ReleaseKind.Staging => new ReleaseVersion(Major, Minor + 1, 0, Code + 1),
            ReleaseKind.Production => new ReleaseVersion(Major + 1, 0, 0, Code + 1),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        public string Name => $"{Major}.{Minor}.{Patch}";

        public bool Equals(ReleaseVersion o) => Major == o.Major && Minor == o.Minor && Patch == o.Patch && Code == o.Code;
        public override bool Equals(object obj) => obj is ReleaseVersion o && Equals(o);
        public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Code);
        public override string ToString() => $"{Name} ({Code})";
    }
}
