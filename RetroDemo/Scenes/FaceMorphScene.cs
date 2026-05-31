using System.Numerics;
using Raylib_cs;
using static RetroDemo.ColorHelper;

namespace RetroDemo.Scenes;

/// <summary>
/// Shows a stylised 3-D female face that smiles, then morphs into a robot face.
/// Two render-textures are alpha-composited for the crossfade transition.
/// </summary>
public sealed class FaceMorphScene : IScene
{
    private readonly int _w;
    private readonly int _h;

    // ── render textures ───────────────────────────────────────────────────────
    private RenderTexture2D _rtFemale;
    private RenderTexture2D _rtRobot;

    // ── timing ────────────────────────────────────────────────────────────────
    private const float FemaleShowTime  = 4.0f;   // seconds female face is shown
    private const float MorphDuration   = 3.0f;   // seconds of morph transition
    private const float RobotShowTime   = 3.5f;   // seconds robot face is shown
    private const float OutroDuration   = 0.8f;

    private float _time = 0f;
    private float _morphT = 0f;   // 0 = female, 1 = robot
    private float _smileT = 0f;   // 0 = neutral, 1 = full smile
    private float _outroT = 0f;

    // ── camera ────────────────────────────────────────────────────────────────
    private Camera3D _camera = new()
    {
        Position   = new Vector3(0, 0.2f, 6.5f),
        Target     = new Vector3(0, 0, 0),
        Up         = Vector3.UnitY,
        FovY       = 40f,
        Projection = CameraProjection.Perspective,
    };

    // ── particle system ───────────────────────────────────────────────────────
    private struct Particle
    {
        public Vector2 Pos;
        public Vector2 Vel;
        public Color   Col;
        public float   Life;
        public float   MaxLife;
    }

    private readonly Particle[] _particles = new Particle[300];
    private readonly Random _rng = new(42);
    private bool _particlesBurst = false;

    // ── skin / metal colours ──────────────────────────────────────────────────
    private static Color Skin    => new(255, 200, 165, 255);
    private static Color SkinDk  => new(220, 165, 130, 255);
    private static Color HairCol => new(60,  30,  10,  255);
    private static Color IrisCol => new(80,  130, 230, 255);
    private static Color LipCol  => new(220, 100,  90, 255);
    private static Color MetalLt => new(160, 175, 185, 255);
    private static Color MetalDk => new(80,   90, 100, 255);
    private static Color GlowRed => new(255,  40,  20, 255);
    private static Color DarkTint=> new(10,   10,  30, 255);

    public FaceMorphScene(int w, int h)
    {
        _w = w;
        _h = h;
        _rtFemale = Raylib.LoadRenderTexture(w, h);
        _rtRobot  = Raylib.LoadRenderTexture(w, h);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public bool Update(float dt)
    {
        _time += dt;

        // Smile animation: starts at FemaleShowTime - 1.5 sec
        float smileStart = FemaleShowTime - 1.5f;
        if (_time >= smileStart)
            _smileT = Math.Clamp((_time - smileStart) / 1.2f, 0f, 1f);

        // Morph factor
        float morphStart = FemaleShowTime;
        if (_time >= morphStart)
        {
            _morphT = Math.Clamp((_time - morphStart) / MorphDuration, 0f, 1f);
            if (!_particlesBurst && _morphT > 0.05f)
            {
                BurstParticles();
                _particlesBurst = true;
            }
        }

        // Outro
        float outroStart = FemaleShowTime + MorphDuration + RobotShowTime;
        if (_time >= outroStart)
        {
            _outroT += dt / OutroDuration;
            if (_outroT >= 1f) return true;
        }

        // Update particles
        for (int i = 0; i < _particles.Length; i++)
        {
            ref var p = ref _particles[i];
            if (p.Life <= 0f) continue;
            p.Life    -= dt;
            p.Pos     += p.Vel * dt;
            p.Vel.Y   += 80f * dt; // gravity
        }

        return false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void Draw()
    {
        float t = _time;

        // ── render female face to RT ──────────────────────────────────────────
        Raylib.BeginTextureMode(_rtFemale);
        Raylib.ClearBackground(Rgba(10, 5, 30, 255));
        DrawBackground(t, female: true);
        Raylib.BeginMode3D(_camera);
        float rotY = t * 18f;                              // gentle rotation
        DrawFemaleFace(rotY, _smileT);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        // ── render robot face to RT ───────────────────────────────────────────
        Raylib.BeginTextureMode(_rtRobot);
        Raylib.ClearBackground(Rgba(5, 10, 20, 255));
        DrawBackground(t, female: false);
        Raylib.BeginMode3D(_camera);
        DrawRobotFace(rotY);
        Raylib.EndMode3D();
        Raylib.EndTextureMode();

        // ── composite onto screen ─────────────────────────────────────────────
        // Raylib render-textures have Y flipped; use negative-height source rect.
        var srcRect = new Rectangle(0, 0, _w, -_h);
        var dstRect = new Rectangle(0, 0, _w, _h);

        float globalAlpha = 1f - Math.Clamp(_outroT, 0f, 1f);
        byte  ga          = (byte)(globalAlpha * 255);

        byte femaleA = (byte)(Math.Clamp(1f - _morphT, 0f, 1f) * ga);
        byte robotA  = (byte)(Math.Clamp(_morphT,      0f, 1f) * ga);

        if (femaleA > 0)
            Raylib.DrawTexturePro(_rtFemale.Texture, srcRect, dstRect,
                                  Vector2.Zero, 0f, Rgba(255, 255, 255, femaleA));
        if (robotA > 0)
            Raylib.DrawTexturePro(_rtRobot.Texture, srcRect, dstRect,
                                  Vector2.Zero, 0f, Rgba(255, 255, 255, robotA));

        // ── chromatic-aberration glitch during morph ──────────────────────────
        if (_morphT > 0f && _morphT < 1f)
            DrawGlitch(_morphT);

        // ── particles ─────────────────────────────────────────────────────────
        DrawParticles();

        // ── scanlines ─────────────────────────────────────────────────────────
        DrawScanlines();

        // ── label ────────────────────────────────────────────────────────────
        DrawLabel();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Background gradient / stars
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawBackground(float t, bool female)
    {
        int steps = 80;
        for (int i = 0; i < steps; i++)
        {
            float f = i / (float)steps;
            float h = female
                ? (220f + f * 80f) % 360f         // purple→blue for female
                : (190f + f * 60f) % 360f;         // teal→blue for robot
            h = (h + t * 10f) % 360f;
            float brightness = 0.12f + f * 0.08f;
            var c = Raylib.ColorFromHSV(h, 0.8f, brightness);
            Raylib.DrawRectangle(0, (int)(i * _h / steps), _w, _h / steps + 1, c);
        }

        // Distant stars
        var rand = new Random(1234);
        for (int s = 0; s < 120; s++)
        {
            int sx = rand.Next(_w);
            int sy = rand.Next(_h);
            float twinkle = 0.5f + 0.5f * MathF.Sin(t * 2.3f + s);
            byte sb = (byte)(twinkle * 180);
            Raylib.DrawPixel(sx, sy, Rgba(sb, sb, (byte)(sb + 30), 255));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3-D Female face (sphere-based)
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawFemaleFace(float rotDeg, float smileT)
    {
        float r   = rotDeg * MathF.PI / 180f;
        float cos = MathF.Cos(r);
        float sin = MathF.Sin(r);

        Vector3 Rot(Vector3 v)
        {
            return new Vector3(v.X * cos - v.Z * sin, v.Y, v.X * sin + v.Z * cos);
        }

        // Hair (slightly oversized sphere – drawn first so face overlaps)
        Raylib.DrawSphereEx(Rot(new Vector3(0, 0.18f, -0.05f)), 1.08f, 12, 12, HairCol);

        // Head
        Raylib.DrawSphereEx(Rot(Vector3.Zero), 1.0f, 16, 16, Skin);

        // Cheeks (for the smile blush)
        if (smileT > 0f)
        {
            byte ba = (byte)(smileT * 80);
            var blush = Rgba(255, 140, 140, ba);
            Raylib.DrawSphereEx(Rot(new Vector3(-0.55f, -0.05f, 0.78f)), 0.28f, 8, 8, blush);
            Raylib.DrawSphereEx(Rot(new Vector3( 0.55f, -0.05f, 0.78f)), 0.28f, 8, 8, blush);
        }

        // Eye whites
        Raylib.DrawSphereEx(Rot(new Vector3(-0.32f, 0.22f, 0.93f)), 0.145f, 8, 8, Color.White);
        Raylib.DrawSphereEx(Rot(new Vector3( 0.32f, 0.22f, 0.93f)), 0.145f, 8, 8, Color.White);

        // Irises
        Raylib.DrawSphereEx(Rot(new Vector3(-0.32f, 0.22f, 1.00f)), 0.085f, 8, 8, IrisCol);
        Raylib.DrawSphereEx(Rot(new Vector3( 0.32f, 0.22f, 1.00f)), 0.085f, 8, 8, IrisCol);

        // Pupils
        Raylib.DrawSphereEx(Rot(new Vector3(-0.32f, 0.22f, 1.05f)), 0.04f, 8, 8, Color.Black);
        Raylib.DrawSphereEx(Rot(new Vector3( 0.32f, 0.22f, 1.05f)), 0.04f, 8, 8, Color.Black);

        // Nose
        Raylib.DrawSphereEx(Rot(new Vector3(0, -0.06f, 0.97f)), 0.07f, 8, 8, SkinDk);

        // Mouth: corners animate upward with smile
        float smileYOff = smileT * 0.12f;
        float smileZOff = smileT * 0.04f;

        // Lower lip
        Raylib.DrawSphereEx(Rot(new Vector3(0, -0.42f + smileYOff * 0.3f, 0.90f)), 0.10f, 8, 8, LipCol);
        // Upper lip
        Raylib.DrawSphereEx(Rot(new Vector3(0, -0.32f, 0.92f)), 0.07f, 8, 8, LipCol);
        // Mouth corners
        Raylib.DrawSphereEx(Rot(new Vector3(-0.20f, -0.39f + smileYOff, 0.88f + smileZOff)), 0.055f, 6, 6, LipCol);
        Raylib.DrawSphereEx(Rot(new Vector3( 0.20f, -0.39f + smileYOff, 0.88f + smileZOff)), 0.055f, 6, 6, LipCol);

        // Light eyebrow arcs (tiny flattened spheres)
        Raylib.DrawSphereEx(Rot(new Vector3(-0.32f, 0.46f, 0.90f)), 0.05f, 6, 4, HairCol);
        Raylib.DrawSphereEx(Rot(new Vector3(-0.18f, 0.50f, 0.87f)), 0.045f, 6, 4, HairCol);
        Raylib.DrawSphereEx(Rot(new Vector3( 0.32f, 0.46f, 0.90f)), 0.05f, 6, 4, HairCol);
        Raylib.DrawSphereEx(Rot(new Vector3( 0.18f, 0.50f, 0.87f)), 0.045f, 6, 4, HairCol);

        // Neck
        Raylib.DrawCylinder(Rot(new Vector3(0, -1.05f, 0)), 0.28f, 0.28f, 0.35f, 12, Skin);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // 3-D Robot face (cube/cylinder-based)
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawRobotFace(float rotDeg)
    {
        float r   = rotDeg * MathF.PI / 180f;
        float cos = MathF.Cos(r);
        float sin = MathF.Sin(r);

        Vector3 Rot(Vector3 v)
        {
            return new Vector3(v.X * cos - v.Z * sin, v.Y, v.X * sin + v.Z * cos);
        }

        // Main head block
        Raylib.DrawCube(Rot(Vector3.Zero), 2.2f, 2.6f, 1.8f, MetalDk);
        Raylib.DrawCubeWires(Rot(Vector3.Zero), 2.2f, 2.6f, 1.8f, Rgba(100, 200, 255, 120));

        // Forehead panel detail
        Raylib.DrawCube(Rot(new Vector3(0, 0.75f, 0.91f)), 1.8f, 0.55f, 0.06f, MetalLt);
        Raylib.DrawCubeWires(Rot(new Vector3(0, 0.75f, 0.91f)), 1.8f, 0.55f, 0.06f, Rgba(60, 180, 255, 100));

        // Horizontal scan lines on forehead (circuit-board style)
        for (int i = -2; i <= 2; i++)
        {
            float lineY = 0.75f + i * 0.09f;
            Raylib.DrawCube(Rot(new Vector3(0, lineY, 0.94f)), 1.6f, 0.018f, 0.02f,
                            Rgba(0, 200, 255, 180));
        }

        // Visor bar (dark tinted)
        Raylib.DrawCube(Rot(new Vector3(0, 0.22f, 0.91f)), 2.0f, 0.28f, 0.08f, DarkTint);

        // Eyes (glowing red cubes with wires)
        float eyeGlow = 0.5f + 0.5f * MathF.Sin(_time * 3.5f); // pulsing
        var eyeCol = Rgba((byte)255, (byte)(int)(40 + eyeGlow * 60), 0, 255);
        Raylib.DrawCube(Rot(new Vector3(-0.52f, 0.22f, 0.93f)), 0.48f, 0.22f, 0.16f, eyeCol);
        Raylib.DrawCube(Rot(new Vector3( 0.52f, 0.22f, 0.93f)), 0.48f, 0.22f, 0.16f, eyeCol);
        Raylib.DrawCubeWires(Rot(new Vector3(-0.52f, 0.22f, 0.93f)), 0.48f, 0.22f, 0.16f, Color.Red);
        Raylib.DrawCubeWires(Rot(new Vector3( 0.52f, 0.22f, 0.93f)), 0.48f, 0.22f, 0.16f, Color.Red);

        // Nose block
        Raylib.DrawCube(Rot(new Vector3(0, -0.06f, 0.93f)), 0.20f, 0.28f, 0.18f, MetalLt);

        // Mouth grill (horizontal slits)
        float[] mouthYs = [-0.36f, -0.46f, -0.56f];
        float[] mouthWs = [0.80f,   0.60f,   0.40f];
        for (int m = 0; m < mouthYs.Length; m++)
        {
            Raylib.DrawCube(Rot(new Vector3(0, mouthYs[m], 0.92f)), mouthWs[m], 0.055f, 0.12f, MetalDk);
            Raylib.DrawCube(Rot(new Vector3(0, mouthYs[m], 0.94f)), mouthWs[m] - 0.1f, 0.022f, 0.04f,
                            Rgba(0, 150, 255, 180));
        }

        // Cheek vents
        for (int v = 0; v < 3; v++)
        {
            float ventY = -0.1f + v * 0.18f;
            Raylib.DrawCube(Rot(new Vector3(-1.02f, ventY, 0.65f)), 0.06f, 0.10f, 0.40f, MetalLt);
            Raylib.DrawCube(Rot(new Vector3( 1.02f, ventY, 0.65f)), 0.06f, 0.10f, 0.40f, MetalLt);
        }

        // Antennae
        Raylib.DrawCylinder(Rot(new Vector3(-0.55f, 1.35f, 0)), 0.04f, 0.02f, 0.70f, 8, MetalLt);
        Raylib.DrawCylinder(Rot(new Vector3( 0.55f, 1.35f, 0)), 0.04f, 0.02f, 0.70f, 8, MetalLt);
        // Antenna tips (glowing)
        Raylib.DrawSphereEx(Rot(new Vector3(-0.55f, 1.71f, 0)), 0.07f, 6, 6, eyeCol);
        Raylib.DrawSphereEx(Rot(new Vector3( 0.55f, 1.71f, 0)), 0.07f, 6, 6, eyeCol);

        // Neck block
        Raylib.DrawCube(Rot(new Vector3(0, -1.35f, 0)), 0.7f, 0.35f, 0.8f, MetalDk);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Chromatic aberration glitch during morph
    // ─────────────────────────────────────────────────────────────────────────
    private void DrawGlitch(float t)
    {
        // t: 0→1, peak intensity around 0.5
        float intensity = 4f * t * (1f - t);  // bell curve
        int numBands = (int)(intensity * 15f);
        var rand = new Random((int)(_time * 100));
        var srcRect = new Rectangle(0, 0, _w, -_h);

        for (int b = 0; b < numBands; b++)
        {
            int y = rand.Next(_h);
            int h = rand.Next(2, 12);
            int offset = (int)(intensity * (rand.Next(0, 16) - 8));

            // Red channel shifted right
            Raylib.DrawTexturePro(_rtRobot.Texture,
                new Rectangle(0, _h - y - h, _w, h),
                new Rectangle(offset * 2, y, _w, h),
                Vector2.Zero, 0f, Rgba(255, 0, 0, 60));

            // Blue channel shifted left
            Raylib.DrawTexturePro(_rtFemale.Texture,
                new Rectangle(0, _h - y - h, _w, h),
                new Rectangle(-offset, y, _w, h),
                Vector2.Zero, 0f, Rgba(0, 0, 255, 60));
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Particle burst at start of morph
    // ─────────────────────────────────────────────────────────────────────────
    private void BurstParticles()
    {
        int cx = _w / 2;
        int cy = _h / 2;
        for (int i = 0; i < _particles.Length; i++)
        {
            float angle  = _rng.NextSingle() * MathF.Tau;
            float speed  = 80f + _rng.NextSingle() * 320f;
            float life   = 0.6f + _rng.NextSingle() * 1.0f;
            float hue    = _rng.NextSingle() * 360f;
            _particles[i] = new Particle
            {
                Pos     = new Vector2(cx, cy),
                Vel     = new Vector2(MathF.Cos(angle) * speed, MathF.Sin(angle) * speed - 60f),
                Col     = Raylib.ColorFromHSV(hue, 1f, 1f),
                Life    = life,
                MaxLife = life,
            };
        }
    }

    private void DrawParticles()
    {
        for (int i = 0; i < _particles.Length; i++)
        {
            ref var p = ref _particles[i];
            if (p.Life <= 0f) continue;
            float a = p.Life / p.MaxLife;
            byte  b = (byte)(a * 255);
            var   c = Rgba(p.Col.R, p.Col.G, p.Col.B, b);
            float r = 2f + a * 3f;
            Raylib.DrawCircleV(p.Pos, r, c);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    private void DrawLabel()
    {
        string label = _morphT < 0.5f ? "FEMALE FACE" : "ROBOT FACE";
        float fadeIn = _morphT < 0.5f ? Math.Clamp(_time / 0.8f, 0f, 1f)
                                       : Math.Clamp((_morphT - 0.5f) * 4f, 0f, 1f);
        byte la = (byte)(fadeIn * 200);
        int fs = 22;
        int lw = Raylib.MeasureText(label, fs);
        Raylib.DrawText(label, (_w - lw) / 2, _h - 50, fs,
                        Rgba(200, 200, 255, la));
    }

    private void DrawScanlines()
    {
        var scanColor = Rgba(0, 0, 0, 55);
        for (int y = 0; y < _h; y += 2)
            Raylib.DrawRectangle(0, y, _w, 1, scanColor);
    }

    // ─────────────────────────────────────────────────────────────────────────
    public void Dispose()
    {
        Raylib.UnloadRenderTexture(_rtFemale);
        Raylib.UnloadRenderTexture(_rtRobot);
    }
}
