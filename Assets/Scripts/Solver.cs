using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;
using Random = Unity.Mathematics.Random;

public class Solver : System.IDisposable {

    protected Config config;

    protected Material solver;
    protected Random rand;

    protected bool needResizeCanvas;
    protected RenderTexture currTarget;
    protected int2 currTargetSize;
    protected float currTargetAspect;

    public DoubleBuffer dye { get; protected set; }
    public DoubleBuffer velocity { get; protected set; }
    public RenderTexture divergence { get; protected set; }
    public RenderTexture curl { get; protected set; }
    public DoubleBuffer pressure { get; protected set; }

    public Solver(Config config, RenderTexture currTarget = null, uint seed = 31) {
        this.config = config;
        SetTarget(currTarget);

        rand = new Random(seed);
        solver = new Material(Resources.Load<Shader>(SHADER_PATH));

        InitFramebuffers();
    }

    public Config CurrConfig {
        get => config;
        set {
            this.config = value;
            needResizeCanvas = true;
        }
    }
    public RenderTexture CurrTarget {
        get => currTarget;
        set => SetTarget(value);
    }
    public Solver SetTarget(RenderTexture target) {
        needResizeCanvas = true;
        currTarget = target;
        if (target != null) {
            currTargetSize = new int2(target.width, target.height);
            currTargetAspect = target.width / (float)target.height;
        } else {
            currTargetSize = int2.zero;
            currTargetAspect = 1f;
        }
        return this;
    }

    public int2 CalcResolution(float aspect, int baseRes) {
        if (aspect < 1f)
            return new int2(baseRes, (int)math.round(baseRes / aspect));
        else
            return new int2((int)math.round(baseRes * aspect), baseRes);
    }
    public void InitFramebuffers() {
        var simRes = CalcResolution(currTargetAspect, (int)config.SIM_RESOLUTION);
        var dyeRes = CalcResolution(currTargetAspect, (int)config.DYE_RESOLUTION);

        var rgba = RenderTextureFormat.ARGBHalf;
        var rg = RenderTextureFormat.RGHalf;
        var r = RenderTextureFormat.RHalf;
        var filtering = FilterMode.Bilinear;
        var wrap = TextureWrapMode.Clamp;

        Debug.Log($"{nameof(InitFramebuffers)}: screen={currTargetSize} sim={simRes} dye={dyeRes}");

        if (dye == null)
            dye = new DoubleBuffer(dyeRes, rgba, filtering, wrap);
        else
            dye.Resize(dyeRes);

        if (velocity == null)
            velocity = new DoubleBuffer(simRes, rg, filtering, wrap);
        else
            velocity.Resize(simRes);

        if (divergence == null)
            divergence = simRes.CreateRenderTexture(r, filtering, wrap);
        else
            divergence.Resize(simRes);

        if (curl == null)
            curl = simRes.CreateRenderTexture(r, filtering, wrap);
        else
            curl.Resize(simRes);

        if (pressure == null)
            pressure = new DoubleBuffer(simRes, r, filtering, wrap);
        else
            pressure.Resize(simRes);
    }
    public void Update(float dt) {
        if (needResizeCanvas) {
            needResizeCanvas = false;
            InitFramebuffers();
        }
        Step(dt);
        Render(CurrTarget);
    }
    public void Step(float dt) {
        var velocityTexelSize = velocity.TexelSize;
        var dyeTexelSize = dye.TexelSize;

        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UVelocity, velocity.Read);
        Graphics.Blit(null, curl, solver, (int)ShaderPass.Curl);

        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UVelocity, velocity.Read);
        solver.SetTexture(P_UCurl, curl);
        solver.SetFloat(P_Curl, config.CURL);
        solver.SetFloat(P_Dt, dt);
        Graphics.Blit(null, velocity.Write, solver, (int)ShaderPass.Vorticity);
        velocity.Swap();

        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UVelocity, velocity.Read);
        Graphics.Blit(null, divergence, solver, (int)ShaderPass.Divergence);

        solver.SetTexture(P_UTexture, pressure.Read);
        solver.SetFloat(P_ClearValue, config.PRESSURE);
        Graphics.Blit(null, pressure.Write, solver, (int)ShaderPass.Clear);
        pressure.Swap();

        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UDivergence, divergence);
        for (var i = 0; i < config.PRESSURE_ITERATIONS; i++) {
            solver.SetTexture(P_UPressure, pressure.Read);
            Graphics.Blit(null, pressure.Write, solver, (int)ShaderPass.Pressure);
            pressure.Swap();
        }

        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UPressure, pressure.Read);
        solver.SetTexture(P_UVelocity, velocity.Read);
        Graphics.Blit(null, velocity.Write, solver, (int)ShaderPass.GradientSubtract);
        velocity.Swap();

        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UVelocity, velocity.Read);
        solver.SetTexture(P_USource, velocity.Read);
        solver.SetFloat(P_Dt, dt);
        solver.SetFloat(P_Dissipation, config.VELOCITY_DISSIPATION);
        Graphics.Blit(null, velocity.Write, solver, (int)ShaderPass.Advection);
        velocity.Swap();

        solver.SetTexture(P_UVelocity, velocity.Read);
        solver.SetTexture(P_USource, dye.Read);
        solver.SetFloat(P_Dissipation, config.DENSITY_DISSIPATION);
        Graphics.Blit(null, dye.Write, solver, (int)ShaderPass.Advection);
        dye.Swap();
    }

    public void Render(RenderTexture dst) {
        DrawDisplay(dst);
    }

    public void DrawDisplay(RenderTexture dst) {
        solver.SetTexture(P_UTexture, dye.Read);
        Graphics.Blit(null, dst, solver, (int)ShaderPass.Display);
    }

    public void Splat(float2 point, float2 delta, Color color) {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        Debug.Log($"{nameof(Splat)}: point={point} delta={delta} color={color}");
#endif
        solver.SetTexture(P_UTarget, velocity.Read);
        solver.SetFloat(P_AspectRatio, currTargetAspect);
        solver.SetVector(P_Point, new float4(point.x, point.y, 0, 0));
        solver.SetVector(P_Color, new Color(delta.x, delta.y, 0, 0));
        solver.SetFloat(P_Radius, CorrectRadius(config.SPLAT_RADIUS / 100f));
        Graphics.Blit(null, velocity.Write, solver, (int)ShaderPass.Splat);
        velocity.Swap();

        solver.SetTexture(P_UTarget, dye.Read);
        solver.SetColor(P_Color, color);
        Graphics.Blit(null, dye.Write, solver, (int)ShaderPass.Splat);
        dye.Swap();
    }
    public void MultipleSplats(int amount) {
        for (var i = 0; i < amount; i++) {
            var c = GenerateColor(rand);
            c.r *= 10f;
            c.g *= 10f;
            c.b *= 10f;
            var xy = rand.NextFloat2();
            var dxy = 1000 * (rand.NextFloat2() - 0.5f);
            Splat(xy, dxy, c);
        }
    }
    public float CorrectRadius(float radius) {
        if (currTargetAspect > 1)
            radius *= currTargetAspect;
        return radius;
    }
    public static Color GenerateColor(Random rand) {
        var c = HSVToRGB(new float3(rand.NextFloat(), 1, 1));
        c.r *= 0.15f;
        c.g *= 0.15f;
        c.b *= 0.15f;
        return c;
    }

    public static Color HSVToRGB(float3 hsv) {
        var h = hsv.x;
        var s = hsv.y;
        var v = hsv.z;
        var i = math.floor(h * 6);
        var f = h * 6 - i;
        var p = v * (1 - s);
        var q = v * (1 - f * s);
        var t = v * (1 - (1 - f)) * s;

        switch (i % 6) {
            case 0: return new Color(v, t, p);
            case 1: return new Color(q, v, p);
            case 2: return new Color(p, v, t);
            case 3: return new Color(p, q, v);
            case 4: return new Color(t, p, v);
            default: return new Color(v, p, q);
        }
    }

    #region IDisposable
    public void Dispose() {
        if (dye != null) {
            dye.Dispose();
            dye = null;
        }
        if (velocity != null) {
            velocity.Dispose();
            velocity = null;
        }
        if (divergence != null) {
            Object.Destroy(divergence);
            divergence = null;
        }
        if (curl != null) {
            Object.Destroy(curl);
            curl = null;
        }
        if (pressure != null) {
            pressure.Dispose();
            pressure = null;
        }
        if (solver != null) {
            Object.Destroy(solver);
            solver = null;
        }
    }
    #endregion

    #region DoubleBuffer
    public class DoubleBuffer : System.IDisposable {

        protected RenderTexture tex0, tex1;
        public DoubleBuffer(int2 size, RenderTextureFormat format, FilterMode filter, TextureWrapMode wrap) {
            TexelSize = CalcTexelSize(size);
            tex0 = size.CreateRenderTexture(format, filter, wrap);
            tex1 = size.CreateRenderTexture(format, filter, wrap);
        }

        public RenderTexture Read { get => tex0; }
        public RenderTexture Write { get => tex1; }
        public float4 TexelSize { get; protected set; }

        public DoubleBuffer Resize(int2 size) {
            TexelSize = CalcTexelSize(size);
            tex0.Resize(size);
            tex1.Resize(size);
            return this;
        }
        public void Swap() {
            var temp = tex0;
            tex0 = tex1;
            tex1 = temp;
        }

        public static float4 CalcTexelSize(int2 size) {
            return new float4(1f / size.x, 1f / size.y, size.x, size.y);
        }
        #region IDisposable
        public void Dispose() {
            if (tex0 != null) {
                UnityEngine.Object.Destroy(tex0);
                tex0 = null;
            }
            if (tex1 != null) {
                UnityEngine.Object.Destroy(tex1);
                tex1 = null;
            }
        }
        #endregion
    }
    #endregion

    #region declarations
    public const string SHADER_PATH = "Solver";

    public enum ShaderPass {
        Blur = 0,
        Copy = 1,
        Clear = 2,
        Color = 3,
        Checkerboard = 4,
        BloomPrefilter = 5,
        BloomBlur = 6,
        BloomFinal = 7,
        SunraysMask = 8,
        Sunrays = 9,
        Splat = 10,
        Advection = 11,
        Divergence = 12,
        Curl = 13,
        Vorticity = 14,
        Pressure = 15,
        GradientSubtract = 16,
        Display = 17
    }

    public static readonly int P_UTexture = Shader.PropertyToID("_UTexture");
    public static readonly int P_UVelocity = Shader.PropertyToID("_UVelocity");
    public static readonly int P_USource = Shader.PropertyToID("_USource");
    public static readonly int P_UTarget = Shader.PropertyToID("_UTarget");
    public static readonly int P_UCurl = Shader.PropertyToID("_UCurl");
    public static readonly int P_UPressure = Shader.PropertyToID("_UPressure");
    public static readonly int P_UDivergence = Shader.PropertyToID("_UDivergence");

    public static readonly int P_TexelSize = Shader.PropertyToID("_TexelSize");

    public static readonly int P_Color = Shader.PropertyToID("_Color");

    public static readonly int P_ClearValue = Shader.PropertyToID("_ClearValue");
    public static readonly int P_AspectRatio = Shader.PropertyToID("_AspectRatio");
    public static readonly int P_Point = Shader.PropertyToID("_Point");

    public static readonly int P_Radius = Shader.PropertyToID("_Radius");
    public static readonly int P_Dt = Shader.PropertyToID("_Dt");
    public static readonly int P_Dissipation = Shader.PropertyToID("_Dissipation");
    public static readonly int P_Curl = Shader.PropertyToID("_Curl");

    public enum DyeResolution {
        High = 1024, Medium = 512, Low = 256, VeryLow = 128
    }
    public enum SimResolution {
        R32 = 32, R64 = 64, R128 = 128, R256 = 256
    }
    public enum PressureIterations {
        Low = 10, Medium = 20, High = 30, VeryHigh = 40
    }
    [System.Serializable]
    public class Config {
        public SimResolution SIM_RESOLUTION = (SimResolution)128;
        public DyeResolution DYE_RESOLUTION = (DyeResolution)1024;
        public int CAPTURE_RESOLUTION = 512;
        [Range(0f, 4.0f)]
        public float DENSITY_DISSIPATION = 1;
        [Range(0f, 4.0f)]
        public float VELOCITY_DISSIPATION = 0.2f;
        [Range(0.0f, 1.0f)]
        public float PRESSURE = 0.8f;
        public int PRESSURE_ITERATIONS = 20;
        [Range(0, 50)]
        public int CURL = 30;
        [Range(0.01f, 1.0f)]
        public float SPLAT_RADIUS = 0.25f;
        public float SPLAT_FORCE = 6000;
        public bool COLORFUL = true;
        public float COLOR_UPDATE_SPEED = 10;
        public bool PAUSED = false;
        public Color BACK_COLOR = Color.black;
    }
    #endregion
}

#region extensions
public static class RenderTextureExtensions {

    public static RenderTexture CreateRenderTexture(this int2 size, RenderTextureFormat format, FilterMode filter, TextureWrapMode wrap) {
        var tex = new RenderTexture(size.x, size.y, 0, format, RenderTextureReadWrite.Linear);
        tex.hideFlags = HideFlags.DontSave;
        tex.filterMode = filter;
        tex.wrapMode = wrap;
        tex.Create();
        tex.Clear();
        return tex;
    }
    public static RenderTexture Resize(this RenderTexture renderTexture, int2 size) {
        if (renderTexture.width == size.x && renderTexture.height == size.y)
            return renderTexture;

        var tmp = RenderTexture.GetTemporary(renderTexture.descriptor);
        Graphics.Blit(renderTexture, tmp);

        renderTexture.Release();
        renderTexture.width = size.x;
        renderTexture.height = size.y;
        renderTexture.Create();

        Graphics.Blit(tmp, renderTexture);
        RenderTexture.ReleaseTemporary(tmp);

        return renderTexture;
    }
    public static RenderTexture Clear(this RenderTexture renderTexture, bool clearDepth = true, bool clearColor = true, Color color = default) {
        var tmp = RenderTexture.active;
        RenderTexture.active = renderTexture;
        GL.Clear(clearDepth, clearColor, color);
        RenderTexture.active = tmp;
        return renderTexture;
    }
}
#endregion