using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
using Microsoft.Xna.Framework.Audio;
namespace hammered;

public class AudioManager
{
    

    public ContentManager Content { get => _content; }
    ContentManager _content;

    public GameMain GameMain { get => _game; }
    private GameMain _game;
    private Dictionary<string, Song> _songs = new Dictionary<string, Song>();
    private Dictionary<string, SoundEffect> _soundEffects = new Dictionary<string, SoundEffect>();

    public float Volume;
    public AudioManager(Game game) {

        _game = (GameMain)game;
        _content = new ContentManager(GameMain.Services, "Content");
        MediaPlayer.IsRepeating = true;
        Volume = 0.1f;
        MediaPlayer.Volume = 0.1f;
    }
    public void LoadSong(string name)
    { Song loaded;
        if (_songs.TryGetValue(name, out loaded))
        {

        }
        else {
            loaded = Content.Load<Song>("Audio/" + name) ;
            _songs.Add(name, loaded);
        }
    }
    public void LoadSoundEffect(string name)
    {
        SoundEffect loaded;
        if (_soundEffects.TryGetValue(name, out loaded))
        {

        }
        else
        {
            loaded = Content.Load<SoundEffect>("Audio/" + name);
            _soundEffects.Add(name, loaded);
        }
    }

    public void PlaySong(string name, float volume) {
        Song loaded;
        if (_songs.TryGetValue(name, out loaded))
        {
            MediaPlayer.Play(_songs[name]);
            MediaPlayer.Volume = volume;
        }
        else { 
            
        }
    }

    public void PlaySoundEffect(string name)
    {
        SoundEffect loaded;
        if (_soundEffects.TryGetValue(name, out loaded))
        {
            loaded.Play();
        }
        else
        {

        }
    }
}