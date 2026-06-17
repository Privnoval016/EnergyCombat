// Molten/charred effect creeping from the cut plane across surrounding faces.
// Set _BurnProgress via MaterialPropertyBlock (0=none, 1=fully burnt).
// Three zones: molten core (near cut), ember glow edge, char crust.
Shader "BladeMode/BurnEdge"
{
    Properties
    {
        _MainTex            ("Base Texture",      2D)          = "white" {}
        _BaseColor          ("Base Color",         Color)       = (1,1,1,1)
        _CharColor          ("Char Color",          Color)       = (0.05,0.03,0.02,1)
        [HDR] _EmberColor   ("Ember Glow",         Color)       = (1.0,0.25,0.0,1)
        [HDR] _MoltenColor  ("Molten Core",        Color)       = (1.0,0.65,0.1,1)
        _EmberIntensity     ("Ember Emission",      Float)       = 4.0
        _NoiseScale         ("Noise Scale",         Range(2,40)) = 9.0
        _CutPlaneNormal     ("Cut Plane Normal",    Vector)      = (0,1,0,0)
        _CutPlaneOrigin     ("Cut Plane Origin",    Vector)      = (0,0,0,0)
        _BurnProgress       ("Burn Progress",       Range(0,1))  = 0.0
        _MaxBurnWidth       ("Max Burn Width",      Float)       = 0.45
        _CharWidth          ("Char Zone Width",     Float)       = 0.18
        _EmberEdgeWidth     ("Ember Edge Width",    Float)       = 0.06
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

            float Hash21(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5); }
            float SmoothNoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(Hash21(i),              Hash21(i + float2(1,0)), u.x),
                            lerp(Hash21(i + float2(0,1)), Hash21(i + float2(1,1)), u.x), u.y);
            }
            float FBM(float2 p)
            {
                float v = 0, a = 0.5;
                for (int i = 0; i < 4; i++) { v += a * SmoothNoise(p); p *= 2.1; a *= 0.5; }
                return v;
            }

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _BaseColor, _CharColor, _EmberColor, _MoltenColor;
                float  _EmberIntensity, _NoiseScale;
                float4 _CutPlaneNormal, _CutPlaneOrigin;
                float  _BurnProgress, _MaxBurnWidth, _CharWidth, _EmberEdgeWidth;
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

                // FBM noise displaces the burn front to create an organic flame-edge silhouette
                float2 noiseUV = IN.positionWS.xz * _NoiseScale + IN.positionWS.y * 0.4;
                float  noise   = FBM(noiseUV);
                float  nDist   = dist - (noise - 0.5) * _EmberEdgeWidth * 2.5;

                float burnFront = _BurnProgress * _MaxBurnWidth;
                float active    = step(nDist, burnFront);

                // Three zone blends: char, ember glow, molten core
                float charBlend  = saturate(1.0 - nDist / max(_CharWidth, 0.0001));
                float edgeGlow   = pow(saturate(1.0 - abs(nDist - _CharWidth) / _EmberEdgeWidth), 1.8);
                float moltenCore = charBlend * (1.0 - noise * 0.4);

                float3 charSurface = lerp(base.rgb, _CharColor.rgb, charBlend);
                float3 withEmber   = charSurface + _EmberColor.rgb * edgeGlow * _EmberIntensity;
                float3 withMolten  = lerp(withEmber, _MoltenColor.rgb * _EmberIntensity, moltenCore * 0.5);
                // Extra HDR spike at ember edge for bloom
                withMolten += _EmberColor.rgb * edgeGlow * _EmberIntensity;

                float3 final = lerp(base.rgb, withMolten, active);
                return float4(final, 1.0);
            }
            ENDHLSL
        }
    }
}
