using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace CoinGame.Stage
{
    public sealed class GrassPatch : MonoBehaviour
    {
        private static readonly List<GrassPatch> _activePatches = new List<GrassPatch>();
        private static readonly int _opacityId = Shader.PropertyToID("_GrassOpacity");
        [SerializeField]
        private BoxCollider _volume;
        [SerializeField]
        private Renderer[] _renderers;
        [SerializeField]
        private GrassVisualSettings _settings;
        private MaterialPropertyBlock _properties;
        private float _opacity = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry()
        {
            _activePatches.Clear();
        }

        private void Awake()
        {
            _properties = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            _activePatches.Add(this);
        }

        private void OnDisable()
        {
            _activePatches.Remove(this);
            _opacity = 1f;
            if (_properties != null)
            {
                ApplyOpacity();
            }
        }

        private void Update()
        {
            float target = GetTargetOpacity();
            float speed = (1f - _settings.OccupiedOpacity) / _settings.FadeSeconds;
            float opacity = Mathf.MoveTowards(_opacity, target, Time.unscaledDeltaTime * speed);
            if (_opacity == opacity)
            {
                return;
            }

            _opacity = opacity;
            ApplyOpacity();
        }

        private float GetTargetOpacity()
        {
            // InGame standalone startup loads Persistent asynchronously.
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || !NetworkManager.Singleton.IsClient)
            {
                return 1f;
            }

            NetworkObject local = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
            if (local == null || !Contains(local.transform.position))
            {
                return 1f;
            }

            return _settings.OccupiedOpacity;
        }

        private void ApplyOpacity()
        {
            foreach (Renderer renderer in _renderers)
            {
                // Scene teardown can destroy child renderers before this component.
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(_properties);
                _properties.SetFloat(_opacityId, _opacity);
                renderer.SetPropertyBlock(_properties);
            }
        }

        public bool Contains(Vector3 worldPosition)
        {
            Vector3 position = _volume.transform.InverseTransformPoint(worldPosition) - _volume.center;
            Vector3 halfSize = _volume.size * 0.5f;
            return Mathf.Abs(position.x) <= halfSize.x &&
                Mathf.Abs(position.y) <= halfSize.y &&
                Mathf.Abs(position.z) <= halfSize.z;
        }

        public static bool ContainsAny(Vector3 worldPosition)
        {
            foreach (GrassPatch patch in _activePatches)
            {
                if (patch.Contains(worldPosition))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
