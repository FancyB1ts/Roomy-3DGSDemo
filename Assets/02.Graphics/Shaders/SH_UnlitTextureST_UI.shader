Shader "Custom/UnlitTexture_ST_UI"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Base Color", Color) = (1,1,1,1)
    }

    SubShader
    {
        // UI & world friendly
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100

        Pass
        {
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;   // tiling (xy) + offset (zw)
            float4 _BaseColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                // Honor _MainTex_ST so C# tiling/offset (ST-based fit) drives letterboxing
                o.uv  = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // Detect if the transformed UVs fall outside 0..1 after ST fitting
                bool outX = (uv.x < 0.0) || (uv.x > 1.0);
                bool outY = (uv.y < 0.0) || (uv.y > 1.0);
                if (outX || outY)
                {
                    // Fill letterbox area with the chosen base color
                    return _BaseColor;
                }

                // Inside the image: sample texture (clamp to 0..1 for safety)
                uv = saturate(uv);
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv) * _BaseColor;
                return col;
            }
            ENDHLSL
        }
    }
}