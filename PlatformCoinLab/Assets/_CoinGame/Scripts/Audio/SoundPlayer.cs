using UnityEngine;

namespace CoinGame.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class SoundPlayer : MonoBehaviour
    {
        [SerializeField]
        private AudioClip _clip;
        [SerializeField]
        private bool _playOnEnable;
        private AudioSource _source;

        private void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
        }

        private void OnEnable()
        {
            if (_playOnEnable)
            {
                Play();
            }
        }

        public void Play()
        {
            if (_clip == null)
            {
                return;
            }

            _source.PlayOneShot(_clip);
        }
    }
}
