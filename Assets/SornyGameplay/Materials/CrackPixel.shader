Shader "DungeonDLC/Crack Pixel"
{
    Properties {
        [PerRendererData] _MainTex("Sprite",2D)="white"{}
        _Color("Cor",Color)=(1,1,1,1)
        _Cutoff("Recorte",Range(0,1))=.48
    }
    SubShader
    {
        Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout" "RenderPipeline"="UniversalPipeline" "CanUseSpriteAtlas"="True"}
        Cull Off ZWrite On
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes {float4 positionOS:POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct Varyings {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;};
            TEXTURE2D(_MainTex);SAMPLER(sampler_MainTex);
            CBUFFER_START(UnityPerMaterial)
            half4 _Color;float _Cutoff;
            CBUFFER_END
            Varyings vert(Attributes v){Varyings o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.uv=v.uv;o.color=v.color;return o;}
            half4 frag(Varyings i):SV_Target{half4 c=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,i.uv)*i.color*_Color;clip(c.a-_Cutoff);return half4(c.rgb,1);}
            ENDHLSL
        }
    }
}
