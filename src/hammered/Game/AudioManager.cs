using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;
namespace hammered;

public class AudioManager
{
    public Song slow;
    public Song fast;

    public ContentManager Content { get => _content; }
    ContentManager _content;

    public GameMain GameMain { get => _game; }
    private GameMain _game;

    public AudioManager(Game game) {

        _game = (GameMain)game;
        _content = new ContentManager(GameMain.Services, "Content");
        MediaPlayer.IsRepeating = true;
        slow = Content.Load<Song>("Audio/MusicMapSlow");
        fast = Content.Load<Song>("Audio/MusicMapFast");
        MediaPlayer.Volume = 0.1f;
        MediaPlayer.Play(slow);
    }
}