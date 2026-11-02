// Balatro-style swirling background for Unity (Built-in RP or URP).
// Put this on a material, then put the material on a full-screen quad
// behind your scene (see BackgroundFitter.cs).
Shader "Custom/BalatroBackground"
{
    Properties
    {
        _Color1 ("Highlight Color", Color) = (0.87, 0.27, 0.23, 1)   // red
        _Color2 ("Mid Color", Color)       = (0.00, 0.42, 0.71, 1)   // blue
        _Color3 ("Dark Color", Color)      = (0.09, 0.14, 0.15, 1)   // near-black
        _Scale ("Noise Scale", Float) = 2.5
        _Speed ("Speed", Float) = 0.15
        _Swirl ("Swirl Amount", Float) = 2.0
        _SpinSpeed ("Spin Speed", Float) = 0.2
        _Warp ("Warp Strength", Float) = 1.0
        _Contrast ("Contrast", Float) = 1.6
        _PixelSize ("Pixel Size (0 = off)", Float) = 3
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        ZWrite On   // must write depth, or the skybox paints over the quad
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float4 _Color1, _Color2, _Color3;
            float _Scale, _Speed, _Swirl, _SpinSpeed, _Warp, _Contrast, _PixelSize;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f     { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            // ---------- Perlin (gradient) noise ----------
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453);
            }

            float perlin(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                // quintic fade curve (classic Perlin)
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float a = dot(hash2(i),                  f);
                float b = dot(hash2(i + float2(1, 0)),   f - float2(1, 0));
                float c = dot(hash2(i + float2(0, 1)),   f - float2(0, 1));
                float d = dot(hash2(i + float2(1, 1)),   f - float2(1, 1));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Cheap fBm: just 2 octaves (used for the warp offsets)
            float fbm2(float2 p)
            {
                return 0.5 * perlin(p) + 0.25 * perlin(p * 2.02 + float2(1.7, 9.2));
            }

            // 3 octaves for the final visible pattern
            float fbm3(float2 p)
            {
                return 0.5   * perlin(p)
                     + 0.25  * perlin(p * 2.02 + float2(1.7, 9.2))
                     + 0.125 * perlin(p * 4.08 + float2(8.3, 2.8));
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Optional chunky-pixel look
                if (_PixelSize > 0)
                {
                    float2 res = _ScreenParams.xy / _PixelSize;
                    uv = floor(uv * res) / res;
                }

                // Center and correct for aspect ratio
                float2 p = uv - 0.5;
                p.x *= _ScreenParams.x / _ScreenParams.y;

                // Swirl: rotate each point by an angle that grows with distance
                float r = length(p);
                float ang = atan2(p.y, p.x) + _Swirl * r + _Time.y * _SpinSpeed;
                p = float2(cos(ang), sin(ang)) * r * _Scale;

                float t = _Time.y * _Speed;

                // Domain warping: one warp layer instead of two (much cheaper)
                float2 q = float2(fbm2(p + t),
                                  fbm2(p + float2(5.2, 1.3) - t));
                float n = fbm3(p + 6.0 * _Warp * q);

                n = saturate(n * _Contrast + 0.5);

                // Map noise to 3 color bands with soft edges (the "paint" look)
                float3 col = lerp(_Color3.rgb, _Color2.rgb, smoothstep(0.35, 0.45, n));
                col = lerp(col, _Color1.rgb, smoothstep(0.55, 0.65, n));

                return fixed4(col, 1);
            }
            ENDCG
        }
    }
}
