public interface IAudioController
{
	bool MusicEnabled { get; set; }
	bool SfxEnabled { get; set; }

	void Play(SoundType type);
}
