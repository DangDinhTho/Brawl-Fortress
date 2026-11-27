// Greek maps: the top of the sanctuary's marble platform (Sanctuary.cs, mesh SanctuaryArt.Platform, submesh 1; uv = xz /
// top radius, -1..1). Laid like an ancient temenos: around the altar a laurel wreath on a pebble mosaic, then two rings
// of marble slabs with their joints, then a black band with a terracotta key (meander) pattern like a red-figure vase,
// a cream marble edge. Warm firelight from the altar flickers over it and soft gold rings spread out from the altar
// (the healing). Unlit, procedural, cheap for mobile; drawn opaque-looking in the transparent queue.
Shader "BrawlFortress/Sanctuary"
{
    Properties
    {
        _Marble ("Marble", Color) = (0.95, 0.92, 0.85, 1)
        _Vein ("Veins / joints", Color) = (0.72, 0.68, 0.62, 1)
        _Mosaic ("Pebble mosaic", Color) = (0.30, 0.33, 0.38, 1)
        _Laurel ("Laurel", Color) = (0.42, 0.56, 0.26, 1)
        _Terra ("Terracotta", Color) = (0.86, 0.45, 0.22, 1)
        _Black ("Black glaze", Color) = (0.12, 0.10, 0.10, 1)
        _Fire ("Firelight", Color) = (1.0, 0.62, 0.25, 1)
        _Glow ("Healing glow", Color) = (1.0, 0.86, 0.45, 1)
        _Segments ("Key pattern repeats", Float) = 20
        _Pulse ("Seconds between healing rings", Float) = 2.4
    }
    SubShader
    {
        Tags { "Queue" = "Transparent-9" "RenderType" = "Transparent" "IgnoreProjector" = "True" "ForceNoShadowCasting" = "True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"
            // float only: half with step() / comparisons does not compile for Metal / GLES3 / Vulkan
            float4 _Marble, _Vein, _Mosaic, _Laurel, _Terra, _Black, _Fire, _Glow;
            float _Segments, _Pulse;
            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; UNITY_FOG_COORDS(1) };
            v2f vert (appdata v) { v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; UNITY_TRANSFER_FOG(o, o.pos); return o; }
            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453); }
            float box(float2 p, float2 a, float2 b) { return step(a.x, p.x) * step(p.x, b.x) * step(a.y, p.y) * step(p.y, b.y); }
            float4 frag (v2f i) : SV_Target
            {
                float t = _Time.y;
                float r = length(i.uv);
                float ang = atan2(i.uv.y, i.uv.x) / 6.2831853 + 0.5;
                // marble with faint veins
                float v1 = abs(sin(i.uv.x * 11.0 + sin(i.uv.y * 6.0 + i.uv.x * 2.0) * 1.6 + i.uv.y * 3.0));
                float vein = (1.0 - smoothstep(0.0, 0.06, v1)) * (0.5 + 0.5 * sin(i.uv.y * 4.0 + 0.7));
                float3 c = lerp(_Marble.rgb, _Vein.rgb, vein * 0.35);

                // two rings of slabs with radial joints and a little tone per slab
                float ringA = step(0.50, r) * step(r, 0.68), ringB = step(0.68, r) * step(r, 0.85);
                float na = 10.0, nb = 16.0;
                float sa = floor(ang * na), sb = floor(ang * nb + 0.5);
                float slabTone = ringA * hash(float2(sa, 1.0)) + ringB * hash(float2(sb, 2.0));
                c *= 1.0 - 0.06 * slabTone * (ringA + ringB);
                float ja = 1.0 - smoothstep(0.0, 0.012, min(frac(ang * na), 1.0 - frac(ang * na)) * r * 6.2831853 / na);
                float jb = 1.0 - smoothstep(0.0, 0.012, min(frac(ang * nb + 0.5), 1.0 - frac(ang * nb + 0.5)) * r * 6.2831853 / nb);
                float joints = ringA * ja + ringB * jb
                             + (1.0 - smoothstep(0.0, 0.012, abs(r - 0.50))) + (1.0 - smoothstep(0.0, 0.012, abs(r - 0.68))) + (1.0 - smoothstep(0.0, 0.012, abs(r - 0.85)));
                c = lerp(c, _Vein.rgb, saturate(joints) * 0.8);

                // pebble mosaic around the altar with a laurel wreath
                float inMosaic = step(r, 0.50);
                float2 pg = i.uv * 26.0; float2 pc = floor(pg); float2 pf = frac(pg) - 0.5;
                float pebble = 1.0 - smoothstep(0.32, 0.45, length(pf));
                float3 mosaic = lerp(_Mosaic.rgb * 0.75, _Mosaic.rgb * (0.95 + 0.25 * hash(pc)), pebble);
                float rw = 0.40, vband = (r - (rw - 0.075)) / 0.15;
                float N = 26.0;
                float u = frac(ang * N);
                float side = step(0.5, frac(ang * N * 0.5));
                float v0 = lerp(0.30, 0.70, side);
                float leafW = 0.22 * sin(u * 3.14159);
                float leaf = step(abs(vband - v0), leafW) * step(0.0, vband) * step(vband, 1.0);
                float stem = (1.0 - smoothstep(0.0, 0.05, abs(vband - 0.5))) * step(0.0, vband) * step(vband, 1.0);
                mosaic = lerp(mosaic, _Laurel.rgb * (0.85 + 0.3 * u), saturate(leaf));
                mosaic = lerp(mosaic, _Laurel.rgb * 0.6, stem * (1.0 - leaf));
                c = lerp(c, mosaic, inMosaic);

                // black band with a terracotta key pattern (red-figure vase)
                float inBand = step(0.865, r) * step(r, 0.965);
                float2 p = float2(frac(ang * _Segments), saturate((r - 0.865) / 0.10));
                float key = box(p, float2(0.0, 0.08), float2(1.0, 0.20)) + box(p, float2(0.0, 0.80), float2(1.0, 0.92))
                          + box(p, float2(0.0, 0.08), float2(0.12, 0.66)) + box(p, float2(0.0, 0.54), float2(0.62, 0.66))
                          + box(p, float2(0.50, 0.32), float2(0.62, 0.66)) + box(p, float2(0.26, 0.32), float2(0.62, 0.44));
                float3 band = lerp(_Black.rgb, _Terra.rgb, saturate(key));
                c = lerp(c, band, inBand);
                float edge = step(0.965, r);
                c = lerp(c, _Marble.rgb * 0.97, edge);

                // firelight from the altar, flickering
                float flick = 0.8 + 0.12 * sin(t * 9.0) + 0.08 * sin(t * 13.7 + 1.3);
                float fire = (1.0 - smoothstep(0.15, 0.75, r)) * 0.32 * flick;
                c = lerp(c, c * _Fire.rgb * 1.35, fire);

                // soft gold rings spreading out from the altar: the healing
                float ph = frac(t / _Pulse);
                float rr = lerp(0.32, 0.86, ph);
                float ring = (1.0 - smoothstep(0.0, 0.03, abs(r - rr))) * (1.0 - ph);
                c = lerp(c, _Glow.rgb, ring * 0.6);

                float a = 1.0 - smoothstep(0.995, 1.01, r);
                float4 o = float4(c, a);
                UNITY_APPLY_FOG(i.fogCoord, o);
                return o;
            }
            ENDCG
        }
    }
    Fallback Off
}
