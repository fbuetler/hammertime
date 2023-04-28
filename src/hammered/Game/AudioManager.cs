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

    public float SongVolume { get => MediaPlayer.Volume; set => MediaPlayer.Volume = value; }

    public float SoundEffectVolume { get => SoundEffect.MasterVolume; set => SoundEffect.MasterVolume = value; }

    private const string audioRootPath = "Audio/";

    private const float defaultVolume = 0.4f;
    private const float defaultEffectVolume = 0.4f;

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

    public void PlaySong(string name)
    {
        PlaySong(name, TimeSpan.FromSeconds(0));
    }

    public void PlaySong(string name, TimeSpan startPosition)
    {
        Song loaded;
        if (_songs.TryGetValue(name, out loaded))
        {
            MediaPlayer.Play(loaded, startPosition);
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

    public void Stop()
    {
        MediaPlayer.Stop();
    }
}