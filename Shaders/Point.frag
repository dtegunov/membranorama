#version 420

in VS_OUT
{
	vec3 color;
} fs_in;

out vec4 color;

void main(void)
{	
	color = vec4(fs_in.color, 1.0f);
}