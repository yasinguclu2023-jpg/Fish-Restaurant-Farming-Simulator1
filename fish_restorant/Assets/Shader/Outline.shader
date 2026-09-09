Shader "Custom/OutlineOnly"
{
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.02
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        
        Pass
        {
            Cull Front
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            
            float _OutlineWidth;
            float4 _OutlineColor;
            
            float4 vert(float4 vertex : POSITION, float3 normal : NORMAL) : SV_POSITION
            {
                return UnityObjectToClipPos(vertex + normalize(normal) * _OutlineWidth);
            }
            
            fixed4 frag() : SV_Target { return _OutlineColor; }
            ENDCG
        }
    }
}