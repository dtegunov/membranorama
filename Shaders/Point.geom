#version 440
//#extension GL_ARB_bindless_texture : require

uniform mat4 worldViewProjMatrix;
uniform float cubeSize;

layout (points) in;
layout (triangle_strip, max_vertices = 14) out;

in VS_OUT
{
	vec4 color;
	flat bool selected;
	mat3 orientation;
} gs_in[];

out GS_OUT
{
	vec4 color;
	flat bool selected;
} gs_out;

void main(void)
{	
	vec4 center = gl_in[0].gl_Position;
	vec4 color = gs_in[0].color;
	bool selected = gs_in[0].selected;
	
	vec3 tx = gs_in[0].orientation * vec3(cubeSize * 0.5f, 0.0f, 0.0f);
	vec3 ty = gs_in[0].orientation * vec3(0.0f, cubeSize * 0.5f, 0.0f);
	vec3 tz = gs_in[0].orientation * vec3(0.0f, 0.0f, cubeSize * 0.5f);
	
	vec4 corners[8];
	corners[0] = worldViewProjMatrix * vec4(center.xyz + tx + ty - tz, center.w);
	corners[1] = worldViewProjMatrix * vec4(center.xyz - tx + ty - tz, center.w);
	corners[2] = worldViewProjMatrix * vec4(center.xyz + tx - ty - tz, center.w);
	corners[3] = worldViewProjMatrix * vec4(center.xyz - tx - ty - tz, center.w);
	
	corners[4] = worldViewProjMatrix * vec4(center.xyz + tx + ty + tz, center.w);
	corners[5] = worldViewProjMatrix * vec4(center.xyz - tx + ty + tz, center.w);
	corners[6] = worldViewProjMatrix * vec4(center.xyz - tx - ty + tz, center.w);
	corners[7] = worldViewProjMatrix * vec4(center.xyz + tx - ty + tz, center.w);
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[3];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[2];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[6];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[7];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[4];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[2];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[0];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[3];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[1];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[6];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[5];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[4];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[1];
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = corners[0];
	EmitVertex();
}