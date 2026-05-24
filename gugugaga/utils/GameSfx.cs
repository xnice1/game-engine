using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace JumpKingClone.Core;

public sealed class GameSfx
{
    private readonly SoundEffect _jump;
    private readonly SoundEffect _land;
    private readonly SoundEffect _wallRecochet;
    private readonly SoundEffect _win;
    private readonly Song _bgm;

    public GameSfx(ContentManager content)
    {
        _jump = TryLoadSfx(content, "Sounds/jump");
        _land = TryLoadSfx(content, "Sounds/land");
        _wallRecochet = TryLoadSfx(content, "Sounds/wall-recochet");
        _win = TryLoadSfx(content, "Sounds/win-sound");
        _bgm = TryLoadSong(content, "Sounds/bgm background music");
    }

    public void StartBgm(float volume = 0.35f)
    {
        if (_bgm == null) return;
        MediaPlayer.IsRepeating = true;
        MediaPlayer.Volume = volume;
        MediaPlayer.Play(_bgm);
    }

    public void PlayJump() => Play(_jump, 0.65f);

    public void PlayLand() => Play(_land, 0.5f);

    public void PlayWallRecochet() => Play(_wallRecochet, 0.45f);

    public void PlayWin() => Play(_win, 0.75f);

    private static SoundEffect TryLoadSfx(ContentManager content, string assetName)
    {
        try { return content.Load<SoundEffect>(assetName); }
        catch { return null; }
    }

    private static Song TryLoadSong(ContentManager content, string assetName)
    {
        try { return content.Load<Song>(assetName); }
        catch { return null; }
    }

    private static void Play(SoundEffect effect, float volume)
    {
        if (effect == null) return;
        effect.Play(volume, 0f, 0f);
    }
}
