using System;
using System.Collections.Generic;
using JumpKingClone.Components;
using KripakEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyGame;

public class AirHockeyScene : Scene
{
    private Texture2D _pixel;
    private Texture2D _bgTex, _playerTex, _enemyTex, _puckTex;
    private List<Entity> _entities = new();

    // Layout
    private const int W = 800, H = 500;
    private const int WallV = 58;  // left/right frame thickness
    private const int WallH = 42;  // top/bottom frame thickness
    private const int Wall = WallH; // used for generic clamping (smallest wall)
    private const int GoalH = 130;
    private const int GoalTop = (H - GoalH) / 2;
    private const int GoalBot = GoalTop + GoalH;

    private const int PuckR = 12, PaddleR = 26;
    private const int PuckSize = PuckR * 2, PaddleSize = PaddleR * 2;

    private const float PlayerSpeed = 5.5f, BotSpeed = 2.0f;
    private const float MaxPuckSpeed = 13f, Friction = 0.9985f;

    // Match rules
    private const int WinScore = 5;
    private const float MatchDuration = 120f;
    private const float PauseDuration = 1.2f;

    // State machine
    private enum GameState { Playing, GoalPause, SuddenDeath, GameOver }
    private GameState _state = GameState.Playing;

    private float _timeRemaining = MatchDuration;
    private float _pauseTimer;
    private int _playerScore, _botScore;
    private bool _botLastScored;
    private bool _playerWon;
    private float _flashTimer;

    public AirHockeyScene(Texture2D pixel, Texture2D bgTex, Texture2D playerTex, Texture2D enemyTex, Texture2D puckTex)
    {
        _pixel = pixel;
        _bgTex = bgTex;
        _playerTex = playerTex;
        _enemyTex = enemyTex;
        _puckTex = puckTex;
        SpawnEntities();
    }

    // ── entity setup ─────────────────────────────────────────────────────────

    private void SpawnEntities()
    {
        _entities.Clear();

        Entity player = new Entity();
        player.AddComponent(new TransformComponent(new Vector2(80, H / 2f - PaddleR), PaddleSize, PaddleSize));
        player.AddComponent(new PaddleComponent(PlayerSpeed, isPlayer: true));
        player.AddComponent(new RenderComponent(Color.DodgerBlue));
        player.AddComponent(new SpriteComponent(_playerTex));
        _entities.Add(player);

        Entity bot = new Entity();
        bot.AddComponent(new TransformComponent(new Vector2(W - 80 - PaddleSize, H / 2f - PaddleR), PaddleSize, PaddleSize));
        bot.AddComponent(new PaddleComponent(BotSpeed, isPlayer: false));
        bot.AddComponent(new RenderComponent(Color.OrangeRed));
        bot.AddComponent(new SpriteComponent(_enemyTex));
        _entities.Add(bot);

        Entity puck = new Entity();
        puck.AddComponent(new TransformComponent(new Vector2(W / 2f - PuckR, H / 2f - PuckR), PuckSize, PuckSize));
        puck.AddComponent(new PuckComponent(new Vector2(-3.5f, 1.5f)));
        puck.AddComponent(new RenderComponent(Color.WhiteSmoke));
        puck.AddComponent(new SpriteComponent(_puckTex));
        _entities.Add(puck);
    }

    private Entity Puck() { foreach (var e in _entities) if (e.HasComponent<PuckComponent>()) return e; return null; }
    private Entity Player() { foreach (var e in _entities) { var p = e.GetComponent<PaddleComponent>(); if (p?.IsPlayer == true) return e; } return null; }
    private Entity Bot() { foreach (var e in _entities) { var p = e.GetComponent<PaddleComponent>(); if (p != null && !p.IsPlayer) return e; } return null; }

    // ── update ───────────────────────────────────────────────────────────────

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _flashTimer += dt;

        if (_state == GameState.GameOver)
        {
            if (Input.Pressed(Keys.Space) || Input.Pressed(Keys.Enter))
                FullReset();
            return;
        }

        if (_state == GameState.GoalPause)
        {
            _pauseTimer -= dt;
            if (_pauseTimer <= 0)
            {
                if (CheckWin()) return;
                if (_timeRemaining <= 0)
                {
                    if (_playerScore == _botScore)
                        _state = GameState.SuddenDeath;
                    else
                    {
                        _playerWon = _playerScore > _botScore;
                        _state = GameState.GameOver;
                        return;
                    }
                }
                else
                    _state = GameState.Playing;
                ResetPositions();
            }
            return;
        }

        // Tick match clock only while Playing (clock stops during GoalPause)
        if (_state == GameState.Playing)
        {
            _timeRemaining -= dt;
            if (_timeRemaining <= 0)
            {
                _timeRemaining = 0;
                if (_playerScore == _botScore)
                    _state = GameState.SuddenDeath;
                else
                {
                    _playerWon = _playerScore > _botScore;
                    _state = GameState.GameOver;
                    return;
                }
            }
        }

        // Gameplay (Playing or SuddenDeath)
        var puckT = Puck().GetComponent<TransformComponent>();
        var puckP = Puck().GetComponent<PuckComponent>();

        PlayerInput(Player());
        BotAI(Bot(), puckT, puckP);

        puckP.Velocity *= Friction;
        puckT.Position += puckP.Velocity;

        WallCollision(puckT, puckP);
        PaddleCollision(Player().GetComponent<TransformComponent>(), Player().GetComponent<PaddleComponent>(), puckT, puckP);
        PaddleCollision(Bot().GetComponent<TransformComponent>(), Bot().GetComponent<PaddleComponent>(), puckT, puckP);
    }

    private bool CheckWin()
    {
        if (_playerScore >= WinScore) { _playerWon = true; _state = GameState.GameOver; return true; }
        if (_botScore >= WinScore) { _playerWon = false; _state = GameState.GameOver; return true; }
        return false;
    }

    private void FullReset()
    {
        _playerScore = 0;
        _botScore = 0;
        _timeRemaining = MatchDuration;
        _state = GameState.Playing;
        _flashTimer = 0;
        SpawnEntities();
    }

    // ── systems ──────────────────────────────────────────────────────────────

    private void PlayerInput(Entity player)
    {
        var t = player.GetComponent<TransformComponent>();
        var pad = player.GetComponent<PaddleComponent>();
        Vector2 prev = t.Position;

        if (Input.Down(Keys.W)) t.Position.Y -= pad.Speed;
        if (Input.Down(Keys.S)) t.Position.Y += pad.Speed;
        if (Input.Down(Keys.A)) t.Position.X -= pad.Speed;
        if (Input.Down(Keys.D)) t.Position.X += pad.Speed;

        t.Position.X = MathHelper.Clamp(t.Position.X, WallV, W / 2 - PaddleSize - 4);
        t.Position.Y = MathHelper.Clamp(t.Position.Y, WallH, H - WallH - PaddleSize);
        pad.Velocity = t.Position - prev;
    }

    private void BotAI(Entity bot, TransformComponent puckT, PuckComponent puckP)
    {
        var t = bot.GetComponent<TransformComponent>();
        var pad = bot.GetComponent<PaddleComponent>();
        Vector2 prev = t.Position;

        Vector2 puckCenter = puckT.Position + new Vector2(PuckR);
        Vector2 botCenter = t.Position + new Vector2(PaddleR);

        bool puckStuck = Math.Abs(puckP.Velocity.X) < 1.0f && puckCenter.X > W / 2f;

        // Y: track puck directly when stuck, otherwise use prediction
        float targetY = puckStuck
            ? puckCenter.Y - PaddleR
            : PredictY(puckCenter, puckP.Velocity, botCenter.X) - PaddleR;
        t.Position.Y += MathHelper.Clamp(targetY - t.Position.Y, -pad.Speed, pad.Speed);

        // X: chase puck to unstick it, advance to meet it, or defend
        float targetX = puckStuck
            ? MathHelper.Clamp(puckCenter.X - PaddleR, W / 2f + 10, W - WallV - PaddleSize - 10)
            : (puckCenter.X > W / 2f && puckP.Velocity.X > 0.5f)
                ? MathHelper.Clamp(puckCenter.X - PaddleSize * 1.8f, W / 2f + 10, W - WallV - PaddleSize - 10)
                : W - WallV - PaddleSize - 20;

        float dx = targetX - t.Position.X;
        t.Position.X += MathHelper.Clamp(dx, -pad.Speed, pad.Speed);

        t.Position.X = MathHelper.Clamp(t.Position.X, W / 2, W - WallV - PaddleSize);
        t.Position.Y = MathHelper.Clamp(t.Position.Y, WallH, H - WallH - PaddleSize);
        pad.Velocity = t.Position - prev;
    }

    // Predict puck Y at targetX, simulating wall reflections
    private float PredictY(Vector2 pos, Vector2 vel, float targetX)
    {
        if (Math.Abs(vel.X) < 0.01f) return H / 2f;
        float t = (targetX - pos.X) / vel.X;
        if (t < 0) return H / 2f;

        float y = pos.Y + vel.Y * t;
        float minY = WallH + PuckR, maxY = H - WallH - PuckR;
        float range = maxY - minY;
        if (range <= 0) return H / 2f;

        y -= minY;
        y = y % (range * 2);
        if (y < 0) y += range * 2;
        if (y > range) y = range * 2 - y;
        return y + minY;
    }

    private void WallCollision(TransformComponent puckT, PuckComponent puckP)
    {
        Vector2 pos = puckT.Position;
        Vector2 vel = puckP.Velocity;

        if (pos.Y < WallH) { pos.Y = WallH; vel.Y = Math.Abs(vel.Y); }
        if (pos.Y + PuckSize > H - WallH) { pos.Y = H - WallH - PuckSize; vel.Y = -Math.Abs(vel.Y); }

        if (pos.X < WallV)
        {
            bool inGoal = pos.Y + PuckSize > GoalTop && pos.Y < GoalBot;
            if (!inGoal) { pos.X = WallV; vel.X = Math.Abs(vel.X); }
            else if (pos.X + PuckSize < 0) ScoreGoal(scoredByBot: true);
        }

        if (pos.X + PuckSize > W - WallV)
        {
            bool inGoal = pos.Y + PuckSize > GoalTop && pos.Y < GoalBot;
            if (!inGoal) { pos.X = W - WallV - PuckSize; vel.X = -Math.Abs(vel.X); }
            else if (pos.X > W) ScoreGoal(scoredByBot: false);
        }

        puckT.Position = pos;
        puckP.Velocity = vel;
    }

    private void PaddleCollision(TransformComponent padT, PaddleComponent pad, TransformComponent puckT, PuckComponent puckP)
    {
        Vector2 puckCenter = puckT.Position + new Vector2(PuckR);
        Vector2 padCenter = padT.Position + new Vector2(PaddleR);
        float combR = PuckR + PaddleR;

        Vector2 delta = puckCenter - padCenter;
        float dist = delta.Length();
        if (dist >= combR || dist < 0.001f) return;

        Vector2 normal = delta / dist;
        puckT.Position = padCenter + normal * combR - new Vector2(PuckR);

        Vector2 relVel = puckP.Velocity - pad.Velocity;
        float approach = Vector2.Dot(relVel, normal);
        if (approach >= 0) return;

        puckP.Velocity -= (1 + 1.08f) * approach * normal;
        puckP.Velocity += pad.Velocity * 0.6f;

        float spd = puckP.Velocity.Length();
        if (spd > MaxPuckSpeed)
            puckP.Velocity = puckP.Velocity / spd * MaxPuckSpeed;
    }

    private void ScoreGoal(bool scoredByBot)
    {
        if (_state == GameState.GoalPause || _state == GameState.GameOver) return;
        if (scoredByBot) _botScore++; else _playerScore++;
        _botLastScored = scoredByBot;
        _state = GameState.GoalPause;
        _pauseTimer = PauseDuration;
        Puck().GetComponent<PuckComponent>().Velocity = Vector2.Zero;
    }

    private void ResetPositions()
    {
        var puckT = Puck().GetComponent<TransformComponent>();
        var puckP = Puck().GetComponent<PuckComponent>();
        puckT.Position = new Vector2(W / 2f - PuckR, H / 2f - PuckR);
        puckP.Velocity = _botLastScored ? new Vector2(3f, -1.5f) : new Vector2(-3f, 1.5f);

        Player().GetComponent<TransformComponent>().Position = new Vector2(80, H / 2f - PaddleR);
        Bot().GetComponent<TransformComponent>().Position = new Vector2(W - 80 - PaddleSize, H / 2f - PaddleR);
    }

    // ── draw ─────────────────────────────────────────────────────────────────

    public override void Draw(SpriteBatch spriteBatch)
    {
        spriteBatch.Draw(_bgTex, new Rectangle(0, 0, W, H), Color.White);
        DrawRink(spriteBatch);
        DrawTimerBar(spriteBatch);
        DrawScore(spriteBatch);

        foreach (var entity in _entities)
        {
            if (!entity.HasComponent<TransformComponent>()) continue;
            var t = entity.GetComponent<TransformComponent>();

            if (entity.HasComponent<SpriteComponent>())
                spriteBatch.Draw(entity.GetComponent<SpriteComponent>().Texture, t.Bounds, Color.White);
            else if (entity.HasComponent<RenderComponent>())
                spriteBatch.Draw(_pixel, t.Bounds, entity.GetComponent<RenderComponent>().DefaultColor);
        }

        if (_state == GameState.SuddenDeath)
            DrawSuddenDeathOverlay(spriteBatch);

        if (_state == GameState.GameOver)
            DrawGameOver(spriteBatch);
    }

    private void DrawRink(SpriteBatch spriteBatch)
    {
        if (_state != GameState.GoalPause) return;

        float alpha = Math.Min(1f, _pauseTimer / PauseDuration);
        if (_botLastScored)
            spriteBatch.Draw(_pixel, new Rectangle(0, GoalTop, WallV + 8, GoalH), new Color(30, 200, 80, (int)(alpha * 200)));
        else
            spriteBatch.Draw(_pixel, new Rectangle(W - WallV - 8, GoalTop, WallV + 8, GoalH), new Color(200, 80, 30, (int)(alpha * 200)));
    }

    private void DrawTimerBar(SpriteBatch spriteBatch)
    {
        if (_state == GameState.SuddenDeath || _state == GameState.GameOver) return;

        float fraction = _timeRemaining / MatchDuration;
        int barW = (int)(fraction * (W - WallV * 2));

        spriteBatch.Draw(_pixel, new Rectangle(WallV, WallH / 2 - 2, W - WallV * 2, 4), new Color(25, 25, 25));

        if (barW > 0)
        {
            Color barColor = fraction > 0.5f
                ? Color.Lerp(Color.Yellow, Color.LimeGreen, (fraction - 0.5f) * 2f)
                : Color.Lerp(Color.Red, Color.Yellow, fraction * 2f);
            spriteBatch.Draw(_pixel, new Rectangle(WallV, WallH / 2 - 2, barW, 4), barColor);
        }
    }

    private void DrawScore(SpriteBatch spriteBatch)
    {
        for (int i = 0; i < WinScore; i++)
        {
            // Empty slot
            spriteBatch.Draw(_pixel, new Rectangle(W / 2 - 22 - i * 18, 18, 12, 12), new Color(40, 40, 40));
            spriteBatch.Draw(_pixel, new Rectangle(W / 2 + 10 + i * 18, 18, 12, 12), new Color(40, 40, 40));
        }
        for (int i = 0; i < _playerScore; i++)
            spriteBatch.Draw(_pixel, new Rectangle(W / 2 - 22 - i * 18, 18, 12, 12), Color.DodgerBlue);
        for (int i = 0; i < _botScore; i++)
            spriteBatch.Draw(_pixel, new Rectangle(W / 2 + 10 + i * 18, 18, 12, 12), Color.OrangeRed);
    }

    private void DrawSuddenDeathOverlay(SpriteBatch spriteBatch)
    {
        // Pulse the center line between blue and red to signal sudden death
        float t = (float)(Math.Sin(_flashTimer * 5) * 0.5 + 0.5);
        Color lineColor = Color.Lerp(Color.OrangeRed, Color.DodgerBlue, t);
        spriteBatch.Draw(_pixel, new Rectangle(W / 2 - 3, Wall + 8, 6, H - Wall * 2 - 8), lineColor);

        // Small pulsing bar along top to indicate sudden death mode
        float pulse = (float)(Math.Sin(_flashTimer * 4) * 0.5 + 0.5);
        int barW = (int)(pulse * (W - Wall * 2));
        spriteBatch.Draw(_pixel, new Rectangle(Wall, Wall + 2, W - Wall * 2, 4), new Color(25, 25, 25));
        spriteBatch.Draw(_pixel, new Rectangle(Wall + (W - Wall * 2 - barW) / 2, Wall + 2, barW, 4),
            Color.Lerp(Color.OrangeRed, Color.DodgerBlue, pulse));
    }

    private void DrawGameOver(SpriteBatch spriteBatch)
    {
        Color overlayColor = _playerWon ? new Color(20, 60, 160, 190) : new Color(160, 40, 15, 190);
        spriteBatch.Draw(_pixel, new Rectangle(0, 0, W, H), overlayColor);

        // Big score dots of the winner centered on screen
        Color winColor = _playerWon ? Color.DodgerBlue : Color.OrangeRed;
        for (int i = 0; i < WinScore; i++)
            spriteBatch.Draw(_pixel, new Rectangle(W / 2 - WinScore * 14 + i * 28, H / 2 - 12, 22, 22), winColor);

        // Pulsing "press space" bar at bottom
        float pulse = (float)(Math.Sin(_flashTimer * 3) * 0.5 + 0.5);
        int promptAlpha = (int)(pulse * 220 + 35);
        spriteBatch.Draw(_pixel, new Rectangle(W / 2 - 70, H - 45, 140, 6), new Color(255, 255, 255, promptAlpha));
    }
}
