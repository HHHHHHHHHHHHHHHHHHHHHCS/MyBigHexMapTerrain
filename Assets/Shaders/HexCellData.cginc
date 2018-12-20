sampler2D _HexCellData;
float4 _HexCellData_TexelSize;

//编辑模式下可见度
inline float4 FilterCellData(float4 data)
{
	#if defined(HEX_MAP_EDIT_MODE)
		data.xy = 1;
	#endif
	return data;
}


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
	return FilterCellData(data);
}

//计算可见度 2个cell用的
float  GetVisibilityBy2(inout appdata_full v)
{
	float4 cell0 = GetCellData(v, 0);
	float4 cell1 = GetCellData(v, 1);
	
	float2 visibility;
	visibility.x = cell0.x * v.color.x + cell1.x * v.color.y;
	visibility.x = lerp(0.25, 1, visibility.x);
	visibility.y = cell0.y * v.color.x + cell1.y * v.color.y;
	return visibility;
}

//计算可见度 3个cell用的
float  GetVisibilityBy3(inout appdata_full v)
{
	float4 cell0 = GetCellData(v, 0);
	float4 cell1 = GetCellData(v, 1);
	float4 cell2 = GetCellData(v, 2);
	
	float2 visibility;
	visibility.x = cell0.x * v.color.x + cell1.x * v.color.y + cell2.x * v.color.z;
	visibility.x = lerp(0.25, 1, visibility.x);
	visibility.y = cell0.y * v.color.x + cell1.y * v.color.y + cell2.y * v.color.z;
	return visibility;
}
