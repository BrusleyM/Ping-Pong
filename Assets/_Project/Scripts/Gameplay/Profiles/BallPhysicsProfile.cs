using PingPong.Core;
using UnityEngine;

namespace PingPong.Gameplay
{
    /// <summary>Ball physics. The live ball and every prediction use the model built from this.</summary>
    [CreateAssetMenu(menuName = "Ping Pong/Ball Physics Profile", fileName = "BallPhysics")]
    public sealed class BallPhysicsProfile : ScriptableObject
    {
        [SerializeField] BallPhysicsSettings _settings = BallPhysicsSettings.Regulation;

        BallFlightModel _model;

        public BallPhysicsSettings Settings => _settings;

        /// <summary>Flight model for these settings. Rebuilt when the asset is edited.</summary>
        public BallFlightModel Model => _model ??= new BallFlightModel(_settings);

        void OnValidate() => _model = null;
    }
}
