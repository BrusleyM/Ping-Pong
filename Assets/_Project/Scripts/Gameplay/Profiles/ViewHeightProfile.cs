using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    [CreateAssetMenu(menuName = "Ping Pong/View Height Profile", fileName = "ViewHeight")]
    public sealed class ViewHeightProfile : ScriptableObject
    {
        [SerializeField] ViewHeightTuning _tuning = ViewHeightTuning.Default;

        public ViewHeightTuning Tuning => _tuning;
    }
}
