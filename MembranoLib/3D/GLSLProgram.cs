using System;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Membranogram.Helpers;

namespace Membranogram
{
    public class GLSLProgram
    {
        private int Handle;

	    public GLSLProgram(string pathVS, string pathTSControl, string pathTSEval, string pathGS, string pathFS)
	    {
			int VertexShader = 0, TesselationControlShader = 0, TesselationEvalShader = 0, GeometryShader = 0, FragmentShader = 0;
			int ShaderProgram = GL.CreateProgram();
		
			// Load and compile individual shaders:
			
			if (pathVS != null)
			{
				VertexShader = GL.CreateShader(ShaderType.VertexShader);
				GL.ShaderSource(VertexShader, 1, new[] { GetText(pathVS) }, new int[0]);
				GL.CompileShader(VertexShader);
				
				GL.AttachShader(ShaderProgram, VertexShader);
			}			
			
			if (pathTSControl != null && pathTSEval != null)
			{
				TesselationControlShader = GL.CreateShader(ShaderType.TessControlShader);
				GL.ShaderSource(TesselationControlShader, 1, new[] { GetText(pathTSControl) }, new int[0]);
				GL.CompileShader(TesselationControlShader);

				TesselationEvalShader = GL.CreateShader(ShaderType.TessEvaluationShader);
				GL.ShaderSource(TesselationEvalShader, 1, new[] { GetText(pathTSEval) }, new int[0]);
				GL.CompileShader(TesselationEvalShader);
				
				GL.AttachShader(ShaderProgram, TesselationControlShader);
				GL.AttachShader(ShaderProgram, TesselationEvalShader);
			}
			
			if (pathGS != null)
			{
				GeometryShader = GL.CreateShader(ShaderType.GeometryShader);
				GL.ShaderSource(GeometryShader, 1, new[] { GetText(pathGS) }, new int[0]);
				GL.CompileShader(GeometryShader);
				
				GL.AttachShader(ShaderProgram, GeometryShader);
			}
			
			if (pathFS != null)
			{
				FragmentShader = GL.CreateShader(ShaderType.FragmentShader);
				GL.ShaderSource(FragmentShader, 1, new[] { GetText(pathFS) }, new int[0]);
				GL.CompileShader(FragmentShader);
				
				GL.AttachShader(ShaderProgram, FragmentShader);
			}
			
			// Link overall program:
			
			GL.LinkProgram(ShaderProgram);
			GL.ValidateProgram(ShaderProgram);
			
			// Delete shaders:
			
			if (pathVS != null)
				GL.DeleteShader(VertexShader);
			if (pathTSControl != null && pathTSEval != null)
			{
				GL.DeleteShader(TesselationControlShader);
				GL.DeleteShader(TesselationEvalShader);
			}
			if (pathGS != null)
				GL.DeleteShader(GeometryShader);
			if (pathFS != null)
				GL.DeleteShader(FragmentShader);
			
			// Find out what went wrong:			
            Console.WriteLine(GL.GetProgramInfoLog(ShaderProgram));
	        
	        Handle = ShaderProgram;
	    }

        public void Use()
        {
            GL.UseProgram(Handle);
        }

        public void SetUniform(string name, Matrix4 value)
        {
            float[] AsArray = OpenGLHelper.ToFloatArray(value);
            GL.ProgramUniformMatrix4(Handle, GetUniformIndex(name), 1, false, AsArray);
        }

        public void SetUniform(string name, Matrix3 value)
        {
            float[] AsArray = OpenGLHelper.ToFloatArray(value);
            GL.ProgramUniformMatrix3(Handle, GetUniformIndex(name), 1, false, AsArray);
        }

        public void SetUniform(string name, float value)
        {
            GL.ProgramUniform1(Handle, GetUniformIndex(name), value);
        }

        public void SetUniform(string name, Vector2 value)
        {
            GL.ProgramUniform2(Handle, GetUniformIndex(name), value.X, value.Y);
        }

        public void SetUniform(string name, Vector3 value)
        {
            GL.ProgramUniform3(Handle, GetUniformIndex(name), value.X, value.Y, value.Z);
        }

        public void SetUniform(string name, Vector4 value)
        {
            GL.ProgramUniform4(Handle, GetUniformIndex(name), value.X, value.Y, value.Z, value.W);
        }

        public void SetUniform(string name, int x)
        {
            GL.ProgramUniform1(Handle, GetUniformIndex(name), x);
        }

        public void SetUniform(string name, int x, int y)
        {
            GL.ProgramUniform2(Handle, GetUniformIndex(name), x, y);
        }

        public void SetUniform(string name, int x, int y, int z)
        {
            GL.ProgramUniform3(Handle, GetUniformIndex(name), x, y, z);
        }

        public void SetUniform(string name, int x, int y, int z, int w)
        {
            GL.ProgramUniform4(Handle, GetUniformIndex(name), x, y, z, w);
        }

        private int GetUniformIndex(string name)
        {
            int Index = GL.GetProgramResourceIndex(Handle, ProgramInterface.Uniform, name);
            if (Index < 0)
                Console.WriteLine(name + " does not exist in this GLSL program.");

            return Index;
        }
	
	    private static string GetText(string path)
	    {
            string ProgramText;
            using (TextReader Reader = new StreamReader(File.OpenRead(path)))
            {
                ProgramText = Reader.ReadToEnd();
            }

            return ProgramText;
	    }
    }
}
