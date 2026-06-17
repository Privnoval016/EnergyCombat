// Ice crystalline effect that creeps from the cut plane across surrounding faces.
// Set _FreezeProgress via MaterialPropertyBlock (0=none, 1=fully frozen).
// Set _CutPlaneNormal and _CutPlaneOrigin to match the cut that was executed.
Shader "BladeMode/FrozenEdge"
{
    Properties
    {
        _MainTex            ("Base Texture",     2D)          = "white" {}
        _BaseColor          ("Base Color",        Color)       = (1,1,1,1)
        [HDR] _IceColor     ("Ice Color",         Color)       = (0.55, 0.82, 1.0, 1.0)
        [HDR] _IceGlow      ("Ice Rim Glow",      Color)       = (0.4, 0.75, 1.0, 1.0)
        _FresnelPow         ("Fresnel Power",      Range(1,8)) = 3.0
        _CrystalScale       ("Crystal Scale",      Range(2,60)) = 18.0
        _CrystalContrast    ("Crystal Contrast",   Range(0,1)) = 0.65
        _CutPlaneNormal     ("Cut Plane Normal",   Vector)     = (0,1,0,0)
        _CutPlaneOrigin     ("Cut Plane Origin",   Vector)     = (0,0,0,0)
        _FreezeProgress     ("Freeze Progress",    Range(0,1)) = 0.0
        _MaxFreezeWidth     ("Max Freeze Width",   Float)      = 0.35
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // F1 Voronoi noise for crystal cell pattern
            float2 VHash(float2 p)
            {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)),
                                      dot(p, float2(269.5, 183.3)))) * 43758.5);
            }
            float Voronoi(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float md = 8.0;
                for (int x = -1; x <= 1; x++)
                    for (int y = -1; y <= 1; y++)
                    {
                        float2 n = float2(x, y);
                        float2 d = n + VHash(i + n) - f;
                        md = min(md, dot(d, d));
                    }
                return sqrt(md);
            }

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor, _IceColor, _IceGlow;
                float  _FresnelPow, _CrystalScale, _CrystalContrast;
                float4 _CutPlaneNormal, _CutPlaneOrigin;
                float  _FreezeProgress, _MaxFreezeWidth;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalOS   : NORMAL;
            };
            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionHCS = p.positionCS;
                OUT.positionWS  = p.positionWS;
                OUT.normalWS    = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float4 base = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * _BaseColor;

                float3 pN   = normalize(_CutPlaneNormal.xyz);
                float  dist = abs(dot(IN.positionWS - _CutPlaneOrigin.xyz, pN));
                float  zone = _FreezeProgress * _MaxFreezeWidth;
                float  blend = pow(saturate(1.0 - dist / max(zone, 0.0001)), 0.6);

                float2 noiseCoord = IN.positionWS.xz * _CrystalScale + IN.positionWS.y;
                float  crystal = saturate(Voronoi(noiseCoord) - _CrystalContrast);

                float3 viewDir = normalize(GetWorldSpaceViewDir(IN.positionWS));
                float  fresnel = pow(1.0 - saturate(dot(viewDir, normalize(IN.normalWS))), _FresnelPow);

                float3 iceSurface = lerp(_IceColor.rgb, _IceColor.rgb + crystal * 0.3, 0.6);
                float3 iceWithRim = iceSurface + _IceGlow.rgb * fresnel;
                float3 final      = lerp(base.rgb, iceWithRim, blend);

                return float4(final, 1.0);
            }
            ENDHLSL
        }
    }
}
