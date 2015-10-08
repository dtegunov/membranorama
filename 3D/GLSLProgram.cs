using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace Membranogram
{
    public class GLSLProgram
    {
	    public static int CompileProgram(string pathVS, string pathTSControl, string pathTSEval, string pathGS, string pathFS)
	    {
			int vertexShader = 0, tesselationControlShader = 0, tesselationEvalShader = 0, geometryShader = 0, fragmentShader = 0;
			int shaderProgram = GL.CreateProgram();
		
			// Load and compile individual shaders:
			
			if (pathVS != null)
			{
				vertexShader = GL.CreateShader(ShaderType.VertexShader);
				GL.ShaderSource(vertexShader, 1, new string[] { GetText(pathVS) }, new int[0]);
				GL.CompileShader(vertexShader);
				
				GL.AttachShader(shaderProgram, vertexShader);
			}			
			
			if (pathTSControl != null && pathTSEval != null)
			{
				tesselationControlShader = GL.CreateShader(ShaderType.TessControlShader);
				GL.ShaderSource(tesselationControlShader, 1, new string[] { GetText(pathTSControl) }, new int[0]);
				GL.CompileShader(tesselationControlShader);

				tesselationEvalShader = GL.CreateShader(ShaderType.TessEvaluationShader);
				GL.ShaderSource(tesselationEvalShader, 1, new string[] { GetText(pathTSEval) }, new int[0]);
				GL.CompileShader(tesselationEvalShader);
				
				GL.AttachShader(shaderProgram, tesselationControlShader);
				GL.AttachShader(shaderProgram, tesselationEvalShader);
			}
			
			if (pathGS != null)
			{
				geometryShader = GL.CreateShader(ShaderType.GeometryShader);
				GL.ShaderSource(geometryShader, 1, new string[] { GetText(pathGS) }, new int[0]);
				GL.CompileShader(geometryShader);
				
				GL.AttachShader(shaderProgram, geometryShader);
			}
			
			if (pathFS != null)
			{
				fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
				GL.ShaderSource(fragmentShader, 1, new string[] { GetText(pathFS) }, new int[0]);
				GL.CompileShader(fragmentShader);
				
				GL.AttachShader(shaderProgram, fragmentShader);
			}
			
			// Link overall program:
			
			GL.LinkProgram(shaderProgram);
			GL.ValidateProgram(shaderProgram);
			
			// Delete shaders:
			
			if (pathVS != null)
				GL.DeleteShader(vertexShader);
			if (pathTSControl != null && pathTSEval != null)
			{
				GL.DeleteShader(tesselationControlShader);
				GL.DeleteShader(tesselationEvalShader);
			}
			if (pathGS != null)
				GL.DeleteShader(geometryShader);
			if (pathFS != null)
				GL.DeleteShader(fragmentShader);
			
			// Find out what went wrong:			
            Console.WriteLine(GL.GetProgramInfoLog(shaderProgram));
	        
	        return shaderProgram;
	    }
	
	    private static string GetText(string path)
	    {
            string ProgramText = "";
            using (TextReader Reader = new StreamReader(File.OpenRead(path)))
            {
                ProgramText = Reader.ReadToEnd();
            }

            return ProgramText;
	    }
    }
}
