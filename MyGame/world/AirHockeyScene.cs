using System;
using System.Collections.Generic;
using KripakEngine;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyGame;

public class AirHockeyScene : Scene
{
    private Texture2D _pixel;
    private List<Entity> _entities = new();

    // Layout
    private const int W = 800, H = 500;
    private const int Wall = 14;
    private const int GoalH = 130;
    private const int GoalTop = (H - GoalH) / 2;   // 185
    private const int GoalBot = GoalTop + GoalH;     // 315

    private const int PuckR = 12, PaddleR = 26;
    private const int PuckSize = PuckR * 2, PaddleSize = PaddleR * 2;

    private const float PlayerSpeed = 5.5f, BotSpeed = 4.0f;
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

    public AirHockeyScene(Texture2D pixel)
    {
        _pixel = pixel;
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
        _entities.Add(player);

        Entity bot = new Entity();
        bot.AddComponent(new TransformComponent(new Vector2(W - 80 - PaddleSize, H / 2f - PaddleR), PaddleSize, PaddleSize));
        bot.AddComponent(new PaddleComponent(BotSpeed, isPlayer: false));
        bot.AddComponent(new RenderComponent(Color.OrangeRed));
        _entities.Add(bot);

        Entity puck = new Entity();
        puck.AddComponent(new TransformComponent(new Vector2(W / 2f - PuckR, H / 2f - PuckR), PuckSize, PuckSize));
        puck.AddComponent(new PuckComponent(new Vector2(-3.5f, 1.5f)));
        puck.AddComponent(new RenderComponent(Color.WhiteSmoke));
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
                _state = _timeRemaining <= 0 ? GameState.SuddenDeath : GameState.Playing;
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

        t.Position.X = MathHelper.Clamp(t.Position.X, Wall, W / 2 - PaddleSize - 4);
        t.Position.Y = MathHelper.Clamp(t.Position.Y, Wall, H - Wall - PaddleSize);
        pad.Velocity = t.Position - prev;
    }

    private void BotAI(Entity bot, TransformComponent puckT, PuckComponent puckP)
    {
        var t = bot.GetComponent<TransformComponent>();
        var pad = bot.GetComponent<PaddleComponent>();
        Vector2 prev = t.Position;

        Vector2 puckCenter = puckT.Position + new Vector2(PuckR);
        Vector2 botCenter = t.Position + new Vector2(PaddleR);

        // Y: move toward predicted puck intercept
        float predictedY = PredictY(puckCenter, puckP.Velocity, botCenter.X);
        float dy = (predictedY - PaddleR) - t.Position.Y;
        t.Position.Y += MathHelper.Clamp(dy, -pad.Speed, pad.Speed);

        // X: advance to meet puck when it's on bot's half, defend otherwise
        float targetX = (puckCenter.X > W / 2f && puckP.Velocity.X > 0.5f)
            ? MathHelper.Clamp(puckCenter.X - PaddleSize * 1.8f, W / 2f + 10, W - Wall - PaddleSize - 10)
            : W - Wall - PaddleSize - 20;

        float dx = targetX - t.Position.X;
        t.Position.X += MathHelper.Clamp(dx, -pad.Speed, pad.Speed);

        t.Position.X = MathHelper.Clamp(t.Position.X, W / 2, W - Wall - PaddleSize);
        t.Position.Y = MathHelper.Clamp(t.Position.Y, Wall, H - Wall - PaddleSize);
        pad.Velocity = t.Position - prev;
    }

    // Predict puck Y at targetX, simulating wall reflections
    private float PredictY(Vector2 pos, Vector2 vel, float targetX)
    {
        if (Math.Abs(vel.X) < 0.01f) return H / 2f;
        float t = (targetX - pos.X) / vel.X;
        if (t < 0) return H / 2f;

        float y = pos.Y + vel.Y * t;
        float minY = Wall + PuckR, maxY = H - Wall - PuckR;
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

        if (pos.Y < Wall) { pos.Y = Wall; vel.Y = Math.Abs(vel.Y); }
        if (pos.Y + PuckSize > H - Wall) { pos.Y = H - Wall - PuckSize; vel.Y = -Math.Abs(vel.Y); }

        if (pos.X < Wall)
        {
            bool inGoal = pos.Y + PuckSize > GoalTop && pos.Y < GoalBot;
            if (!inGoal) { pos.X = Wall; vel.X = Math.Abs(vel.X); }
            else if (pos.X + PuckSize < 0) ScoreGoal(scoredByBot: true);
        }

        if (pos.X + PuckSize > W - Wall)
        {
            bool inGoal = pos.Y + PuckSize > GoalTop && pos.Y < GoalBot;
            if (!inGoal) { pos.X = W - Wall - PuckSize; vel.X = -Math.Abs(vel.X); }
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
        DrawRink(spriteBatch);
        DrawTimerBar(spriteBatch);
        DrawScore(spriteBatch);

        foreach (var entity in _entities)
        {
            if (!entity.HasComponent<TransformComponent>() || !entity.HasComponent<RenderComponent>()) continue;
            var t = entity.GetComponent<TransformComponent>();
            var r = entity.GetComponent<RenderComponent>();
            spriteBatch.Draw(_pixel, t.Bounds, r.DefaultColor);
        }

        if (_state == GameState.SuddenDeath)
            DrawSuddenDeathOverlay(spriteBatch);

        if (_state == GameState.GameOver)
            DrawGameOver(spriteBatch);
    }

    private void DrawRink(SpriteBatch spriteBatch)
    {
        Color wallCol = new Color(200, 200, 210);

        spriteBatch.Draw(_pixel, new Rectangle(0, GoalTop, Wall, GoalH), new Color(30, 100, 50));
        spriteBatch.Draw(_pixel, new Rectangle(W - Wall, GoalTop, Wall, GoalH), new Color(120, 40, 30));

        spriteBatch.Draw(_pixel, new Rectangle(0, 0, W, Wall), wallCol);
        spriteBatch.Draw(_pixel, new Rectangle(0, H - Wall, W, Wall), wallCol);
        spriteBatch.Draw(_pixel, new Rectangle(0, Wall, Wall, GoalTop - Wall), wallCol);
        spriteBatch.Draw(_pixel, new Rectangle(0, GoalBot, Wall, H - GoalBot), wallCol);
        spriteBatch.Draw(_pixel, new Rectangle(W - Wall, Wall, Wall, GoalTop - Wall), wallCol);
        spriteBatch.Draw(_pixel, new Rectangle(W - Wall, GoalBot, Wall, H - GoalBot), wallCol);

        spriteBatch.Draw(_pixel, new Rectangle(W / 2 - 1, Wall, 2, H - Wall * 2), new Color(60, 70, 60));

        for (int i = 0; i < 16; i++)
        {
            double a = i * Math.PI * 2 / 16;
            int rx = (int)(W / 2 + Math.Cos(a) * 44);
            int ry = (int)(H / 2 + Math.Sin(a) * 44);
            spriteBatch.Draw(_pixel, new Rectangle(rx - 2, ry - 2, 4, 4), new Color(60, 70, 60));
        }
        spriteBatch.Draw(_pixel, new Rectangle(W / 2 - 3, H / 2 - 3, 6, 6), new Color(60, 70, 60));

        // Goal flash on score
        if (_state == GameState.GoalPause)
        {
            float alpha = Math.Min(1f, _pauseTimer / PauseDuration);
            if (_botLastScored)
                spriteBatch.Draw(_pixel, new Rectangle(0, GoalTop, Wall + 8, GoalH), new Color(30, 200, 80, (int)(alpha * 200)));
            else
                spriteBatch.Draw(_pixel, new Rectangle(W - Wall - 8, GoalTop, Wall + 8, GoalH), new Color(200, 80, 30, (int)(alpha * 200)));
        }
    }

    private void DrawTimerBar(SpriteBatch spriteBatch)
    {
        if (_state == GameState.SuddenDeath || _state == GameState.GameOver) return;

        float fraction = _timeRemaining / MatchDuration;
        int barW = (int)(fraction * (W - Wall * 2));

        spriteBatch.Draw(_pixel, new Rectangle(Wall, Wall + 2, W - Wall * 2, 4), new Color(25, 25, 25));

        if (barW > 0)
        {
            Color barColor = fraction > 0.5f
                ? Color.Lerp(Color.Yellow, Color.LimeGreen, (fraction - 0.5f) * 2f)
                : Color.Lerp(Color.Red, Color.Yellow, fraction * 2f);
            spriteBatch.Draw(_pixel, new Rectangle(Wall, Wall + 2, barW, 4), barColor);
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
