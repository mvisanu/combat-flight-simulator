Shader "PacificCombat/Ocean"
{
    Properties { _BaseColor ("Ocean", Color) = (0.018,0.125,0.19,1) }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float fog:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            Varyings vert(Attributes input) { Varyings o; o.world=TransformObjectToWorld(input.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.fog=ComputeFogFactor(o.positionCS.z); return o; }
            float FilteredWave(float phase) { float footprint=fwidth(phase); return sin(phase)*exp(-footprint*footprint*.5); }
            float WaterHash(float2 p) { float3 q=frac(float3(p.xyx)*.1031); q+=dot(q,q.yzx+33.33); return frac((q.x+q.y)*q.z); }
            float WaterNoise(float2 p) { float2 q=floor(p),f=frac(p); f=f*f*(3-2*f); return lerp(lerp(WaterHash(q),WaterHash(q+float2(1,0)),f.x),lerp(WaterHash(q+float2(0,1)),WaterHash(q+1),f.x),f.y); }
            half4 frag(Varyings i):SV_Target
            {
                float t=_Time.y;
                float2 p=i.world.xz;
                float distanceToCamera=distance(_WorldSpaceCameraPos,i.world);
                // Randomized surface normals fade before their features become subpixel;
                // long coherent sine waves otherwise turn into diagonal moire at altitude.
                float footprint=max(length(ddx(p)),length(ddy(p)));
                float waveStrength=.06*exp(-distanceToCamera*.001)/(1+footprint*.08);
                float2 surface=float2(WaterNoise(p*.045+float2(t*.06,0)),WaterNoise(p*.041+float2(37,t*.045)))-.5;
                float3 n=normalize(float3(surface.x*waveStrength,1,surface.y*waveStrength));
                float3 v=GetWorldSpaceNormalizeViewDir(i.world);
                Light sun=GetMainLight();
                float fresnel=pow(1-saturate(dot(n,v)),4);
                float glint=pow(saturate(dot(n,normalize(sun.direction+v))),180);
                float ripples=(WaterNoise(p*.009)-.5)*.002*exp(-distanceToCamera*.0006);
                float broad=(WaterNoise(p*.0007)-.5)*.2;
                half3 reflection=lerp(half3(.26,.45,.59),half3(.54,.67,.73),fresnel);
                half3 color=lerp(_BaseColor.rgb+ripples+broad*.008,reflection,.1+fresnel*.58)+sun.color*glint*.68;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
