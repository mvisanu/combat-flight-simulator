Shader "PacificCombat/Ocean"
{
    Properties { _BaseColor ("Ocean", Color) = (0.025,0.16,0.23,1) }
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
            half4 frag(Varyings i):SV_Target
            {
                float t=_Time.y;
                float2 p=i.world.xz;
                float distanceToCamera=distance(_WorldSpaceCameraPos,i.world);
                float waveStrength=.035*exp(-distanceToCamera*.0007);
                float3 n=normalize(float3(FilteredWave(p.x*.23+p.y*.08+t*.8)*waveStrength,1,FilteredWave(p.y*.19-p.x*.07+t*.7)*waveStrength));
                float3 v=GetWorldSpaceNormalizeViewDir(i.world);
                Light sun=GetMainLight();
                float fresnel=pow(1-saturate(dot(n,v)),4);
                float glint=pow(saturate(dot(n,normalize(sun.direction+v))),120);
                float ripples=FilteredWave(p.x*.07+p.y*.06+t*.4)*.003*exp(-distanceToCamera*.0005);
                half3 color=lerp(_BaseColor.rgb+ripples,half3(.40,.59,.66),fresnel*.6)+sun.color*glint*.45;
                return half4(MixFog(color,i.fog),1);
            }
            ENDHLSL
        }
    }
}
