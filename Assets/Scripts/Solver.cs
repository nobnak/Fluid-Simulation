using Unity.Mathematics;
using UnityEngine;

public class Solver : System.IDisposable {

    protected Config config;

    protected Material solver;

    protected DoubleBuffer dye;
    protected DoubleBuffer velocity;
    protected RenderTexture divergence;
    protected RenderTexture curl;
    protected DoubleBuffer pressure;

    protected float lastUpdateTime;
    protected float colorUpdateTimer;

    public Solver(Config config) {
        this.config = config;
        InitFramebuffers();
        lastUpdateTime = Time.time;
        colorUpdateTimer = 0f;

        solver = new Material(Resources.Load<Shader>(SHADER_PATH));
    }

    public int2 GetResolution(int2 screen, int res) {
        var aspect = screen.x / (float)screen.y;
        if (screen.x < screen.y)
            return new int2(res, (int)math.round(res / aspect));
        else
            return new int2((int)math.round(res * aspect), res);
    }
    public void InitFramebuffers() {
        var screenSize = new int2(Screen.width, Screen.height);
        var simRes = GetResolution(screenSize, config.SIM_RESOLUTION);
        var dyeRes = GetResolution(screenSize, config.DYE_RESOLUTION);

        var rgba = RenderTextureFormat.ARGBHalf;
        var rg = RenderTextureFormat.RGHalf;
        var r = RenderTextureFormat.RHalf;
        var filtering = FilterMode.Bilinear;
        var wrap = TextureWrapMode.Clamp;

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

    public void Update() {
        var dt = CalcDeltaTime();
        UpdateColors(dt);
        Step(dt);

        //function update() {
        //    const dt = calcDeltaTime();
        //    if (resizeCanvas())
        //        initFramebuffers();
        //    updateColors(dt);
        //    applyInputs();
        //    if (!config.PAUSED)
        //        step(dt);
        //    render(null);
        //    requestAnimationFrame(update);
        //}
    }
    public float CalcDeltaTime() {
        var tnow = Time.time;
        var dt = tnow - lastUpdateTime;
        lastUpdateTime = tnow;
        return dt;
    }
    public void UpdateColors(float dt) {
        //function updateColors(dt) {
        //    if (!config.COLORFUL) return;

        //    colorUpdateTimer += dt * config.COLOR_UPDATE_SPEED;
        //    if (colorUpdateTimer >= 1) {
        //        colorUpdateTimer = wrap(colorUpdateTimer, 0, 1);
        //        pointers.forEach(p => {
        //            p.color = generateColor();
        //        });
        //    }
        //}
    }
    public void ApplyInputs() {
        //function applyInputs() {
        //    if (splatStack.length > 0)
        //        multipleSplats(splatStack.pop());

        //    pointers.forEach(p => {
        //        if (p.moved) {
        //            p.moved = false;
        //            splatPointer(p);
        //        }
        //    });
        //}
    }
    public void Step(float dt) {
        var velocityTexelSize = velocity.TexelSize;
        var dyeTexelSize = dye.TexelSize;

        solver.SetPass((int)ShaderPass.Curl);
        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UVelocity, velocity.Read);
        Graphics.Blit(null, curl, solver);

        solver.SetPass((int)ShaderPass.Vorticity);
        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UVelocity, velocity.Read);
        solver.SetTexture(P_UCurl, curl);
        solver.SetFloat(P_Curl, config.CURL);
        solver.SetFloat(P_Dt, dt);
        Graphics.Blit(null, velocity.Write, solver);
        velocity.Swap();

        solver.SetPass((int)ShaderPass.Divergence);
        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UVelocity, velocity.Read);
        Graphics.Blit(null, divergence, solver);

        solver.SetPass((int)ShaderPass.Clear);
        solver.SetTexture(P_UTexture, pressure.Read);
        solver.SetFloat(P_ClearValue, config.PRESSURE);
        Graphics.Blit(null, pressure.Write, solver);
        pressure.Swap();

        solver.SetPass((int)ShaderPass.Pressure);
        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UDivergence, divergence);
        for (var i = 0; i < config.PRESSURE_ITERATIONS; i++) {
            solver.SetTexture(P_UPressure, pressure.Read);
            Graphics.Blit(null, pressure.Write, solver);
            pressure.Swap();
        }

        solver.SetPass((int)ShaderPass.GradientSubtract);
        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UPressure, pressure.Read);
        solver.SetTexture(P_UVelocity, velocity.Read);
        Graphics.Blit(null, velocity.Write, solver);
        velocity.Swap();

        solver.SetPass((int)ShaderPass.Advection);
        solver.SetVector(P_TexelSize, velocityTexelSize);
        solver.SetTexture(P_UVelocity, velocity.Read);
        solver.SetTexture(P_USource, velocity.Read);
        solver.SetFloat(P_Dt, dt);
        solver.SetFloat(P_Dissipation, config.VELOCITY_DISSIPATION);
        Graphics.Blit(null, velocity.Write, solver);
        velocity.Swap();

        solver.SetTexture(P_UVelocity, velocity.Read);
        solver.SetTexture(P_USource, dye.Read);
        solver.SetFloat(P_Dissipation, config.DENSITY_DISSIPATION);
        Graphics.Blit(null, dye.Write, solver);
        dye.Swap();
    }

    public void Render() {
        //function render(target) {
        //    if (config.BLOOM)
        //        applyBloom(dye.read, bloom);
        //    if (config.SUNRAYS) {
        //        applySunrays(dye.read, dye.write, sunrays);
        //        blur(sunrays, sunraysTemp, 1);
        //    }

        //    if (target == null || !config.TRANSPARENT) {
        //        gl.blendFunc(gl.ONE, gl.ONE_MINUS_SRC_ALPHA);
        //        gl.enable(gl.BLEND);
        //    } else {
        //        gl.disable(gl.BLEND);
        //    }

        //    if (!config.TRANSPARENT)
        //        drawColor(target, normalizeColor(config.BACK_COLOR));
        //    if (target == null && config.TRANSPARENT)
        //        drawCheckerboard(target);
        //    drawDisplay(target);
        //}
    }

    public void DrawDisplay(RenderTexture dst) {


        //function drawDisplay(target) {
        //    let width = target == null ? gl.drawingBufferWidth : target.width;
        //    let height = target == null ? gl.drawingBufferHeight : target.height;

        //    displayMaterial.bind();
        //    if (config.SHADING)
        //        gl.uniform2f(displayMaterial.uniforms.texelSize, 1.0 / width, 1.0 / height);
        //    gl.uniform1i(displayMaterial.uniforms.uTexture, dye.read.attach(0));
        //    if (config.BLOOM) {
        //        gl.uniform1i(displayMaterial.uniforms.uBloom, bloom.attach(1));
        //        gl.uniform1i(displayMaterial.uniforms.uDithering, ditheringTexture.attach(2));
        //        let scale = getTextureScale(ditheringTexture, width, height);
        //        gl.uniform2f(displayMaterial.uniforms.ditherScale, scale.x, scale.y);
        //    }
        //    if (config.SUNRAYS)
        //        gl.uniform1i(displayMaterial.uniforms.uSunrays, sunrays.attach(3));
        //    blit(target);
        //}
    }

    public void Splat() {


        //function splat(x, y, dx, dy, color) {
        //    splatProgram.bind();
        //    gl.uniform1i(splatProgram.uniforms.uTarget, velocity.read.attach(0));
        //    gl.uniform1f(splatProgram.uniforms.aspectRatio, canvas.width / canvas.height);
        //    gl.uniform2f(splatProgram.uniforms.point, x, y);
        //    gl.uniform3f(splatProgram.uniforms.color, dx, dy, 0.0);
        //    gl.uniform1f(splatProgram.uniforms.radius, correctRadius(config.SPLAT_RADIUS / 100.0));
        //    blit(velocity.write);
        //    velocity.swap();

        //    gl.uniform1i(splatProgram.uniforms.uTarget, dye.read.attach(0));
        //    gl.uniform3f(splatProgram.uniforms.color, color.r, color.g, color.b);
        //    blit(dye.write);
        //    dye.swap();
        //}
    }
    public float CorrectAspect(float radius) {
        var aspectRatio = Screen.width / (float)Screen.height;
        if (aspectRatio > 1)
            radius *= aspectRatio;
        return radius;
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
            tex0 = size.CreateRenderTexture(RenderTextureFormat.ARGBHalf, filter, wrap);
            tex1 = size.CreateRenderTexture(RenderTextureFormat.ARGBHalf, filter, wrap);
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
        GradientSubtract = 16
    }

    public static readonly int P_UTexture = Shader.PropertyToID("_UTexture");
    public static readonly int P_UVelocity = Shader.PropertyToID("_UVelocity");
    public static readonly int P_USource = Shader.PropertyToID("_USource");
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

    [System.Serializable]
    public class Config {
        public int SIM_RESOLUTION = 128;
        public int DYE_RESOLUTION = 1024;
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