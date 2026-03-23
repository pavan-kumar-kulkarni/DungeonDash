using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Collections.Generic;

namespace DungeonDash
{
    public static class SoundFactory
    {
        private static Dictionary<string, SoundEffect> _cache = new();

        // Public API: Get procedural sound by name
        public static SoundEffect Get(string name)
        {
            if (_cache.TryGetValue(name, out var sfx))
                return sfx;
            sfx = name switch
            {
                "move" => CreateMove(),
                "attack" => CreateAttack(),
                "pickup" => CreatePickup(),
                "levelup" => CreateLevelUp(),
                "death" => CreateDeath(),
                "stairs" => CreateStairs(),
                _ => null
            };
            if (sfx != null)
                _cache[name] = sfx;
            return sfx;
        }

        // Procedural SFX: Simple waveform synthesis
        private static SoundEffect CreateMove()
        {
            return CreateBeep(220, 0.07f, 0.2f, 0.1f);
        }
        private static SoundEffect CreateAttack()
        {
            return CreateBeep(440, 0.09f, 0.3f, 0.2f, true);
        }
        private static SoundEffect CreatePickup()
        {
            return CreateBeep(880, 0.08f, 0.4f, 0.2f);
        }
        private static SoundEffect CreateLevelUp()
        {
            return CreateBeep(660, 0.13f, 0.5f, 0.3f, false, true);
        }
        private static SoundEffect CreateDeath()
        {
            return CreateBeep(110, 0.18f, 0.2f, 0.5f, true, false, true);
        }
        private static SoundEffect CreateStairs()
        {
            return CreateBeep(330, 0.12f, 0.3f, 0.2f, false, true);
        }

        // Core: Generate a beep with optional vibrato, pitch slide, or noise
        private static SoundEffect CreateBeep(float freq, float duration, float volume, float vibrato = 0f, bool slide = false, bool arpeggio = false, bool noise = false)
        {
            int sampleRate = 22050;
            int samples = (int)(sampleRate * duration);
            float[] data = new float[samples];
            float phase = 0f;
            float slideAmount = slide ? freq * 0.5f : 0f;
            float arpStep = arpeggio ? freq * 0.5f : 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = i / (float)sampleRate;
                float f = freq;
                if (slide) f += slideAmount * (i / (float)samples);
                if (arpeggio) f += arpStep * (float)Math.Sin(8 * Math.PI * t);
                float v = vibrato > 0f ? (float)Math.Sin(2 * Math.PI * vibrato * t) * 0.1f : 0f;
                float sample = noise ? (float)(Random.Shared.NextDouble() * 2 - 1) : (float)Math.Sin(2 * Math.PI * f * t + v);
                data[i] = sample * volume * (1f - (i / (float)samples)); // fade out
            }
            byte[] buffer = new byte[samples * 2];
            for (int i = 0; i < samples; i++)
            {
                short s = (short)(Math.Clamp(data[i], -1f, 1f) * short.MaxValue);
                buffer[i * 2] = (byte)(s & 0xff);
                buffer[i * 2 + 1] = (byte)((s >> 8) & 0xff);
            }
            return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
        }
    }
}
