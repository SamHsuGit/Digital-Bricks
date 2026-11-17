using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Unity.Multiplayer.GettingStarted.PlayerMovement
{
    /// <summary>
    /// A basic example of client authoritative movement. It works in both client-server and distributed-authority scenarios.
    /// If you want to modify this Script please copy it into your own project and add it to your Player Prefab.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(NetworkTransform))]
    public class PlayerMovementController : NetworkBehaviour, INetworkUpdateSystem
    {
        private InputAction _moveAction;
        private CharacterController _controller;
        private NetworkTransform _networkTransform;
        private const string k_InputActionName = "Move";

        /// <summary>
        /// Movement Speed
        /// </summary>
        [Tooltip("Movement Speed")]
        [Range(0.1f, 10.0f)] public float Speed = 5;

        /// <summary>
        /// Use post-spawn to determine which instance will check for player input.
        /// </summary>
        protected override void OnNetworkPostSpawn()
        {
            _controller = GetComponent<CharacterController>();
            // Get a reference to the PlayerInput that drives the character
            _moveAction = GetComponent<PlayerInput>()?.actions[k_InputActionName];
            _networkTransform  = GetComponent<NetworkTransform>();

            // To prevent from having to early exit and have every instance of
            // each player on all clients from invoking the primary Update method,
            // the motion authority registers for the Update stage by implementing
            // the INetworkUpdateSystem and registering with NetworkUpdateLoop.
            if (_networkTransform.CanCommitToTransform)
            {
                this.RegisterNetworkUpdate(NetworkUpdateStage.Update);
            }

            base.OnNetworkPostSpawn();
        }

        /// <summary>
        /// Cleaning up while depending upon spawned settings should be done when
        /// <see cref="NetworkBehaviour.OnNetworkPreDespawn"/> is invoked prior to
        /// actually running through the de-spawn process.
        /// </summary>
        public override void OnNetworkPreDespawn()
        {
            if (_networkTransform.CanCommitToTransform)
            {
                this.UnregisterNetworkUpdate(NetworkUpdateStage.Update);
            }

            base.OnNetworkPreDespawn();
        }

        /// <summary>
        /// Only invoked on the motion authority instance.
        /// The motion authority is dictated by the <see cref="NetworkTransform.AuthorityMode"/>
        /// property.
        /// </summary>
        /// <param name="updateStage">The update stage being invoked.</param>
        public void NetworkUpdate(NetworkUpdateStage updateStage)
        {
            _controller?.Move(GetPlayerMovement());
        }

        private Vector3 GetPlayerMovement()
        {
            var moveDirection = _moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

            return new Vector3(moveDirection.x, 0, moveDirection.y) * (Speed * Time.deltaTime);
        }
    }
}
