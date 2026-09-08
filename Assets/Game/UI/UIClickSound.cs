using UnityEngine;

namespace PacificCombat
{
    public sealed class UIClickSound : MonoBehaviour
    {
        static UIClickSound active;
        AudioSource source;
        AudioClip click;
        void Awake()
        {
            active = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.spatialBlend = 0; source.ignoreListenerPause = true;
            const int rate = 22050;
            var samples = new float[1102];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = i / (float)rate;
                samples[i] = Mathf.Sin(t * 2 * Mathf.PI * 720) * Mathf.Exp(-t * 100) * .22f;
            }
            click = AudioClip.Create("Cockpit switch click", samples.Length, 1, rate, false);
            click.SetData(samples, 0);
        }
        public static void Play()
        {
            if (active && active.source) active.source.PlayOneShot(active.click, GameSettings.Current.UIVolume);
        }
        void OnDestroy()
        {
            if (active == this) active = null;
            if (click) Destroy(click);
        }
    }
}
