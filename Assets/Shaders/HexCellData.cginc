sampler2D _HexCellData;
float4 _HexCellData_TexelSize;

//加0.5是为了像素中心采样
//我们是对顶点采样所以用tex2dlod 但是没有mipmap 所以后面为0,0
float4 GetCellData(appdata_full v, int index)
{
	float2 uv;
	uv.x = (v.texcoord2[index] + 0.5) * _HexCellData_TexelSize.x;
	float row = floor(uv.x);
	uv.x -= row;
	uv.y = (row + 0.5) * _HexCellData_TexelSize.y;
	float4 data = tex2Dlod(_HexCellData, float4(uv, 0, 0));
	data.w *= 255;//颜色会被压缩到0-1 w是地图类型
	return data;
}
