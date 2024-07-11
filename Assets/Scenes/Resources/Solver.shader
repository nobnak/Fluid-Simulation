Shader "Hidden/Solver" {
    Properties {
    }
    SubShader {
        Cull Off ZWrite Off ZTest Always

CGINCLUDE
struct appdata {
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct v2fbase {
    float4 vertex : SV_POSITION;
    float2 vUv : TEXCOORD0;
    float2 vL : TEXCOORD1;
    float2 vR : TEXCOORD2;
    float2 vT : TEXCOORD3;
    float2 vB : TEXCOORD4;
};
struct v2fblur {
    float4 vertex : SV_POSITION;
    float2 vUv : TEXCOORD0;
    float2 vL : TEXCOORD1;
    float2 vR : TEXCOORD2;
    float2 vT : TEXCOORD3;
    float2 vB : TEXCOORD4;
};

sampler2D _MainTex;
sampler2D _UVelocity;
sampler2D _USource;
sampler2D _UCurl;
sampler2D _UPressure;
sampler2D _UDivergence;

float4 _MainTex_TexelSize;

float4 _Color;

float _ClearValue;
float _AspectRatio;
float2 _Point;

float _Radius;
float _Dt;
float _Dissipation;

v2fbase baseVertexShader (appdata v) {
    v2fbase o;
    o.vertex = UnityObjectToClipPos(v.vertex);
    o.vUv = v.uv;
    o.vL = o.vUv - float2(_MainTex_TexelSize.x, 0.0);
    o.vR = o.vUv + float2(_MainTex_TexelSize.x, 0.0);
    o.vT = o.vUv + float2(0.0, _MainTex_TexelSize.y);
    o.vB = o.vUv - float2(0.0, _MainTex_TexelSize.y);
    return o;
}

float4 copyShader(v2fbase i) : SV_Target {
	return tex2D(_MainTex, i.vUv);
}

float4 clearShader(v2fbase i) : SV_Target {
	return _ClearValue * tex2D(_MainTex, i.vUv);
}

float4 colorShader(v2fbase i) : SV_Target {
	return _Color;
}

float4 checkerboardShader(v2fbase i) : SV_Target {
	float SCALE = 25.0;
	float2 uv = floor(i.vUv * SCALE * float2(_AspectRatio, 1.0));
	float v = fmod(uv.x + uv.y, 2.0);
	v = v * 0.1 + 0.8;
	return float4(v, v, v, 1.0);
}

float4 displayShaderSource(v2fbase i) : SV_Target {
	float3 c = tex2D(_MainTex, i.vUv).rgb;
    float a = max(c.r, max(c.g, c.b));
    return float4(c, a);
}

float4 splatShader(v2fbase i) : SV_Target {
	float2 p = i.vUv - _Point.xy;
	p.x *= _AspectRatio;
	float3 splat = exp(-dot(p, p) / _Radius) * _Color.xyz;
	float3 base = tex2D(_MainTex, i.vUv).xyz;
	return float4(base + splat, 1.0);
}

float4 advectionShader(v2fbase i) : SV_Target {
	float2 coord = i.vUv - _Dt * tex2D(_UVelocity, i.vUv).xy * _MainTex_TexelSize.xy;
	float4 result = tex2D(_USource, coord);
    float decay = 1.0 + _Dissipation * _Dt;
	return result / decay;
}

float4 divergenceShader(v2fbase i) : SV_Target {
	float L = tex2D(_UVelocity, i.vL).x;
	float R = tex2D(_UVelocity, i.vR).x;
	float T = tex2D(_UVelocity, i.vT).y;
	float B = tex2D(_UVelocity, i.vB).y;

	float2 C = tex2D(_UVelocity, i.vUv).xy;
	if (i.vL.x < 0.0) { L = -C.x; }
	if (i.vR.x > 1.0) { R = -C.x; }
	if (i.vT.y > 1.0) { T = -C.y; }
	if (i.vB.y < 0.0) { B = -C.y; }

	float div = 0.5 * (R - L + T - B);
	return float4(div, 0.0, 0.0, 1.0);
}

float4 curlShader(v2fbase i) : SV_Target {
	float L = tex2D(_UVelocity, i.vL).y;
	float R = tex2D(_UVelocity, i.vR).y;
	float T = tex2D(_UVelocity, i.vT).x;
	float B = tex2D(_UVelocity, i.vB).x;
	float vorticity = R - L - T + B;
	return float4(0.5 * vorticity, 0.0, 0.0, 1.0);
}

float4 vorticityShader(v2fbase i) : SV_Target {
	float L = tex2D(_UCurl, i.vL).x;
	float R = tex2D(_UCurl, i.vR).x;
	float T = tex2D(_UCurl, i.vT).x;
	float B = tex2D(_UCurl, i.vB).x;
	float C = tex2D(_UCurl, i.vUv).x;

	float2 force = 0.5 * float2(abs(T) - abs(B), abs(R) - abs(L));
	force /= length(force) + 0.0001;
	force *= 0.1 * C;
	force.y *= -1.0;

	float2 velocity = tex2D(_UVelocity, i.vUv).xy;
	velocity += force * _Dt;
	velocity = min(max(velocity, -1000.0), 1000.0);
	return float4(velocity, 0.0, 1.0);
}

float4 pressureShader(v2fbase i) : SV_Target {
	float L = tex2D(_UPressure, i.vL).x;
	float R = tex2D(_UPressure, i.vR).x;
	float T = tex2D(_UPressure, i.vT).x;
	float B = tex2D(_UPressure, i.vB).x;
	float C = tex2D(_UPressure, i.vUv).x;
	float divergence = tex2D(_UDivergence, i.vUv).x;
	float pressure = (L + R + B + T - divergence) * 0.25;
	return float4(pressure, 0.0, 0.0, 1.0);
}

float4 gradientSubtractShader(v2fbase i) : SV_Target {
	float L = tex2D(_UPressure, i.vL).x;
	float R = tex2D(_UPressure, i.vR).x;
	float T = tex2D(_UPressure, i.vT).x;
	float B = tex2D(_UPressure, i.vB).x;
	float2 velocity = tex2D(_UVelocity, i.vUv).xy;
	velocity.xy -= float2(R - L, T - B);
	return float4(velocity, 0.0, 1.0);
}
ENDCG

        Pass {
CGPROGRAM
#pragma vertex baseVertexShader
#pragma fragment frag

#include "UnityCG.cginc"

float4 frag (v2fbase i) : SV_Target {
    float4 col = tex2D(_MainTex, i.vUv);
    return col;
}
ENDCG
        }
    }
}

// https://github.com/PavelDoGreat/WebGL-Fluid-Simulation/blob/54ed78b00d7d8209790dd167dece747bfe9c5b88/script.js
// https://developer.nvidia.com/gpugems/gpugems/part-vi-beyond-triangles/chapter-38-fast-fluid-dynamics-simulation-gpu

// const baseVertexShader = compileShader(gl.VERTEX_SHADER, `
//     precision highp float;

//     attribute vec2 aPosition;
//     varying vec2 vUv;
//     varying vec2 vL;
//     varying vec2 vR;
//     varying vec2 vT;
//     varying vec2 vB;
//     uniform vec2 texelSize;

//     void main () {
//         vUv = aPosition * 0.5 + 0.5;
//         vL = vUv - vec2(texelSize.x, 0.0);
//         vR = vUv + vec2(texelSize.x, 0.0);
//         vT = vUv + vec2(0.0, texelSize.y);
//         vB = vUv - vec2(0.0, texelSize.y);
//         gl_Position = vec4(aPosition, 0.0, 1.0);
//     }
// `);

// const blurVertexShader = compileShader(gl.VERTEX_SHADER, `
//     precision highp float;

//     attribute vec2 aPosition;
//     varying vec2 vUv;
//     varying vec2 vL;
//     varying vec2 vR;
//     uniform vec2 texelSize;

//     void main () {
//         vUv = aPosition * 0.5 + 0.5;
//         float offset = 1.33333333;
//         vL = vUv - texelSize * offset;
//         vR = vUv + texelSize * offset;
//         gl_Position = vec4(aPosition, 0.0, 1.0);
//     }
// `);

// const blurShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision mediump float;
//     precision mediump sampler2D;

//     varying vec2 vUv;
//     varying vec2 vL;
//     varying vec2 vR;
//     uniform sampler2D uTexture;

//     void main () {
//         vec4 sum = texture2D(uTexture, vUv) * 0.29411764;
//         sum += texture2D(uTexture, vL) * 0.35294117;
//         sum += texture2D(uTexture, vR) * 0.35294117;
//         gl_FragColor = sum;
//     }
// `);

// const copyShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision mediump float;
//     precision mediump sampler2D;

//     varying highp vec2 vUv;
//     uniform sampler2D uTexture;

//     void main () {
//         gl_FragColor = texture2D(uTexture, vUv);
//     }
// `);

// const clearShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision mediump float;
//     precision mediump sampler2D;

//     varying highp vec2 vUv;
//     uniform sampler2D uTexture;
//     uniform float value;

//     void main () {
//         gl_FragColor = value * texture2D(uTexture, vUv);
//     }
// `);

// const colorShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision mediump float;

//     uniform vec4 color;

//     void main () {
//         gl_FragColor = color;
//     }
// `);

// const checkerboardShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision highp float;
//     precision highp sampler2D;

//     varying vec2 vUv;
//     uniform sampler2D uTexture;
//     uniform float aspectRatio;

//     #define SCALE 25.0

//     void main () {
//         vec2 uv = floor(vUv * SCALE * vec2(aspectRatio, 1.0));
//         float v = mod(uv.x + uv.y, 2.0);
//         v = v * 0.1 + 0.8;
//         gl_FragColor = vec4(vec3(v), 1.0);
//     }
// `);

// const displayShaderSource = `
//     precision highp float;
//     precision highp sampler2D;

//     varying vec2 vUv;
//     varying vec2 vL;
//     varying vec2 vR;
//     varying vec2 vT;
//     varying vec2 vB;
//     uniform sampler2D uTexture;
//     uniform sampler2D uBloom;
//     uniform sampler2D uSunrays;
//     uniform sampler2D uDithering;
//     uniform vec2 ditherScale;
//     uniform vec2 texelSize;

//     vec3 linearToGamma (vec3 color) {
//         color = max(color, vec3(0));
//         return max(1.055 * pow(color, vec3(0.416666667)) - 0.055, vec3(0));
//     }

//     void main () {
//         vec3 c = texture2D(uTexture, vUv).rgb;

//     #ifdef SHADING
//         vec3 lc = texture2D(uTexture, vL).rgb;
//         vec3 rc = texture2D(uTexture, vR).rgb;
//         vec3 tc = texture2D(uTexture, vT).rgb;
//         vec3 bc = texture2D(uTexture, vB).rgb;

//         float dx = length(rc) - length(lc);
//         float dy = length(tc) - length(bc);

//         vec3 n = normalize(vec3(dx, dy, length(texelSize)));
//         vec3 l = vec3(0.0, 0.0, 1.0);

//         float diffuse = clamp(dot(n, l) + 0.7, 0.7, 1.0);
//         c *= diffuse;
//     #endif

//     #ifdef BLOOM
//         vec3 bloom = texture2D(uBloom, vUv).rgb;
//     #endif

//     #ifdef SUNRAYS
//         float sunrays = texture2D(uSunrays, vUv).r;
//         c *= sunrays;
//     #ifdef BLOOM
//         bloom *= sunrays;
//     #endif
//     #endif

//     #ifdef BLOOM
//         float noise = texture2D(uDithering, vUv * ditherScale).r;
//         noise = noise * 2.0 - 1.0;
//         bloom += noise / 255.0;
//         bloom = linearToGamma(bloom);
//         c += bloom;
//     #endif

//         float a = max(c.r, max(c.g, c.b));
//         gl_FragColor = vec4(c, a);
//     }
// `;

// const bloomPrefilterShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision mediump float;
//     precision mediump sampler2D;

//     varying vec2 vUv;
//     uniform sampler2D uTexture;
//     uniform vec3 curve;
//     uniform float threshold;

//     void main () {
//         vec3 c = texture2D(uTexture, vUv).rgb;
//         float br = max(c.r, max(c.g, c.b));
//         float rq = clamp(br - curve.x, 0.0, curve.y);
//         rq = curve.z * rq * rq;
//         c *= max(rq, br - threshold) / max(br, 0.0001);
//         gl_FragColor = vec4(c, 0.0);
//     }
// `);

// const bloomBlurShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision mediump float;
//     precision mediump sampler2D;

//     varying vec2 vL;
//     varying vec2 vR;
//     varying vec2 vT;
//     varying vec2 vB;
//     uniform sampler2D uTexture;

//     void main () {
//         vec4 sum = vec4(0.0);
//         sum += texture2D(uTexture, vL);
//         sum += texture2D(uTexture, vR);
//         sum += texture2D(uTexture, vT);
//         sum += texture2D(uTexture, vB);
//         sum *= 0.25;
//         gl_FragColor = sum;
//     }
// `);

// const bloomFinalShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision mediump float;
//     precision mediump sampler2D;

//     varying vec2 vL;
//     varying vec2 vR;
//     varying vec2 vT;
//     varying vec2 vB;
//     uniform sampler2D uTexture;
//     uniform float intensity;

//     void main () {
//         vec4 sum = vec4(0.0);
//         sum += texture2D(uTexture, vL);
//         sum += texture2D(uTexture, vR);
//         sum += texture2D(uTexture, vT);
//         sum += texture2D(uTexture, vB);
//         sum *= 0.25;
//         gl_FragColor = sum * intensity;
//     }
// `);

// const sunraysMaskShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision highp float;
//     precision highp sampler2D;

//     varying vec2 vUv;
//     uniform sampler2D uTexture;

//     void main () {
//         vec4 c = texture2D(uTexture, vUv);
//         float br = max(c.r, max(c.g, c.b));
//         c.a = 1.0 - min(max(br * 20.0, 0.0), 0.8);
//         gl_FragColor = c;
//     }
// `);

// const sunraysShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision highp float;
//     precision highp sampler2D;

//     varying vec2 vUv;
//     uniform sampler2D uTexture;
//     uniform float weight;

//     #define ITERATIONS 16

//     void main () {
//         float Density = 0.3;
//         float Decay = 0.95;
//         float Exposure = 0.7;

//         vec2 coord = vUv;
//         vec2 dir = vUv - 0.5;

//         dir *= 1.0 / float(ITERATIONS) * Density;
//         float illuminationDecay = 1.0;

//         float color = texture2D(uTexture, vUv).a;

//         for (int i = 0; i < ITERATIONS; i++)
//         {
//             coord -= dir;
//             float col = texture2D(uTexture, coord).a;
//             color += col * illuminationDecay * weight;
//             illuminationDecay *= Decay;
//         }

//         gl_FragColor = vec4(color * Exposure, 0.0, 0.0, 1.0);
//     }
// `);

// const splatShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision highp float;
//     precision highp sampler2D;

//     varying vec2 vUv;
//     uniform sampler2D uTarget;
//     uniform float aspectRatio;
//     uniform vec3 color;
//     uniform vec2 point;
//     uniform float radius;

//     void main () {
//         vec2 p = vUv - point.xy;
//         p.x *= aspectRatio;
//         vec3 splat = exp(-dot(p, p) / radius) * color;
//         vec3 base = texture2D(uTarget, vUv).xyz;
//         gl_FragColor = vec4(base + splat, 1.0);
//     }
// `);

// const advectionShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision highp float;
//     precision highp sampler2D;

//     varying vec2 vUv;
//     uniform sampler2D uVelocity;
//     uniform sampler2D uSource;
//     uniform vec2 texelSize;
//     uniform vec2 dyeTexelSize;
//     uniform float dt;
//     uniform float dissipation;

//     vec4 bilerp (sampler2D sam, vec2 uv, vec2 tsize) {
//         vec2 st = uv / tsize - 0.5;

//         vec2 iuv = floor(st);
//         vec2 fuv = fract(st);

//         vec4 a = texture2D(sam, (iuv + vec2(0.5, 0.5)) * tsize);
//         vec4 b = texture2D(sam, (iuv + vec2(1.5, 0.5)) * tsize);
//         vec4 c = texture2D(sam, (iuv + vec2(0.5, 1.5)) * tsize);
//         vec4 d = texture2D(sam, (iuv + vec2(1.5, 1.5)) * tsize);

//         return mix(mix(a, b, fuv.x), mix(c, d, fuv.x), fuv.y);
//     }

//     void main () {
//     #ifdef MANUAL_FILTERING
//         vec2 coord = vUv - dt * bilerp(uVelocity, vUv, texelSize).xy * texelSize;
//         vec4 result = bilerp(uSource, coord, dyeTexelSize);
//     #else
//         vec2 coord = vUv - dt * texture2D(uVelocity, vUv).xy * texelSize;
//         vec4 result = texture2D(uSource, coord);
//     #endif
//         float decay = 1.0 + dissipation * dt;
//         gl_FragColor = result / decay;
//     }`,
//     ext.supportLinearFiltering ? null : ['MANUAL_FILTERING']
// );

// const divergenceShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision mediump float;
//     precision mediump sampler2D;

//     varying highp vec2 vUv;
//     varying highp vec2 vL;
//     varying highp vec2 vR;
//     varying highp vec2 vT;
//     varying highp vec2 vB;
//     uniform sampler2D uVelocity;

//     void main () {
//         float L = texture2D(uVelocity, vL).x;
//         float R = texture2D(uVelocity, vR).x;
//         float T = texture2D(uVelocity, vT).y;
//         float B = texture2D(uVelocity, vB).y;

//         vec2 C = texture2D(uVelocity, vUv).xy;
//         if (vL.x < 0.0) { L = -C.x; }
//         if (vR.x > 1.0) { R = -C.x; }
//         if (vT.y > 1.0) { T = -C.y; }
//         if (vB.y < 0.0) { B = -C.y; }

//         float div = 0.5 * (R - L + T - B);
//         gl_FragColor = vec4(div, 0.0, 0.0, 1.0);
//     }
// `);

// const curlShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision mediump float;
//     precision mediump sampler2D;

//     varying highp vec2 vUv;
//     varying highp vec2 vL;
//     varying highp vec2 vR;
//     varying highp vec2 vT;
//     varying highp vec2 vB;
//     uniform sampler2D uVelocity;

//     void main () {
//         float L = texture2D(uVelocity, vL).y;
//         float R = texture2D(uVelocity, vR).y;
//         float T = texture2D(uVelocity, vT).x;
//         float B = texture2D(uVelocity, vB).x;
//         float vorticity = R - L - T + B;
//         gl_FragColor = vec4(0.5 * vorticity, 0.0, 0.0, 1.0);
//     }
// `);

// const vorticityShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision highp float;
//     precision highp sampler2D;

//     varying vec2 vUv;
//     varying vec2 vL;
//     varying vec2 vR;
//     varying vec2 vT;
//     varying vec2 vB;
//     uniform sampler2D uVelocity;
//     uniform sampler2D uCurl;
//     uniform float curl;
//     uniform float dt;

//     void main () {
//         float L = texture2D(uCurl, vL).x;
//         float R = texture2D(uCurl, vR).x;
//         float T = texture2D(uCurl, vT).x;
//         float B = texture2D(uCurl, vB).x;
//         float C = texture2D(uCurl, vUv).x;

//         vec2 force = 0.5 * vec2(abs(T) - abs(B), abs(R) - abs(L));
//         force /= length(force) + 0.0001;
//         force *= curl * C;
//         force.y *= -1.0;

//         vec2 velocity = texture2D(uVelocity, vUv).xy;
//         velocity += force * dt;
//         velocity = min(max(velocity, -1000.0), 1000.0);
//         gl_FragColor = vec4(velocity, 0.0, 1.0);
//     }
// `);

// const pressureShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision mediump float;
//     precision mediump sampler2D;

//     varying highp vec2 vUv;
//     varying highp vec2 vL;
//     varying highp vec2 vR;
//     varying highp vec2 vT;
//     varying highp vec2 vB;
//     uniform sampler2D uPressure;
//     uniform sampler2D uDivergence;

//     void main () {
//         float L = texture2D(uPressure, vL).x;
//         float R = texture2D(uPressure, vR).x;
//         float T = texture2D(uPressure, vT).x;
//         float B = texture2D(uPressure, vB).x;
//         float C = texture2D(uPressure, vUv).x;
//         float divergence = texture2D(uDivergence, vUv).x;
//         float pressure = (L + R + B + T - divergence) * 0.25;
//         gl_FragColor = vec4(pressure, 0.0, 0.0, 1.0);
//     }
// `);

// const gradientSubtractShader = compileShader(gl.FRAGMENT_SHADER, `
//     precision mediump float;
//     precision mediump sampler2D;

//     varying highp vec2 vUv;
//     varying highp vec2 vL;
//     varying highp vec2 vR;
//     varying highp vec2 vT;
//     varying highp vec2 vB;
//     uniform sampler2D uPressure;
//     uniform sampler2D uVelocity;

//     void main () {
//         float L = texture2D(uPressure, vL).x;
//         float R = texture2D(uPressure, vR).x;
//         float T = texture2D(uPressure, vT).x;
//         float B = texture2D(uPressure, vB).x;
//         vec2 velocity = texture2D(uVelocity, vUv).xy;
//         velocity.xy -= vec2(R - L, T - B);
//         gl_FragColor = vec4(velocity, 0.0, 1.0);
//     }
// `);
