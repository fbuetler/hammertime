using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;

namespace hammered;

public class AudioManager
{

    public ContentManager Content { get => _content; }
    private ContentManager _content;

    public GameMain GameMain { get => _game; }
    private GameMain _game;

    private Dictionary<string, Song> _songs = new Dictionary<string, Song>();
    private Dictionary<string, SoundEffect> _soundEffects = new Dictionary<string, SoundEffect>();

    public float Volume { get => MediaPlayer.Volume; }
    public float VolumeSoundEffects { get => SoundEffect.MasterVolume; }

    private const string audioRootPath = "Audio/";

    private const float defaultVolume = 0.1f;
    private const float defaultEffectVolume = 0.05f;

    public AudioManager(Game game)
    {
        _game = (GameMain)game;

        _content = new ContentManager(GameMain.Services, "Content");

        MediaPlayer.Volume = defaultVolume;
        MediaPlayer.IsRepeating = true;
        SoundEffect.MasterVolume = defaultEffectVolume;
    }

    public void LoadSong(string name)
    {
        Song loaded;
        if (!_songs.TryGetValue(name, out loaded))
        {
            loaded = Content.Load<Song>(audioRootPath + name);
            _songs.Add(name, loaded);
        }
    }

    public void LoadSoundEffect(string name)
    {
        SoundEffect loaded;
        if (!_soundEffects.TryGetValue(name, out loaded))
        {
            loaded = Content.Load<SoundEffect>(audioRootPath + name);
            _soundEffects.Add(name, loaded);
        }
    }

    // TODO (fbuetler) @fred do we really need to expose volume here?
    //we don't but if its ever needed we have both now.
    public void PlaySong(string name)
    {
        PlaySong(name, MediaPlayer.Volume, TimeSpan.FromSeconds(0));
    }

    public void PlaySong(string name, float volume)
    {
        PlaySong(name, volume, TimeSpan.FromSeconds(0));
    }

    public void PlaySong(string name, float volume, TimeSpan startPosition)
    {
        Song loaded;
        if (_songs.TryGetValue(name, out loaded))
        {
            MediaPlayer.Play(loaded, startPosition);
            MediaPlayer.Volume = volume;
        }
    }

    public void PlaySoundEffect(string name)
    {
        SoundEffect loaded;
        if (_soundEffects.TryGetValue(name, out loaded))
        {
            loaded.Play();
        }
    }

}