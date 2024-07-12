using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Solver : System.IDisposable {

    protected Config config;

    protected DoubleBuffer dye;
    protected DoubleBuffer velocity;
    protected RenderTexture divergence;
    protected RenderTexture curl;
    protected DoubleBuffer pressure;


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

        if (dye == null)
            dye = new DoubleBuffer(dyeRes.x, dyeRes.y, rgba, filtering, TextureWrapMode.Clamp);
        else
            dye.Resize(dyeRes.x, dyeRes.y);

        if (velocity == null)
            velocity = new DoubleBuffer(simRes.x, simRes.y, rg, filtering, TextureWrapMode.Clamp);
        else
            velocity.Resize(simRes.x, simRes.y);

        if (divergence == null)
            divergence = new RenderTexture(simRes.x, simRes.y, 0, r, RenderTextureReadWrite.Linear);
        else
            divergence.Resize(simRes.x, simRes.y);

        if (curl == null)
            curl = new RenderTexture(simRes.x, simRes.y, 0, r, RenderTextureReadWrite.Linear);
        else
            curl.Resize(simRes.x, simRes.y);

        if (pressure == null)
            pressure = new DoubleBuffer(simRes.x, simRes.y, r, filtering, TextureWrapMode.Clamp);
        else
            pressure.Resize(simRes.x, simRes.y);
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
    }
    #endregion

    #region DoubleBuffer
    public class DoubleBuffer : System.IDisposable {

        protected RenderTexture tex0, tex1;
        public DoubleBuffer(int width, int height, RenderTextureFormat format, FilterMode filter, TextureWrapMode wrap) {
            tex0 = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
            tex1 = new RenderTexture(width, height, 0, format, RenderTextureReadWrite.Linear);
            tex0.filterMode = filter;
            tex1.filterMode = filter;
            tex0.wrapMode = wrap;
            tex1.wrapMode = wrap;
        }
        public RenderTexture Read { get => tex0; }
        public RenderTexture Write { get => tex1; }

        public DoubleBuffer Resize(int width, int height) {
            tex0.Resize(width, height);
            tex1.Resize(width, height);
            return this;
        }
        public void Swap() {
            var temp = tex0;
            tex0 = tex1;
            tex1 = temp;
        }

        #region IDisposable
        public void Dispose() {
            if (tex0 != null) {
                Object.Destroy(tex0);
                tex0 = null;
            }
            if (tex1 != null) {
                Object.Destroy(tex1);
                tex1 = null;
            }
        }
        #endregion
    }
    #endregion

    #region declarations
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

public static class RenderTextureExtensions {
    public static RenderTexture Resize(this RenderTexture renderTexture, int width, int height) {
        if (renderTexture.width == width && renderTexture.height == height)
            return renderTexture;
        renderTexture.Release();
        renderTexture.width = width;
        renderTexture.height = height;
        renderTexture.Create();
        return renderTexture;
    }