using System;
using UnityEngine;

public enum SoundType
{
	Click,
	Purchase,
	Upgrade,
	Sell,
	Error,
	Claim,
	Explosion,
	Unlock
}

/// <summary>
/// Generates the game's sound effects and music loop in code, so no audio files are needed.
/// Clips set in GameSettings audio replace the generated ones.
/// </summary>
public static class SoundSynth
{
	private const int SampleRate = 22050;

	// Music: Am, F, C, G pads, each chord fading into the next, looping seamlessly
	private const float ChordLength = 8f;
	private const float ChordFade = 2.5f;
	private static readonly float[][] Chords =
	{
		new[] { 110.00f, 164.81f, 220.00f, 261.63f },
		new[] { 87.31f, 130.81f, 174.61f, 220.00f },
		new[] { 130.81f, 196.00f, 261.63f, 329.63f },
		new[] { 98.00f, 146.83f, 196.00f, 246.94f }
	};

	public static AudioClip Create(SoundType type)
	{
		switch (type)
		{
			case SoundType.Click:
				return Build("Click", 0.05f, t => Tone(t, 1400f) * Decay(t, 60f) * 0.3f);
			case SoundType.Purchase:
				return Build("Purchase", 0.35f, t => Note(t, 0f, 987.77f, 18f) + Note(t, 0.08f, 1318.51f, 10f));
			case SoundType.Upgrade:
				return Build("Upgrade", 0.4f, t => Note(t, 0f, 523.25f, 20f) + Note(t, 0.07f, 659.25f, 20f) + Note(t, 0.14f, 783.99f, 12f));
			case SoundType.Sell:
				return Build("Sell", 0.45f, t => Note(t, 0f, 1567.98f, 22f) + Note(t, 0.06f, 2093.00f, 22f) + Note(t, 0.12f, 2637.02f, 14f) + Noise(t) * Decay(t, 40f) * 0.1f);
			case SoundType.Error:
				return Build("Error", 0.22f, t => Square(t, 180f - 120f * t) * Decay(t, 12f) * 0.35f);
			case SoundType.Claim:
				return Build("Claim", 0.9f, t => Note(t, 0f, 783.99f, 8f) + Note(t, 0.08f, 987.77f, 8f) + Note(t, 0.16f, 1174.66f, 7f) + Note(t, 0.24f, 1567.98f, 5f));
			case SoundType.Explosion:
				return Build("Explosion", 0.8f, LowNoise);
			case SoundType.Unlock:
				return Build("Unlock", 1.2f, t => (Note(t, 0f, 523.25f, 3f) + Note(t, 0.1f, 659.25f, 3f) + Note(t, 0.2f, 783.99f, 3f) + Note(t, 0.3f, 1046.50f, 2.5f)) * 0.8f);
			default:
				throw new ArgumentOutOfRangeException(nameof(type));
		}
	}

	/// <summary>Music loop samples. Pure maths, so it can run on a worker thread.</summary>
	public static float[] RenderMusic()
	{
		float loopLength = ChordLength * Chords.Length;
		return Render(loopLength, t =>
		{
			float sample = 0f;
			for (int i = 0; i < Chords.Length; i++)
			{
				// Time since this chord started, wrapped so the last chord fades into the first
				float local = Mathf.Repeat(t - i * ChordLength, loopLength);
				if (local > ChordLength + ChordFade)
				{
					continue;
				}

				float envelope = Mathf.Clamp01(local / ChordFade) * Mathf.Clamp01((ChordLength + ChordFade - local) / ChordFade);
				foreach (float frequency in Chords[i])
				{
					// Slow vibrato and a soft octave keep the pad from sounding static
					float vibrato = 1f + 0.002f * Mathf.Sin(2f * Mathf.PI * 0.3f * t + frequency);
					sample += envelope * (Tone(t, frequency * vibrato) + 0.25f * Tone(t, frequency * 2f * vibrato));
				}
			}

			return sample * 0.09f;
		}, true);
	}

	public static AudioClip ToClip(string name, float[] samples)
	{
		AudioClip clip = AudioClip.Create(name, samples.Length, 1, SampleRate, false);
		clip.SetData(samples, 0);
		return clip;
	}

	private static AudioClip Build(string name, float duration, Func<float, float> wave)
	{
		return ToClip(name, Render(duration, wave, false));
	}

	private static float[] Render(float duration, Func<float, float> wave, bool loop)
	{
		int length = Mathf.CeilToInt(duration * SampleRate);
		float[] samples = new float[length];
		for (int i = 0; i < length; i++)
		{
			float t = (float)i / SampleRate;
			float sample = wave(t);

			// Short fades stop clicks at the start and end of one-shot sounds
			if (!loop)
			{
				sample *= Mathf.Clamp01(i / 40f) * Mathf.Clamp01((length - i) / 200f);
			}

			samples[i] = Mathf.Clamp(sample, -1f, 1f);
		}

		return samples;
	}

	/// <summary>A bell-like note starting at the given time.</summary>
	private static float Note(float t, float start, float frequency, float decay)
	{
		float local = t - start;
		if (local < 0f)
		{
			return 0f;
		}

		return (Tone(local, frequency) + 0.3f * Tone(local, frequency * 2f)) * Decay(local, decay) * 0.3f;
	}

	private static float Tone(float t, float frequency)
	{
		return Mathf.Sin(2f * Mathf.PI * frequency * t);
	}

	private static float Square(float t, float frequency)
	{
		return Mathf.Sign(Tone(t, frequency)) * 0.6f + Tone(t, frequency) * 0.4f;
	}

	private static float Decay(float t, float speed)
	{
		return Mathf.Exp(-t * speed);
	}

	// Cheap repeatable noise, so clips sound the same every run
	private static float Noise(float t)
	{
		float x = Mathf.Sin(t * 12.9898f * SampleRate) * 43758.5453f;
		return (x - Mathf.Floor(x)) * 2f - 1f;
	}

	private static float lowNoiseState;

	// Noise smoothed more and more over time, like a rumbling blast
	private static float LowNoise(float t)
	{
		if (t == 0f)
		{
			lowNoiseState = 0f;
		}

		float smoothing = Mathf.Lerp(0.5f, 0.03f, Mathf.Clamp01(t / 0.5f));
		lowNoiseState += (Noise(t) - lowNoiseState) * smoothing;
		return lowNoiseState * Decay(t, 5f) * 0.9f;
	}
}
