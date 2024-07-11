using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Solver : System.IDisposable {
    public void Dispose() {
        throw new System.NotImplementedException();
    }

    public class DoubleBuffer : System.IDisposable {

        protected RenderTexture tex0, tex1;

        public void InitFramebuffers() {


            //let simRes = getResolution(config.SIM_RESOLUTION);
            //let dyeRes = getResolution(config.DYE_RESOLUTION);

            //const texType = ext.halfFloatTexType;
            //const rgba    = ext.formatRGBA;
            //const rg      = ext.formatRG;
            //const r       = ext.formatR;
            //const filtering = ext.supportLinearFiltering ? gl.LINEAR : gl.NEAREST;

            //gl.disable(gl.BLEND);

            //if (dye == null)
            //    dye = createDoubleFBO(dyeRes.width, dyeRes.height, rgba.internalFormat, rgba.format, texType, filtering);
            //else
            //    dye = resizeDoubleFBO(dye, dyeRes.width, dyeRes.height, rgba.internalFormat, rgba.format, texType, filtering);

            //if (velocity == null)
            //    velocity = createDoubleFBO(simRes.width, simRes.height, rg.internalFormat, rg.format, texType, filtering);
            //else
            //    velocity = resizeDoubleFBO(velocity, simRes.width, simRes.height, rg.internalFormat, rg.format, texType, filtering);

            //divergence = createFBO(simRes.width, simRes.height, r.internalFormat, r.format, texType, gl.NEAREST);
            //curl = createFBO(simRes.width, simRes.height, r.internalFormat, r.format, texType, gl.NEAREST);
            //pressure = createDoubleFBO(simRes.width, simRes.height, r.internalFormat, r.format, texType, gl.NEAREST);

        }

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
    }

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