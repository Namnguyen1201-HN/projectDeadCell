Shader "ProjectDeadCell/Autumn Emerald Grass"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                fixed4 color : COLOR;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            v2f vert(appdata input)
            {
                v2f output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color * _Color;
                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 source = tex2D(_MainTex, input.uv) * input.color;
                float warmChroma = max(source.r, source.g) - source.b;
                float foliageChroma = max(source.r, source.g) - min(source.r, source.g);
                float warmFoliage = smoothstep(0.025, 0.09, warmChroma) *
                                    smoothstep(0.10, 0.24, max(source.r, source.g)) *
                                    smoothstep(0.01, 0.07, max(warmChroma, foliageChroma));
                float lightness = dot(source.rgb, fixed3(0.299, 0.587, 0.114));
                fixed3 emerald = fixed3(
                    0.06 + lightness * 0.20,
                    0.42 + lightness * 0.52,
                    0.34 + lightness * 0.48);
                source.rgb = lerp(source.rgb, emerald, saturate(warmFoliage));
                return source;
            }
            ENDCG
        }
    }
}
