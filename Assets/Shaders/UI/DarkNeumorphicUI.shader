Shader "CarGame/UI/Dark Neumorphic"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Graphic Tint", Color) = (1, 1, 1, 1)
        _BaseColor ("Base Color", Color) = (0.075, 0.082, 0.095, 1)
        _LightColor ("Light Edge Color", Color) = (0.18, 0.20, 0.23, 1)
        _DarkColor ("Dark Edge Color", Color) = (0.01, 0.012, 0.016, 1)
        _CornerRadius ("Corner Radius", Range(0, 0.5)) = 0.16
        _ShapePadding ("Shape Padding", Range(0, 0.25)) = 0.075
        _Aspect ("Rect Aspect", Float) = 1
        _EdgeSoftness ("Edge Softness", Range(0.001, 0.08)) = 0.012
        _BevelSize ("Bevel Size", Range(0.001, 0.25)) = 0.095
        _HighlightStrength ("Highlight Strength", Range(0, 1)) = 0.45
        _SurfaceShadowStrength ("Surface Shadow Strength", Range(0, 1)) = 0.72
        _Inset ("Inset Amount", Range(0, 1)) = 0
        _PressAmount ("Press Amount", Range(0, 1)) = 0
        _PressedShadowFade ("Pressed Shadow Fade", Range(0, 1)) = 0.78
        _PressedDarken ("Pressed Surface Darken", Range(0, 0.25)) = 0.07
        _ShadowOffset ("Outer Shadow Offset", Vector) = (0.028, -0.028, 0, 0)
        _ShadowSoftness ("Outer Shadow Softness", Range(0.001, 0.18)) = 0.06
        _ShadowSpread ("Outer Shadow Spread", Range(-0.08, 0.08)) = 0.015
        _ShadowOpacity ("Dark Shadow Opacity", Range(0, 1)) = 0.55
        _LightShadowOpacity ("Light Shadow Opacity", Range(0, 1)) = 0.18
        _LightDirection ("Light Direction", Vector) = (-1, 1, 0, 0)

        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;

            fixed4 _BaseColor;
            fixed4 _LightColor;
            fixed4 _DarkColor;
            float _CornerRadius;
            float _ShapePadding;
            float _Aspect;
            float _EdgeSoftness;
            float _BevelSize;
            float _HighlightStrength;
            float _SurfaceShadowStrength;
            float _Inset;
            float _PressAmount;
            float _PressedShadowFade;
            float _PressedDarken;
            float4 _ShadowOffset;
            float _ShadowSoftness;
            float _ShadowSpread;
            float _ShadowOpacity;
            float _LightShadowOpacity;
            float4 _LightDirection;

            float RoundedRectSDF(float2 uv, float aspect, float radius)
            {
                aspect = max(0.001, aspect);
                float2 p = uv - 0.5;
                p.x *= aspect;

                float2 halfSize = float2(0.5 * aspect, 0.5);
                float r = min(radius, min(halfSize.x, halfSize.y) - 0.0001);
                float2 q = abs(p) - (halfSize - r);
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            float ShapeMask(float signedDistance, float softness)
            {
                return saturate(1.0 - smoothstep(0.0, max(0.0001, softness), signedDistance));
            }

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.uv = v.texcoord;
                OUT.color = v.color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 texColor = (tex2D(_MainTex, IN.uv) + _TextureSampleAdd) * IN.color * _Color;

                float padding = saturate(_ShapePadding);
                float paddedScale = max(0.001, 1.0 - padding * 2.0);
                float2 shapeUv = (IN.uv - padding) / paddedScale;
                float aspect = max(0.001, _Aspect);
                float radius = saturate(_CornerRadius);
                float signedDistance = RoundedRectSDF(shapeUv, aspect, radius);
                float shape = ShapeMask(signedDistance, _EdgeSoftness);

                float2 offset = _ShadowOffset.xy;
                float darkShadowDistance = RoundedRectSDF(shapeUv - offset, aspect, radius + _ShadowSpread);
                float lightShadowDistance = RoundedRectSDF(shapeUv + offset, aspect, radius + _ShadowSpread);
                float outside = 1.0 - shape;
                float press = saturate(_PressAmount);
                float pressShadowFade = lerp(1.0, 1.0 - _PressedShadowFade, press);
                float darkShadow = ShapeMask(darkShadowDistance, _ShadowSoftness) * outside * _ShadowOpacity * pressShadowFade;
                float lightShadow = ShapeMask(lightShadowDistance, _ShadowSoftness) * outside * _LightShadowOpacity * pressShadowFade;

                float insideDistance = max(0.0, -signedDistance);
                float edgeBand = 1.0 - smoothstep(0.0, max(0.0001, _BevelSize), insideDistance);

                float2 normalDirection = shapeUv - 0.5;
                normalDirection.x *= aspect;
                normalDirection = normalize(normalDirection + float2(0.0001, 0.0001));

                float2 lightDirection = normalize(_LightDirection.xy + float2(0.0001, 0.0001));
                float directional = dot(normalDirection, lightDirection);
                float raisedLight = edgeBand * saturate(directional) * _HighlightStrength;
                float raisedDark = edgeBand * saturate(-directional) * _SurfaceShadowStrength;

                float inset = saturate(max(_Inset, press));
                float finalLight = lerp(raisedLight, raisedDark, inset);
                float finalDark = lerp(raisedDark, raisedLight, inset);

                float3 surface = _BaseColor.rgb * texColor.rgb;
                surface *= 1.0 - (press * _PressedDarken);
                surface = lerp(surface, _LightColor.rgb, finalLight);
                surface = lerp(surface, _DarkColor.rgb, finalDark);

                float shadowAlpha = saturate(darkShadow + lightShadow);
                float3 shadowColor = (_DarkColor.rgb * darkShadow + _LightColor.rgb * lightShadow) / max(0.0001, shadowAlpha);
                float3 rgb = lerp(shadowColor, surface, shape);
                float alpha = saturate(shape * texColor.a * _BaseColor.a + shadowAlpha * outside);

                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(alpha - 0.001);
                #endif

                return fixed4(rgb, alpha);
            }
            ENDCG
        }
    }
}
