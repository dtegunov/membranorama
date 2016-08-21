using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Warp.Tools;

namespace Membranogram
{
    public class Mesh : IDisposable
    {
        #region Tables
        private static readonly int[] EdgeTable =
        {
                0x0, 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
                0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
                0x190, 0x99, 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
                0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
                0x230, 0x339, 0x33, 0x13a, 0x636, 0x73f, 0x435, 0x53c,
                0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
                0x3a0, 0x2a9, 0x1a3, 0xaa, 0x7a6, 0x6af, 0x5a5, 0x4ac,
                0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
                0x460, 0x569, 0x663, 0x76a, 0x66, 0x16f, 0x265, 0x36c,
                0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
                0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff, 0x3f5, 0x2fc,
                0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
                0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55, 0x15c,
                0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
                0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc,
                0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
                0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
                0xcc, 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
                0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
                0x15c, 0x55, 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
                0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
                0x2fc, 0x3f5, 0xff, 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
                0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
                0x36c, 0x265, 0x16f, 0x66, 0x76a, 0x663, 0x569, 0x460,
                0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
                0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa, 0x1a3, 0x2a9, 0x3a0,
                0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
                0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33, 0x339, 0x230,
                0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
                0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99, 0x190,
                0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
                0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0
            };

        private static readonly int[,] TriTable =
        {
                { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
                { 2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
                { 8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1 },
                { 3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1 },
                { 4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
                { 4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
                { 5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
                { 2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1 },
                { 9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1 },
                { 2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1 },
                { 10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1 },
                { 5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1 },
                { 5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1 },
                { 10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1 },
                { 8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1 },
                { 2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1 },
                { 7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
                { 2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1 },
                { 11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1 },
                { 5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1 },
                { 11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1 },
                { 11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
                { 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1 },
                { 2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1 },
                { 5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1 },
                { 6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
                { 3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1 },
                { 6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1 },
                { 5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1 },
                { 10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1 },
                { 6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1 },
                { 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1 },
                { 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1 },
                { 3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1 },
                { 5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1 },
                { 0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1 },
                { 9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1 },
                { 8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1 },
                { 5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1 },
                { 0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1 },
                { 6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1 },
                { 10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1 },
                { 10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
                { 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
                { 1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1 },
                { 0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1 },
                { 10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1 },
                { 3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1 },
                { 6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1 },
                { 9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1 },
                { 8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1 },
                { 3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1 },
                { 6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1 },
                { 10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1 },
                { 10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
                { 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1 },
                { 7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1 },
                { 7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1 },
                { 2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1 },
                { 1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1 },
                { 11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1 },
                { 8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1 },
                { 0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1 },
                { 7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
                { 10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
                { 2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1 },
                { 6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1 },
                { 7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
                { 2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1 },
                { 10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
                { 10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1 },
                { 0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1 },
                { 7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1 },
                { 6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
                { 8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1 },
                { 6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1 },
                { 4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1 },
                { 10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1 },
                { 8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1 },
                { 1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1 },
                { 8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1 },
                { 10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1 },
                { 10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1 },
                { 5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
                { 11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1 },
                { 9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1 },
                { 6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1 },
                { 7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1 },
                { 3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1 },
                { 7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1 },
                { 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1 },
                { 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1 },
                { 9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1 },
                { 1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1 },
                { 4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1 },
                { 7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1 },
                { 6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1 },
                { 0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1 },
                { 6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1 },
                { 0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1 },
                { 11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1 },
                { 6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1 },
                { 5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1 },
                { 9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1 },
                { 1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1 },
                { 10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1 },
                { 0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1 },
                { 5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1 },
                { 10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1 },
                { 11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1 },
                { 9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1 },
                { 7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1 },
                { 2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
                { 8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1 },
                { 9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1 },
                { 9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1 },
                { 1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
                { 5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1 },
                { 0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1 },
                { 10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1 },
                { 2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1 },
                { 0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1 },
                { 0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1 },
                { 9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1 },
                { 5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1 },
                { 5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1 },
                { 8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1 },
                { 9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1 },
                { 1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1 },
                { 3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1 },
                { 4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1 },
                { 9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1 },
                { 11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1 },
                { 11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1 },
                { 2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1 },
                { 9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1 },
                { 3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1 },
                { 1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1 },
                { 4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1 },
                { 0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1 },
                { 9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1 },
                { 1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { 0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 },
                { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1 }
            };
        #endregion

        public GLControl GLContext;

        public List<Vertex> Vertices = new List<Vertex>();
        public List<Edge> Edges = new List<Edge>();
        public List<Triangle> Triangles = new List<Triangle>();

        /// <summary>
        /// Offset surface that is used for collision tests. The rendered offset 
        /// surface is created on the fly, so it is not dependent on these data.
        /// </summary>
        private List<Triangle> ProcessedTriangles = new List<Triangle>();

        /// <summary>
        /// Once collision tests have been performed, this mapping is used to refer 
        /// to the original triangles before the offset surface duplication.
        /// </summary>
        private Dictionary<Triangle, Triangle> ProcessedTriangleMapping = new Dictionary<Triangle, Triangle>();

        private Vector3[] _BoundingBoxCorners = null;
        public Vector3[] BoundingBoxCorners
        {
            get
            {
                if (_BoundingBoxCorners == null)
                {
                    Vector3 Min = GetMin();
                    Vector3 Max = GetMax();

                    _BoundingBoxCorners = new []
                    {
                        new Vector3(Min.X, Min.Y, Min.Z),
                        new Vector3(Max.X, Min.Y, Min.Z),
                        new Vector3(Min.X, Max.Y, Min.Z),
                        new Vector3(Max.X, Max.Y, Min.Z),
                        new Vector3(Min.X, Min.Y, Max.Z),
                        new Vector3(Max.X, Min.Y, Max.Z),
                        new Vector3(Min.X, Max.Y, Max.Z),
                        new Vector3(Max.X, Max.Y, Max.Z)
                    };
                }

                return _BoundingBoxCorners;
            }
        }

        public MeshVertexComponents UsedComponents = (MeshVertexComponents)int.MaxValue;    // Use every component by default

        int BufferPosition = -1, BufferNormal = -1, BufferVolumePosition = -1, BufferVolumeNormal = -1;
        int BufferColor = -1;
        int BufferSelection = -1;
        int VertexArray = -1;

        int NumVisibleTriangles;

        public void UpdateBuffers()
        {
            GLContext.MakeCurrent();

            FreeBuffers();
            _BoundingBoxCorners = null; // Assume that geometry has been updated, thus the previous BB is invalid.

            List<Triangle> VisibleTriangles = Triangles.Where(t => t.IsVisible).ToList();
            NumVisibleTriangles = VisibleTriangles.Count;
            int NumVertices = NumVisibleTriangles * 3;

            VertexArray = GL.GenVertexArray();
            GL.BindVertexArray(VertexArray);

            int NComponents = 0;

            if ((UsedComponents & MeshVertexComponents.Position) > 0)
            {
                BufferPosition = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, BufferPosition);
                {
                    Vector3[] Data = new Vector3[NumVertices];
                    for (int i = 0; i < VisibleTriangles.Count; i++)
                    {
                        Data[i * 3 + 0] = VisibleTriangles[i].Vertices[0].Position;
                        Data[i * 3 + 1] = VisibleTriangles[i].Vertices[1].Position;
                        Data[i * 3 + 2] = VisibleTriangles[i].Vertices[2].Position;
                    }

                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(NComponents, 3, VertexAttribPointerType.Float, false, 3 * sizeof (float), 0);
                    GL.EnableVertexAttribArray(NComponents++);
                }
            }

            if ((UsedComponents & MeshVertexComponents.Normal) > 0)
            {
                BufferNormal = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, BufferNormal);
                {
                    Vector3[] Data = new Vector3[NumVertices];
                    for (int i = 0; i < VisibleTriangles.Count; i++)
                    {
                        Data[i * 3 + 0] = VisibleTriangles[i].Vertices[0].SmoothNormal;
                        Data[i * 3 + 1] = VisibleTriangles[i].Vertices[1].SmoothNormal;
                        Data[i * 3 + 2] = VisibleTriangles[i].Vertices[2].SmoothNormal;
                    }

                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(NComponents, 3, VertexAttribPointerType.Float, false, 3 * sizeof (float), 0);
                    GL.EnableVertexAttribArray(NComponents++);
                }
            }

            if ((UsedComponents & MeshVertexComponents.VolumePosition) > 0)
            {
                BufferVolumePosition = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, BufferVolumePosition);
                {
                    Vector3[] Data = new Vector3[NumVertices];
                    for (int i = 0; i < VisibleTriangles.Count; i++)
                    {
                        Data[i * 3 + 0] = VisibleTriangles[i].Vertices[0].VolumePosition;
                        Data[i * 3 + 1] = VisibleTriangles[i].Vertices[1].VolumePosition;
                        Data[i * 3 + 2] = VisibleTriangles[i].Vertices[2].VolumePosition;
                    }

                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(NComponents, 3, VertexAttribPointerType.Float, false, 3 * sizeof (float), 0);
                    GL.EnableVertexAttribArray(NComponents++);
                }
            }

            if ((UsedComponents & MeshVertexComponents.VolumeNormal) > 0)
            {
                BufferVolumeNormal = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, BufferVolumeNormal);
                {
                    Vector3[] Data = new Vector3[NumVertices];
                    for (int i = 0; i < VisibleTriangles.Count; i++)
                    {
                        Data[i * 3 + 0] = VisibleTriangles[i].Vertices[0].SmoothVolumeNormal;
                        Data[i * 3 + 1] = VisibleTriangles[i].Vertices[1].SmoothVolumeNormal;
                        Data[i * 3 + 2] = VisibleTriangles[i].Vertices[2].SmoothVolumeNormal;
                    }

                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector3.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(NComponents, 3, VertexAttribPointerType.Float, false, 3 * sizeof (float), 0);
                    GL.EnableVertexAttribArray(NComponents++);
                }
            }

            if ((UsedComponents & MeshVertexComponents.Color) > 0)
            {
                BufferColor = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, BufferColor);
                {
                    Vector4[] Data = new Vector4[NumVertices];
                    for (int i = 0; i < VisibleTriangles.Count; i++)
                    {
                        Data[i * 3 + 0] = VisibleTriangles[i].Color;
                        Data[i * 3 + 1] = VisibleTriangles[i].Color;
                        Data[i * 3 + 2] = VisibleTriangles[i].Color;
                    }

                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * Vector4.SizeInBytes), Data, BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(NComponents, 4, VertexAttribPointerType.Float, false, 4 * sizeof (float), 0);
                    GL.EnableVertexAttribArray(NComponents++);
                }
            }

            if ((UsedComponents & MeshVertexComponents.Selection) > 0)
            {
                BufferSelection = GL.GenBuffer();
                GL.BindBuffer(BufferTarget.ArrayBuffer, BufferSelection);
                {
                    float[] Data = new float[NumVertices];
                    for (int i = 0; i < VisibleTriangles.Count; i++)
                    {
                        Data[i * 3 + 0] = VisibleTriangles[i].IsSelected ? 1f : 0f;
                        Data[i * 3 + 1] = VisibleTriangles[i].IsSelected ? 1f : 0f;
                        Data[i * 3 + 2] = VisibleTriangles[i].IsSelected ? 1f : 0f;
                    }

                    GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(NumVertices * sizeof (float)), Data, BufferUsageHint.StaticDraw);

                    GL.VertexAttribPointer(NComponents, 1, VertexAttribPointerType.Float, false, 1 * sizeof (float), 0);
                    GL.EnableVertexAttribArray(NComponents++);
                }
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void FreeBuffers()
        {
            GLContext.MakeCurrent();

            if (VertexArray != -1)
            {
                GL.DeleteVertexArray(VertexArray);
                VertexArray = -1;
            }
            if (BufferPosition != -1)
            {
                GL.DeleteBuffer(BufferPosition);
                BufferPosition = -1;
            }
            if (BufferNormal != -1)
            {
                GL.DeleteBuffer(BufferNormal);
                BufferNormal = -1;
            }
            if (BufferVolumePosition != -1)
            {
                GL.DeleteBuffer(BufferVolumePosition);
                BufferVolumePosition = -1;
            }
            if (BufferVolumeNormal != -1)
            {
                GL.DeleteBuffer(BufferVolumeNormal);
                BufferVolumeNormal = -1;
            }
            if (BufferColor != -1)
            {
                GL.DeleteBuffer(BufferColor);
                BufferColor = -1;
            }
            if (BufferSelection != -1)
            {
                GL.DeleteBuffer(BufferSelection);
                BufferSelection = -1;
            }
        }

        public void UpdateColors()
        {
            if ((UsedComponents & MeshVertexComponents.Color) > 0)
            {
                GLContext.MakeCurrent();

                GL.BindBuffer(BufferTarget.ArrayBuffer, BufferColor);
                {
                    List<Triangle> VisibleTriangles = Triangles.Where(t => t.IsVisible).ToList();
                    IntPtr DeviceColors = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr)(VisibleTriangles.Count * 3 * Vector4.SizeInBytes), BufferAccessMask.MapWriteBit | BufferAccessMask.MapInvalidateBufferBit);

                    unsafe
                    {
                        Vector4* DeviceColorsPtr = (Vector4*)DeviceColors;
                        for (int i = 0; i < VisibleTriangles.Count; i++)
                        {
                            *DeviceColorsPtr++ = VisibleTriangles[i].Color;
                            *DeviceColorsPtr++ = VisibleTriangles[i].Color;
                            *DeviceColorsPtr++ = VisibleTriangles[i].Color;
                        }
                    }

                    GL.UnmapBuffer(BufferTarget.ArrayBuffer);
                }
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        public void UpdateSelection()
        {
            if ((UsedComponents & MeshVertexComponents.Selection) > 0)
            {
                GLContext.MakeCurrent();

                GL.BindBuffer(BufferTarget.ArrayBuffer, BufferSelection);
                {
                    List<Triangle> VisibleTriangles = Triangles.Where(t => t.IsVisible).ToList();
                    IntPtr DeviceSelection = GL.MapBufferRange(BufferTarget.ArrayBuffer, IntPtr.Zero, (IntPtr)(VisibleTriangles.Count * 3 * sizeof (float)), BufferAccessMask.MapWriteBit | BufferAccessMask.MapInvalidateBufferBit);

                    unsafe
                    {
                        float* DeviceSelectionPtr = (float*)DeviceSelection;
                        for (int i = 0; i < VisibleTriangles.Count; i++)
                        {
                            *DeviceSelectionPtr++ = VisibleTriangles[i].IsSelected ? 1f : 0f;
                            *DeviceSelectionPtr++ = VisibleTriangles[i].IsSelected ? 1f : 0f;
                            *DeviceSelectionPtr++ = VisibleTriangles[i].IsSelected ? 1f : 0f;
                        }
                    }

                    GL.UnmapBuffer(BufferTarget.ArrayBuffer);
                }
                GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            }
        }

        public void Draw()
        {
            GL.BindVertexArray(VertexArray);
            GL.DrawArrays(PrimitiveType.Triangles, 0, NumVisibleTriangles * 3);
        }

        public void UpdateGraph()
        {
            // Face neighbors
            foreach (Triangle t in Triangles)
                t.UpdateNeighbors();

            // Face edges
            foreach (Vertex v in Vertices)
                v.Edges.Clear();
            foreach (Triangle t in Triangles)
                t.UpdateEdges();
            foreach (Vertex v in Vertices)
                v.UpdateNeighbors();

            // Assemble global list of edges
            HashSet<Edge> EdgeSet = new HashSet<Edge>();
            foreach (Triangle t in Triangles)
                foreach (Edge e in t.Edges)
                    if (!EdgeSet.Contains(e))
                        EdgeSet.Add(e);
            Edges.AddRange(EdgeSet);
        }

        public void UpdateVertexIDs()
        {
            for (int i = 0; i < Vertices.Count; i++)
                Vertices[i].ID = i;
        }

        /// <summary>
        /// Gets the minimum XYZ extent of all vertices.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetMin()
        {
            Vector3 Min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);

            return Vertices.Aggregate(Min, (current, v) => Vector3.ComponentMin(current, v.Position));
        }

        /// <summary>
        /// Gets the maximum XYZ extent of all vertices.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetMax()
        {
            Vector3 Max = new Vector3(-float.MaxValue, -float.MaxValue, -float.MaxValue);

            return Vertices.Aggregate(Max, (current, v) => Vector3.ComponentMax(current, v.Position));
        }

        public Vector3 GetCentroid()
        {
            Vector3 Centroid = new Vector3(0, 0, 0);
            Centroid = Vertices.Aggregate(Centroid, (current, v) => current + v.Position);
            Centroid /= Vertices.Count;

            return Centroid;
        }

        public void UpdateProcessedGeometry(float offset)
        {
            ProcessedTriangles = new List<Triangle>(Triangles.Count);
            ProcessedTriangleMapping.Clear();

            foreach (Triangle t in Triangles)
            {
                Vertex IV0 = new Vertex(t.Vertices[0].Position + t.Vertices[0].SmoothNormal * offset, new Vector3());
                Vertex IV1 = new Vertex(t.Vertices[1].Position + t.Vertices[1].SmoothNormal * offset, new Vector3());
                Vertex IV2 = new Vertex(t.Vertices[2].Position + t.Vertices[2].SmoothNormal * offset, new Vector3());

                Triangle IT = new Triangle(t.ID, IV0, IV1, IV2)
                {
                    IsVisible = t.IsVisible,
                    Patch = t.Patch
                };

                ProcessedTriangles.Add(IT);

                ProcessedTriangleMapping.Add(IT, t);
            }
        }

        public List<Intersection> Intersect(Ray3 ray)
        {
            List<Intersection> Intersections = new List<Intersection>();

            Parallel.ForEach(ProcessedTriangles.Where(t => t.IsVisible && t.Patch == null), t =>
            {
                Intersection I = t.Intersect(ray);
                if (I != null)
                {
                    I.Target = ProcessedTriangleMapping[(Triangle)I.Target];
                    lock (Intersections)
                        Intersections.Add(I);
                }
            });

            return Intersections;
        }

        public void Dispose()
        {
            FreeBuffers();
        }

        /// <summary>
        /// Creates a Mesh object based on a Wavefront OBJ file.
        /// </summary>
        /// <param name="path">Path to the file</param>
        /// <returns></returns>
        public static Mesh FromOBJ(string path, bool center = false)
        {
            Mesh NewMesh = new Mesh();

            // Parse vertices
            using (TextReader Reader = new StreamReader(File.OpenRead(path)))
            {
                string Line = Reader.ReadLine();
                while (Line != null)
                {
                    string[] Parts = Line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (Parts.Length > 0 && Parts[0] == "v")
                        NewMesh.Vertices.Add(new Vertex(new Vector3(float.Parse(Parts[1]), float.Parse(Parts[2]), float.Parse(Parts[3])), new Vector3(1, 0, 0)));

                    Line = Reader.ReadLine();
                }
            }

            if (center)
            {
                Vector3 Center = Vector3.Zero;
                Vector3 CenterVolume = Vector3.Zero;
                foreach (var v in NewMesh.Vertices)
                {
                    Center += v.Position;
                    CenterVolume += v.VolumePosition;
                }

                Center /= NewMesh.Vertices.Count;
                CenterVolume /= NewMesh.Vertices.Count;

                foreach (var v in NewMesh.Vertices)
                {
                    v.Position -= Center;
                    v.VolumePosition -= CenterVolume;
                }
            }
            
            // Parse faces
            using (TextReader Reader = new StreamReader(File.OpenRead(path)))
            {
                string Line = Reader.ReadLine();
                while (Line != null)
                {
                    string[] Parts = Line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (Parts.Length > 0 && Parts[0] == "f")
                    {
                        string[] FaceParts0 = Parts[1].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        string[] FaceParts1 = Parts[2].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        string[] FaceParts2 = Parts[3].Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
                        NewMesh.Triangles.Add(new Triangle(NewMesh.Triangles.Count,
                                                           NewMesh.Vertices[int.Parse(FaceParts0[0]) - 1],
                                                           NewMesh.Vertices[int.Parse(FaceParts1[0]) - 1],
                                                           NewMesh.Vertices[int.Parse(FaceParts2[0]) - 1]));
                    }
                    Line = Reader.ReadLine();
                }
            }

            // Amira likes to put unused vertices into the OBJ
            NewMesh.Vertices.RemoveAll(v => v.Triangles.Count == 0);

            NewMesh.UpdateGraph();
            NewMesh.UpdateVertexIDs();

            return NewMesh;
        }

        /// <summary>
        /// Implementation of Marching Cubes based on http://paulbourke.net/geometry/polygonise/
        /// </summary>
        /// <param name="volume">Volume intensity values</param>
        /// <param name="dims">Volume dimensions</param>
        /// <param name="angpix">Angstrom per pixel</param>
        /// <param name="threshold">Isosurface threshold</param>
        /// <returns></returns>
        public static Mesh FromVolume(float[] volume, int3 dims, float angpix, float threshold)
        {
            Triangle[][] CellTriangles = new Triangle[volume.Length][];

            unsafe
            {
                fixed (float* volumePtr = volume)
                    for (int z = 0; z < dims.Z - 1; z++)
                    {
                        float zz = (z - dims.Z / 2) * angpix;
                        for (int y = 0; y < dims.Y - 1; y++)
                        {
                            float yy = (y - dims.Y / 2) * angpix;
                            for (int x = 0; x < dims.X - 1; x++)
                            {
                                float xx = (x - dims.X / 2) * angpix;

                                Vector3[] p = new Vector3[8];
                                float[] val = new float[8];

                                p[0] = new Vector3(xx, yy, zz);
                                p[1] = new Vector3(xx + angpix, yy, zz);
                                p[2] = new Vector3(xx + angpix, yy + angpix, zz);
                                p[3] = new Vector3(xx, yy + angpix, zz);
                                p[4] = new Vector3(xx, yy, zz + angpix);
                                p[5] = new Vector3(xx + angpix, yy, zz + angpix);
                                p[6] = new Vector3(xx + angpix, yy + angpix, zz + angpix);
                                p[7] = new Vector3(xx, yy + angpix, zz + angpix);

                                val[0] = volumePtr[((z + 0) * dims.Y + (y + 0)) * dims.X + (x + 0)];
                                val[1] = volumePtr[((z + 0) * dims.Y + (y + 0)) * dims.X + (x + 1)];
                                val[2] = volumePtr[((z + 0) * dims.Y + (y + 1)) * dims.X + (x + 1)];
                                val[3] = volumePtr[((z + 0) * dims.Y + (y + 1)) * dims.X + (x + 0)];
                                val[4] = volumePtr[((z + 1) * dims.Y + (y + 0)) * dims.X + (x + 0)];
                                val[5] = volumePtr[((z + 1) * dims.Y + (y + 0)) * dims.X + (x + 1)];
                                val[6] = volumePtr[((z + 1) * dims.Y + (y + 1)) * dims.X + (x + 1)];
                                val[7] = volumePtr[((z + 1) * dims.Y + (y + 1)) * dims.X + (x + 0)];

                                CellTriangles[(z * dims.Y + y) * dims.X + x] = Polygonize(p, val, threshold);
                            }
                        }
                    }
            }

            Mesh NewMesh = new Mesh();

            for (int i = 0; i < CellTriangles.Length; i++)
            {
                if (CellTriangles[i] == null)
                    continue;

                foreach (var tri in CellTriangles[i])
                {
                    NewMesh.Vertices.Add(tri.V0);
                    NewMesh.Vertices.Add(tri.V1);
                    NewMesh.Vertices.Add(tri.V2);

                    tri.ID = NewMesh.Triangles.Count;
                    NewMesh.Triangles.Add(tri);
                }
            }

            NewMesh.UpdateGraph();
            NewMesh.UpdateVertexIDs();

            return NewMesh;
        }

        private static Triangle[] Polygonize(Vector3[] p, float[] val, float threshold)
        {
            // Determine the index into the edge table which
            // tells us which vertices are inside of the surface
            uint cubeindex = 0;
            if (val[0] < threshold) cubeindex |= 1;
            if (val[1] < threshold) cubeindex |= 2;
            if (val[2] < threshold) cubeindex |= 4;
            if (val[3] < threshold) cubeindex |= 8;
            if (val[4] < threshold) cubeindex |= 16;
            if (val[5] < threshold) cubeindex |= 32;
            if (val[6] < threshold) cubeindex |= 64;
            if (val[7] < threshold) cubeindex |= 128;

            // Cube is entirely in/out of the surface
            if (EdgeTable[cubeindex] == 0)
                return null;

            Vector3[] VertList = new Vector3[12];

            // Find the vertices where the surface intersects the cube
            if ((EdgeTable[cubeindex] & 1) > 0)
                VertList[0] =
                   VertexInterp(threshold, p[0], p[1], val[0], val[1]);
            if ((EdgeTable[cubeindex] & 2) > 0)
                VertList[1] =
                   VertexInterp(threshold, p[1], p[2], val[1], val[2]);
            if ((EdgeTable[cubeindex] & 4) > 0)
                VertList[2] =
                   VertexInterp(threshold, p[2], p[3], val[2], val[3]);
            if ((EdgeTable[cubeindex] & 8) > 0)
                VertList[3] =
                   VertexInterp(threshold, p[3], p[0], val[3], val[0]);
            if ((EdgeTable[cubeindex] & 16) > 0)
                VertList[4] =
                   VertexInterp(threshold, p[4], p[5], val[4], val[5]);
            if ((EdgeTable[cubeindex] & 32) > 0)
                VertList[5] =
                   VertexInterp(threshold, p[5], p[6], val[5], val[6]);
            if ((EdgeTable[cubeindex] & 64) > 0)
                VertList[6] =
                   VertexInterp(threshold, p[6], p[7], val[6], val[7]);
            if ((EdgeTable[cubeindex] & 128) > 0)
                VertList[7] =
                   VertexInterp(threshold, p[7], p[4], val[7], val[4]);
            if ((EdgeTable[cubeindex] & 256) > 0)
                VertList[8] =
                   VertexInterp(threshold, p[0], p[4], val[0], val[4]);
            if ((EdgeTable[cubeindex] & 512) > 0)
                VertList[9] =
                   VertexInterp(threshold, p[1], p[5], val[1], val[5]);
            if ((EdgeTable[cubeindex] & 1024) > 0)
                VertList[10] =
                   VertexInterp(threshold, p[2], p[6], val[2], val[6]);
            if ((EdgeTable[cubeindex] & 2048) > 0)
                VertList[11] =
                   VertexInterp(threshold, p[3], p[7], val[3], val[7]);

            /* Create the triangle */
            uint ntriang = 0;
            for (uint i = 0; TriTable[cubeindex, i] != -1; i += 3)
                ntriang++;

            Triangle[] Triangles = new Triangle[ntriang];
            ntriang = 0;

            for (uint i = 0; TriTable[cubeindex, i] != -1; i += 3)
            {
                Triangles[ntriang] = new Triangle(0,
                                                  new Vertex(VertList[TriTable[cubeindex, i]], Vector3.UnitX),
                                                  new Vertex(VertList[TriTable[cubeindex, i + 1]], Vector3.UnitX),
                                                  new Vertex(VertList[TriTable[cubeindex, i + 2]], Vector3.UnitX));
                ntriang++;
            }

            return Triangles;
        }

        private static Vector3 VertexInterp(float threshold, Vector3 p1, Vector3 p2, float valp1, float valp2)
        {

            if (Math.Abs(threshold - valp1) < 0.00001f)
                return (p1);
            if (Math.Abs(threshold - valp2) < 0.00001f)
                return (p2);
            if (Math.Abs(valp1 - valp2) < 0.00001f)
                return (p1);
            
            float mu = (threshold - valp1) / (valp2 - valp1);
            Vector3 p = new Vector3(p1.X + mu * (p2.X - p1.X), p1.Y + mu * (p2.Y - p1.Y), p1.Z + mu * (p2.Z - p1.Z));

            return p;
        }
    }

    [Flags]
    public enum MeshVertexComponents
    {
        Position = 1 << 0,
        Normal = 1 << 1,
        VolumePosition = 1 << 2,
        VolumeNormal = 1 << 3,
        Color = 1 << 4,
        Selection = 1 << 5
    }
}
