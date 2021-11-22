// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/PortalShaderEffect"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_LeftEyeTex ("Texture", 2D) = "white" {}
		_RightEyeTex ("Texture", 2D) = "white" {}
		_CenterEyeTex ("Texture", 2D) = "white" {}

		_RejectPositiveYNormals ("Float", Float) = 0
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Lighting Off
		Cull Back
		ZWrite On
		ZTest Less
		
		Fog{ Mode Off }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert 
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float4 normal : NORMAL;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				//float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD1;
				float3 localNormal : TEXCOORD2;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.localNormal = v.normal;
				o.screenPos = ComputeScreenPos(o.vertex);
				return o;
			}

			/// which eye we are rendering. 0 == left, 1 == right
			uniform int RenderingEye;
			uniform int IsStereoscopic;

			sampler2D _MainTex;
			sampler2D _LeftEyeTex;
			sampler2D _RightEyeTex;
			sampler2D _CenterEyeTex;

			float _RejectPositiveYNormals;

			fixed4 frag (v2f i) : SV_Target
			{
				if (_RejectPositiveYNormals > 0) {
					if (i.localNormal.y > 0) { 
						discard;
					}
                }
				
				i.screenPos /= i.screenPos.w;
				float2 portalUV = float2(i.screenPos.x, i.screenPos.y);

				if (IsStereoscopic) {
					if (RenderingEye == 0) {
						return tex2D(_LeftEyeTex, portalUV);
					} else {
						return tex2D(_RightEyeTex, portalUV);
					}
                } else {
					return tex2D(_CenterEyeTex, portalUV);
                }	
			}
			ENDCG
		}
	}
}
