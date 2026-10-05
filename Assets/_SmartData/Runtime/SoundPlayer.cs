using UnityEngine;

namespace SmartData.FindFake
{
    [DisallowMultipleComponent]
    public class SoundPlayer : MonoBehaviour
    {
        public AudioSource sfx;
        public AudioSource music;

        private SoundBlock cfg;

        public void Configure(SoundBlock settings)
        {
            cfg = settings;
            if (sfx != null)
            {
                sfx.playOnAwake = false;
                sfx.loop = false;
            }
            if (music == null) return;

            music.playOnAwake = false;
            music.loop = true;
            music.volume = settings.musicVolume * settings.masterVolume;
            if (music.clip != settings.music)
            {
                music.Stop();
                music.clip = settings.music;
            }
            if (!Application.isPlaying) return;
            if (settings.enabled && settings.music != null)
            {
                if (!music.isPlaying) music.Play();
            }
            else if (music.isPlaying)
            {
                music.Stop();
            }
        }

        public void Play(AudioClip clip)
        {
            if (cfg == null || !cfg.enabled || clip == null || sfx == null || !Application.isPlaying) return;
            sfx.PlayOneShot(clip, cfg.sfxVolume * cfg.masterVolume);
        }
    }
}
