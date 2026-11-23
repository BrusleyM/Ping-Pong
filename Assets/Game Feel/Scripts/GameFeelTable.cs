using PingPong.Core;
using PingPong.Gameplay;
using UnityEngine;

namespace PingPong.GameFeel
{
    /// <summary>
    /// Table helpers for the game-feel scripts, built on the project's <see cref="PingPongTable"/> and
    /// <see cref="TableFrame"/>. Sides are ints in the game-feel scripts: South (-1) is the Near end (the
    /// local player), North (+1) the Far end.
    /// </summary>
    public static class GameFeelTable
    {
        public const int South = -1;
        public const int North = 1;

        /// <summary>The table the match is played on (set by GameFeelBootstrap; the first table found otherwise).</summary>
        public static PingPongTable Main
        {
            get
            {
                if (_main == null) _main = Object.FindFirstObjectByType<PingPongTable>();
                return _main;
            }
            set => _main = value;
        }

        static PingPongTable _main;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _main = null;

        public static int ToInt(Side side) => side == Side.Far ? North : South;
        public static Side ToSide(int side) => side > 0 ? Side.Far : Side.Near;

        /// <summary>Which end of 'table' a world position is on (South = Near, North = Far).</summary>
        public static int SideOf(PingPongTable table, Vector3 world)
        {
            if (table == null) return world.z >= 0f ? North : South;
            return ToInt(table.Frame.SideOf(world));
        }

        /// <summary>Rotation that faces along the table (forward = from Near to Far).</summary>
        public static Quaternion Rotation(in TableFrame f) => Quaternion.LookRotation(f.Forward, Vector3.up);

        /// <summary>Centre of the playing surface, or the origin if there is no table.</summary>
        public static Vector3 Center(PingPongTable table) => table != null ? table.Frame.SurfaceCenter : Vector3.zero;
    }
}
