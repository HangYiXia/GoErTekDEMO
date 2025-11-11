// Adapted from: 
// https://zenn.dev/r_ngtm/articles/urp-tessellation

float _TessFactor;


struct a2v {
	float4 vertex : POSITION;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	float4 texcoord : TEXCOORD0;
};

struct v2h {
	float4 pos : POS;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	float2 uv : TEXCOORD0;
};

struct HSControlPointOutput{
	float4 pos : POS;
	float3 normal : NORMAL;
	float4 tangent : TANGENT;
	float2 uv : TEXCOORD0;
};

struct HSConstantOutput {
	float tessFactor[3] : SV_TessFactor;
	float insideTessFactor : SV_InsideTessFactor;
};

struct d2f {
	float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
};

v2h vertTess(a2v v) {
	v2h o;

	o.pos = v.vertex;
	// o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
	o.uv = v.texcoord.xy;

	return o;
}

[domain("tri")]
[partitioning("integer")]
[outputtopology("triangle_cw")]
[patchconstantfunc("hullConst")]
[outputcontrolpoints(3)]
HSControlPointOutput hull(InputPatch<v2h, 3> i, uint id : SV_OutputControlPointID) {
	HSControlPointOutput o;

	o.pos = i[id].pos;
	o.normal = i[id].normal;
	o.tangent = i[id].tangent;
	o.uv = i[id].uv;

	return o;
}

HSConstantOutput hullConst() {
	HSConstantOutput o;

	o.tessFactor[0] = _TessFactor;
	o.tessFactor[1] = _TessFactor;
	o.tessFactor[2] = _TessFactor;
	o.insideTessFactor = _TessFactor;

	return o;
}

[domain("tri")]
d2f domain(HSConstantOutput hsConst, const OutputPatch<HSControlPointOutput, 3> i, float3 bary : SV_DomainLocation) {
	d2f o;

	float4 pos = bary.x * i[0].pos + bary.y * i[1].pos + bary.z * i[2].pos;
	float2 uv = bary.x * i[0].uv + bary.y * i[1].uv + bary.z * i[2].uv;

	o.pos = TransformObjectToHClip(pos);
	o.uv = uv;

	return o;
}

