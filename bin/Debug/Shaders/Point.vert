#version 440

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec4 in_color;
layout (location = 2) in float in_selected;
layout (location = 3) in vec3 in_orientationX;
layout (location = 4) in vec3 in_orientationY;
layout (location = 5) in vec3 in_orientationZ;

out VS_OUT
{
	vec4 color;
	flat bool selected;
	mat3 orientation;
} vs_out;

void main(void)
{
	vs_out.color = in_color;
	vs_out.selected = in_selected != 0.0f;
	vs_out.orientation = mat3(in_orientationX, in_orientationY, in_orientationZ);
	
	gl_Position = vec4(in_position, 1.0f);
}