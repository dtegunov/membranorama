#version 440

uniform mat4 worldViewProjMatrix;
uniform float cubeSize;

layout (points) in;
layout (line_strip, max_vertices = 2) out;

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
			
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = worldViewProjMatrix * vec4(center.xyz + gs_in[0].orientation * vec3(0.0f, 0.0f, cubeSize * 0.15f), center.w);
	EmitVertex();
	
	gs_out.color = color;
	gs_out.selected = selected;
	gl_Position = worldViewProjMatrix * vec4(center.xyz + gs_in[0].orientation * vec3(cubeSize * 1.0f, 0.0f, cubeSize * 0.15f), center.w);
	EmitVertex();
}