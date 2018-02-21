﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BriefFiniteElementNet.Common;
using BriefFiniteElementNet.Elements;
using BriefFiniteElementNet.Mathh;
using CSparse.Storage;
using BriefFiniteElementNet.Solver;
using CSparse;
using CSparse.Factorization;
using CSparse.Ordering;
using CCS = CSparse.Double.CompressedColumnStorage;
using Coord = CSparse.Storage.CoordinateStorage<double>;

namespace BriefFiniteElementNet
{
    /// <summary>
    /// Represents a utility for graphs!
    /// </summary>
    public static class CalcUtil
    {
        public static void ApplyPermutation(Array arr, params int[] indexes)
        {
            if (arr.Length != indexes.Length)
                throw new Exception();

            if (indexes.Min() < 0 || indexes.Max() > indexes.Length)
                throw new Exception();
            
            var clone = (Array)arr.Clone();

            //Array.Clear(clone,0,clone.Length);

            for (int i = 0; i < indexes.Length; i++)
            {
                arr.SetValue(clone.GetValue(indexes[i]), i);
            }
            
        }

        public static CCS GetReducedFreeFreeStiffnessMatrix(this Model model)
        {
            return GetReducedFreeFreeStiffnessMatrix(model, LoadCase.DefaultLoadCase);
        }

        public static CCS GetReducedFreeFreeStiffnessMatrix(this Model model,
            LoadCase lc)
        {
            var fullst = MatrixAssemblerUtil.AssembleFullStiffnessMatrix(model);

            var mgr = DofMappingManager.Create(model, lc);

            var dvd = CalcUtil.GetReducedZoneDividedMatrix(fullst, mgr);

            return dvd.ReleasedReleasedPart;
        }

        /// <summary>
        /// Gets the transformation matrix for converting local coordinate to global coordinate for a two node straight element.
        /// </summary>
        /// <param name="v">The [ end - start ] vector.</param>
        /// <param name="webR">The web rotation in radian.</param>
        /// <returns>
        /// transformation matrix
        /// </returns>
        public static Matrix Get2NodeElementTransformationMatrix(Vector v, double webR)
        {
            var cxx = 0.0;
            var cxy = 0.0;
            var cxz = 0.0;

            var cyx = 0.0;
            var cyy = 0.0;
            var cyz = 0.0;

            var czx = 0.0;
            var czy = 0.0;
            var czz = 0.0;


            var teta = webR;

            var s = webR.Equals(0.0) ? 0.0 : Math.Sin(teta);
            var c = webR.Equals(0.0) ? 1.0 : Math.Cos(teta);

            if (MathUtil.Equals(0, v.X) && MathUtil.Equals(0, v.Y))
            {
                if (v.Z > 0)
                {
                    czx = 1;
                    cyy = 1;
                    cxz = -1;
                }
                else
                {
                    czx = -1;
                    cyy = 1;
                    cxz = 1;
                }
            }
            else
            {
                var l = v.Length;
                cxx = v.X/l;
                cyx = v.Y/l;
                czx = v.Z/l;
                var d = Math.Sqrt(cxx*cxx + cyx*cyx);
                cxy = -cyx/d;
                cyy = cxx/d;
                cxz = -cxx*czx/d;
                cyz = -cyx*czx/d;
                czz = d;
            }

            var t = new Matrix(3, 3);

            t[0, 0] = cxx;
            t[0, 1] = cxy*c + cxz*s;
            t[0, 2] = -cxy*s + cxz*c;

            t[1, 0] = cyx;
            t[1, 1] = cyy*c + cyz*s;
            t[1, 2] = -cyy*s + cyz*c;

            t[2, 0] = czx;
            t[2, 1] = czy*c + czz*s;
            t[2, 2] = -czy*s + czz*c;

            return t;
        }


        /// <summary>
        /// Gets the transformation matrix for converting local coordinate to global coordinate for a two node straight element.
        /// </summary>
        /// <param name="v">The [ end - start ] vector.</param>
        /// <returns>
        /// transformation matrix
        /// </returns>
        public static Matrix Get2NodeElementTransformationMatrix(Vector v)
        {
            return Get2NodeElementTransformationMatrix(v, 0);
        }

        /// <summary>
        /// Creates a built in solver appropriated with <see cref="tp"/>.
        /// </summary>
        /// <param name="type">The solver type.</param>
        /// <returns></returns>
        public static ISolver CreateBuiltInSolver(BuiltInSolverType type,CCS A)
        {
            switch (type)
            {
                case BuiltInSolverType.CholeskyDecomposition:
                    return new CholeskySolverFactory().CreateSolver(A);
                    break;
                case BuiltInSolverType.ConjugateGradient:
                    return new ConjugateGradientFactory().CreateSolver(A);// PCG(new SSOR());
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }


        public static ISolverFactory CreateBuiltInSolverFactory(BuiltInSolverType type)
        {
            switch (type)
            {
                case BuiltInSolverType.CholeskyDecomposition:
                    return new CholeskySolverFactory();
                    break;
                case BuiltInSolverType.ConjugateGradient:
                    return new ConjugateGradientFactory();// PCG(new SSOR());
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
        }

        public static int GetHashCode(params Point[] objects)
        {
            if (objects == null)
                throw new ArgumentNullException("objects");

            if (objects.Length == 0)
                return 0;

            var buf = objects[0].GetHashCode();

            for (var i = 1; i < objects.Length; i++)
            {
                buf = (buf*397) ^ GetPointHashCode(objects[i]);
            }

            buf = (buf*397) ^ objects.Length;

            return buf;
        }

        public static int GetPointHashCode(Point pt)
        {
            unchecked
            {
                var hashCode = pt.X.GetHashCode();
                hashCode = (hashCode * 397) ^ pt.Y.GetHashCode();
                hashCode = (hashCode * 397) ^ pt.Z.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        /// Enumerates the discrete parts of the graph.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <returns></returns>
        public static List<List<int>> EnumerateGraphParts(CompressedColumnStorage<double> graph)
        {
            var buf = new List<List<int>>();

            var n = graph.ColumnCount;

            var visited = new bool[n];

            var ri = graph.RowIndices;
            var cp = graph.ColumnPointers;

            for (var i = 0; i < n; i++)
            {
                if (cp[i] == cp[i + 1])
                    visited[i] = true;
            }

            while (true)
            {
                var startPoint = visited.FirstIndexOf(false);

                if (startPoint == -1)
                    break;

                var part = DepthFirstSearch(graph, visited, startPoint).Distinct().ToList();

                buf.Add(part);
            }

            return buf;
        }


        public static void SetAllMembers(this Array arr, object value)
        {
            var n = arr.GetLength(0);

            for (int i = 0; i < n; i++)
            {
                arr.SetValue(value, i);
            }
        }
        /// <summary>
        /// Does the Depth first search, return connected nodes to <see cref="startNode"/>.
        /// </summary>
        /// <param name="graph">The graph.</param>
        /// <param name="visited">The visited map.</param>
        /// <param name="startNode">The start node.</param>
        /// <returns>List of connected nodes to <see cref="startNode"/>.</returns>
        public static List<int> DepthFirstSearch(CompressedColumnStorage<double> graph, bool[] visited, int startNode)
        {
            var buf = new List<int>();

            var ri = graph.RowIndices;
            var cp = graph.ColumnPointers;

            var q = new Queue<int>();

            q.Enqueue(startNode);

            visited[startNode] = true;

            while (q.Count > 0)
            {
                var v = q.Dequeue();

                visited[v] = true;

                buf.Add(v);

                for (var i = cp[v]; i < cp[v + 1]; i++)
                {
                    var neighbor = ri[i];

                    if (!visited[neighbor])
                    {
                        visited[neighbor] = true;
                        q.Enqueue(neighbor);
                        //buf.Add(neighbor);
                    }
                }
            }


            return buf;
        }


        public static int[] GetMasterMapping(Model model, LoadCase cse)
        {
            for (int i = 0; i < model.Nodes.Count; i++)
                model.Nodes[i].Index = i;


            var n = model.Nodes.Count;
            var masters = new int[n];

            for (var i = 0; i < n; i++)
            {
                masters[i] = i;
            }

            var masterCount = 0;

            #region filling the masters

            var distinctElements = GetDistinctRigidElements(model, cse);

            var centralNodePrefreation = new bool[n];


            foreach (var elm in model.RigidElements)
            {
                if (elm.CentralNode != null)
                    centralNodePrefreation[elm.CentralNode.Index] = true;
            }


            foreach (var elm in distinctElements)
            {
                if (elm.Count == 0)
                    continue;

                var elmMasterIndex = GetMasterNodeIndex(model, elm, centralNodePrefreation);

                for (var i = 0; i < elm.Count; i++)
                {
                    masters[elm[i]] = elmMasterIndex;
                }
            }

            #endregion

            return masters;
        }

        public static int[] GetMasterMapping(Model model, LoadCase cse,out bool[] hinged)
        {
            for (int i = 0; i < model.Nodes.Count; i++)
                model.Nodes[i].Index = i;


            var n = model.Nodes.Count;
            var masters = new int[n];
            hinged = new bool[n];


            for (var i = 0; i < n; i++)
            {
                masters[i] = i;
            }

            var masterCount = 0;

            #region filling the masters

            var distinctElements = GetDistinctRigidElements(model, cse);

            var centralNodePrefreation = new bool[n];


            foreach (var elm in model.RigidElements)
            {
                if (elm.CentralNode != null)
                    centralNodePrefreation[elm.CentralNode.Index] = true;
            }


            foreach (var elm in distinctElements)
            {
                if (elm.Count == 0)
                    continue;

                var elmMasterIndex = GetMasterNodeIndex(model, elm, centralNodePrefreation);

                for (var i = 0; i < elm.Count; i++)
                {
                    masters[elm[i]] = elmMasterIndex;
                }
            }

            #endregion

            return masters;
        }

        private static List<List<int>> GetDistinctRigidElements(Model model, LoadCase loadCase)
        {
            for (int i = 0; i < model.Nodes.Count; i++)
                model.Nodes[i].Index = i;

            var n = model.Nodes.Count;

            var ecrd = new CoordinateStorage<double>(n, n, 1);//for storing existence of rigid elements
            var crd = new CoordinateStorage<double>(n, n, 1);//for storing hinged connection of rigid elements

            for (int ii = 0; ii < model.RigidElements.Count; ii++)
            {
                var elm = model.RigidElements[ii];

                if (IsAppliableRigidElement(elm, loadCase))
                {
                    for (var i = 0; i < elm.Nodes.Count; i++)
                    {
                        ecrd.At(elm.Nodes[i].Index, elm.Nodes[i].Index, 1.0);
                    }

                    for (var i = 0; i < elm.Nodes.Count - 1; i++)
                    {
                        ecrd.At(elm.Nodes[i].Index, elm.Nodes[i + 1].Index, 1.0);
                        ecrd.At(elm.Nodes[i + 1].Index, elm.Nodes[i].Index, 1.0);
                    }
                }
            }

            var graph = Converter.ToCompressedColumnStorage(ecrd);

            var buf = CalcUtil.EnumerateGraphParts(graph);

            return buf;
        }


        private static bool IsAppliableRigidElement(RigidElement elm, LoadCase loadCase)
        {
            if (elm.UseForAllLoads)
                return true;

            if (elm.AppliedLoadTypes.Contains(loadCase.LoadType))
                return true;

            if (elm.AppliedLoadCases.Contains(loadCase))
                return true;

            return false;
        }

        private static int GetMasterNodeIndex(Model model, List<int> nodes, bool[] preferation)
        {
            var buf = -1;

            foreach (var node in nodes)
                if (preferation[node])
                {
                    buf = node;
                    break;
                }

            if (buf == -1)
                foreach (var node in nodes)
                    if (model.Nodes[node].Constraints != Constraint.Released)
                    {
                        buf = node;
                        break;
                    }

            foreach (var node in nodes)
                if (model.Nodes[node].Constraints != Constraint.Released)
                    if (node != buf)
                        ExceptionHelper.Throw("MA20000");

            if (buf == -1)
                buf = nodes[0];


            return buf;
        }

        /// <summary>
        /// Fills the whole <see cref="array"/> with -1.
        /// </summary>
        /// <param name="array">The array.</param>
        public static void FillWith<T>(this T[] array,T value)
        {
            for (var i = array.Length - 1; i >= 0; i--)
            {
                array[i] = value;
            }
        }

        public static int Sum(this Constraint ctx)
        {
            return (int) ctx.DX + (int) ctx.DY + (int) ctx.DZ +
                   (int) ctx.RX + (int) ctx.RY + (int) ctx.RZ;
        }

        public static DofConstraint[] ToArray(this Constraint ctx)
        {
            return new DofConstraint[] {ctx.DX, ctx.DY, ctx.DZ, ctx.RX, ctx.RY, ctx.RZ};
        }

        public static double[] Add(double[] a, double[] b)
        {
            if (a.Length != b.Length)
                throw new InvalidOperationException();

            var buf = new double[a.Length];

            for (int i = 0; i < b.Length; i++)
            {
                buf[i] = a[i] + b[i];
            }

            return buf;
        }


        public static double[] Subtract(double[] a, double[] b)
        {
            if (a.Length != b.Length)
                throw new InvalidOperationException();

            var buf = new double[a.Length];

            for (int i = 0; i < b.Length; i++)
            {
                buf[i] = a[i] - b[i];
            }

            return buf;
        }

        /// <summary>
        /// Gets the reduced zone divided matrix.
        /// </summary>
        /// <param name="reducedMatrix">The reduced matrix.</param>
        /// <param name="map">The map.</param>
        /// <returns></returns>
        public static ZoneDevidedMatrix GetReducedZoneDividedMatrix(CCS reducedMatrix, DofMappingManager map)
        {
            var m = map.M;
            var n = map.N;
            var r = reducedMatrix;

            if (r.ColumnCount != r.RowCount || r.RowCount != 6*m)
                throw new InvalidOperationException();

            var ff = new Coord(map.RMap2.Length, map.RMap2.Length,1);
            var fs = new Coord(map.RMap2.Length, map.RMap3.Length,1);
            var sf = new Coord(map.RMap3.Length, map.RMap2.Length,1);
            var ss = new Coord(map.RMap3.Length, map.RMap3.Length,1);

            for (var i = 0; i < 6*m; i++)
            {
                var st = r.ColumnPointers[i];
                var en = r.ColumnPointers[i + 1];

                var col = i;

                for (var j = st; j < en; j++)
                {
                    var row = r.RowIndices[j];
                    var val = r.Values[j];

                    if (map.Fixity[map.RMap1[row]] == DofConstraint.Released &&
                        map.Fixity[map.RMap1[col]] == DofConstraint.Released)
                        ff.At(map.Map2[row], map.Map2[col], val);

                    if (map.Fixity[map.RMap1[row]] == DofConstraint.Released &&
                        map.Fixity[map.RMap1[col]] != DofConstraint.Released)
                        fs.At(map.Map2[row], map.Map3[col], val);

                    if (map.Fixity[map.RMap1[row]] != DofConstraint.Released &&
                        map.Fixity[map.RMap1[col]] == DofConstraint.Released)
                        sf.At(map.Map3[row], map.Map2[col], val);

                    if (map.Fixity[map.RMap1[row]] != DofConstraint.Released &&
                        map.Fixity[map.RMap1[col]] != DofConstraint.Released)
                        ss.At(map.Map3[row], map.Map3[col], val);
                }
            }

            var buf = new ZoneDevidedMatrix();

            buf.ReleasedReleasedPart = ff.ToCCs();
            buf.ReleasedFixedPart = fs.ToCCs();
            buf.FixedReleasedPart = sf.ToCCs();
            buf.FixedFixedPart = ss.ToCCs();

            return buf;
        }

        public static CCS ToCCs(this Coord crd)
        {
            //workitem #6:
            //https://brieffiniteelementnet.codeplex.com/workitem/6
            if (crd.RowCount == 0 || crd.ColumnCount == 0)
                return new CCS(0, 0){ColumnPointers = new int[0], RowIndices = new int[0], Values = new double[0]};


            return (CCS) Converter.ToCompressedColumnStorage(crd);
        }

        /// <summary>
        /// Determines whether defined matrix is diagonal matrix or not.
        /// Diagonal matrix is a matrix that only have nonzero elements on its main diagonal.
        /// </summary>
        /// <param name="mtx">The MTX.</param>
        /// <returns></returns>
        public static bool IsDiagonalMatrix(this CCS mtx)
        {
            var n = mtx.ColumnCount;

            if (n != mtx.RowCount)
                return false;

            if (mtx.Values.Length > n)
                return false;

            for (int i = 0; i < n; i++)
            {
                var col = i;

                var st = mtx.ColumnPointers[i];
                var en = mtx.ColumnPointers[i+1];

                for (int j = st; j < en; j++)
                {
                    var row = mtx.RowIndices[j];

                    if (row != col)
                        return false;
                }
            }


            return true;
        }


        /// <summary>
        /// Does the specified <see cref="action"/> on all members of <see cref="matrix"/>
        /// </summary>
        /// <param name="matrix"></param>
        /// <param name="action"></param>
        internal static void EnumerateMembers(this CCS matrix,Action<int,int,double> action)
        {
            var n = matrix.ColumnCount;

            for (int i = 0; i < n; i++)
            {
                var col = i;

                var st = matrix.ColumnPointers[i];
                var en = matrix.ColumnPointers[i + 1];

                for (int j = st; j < en; j++)
                {
                    var row = matrix.RowIndices[j];

                    var val = matrix.Values[j];

                    action(row, col, val);
                }
            }
        }

        internal static void EnumerateColumnMembers(this CCS matrix, int columnNumber,Action<int, int, double> action)
        {
            var n = matrix.ColumnCount;

            var i = columnNumber;//for (int i = 0; i < n; i++)
            {
                var col = i;

                var st = matrix.ColumnPointers[i];
                var en = matrix.ColumnPointers[i + 1];

                for (int j = st; j < en; j++)
                {
                    var row = matrix.RowIndices[j];

                    var val = matrix.Values[j];

                    action(row, col, val);
                }
            }
        }

        internal static void EnumerateColumnMembers(this CompressedColumnStorage<double> matrix, int columnNumber, Action<int, int, double> action)
        {
            var n = matrix.ColumnCount;

            var i = columnNumber;//for (int i = 0; i < n; i++)
            {
                var col = i;

                var st = matrix.ColumnPointers[i];
                var en = matrix.ColumnPointers[i + 1];

                for (int j = st; j < en; j++)
                {
                    var row = matrix.RowIndices[j];

                    var val = matrix.Values[j];

                    action(row, col, val);
                }
            }
        }
        internal static void EnumerateMembers(this CompressedColumnStorage<double> matrix, Action<int, int, double> action)
        {
            var n = matrix.ColumnCount;

            for (int i = 0; i < n; i++)
            {
                var col = i;

                var st = matrix.ColumnPointers[i];
                var en = matrix.ColumnPointers[i + 1];

                for (int j = st; j < en; j++)
                {
                    var row = matrix.RowIndices[j];

                    var val = matrix.Values[j];

                    action(row, col, val);
                }
            }
        }

        public static  CompressedColumnStorage<double> Clonee(this CompressedColumnStorage<double>  matrix)
        {
            var buf = new CCS(matrix.RowCount, matrix.ColumnCount, matrix.Values.Length);

            matrix.RowIndices.CopyTo(buf.RowIndices, 0);
            matrix.ColumnPointers.CopyTo(buf.ColumnPointers, 0);
            matrix.Values.CopyTo(buf.Values, 0);

            return buf;
        }

        public static CSparse.Double.CompressedColumnStorage Clonee(this CSparse.Double.CompressedColumnStorage matrix)
        {
            var buf = new CSparse.Double.CompressedColumnStorage(matrix.RowCount, matrix.ColumnCount, matrix.Values.Length);

            matrix.RowIndices.CopyTo(buf.RowIndices, 0);
            matrix.ColumnPointers.CopyTo(buf.ColumnPointers, 0);
            matrix.Values.CopyTo(buf.Values, 0);

            return buf;
        }

        internal static void EnumerateColumns(this CCS matrix, Action<int, Dictionary<int,double>> action)
        {
            var n = matrix.ColumnCount;

            for (int i = 0; i < n; i++)
            {
                var col = i;

                var st = matrix.ColumnPointers[i];
                var en = matrix.ColumnPointers[i + 1];

                var dic = new Dictionary<int, double>();

                for (int j = st; j < en; j++)
                {
                    var row = matrix.RowIndices[j];

                    var val = matrix.Values[j];
                    dic[row] = val;
                }


                action(col, dic);
            }

        }

        internal static int GetNnzcForColumn(this CompressedColumnStorage<double> matrix, int column)
        {
            var st = matrix.ColumnPointers[column];
            var en = matrix.ColumnPointers[column+1];

            return en - st;
        }

        public static void MakeMatrixSymetric(this CCS mtx)
        {
            var n = mtx.ColumnCount;

            if (n != mtx.RowCount)
                throw new Exception();


            for (int i = 0; i < n; i++)
            {
                var col = i;

                var st = mtx.ColumnPointers[i];
                var en = mtx.ColumnPointers[i + 1];

                for (int j = st; j < en; j++)
                {
                    var row = mtx.RowIndices[j];



                    var valRowCol = mtx.Values[j];

                    var valColRow = mtx.At(col, row);

                    if (valColRow == valRowCol)
                        continue;


                    var avg = (valRowCol + valColRow) / 2;

                    SetMember(mtx, row, col, avg);
                    SetMember(mtx, col, row, avg);
                }
            }


        }


        public static void SetMember(this CCS matrix, int row, int column, double value)
        {
            int index = matrix.ColumnPointers[column];
            int length = matrix.ColumnPointers[column + 1] - index;
            int pos = Array.BinarySearch(matrix.RowIndices, index, length, row);

            if (pos < 0)
                throw new Exception();

            matrix.Values[pos] = value;
        }

        /// <summary>
        /// Applies the release matrix to calculated local end forces.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="localEndForces">The local end forces.</param>
        /// <returns></returns>
        /// <remarks>
        /// When <see cref="FrameElement2Node"/> has one or two hinged ends, then local end forces due to element interior loads (like distributed loads) 
        /// will be different than normal ones (both ends fixed). This method will apply end releases...
        /// </remarks>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Force[] ApplyReleaseMatrixToEndForces(FrameElement2Node element, Force[] localEndForces)
        {
            if (localEndForces.Length != 2)
                throw new NotImplementedException();

            var fullLoadVector = new double[12];//for applying release matrix

            
            {
                fullLoadVector[00] = localEndForces[0].Fx;
                fullLoadVector[01] = localEndForces[0].Fy;
                fullLoadVector[02] = localEndForces[0].Fz;
                fullLoadVector[03] = localEndForces[0].Mx;
                fullLoadVector[04] = localEndForces[0].My;
                fullLoadVector[05] = localEndForces[0].Mz;

                fullLoadVector[06] = localEndForces[1].Fx;
                fullLoadVector[07] = localEndForces[1].Fy;
                fullLoadVector[08] = localEndForces[1].Fz;
                fullLoadVector[09] = localEndForces[1].Mx;
                fullLoadVector[10] = localEndForces[1].My;
                fullLoadVector[11] = localEndForces[1].Mz;
            }

            var ld = new Matrix(fullLoadVector);
            var rsm = element.GetReleaseMatrix();
            ld = rsm*ld;

            var buf = new Force[2];

            buf[0] = Force.FromVector2D(ld.CoreArray, 0);
            buf[1] = Force.FromVector2D(ld.CoreArray, 6);

            return buf;
        }

        public static double GetTriangleArea(Point p0, Point p1, Point p2)
        {
            var v1 = p1 - p0;
            var v2 = p2 - p0;

            var cross = Vector.Cross(v1, v2);
            return cross.Length / 2;
        }


        /// <summary>
        /// Applies the lambda matrix to transform from local to global.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <param name="lambda">The lambda matrix.</param>
        /// <remarks>
        /// This is used for higher performance applyment of matrix. Does exactly Tt * K * T transforming 
        /// but faster
        /// </remarks>
        [Obsolete("Use TransformManagerL2G instead")]
        public static void ApplyTransformMatrix(Matrix matrix, Matrix lambda)
        {
            if (!lambda.IsSquare() || !matrix.IsSquare())
                throw new Exception();

            if (lambda.RowCount != 3 || matrix.RowCount%3 != 0)
                throw new Exception();

            var count = matrix.RowCount/3;

            var t11 = lambda[0, 0];
            var t12 = lambda[0, 1];
            var t13 = lambda[0, 2];

            var t21 = lambda[1, 0];
            var t22 = lambda[1, 1];
            var t23 = lambda[1, 2];

            var t31 = lambda[2, 0];
            var t32 = lambda[2, 1];
            var t33 = lambda[2, 2];


            for (var ic = 0; ic < count; ic++)
            {
                for (var jc = 0; jc < count; jc++)
                {
                    var iStart = ic * 3;
                    var jStart = jc * 3;


                }
            }

            throw new NotImplementedException();
        }

        public static double DegToRad(double degree)
        {
            return degree/180*Math.PI;
        }

        public static double RadToDeg(double rad)
        {
            return rad*180.0/Math.PI;
        }

        public static double[] GetAngleWithAxises(Vector vec)
        {
            var buf = new List<double>();

            buf.Add(CalcUtil.RadToDeg(Math.Acos(vec.X / vec.Length)));
            buf.Add(CalcUtil.RadToDeg(Math.Acos(vec.Y / vec.Length)));
            buf.Add(CalcUtil.RadToDeg(Math.Acos(vec.Z / vec.Length)));


            return buf.ToArray();
        }

        /// <summary>
        /// Transforms the specified vector using transform matrix.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <returns>transformed vector</returns>
        public static Vector Transform(this Vector vector, Matrix transformMatrix)
        {
            var buf = new Vector();

            buf.X =
                transformMatrix[0, 0] * vector.X +
                transformMatrix[0, 1] * vector.Y +
                transformMatrix[0, 2] * vector.Z;

            buf.Y =
                transformMatrix[1, 0] * vector.X +
                transformMatrix[1, 1] * vector.Y +
                transformMatrix[1, 2] * vector.Z;

            buf.Z =
                transformMatrix[2, 0] * vector.X +
                transformMatrix[2, 1] * vector.Y +
                transformMatrix[2, 2] * vector.Z;

            return buf;
        }

        /// <summary>
        /// Transforms the specified point using transform matrix.
        /// </summary>
        /// <param name="point">The vector.</param>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <returns>transformed vector</returns>
        public static Point Transform(this Point point, Matrix transformMatrix)
        {
            return (Point)((Vector) point).Transform(transformMatrix);
        }

        /// <summary>
        /// Transforms back the specified vector using transform matrix.
        /// </summary>
        /// <param name="vector">The vector.</param>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <returns>transformed vector</returns>
        public static Vector TransformBack(this Vector vector, Matrix transformMatrix)
        {
            var buf = new Vector();

            buf.X =
                transformMatrix[0, 0] * vector.X +
                transformMatrix[1, 0] * vector.Y +
                transformMatrix[2, 0] * vector.Z;

            buf.Y =
                transformMatrix[0, 1] * vector.X +
                transformMatrix[1, 1] * vector.Y +
                transformMatrix[2, 1] * vector.Z;

            buf.Z =
                transformMatrix[0, 2] * vector.X +
                transformMatrix[1, 2] * vector.Y +
                transformMatrix[2, 2] * vector.Z;

            return buf;
        }

        /// <summary>
        /// Transforms back the specified point using transform matrix.
        /// </summary>
        /// <param name="point">The vector.</param>
        /// <param name="transformMatrix">The transform matrix.</param>
        /// <returns>transformed vector</returns>
        public static Point TransformBack(this Point point, Matrix transformMatrix)
        {
            return (Point)((Vector)point).TransformBack(transformMatrix);
        }

        /// <summary>
        /// cacluates the A = B' D B.
        /// </summary>
        /// <param name="B">The B.</param>
        /// <param name="D">The D.</param>
        /// <param name="bi_ColCount">The column count of B[i].</param>
        /// <param name="result">The result.</param>
        public static void Bt_D_B(Matrix B, Matrix D, Matrix result)
        {
            //method 1 is divide B into smallers
            //method 2 is: B' D B = (D' B)' B
            // first find D' B which is possible high performancely and call it R1
            // find R1' B which is possible high performancely too!

            var dd = B.RowCount;//dim of b
            var dOut = B.ColumnCount;// dim of out

            if (!D.IsSquare() || D.RowCount != dd)
                throw new Exception();

            var buf = result;

            if (buf.RowCount != dOut || buf.ColumnCount != dOut)
                throw new Exception();

            var buf1 =
                new Matrix(D.ColumnCount, B.ColumnCount);
                //MatrixPool.Allocate(D.ColumnCount, B.ColumnCount);

            Matrix.TransposeMultiply(D, B, buf1);

            Matrix.TransposeMultiply(buf1, B, buf);

            //MatrixPool.Free(buf1);
        }


        public static void MultiplyWithConstant(this CCS mtx,double coef)
        {
            for (var i = 0; i < mtx.Values.Length; i++)
                mtx.Values[i] = mtx.Values[i] * coef;
        }

        private static void FillBi(Matrix B,int i,Matrix bi)
        {
            var c = bi.ColumnCount;
            for (var ii = 0; ii < bi.RowCount; ii++)
                for (var j = 0; j < bi.RowCount; j++)
                    bi[ii, j] = B[c * i + ii, j];

        }

        public static void LabelNodesIncrementally(this Model model)
        {
            for (var i = 0; i < model.Nodes.Count; i++)
            {
                model.Nodes[i].Label = null;
            }

            for (var i = 0; i < model.Nodes.Count; i++)
            {
                model.Nodes[i].Label = "N" + i.ToString();
            }
        }

        public static void LabelElementsIncrementally(this Model model)
        {
            for (var i = 0; i < model.Elements.Count; i++)
            {
                model.Elements[i].Label = null;
            }

            for (var i = 0; i < model.Elements.Count; i++)
            {
                model.Elements[i].Label = "E" + i.ToString();
            }
        }

        /// <summary>
        /// Generates the permutation for delta for specified model in specified loadCase.
        /// Note that delta permutation = P_delta in reduction process
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="loadCase">The load case.</param>
        /// <returns>delta permutation</returns>
        public static CCS GenerateP_Delta(Model target, LoadCase loadCase)
        {
            throw new NotImplementedException();

            target.ReIndexNodes();

            var buf = new CoordinateStorage<double>(target.Nodes.Count * 6, target.Nodes.Count * 6, 1);
            

            #region rigid elements
            foreach (var elm in target.RigidElements)
            {
                var centralNode = elm.Nodes[0];
                var masterIdx = centralNode.Index;

                for(var i = 1;i<elm.Nodes.Count;i++)
                {
                    var slaveIdx = elm.Nodes[i].Index;

                    var d = centralNode.Location - elm.Nodes[i].Location;

                    //buf[0, 4] = -(buf[1, 3] = d.Z);
                    //buf[2, 3] = -(buf[0, 5] = d.Y);
                    //buf[1, 5] = -(buf[2, 4] = d.X);

                    buf.At(6 * slaveIdx + 0, 6 * masterIdx + 0, 1);
                    buf.At(6 * slaveIdx + 1, 6 * masterIdx + 1, 1);
                    buf.At(6 * slaveIdx + 2, 6 * masterIdx + 2, 1);

                    buf.At(6 * slaveIdx + 1, 6 * masterIdx + 3, d.Z);
                    buf.At(6 * slaveIdx + 0, 6 * masterIdx + 4, -d.Z);
                    
                    buf.At(6 * slaveIdx + 0, 6 * masterIdx + 5, d.Y);
                    buf.At(6 * slaveIdx + 2, 6 * masterIdx + 3, -d.Y);//

                    buf.At(6 * slaveIdx + 2, 6 * masterIdx + 4, d.X);//
                    buf.At(6 * slaveIdx + 1, 6 * masterIdx + 5, -d.X);
                }
                //add to buf
            }
            #endregion

            throw new NotImplementedException();

            return buf.ToCCs();
        }

        [Obsolete("Under development")]
        public static Tuple<CCS, double[]> GenerateP_Delta_Mpc(Model target, LoadCase loadCase, IRrefFinder rrefFinder)
        {
            var totDofCount = target.Nodes.Count * 6;

            target.ReIndexNodes();

            var n = target.Nodes.Count;

            var boundaryConditions = GetModelBoundaryConditions(target, loadCase);

            var lastRow = 0;

            #region step 1
            //step 1: combine all eqs to one system

            //var filledCols = new bool[6 * n];

            var extraEqCount = 0;

            foreach (var mpcElm in target.MpcElements)
                if (mpcElm.AppliesForLoadCase(loadCase))
                    extraEqCount += mpcElm.GetExtraEquationsCount();

            extraEqCount += boundaryConditions.RowCount;

            var allEqsCrd = new CoordinateStorage<double>(extraEqCount, n * 6 + 1, 1);//rows: extra eqs, cols: 6*n+1 (+1 is for right hand side)

            foreach (var mpcElm in target.MpcElements)
            {
                if (mpcElm.AppliesForLoadCase(loadCase))
                {
                    var extras = mpcElm.GetExtraEquations();

                    if (extras.ColumnCount != totDofCount + 1)
                        throw new Exception();

                    foreach (var tuple in extras.EnumerateIndexed2())
                    {
                        var row = tuple.Item1;
                        var col = tuple.Item2;
                        var val = tuple.Item3;


                        allEqsCrd.At(row + lastRow, col, val);
                    }

                    /*
                    extras.EnumerateMembers((row, col, val) =>
                    {
                        allEqsCrd.At(row + lastRow, col, val);
                    });
                    */

                    lastRow += extras.RowCount;
                }
            }

            {
                if (boundaryConditions.ColumnCount != totDofCount + 1)
                    throw new Exception();


                foreach (var tuple in boundaryConditions.EnumerateIndexed2())
                {
                    var row = tuple.Item1;
                    var col = tuple.Item2;
                    var val = tuple.Item3;


                    allEqsCrd.At(row + lastRow, col, val);
                }

                /*
                boundaryConditions.EnumerateMembers((row, col, val) =>
                {
                    allEqsCrd.At(row + lastRow, col, val);
                });
                */

                lastRow += boundaryConditions.RowCount;
            }

            var allEqs = allEqsCrd.ToCCs();


            var empties = allEqs.EmptyRowCount();// - boundaryConditions.EmptyRowCount();

            var dns = allEqs.ToDenseMatrix();

            #endregion

            #region comment
            /*
            #region step 2
            //step 2: create adjacency matrix of variables

            //step 2-1: find nonzero pattern
            var allEqsNonzeroPattern = allEqs.Clone();

            for (var i = 0; i < allEqsNonzeroPattern.Values.Length; i++)
                allEqsNonzeroPattern.Values[i] = 1;

            //https://math.stackexchange.com/questions/2340450/extract-independent-sub-systems-from-a-bigger-linear-eq-system
            var tmp = allEqsNonzeroPattern.Transpose();

            var variableAdj = tmp.Multiply(allEqsNonzeroPattern);
            #endregion

            #region step 3
            //extract parts
            var parts = EnumerateGraphParts(variableAdj);

            #endregion

            #region step 4
            {

                allEqs.EnumerateColumns((colNum, vals) =>
                {
                    if (vals.Count == 0)
                    Console.WriteLine("Col {0} have {1} nonzeros", colNum, vals.Count);
                });

                var order = ColumnOrdering.MinimumDegreeAtPlusA;

                // Partial pivoting tolerance (0.0 to 1.0)
                double tolerance = 1.0;

                var lu = CSparse.Double.Factorization.SparseLU.Create(allEqs, order, tolerance);

            }

            #endregion
            */

            #endregion

            var rref = rrefFinder.CalculateRref(allEqs);

            var rrefSys = SparseEqSystem.Generate(rref);

            #region generate P_Delta

            var pRows = new int[totDofCount]; // pRows[i] = index of equation that its right side is Di (ai*Di = a1*D1+...+an*Dn)

            pRows.FillWith(-1);

            for (var i = 0; i < rrefSys.RowCount; i++)
            {
                foreach (var tpl in rrefSys.Equations[i].EnumerateIndexed())
                {
                    if (rrefSys.ColumnNonzeros[tpl.Item1] == 1)
                    {
                        if (pRows[tpl.Item1] != -1)
                            throw new Exception();

                        pRows[tpl.Item1] = i;
                    }
                }
            }

            int cnt = 0;

            var lastColIndex = rrefSys.ColumnCount - 1;

            var p2Coord = new CoordinateStorage<double>(totDofCount, totDofCount, 1);

            var rightSide = new double[totDofCount];

            



            for (var i = 0; i < totDofCount; i++)
            {
                if (pRows[i] == -1)
                {
                    p2Coord.At(i, i, 1);
                    continue;
                }

                var eq = rrefSys.Equations[pRows[i]];
                eq.Multiply(-1 / eq.GetMember(i));

                var minus1 = eq.GetMember(i);

                if (!minus1.FEquals(-1, 1e-9))
                    throw new Exception();

                //eq.SetMember(i, 0);

                foreach (var tpl in eq.EnumerateIndexed())
                {
                    if (tpl.Item1 != lastColIndex)
                        p2Coord.At(i, tpl.Item1, tpl.Item2);
                    else
                        rightSide[i] = tpl.Item2;
                }
            }

            


            cnt = 0;

            foreach(var eq in rrefSys.Equations)
            {
                if(eq.IsZeroRow(1e-9))
                {
                    cnt++;
                }
            }

            #endregion

            var p2 = p2Coord.ToCCs();

            var colsToRemove = new bool[totDofCount];

            var colNumPerm = new int[totDofCount];

            colNumPerm.FillWith(-1);

            colsToRemove.FillWith(false);

            var tmp = 0;

            for (var i = 0; i < rrefSys.ColumnNonzeros.Length; i++)
                if (i != lastColIndex)
                {
                    if (rrefSys.ColumnNonzeros[i] == 1)
                        colsToRemove[i] = true;
                    else
                        colNumPerm[i] = tmp++;
                }
                

            var p3Crd = new CoordinateStorage<double>(totDofCount, totDofCount - colsToRemove.Count(i => i), 1);

            foreach(var tpl in p2.EnumerateIndexed2())
            {
                if (!colsToRemove[tpl.Item2])
                {
                    p3Crd.At(tpl.Item1, colNumPerm[tpl.Item2], tpl.Item3);
                }
            }

            var p3 = p3Crd.ToCCs();

            //var tmpp = p3.ToDenseMatrix();


            return Tuple.Create(p3, rightSide);

            throw new NotImplementedException();

            //return buf.ToCCs();
        }


        /// <summary>
        /// Gets the boundary conditions of model (support conditions) as a extra eq system for using in master slave model.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="loadCase"></param>
        /// <returns></returns>
        public static CCS GetModelBoundaryConditions(Model model, LoadCase loadCase)
        {
            var fixedDofsCount = model.Nodes.Sum(ii => FixedCount(ii.Constraints));

            var n = model.Nodes.Count * 6;

            var crd = new CoordinateStorage<double>(fixedDofsCount, 6 * model.Nodes.Count + 1, 1);

            var cnt = 0;

            foreach (var node in model.Nodes)
            {
                var stDof = 6 * node.Index;

                var stm = node.Settlements;

                #region 

                if (node.Constraints.DX == DofConstraint.Fixed)
                {
                    crd.At(cnt , stDof + 0, 1);
                    crd.At(cnt , n, stm.DX);
                    cnt++;
                }

                if (node.Constraints.DY == DofConstraint.Fixed)
                {
                    crd.At(cnt, stDof + 1, 1);
                    crd.At(cnt, n, stm.DY);
                    cnt++;
                }

                if (node.Constraints.DZ == DofConstraint.Fixed)
                {
                    crd.At(cnt, stDof + 2, 1);
                    crd.At(cnt, n, stm.DZ);
                    cnt++;
                }


                if (node.Constraints.RX == DofConstraint.Fixed)
                {
                    crd.At(cnt, stDof + 3, 1);
                    crd.At(cnt, n, stm.RX);
                    cnt++;
                }

                if (node.Constraints.RY == DofConstraint.Fixed)
                {
                    crd.At(cnt, stDof + 4, 1);
                    crd.At(cnt, n, stm.RY);
                    cnt++;
                }

                if (node.Constraints.RZ == DofConstraint.Fixed)
                {
                    crd.At(cnt, stDof + 5, 1);
                    crd.At(cnt, n, stm.RZ);
                    cnt++;
                }

                #endregion
            }
            
            return crd.ToCCs();
        }

        public static int FixedCount(Constraint cns)
        {
            var buf = 0;

            if (cns.DX == DofConstraint.Fixed)
                buf++;

            if (cns.DY == DofConstraint.Fixed)
                buf++;

            if (cns.DZ == DofConstraint.Fixed)
                buf++;

            if (cns.RX == DofConstraint.Fixed)
                buf++;

            if (cns.RY == DofConstraint.Fixed)
                buf++;

            if (cns.RZ == DofConstraint.Fixed)
                buf++;

            return buf;
        }

        public static int EmptyRowCount(this CCS matrix)
        {
            var buf = new bool[matrix.RowCount];

            matrix.EnumerateMembers((row, col, val) =>
            {
                buf[row] = true;
            });

            return buf.Count(ii => !ii);
        }

        public static int[] EmptyRows(this CCS matrix)
        {
            var buf = new bool[matrix.RowCount];

            var lst = new List<int>();

            foreach(var tuple in matrix.EnumerateIndexed2())
            {
                var rw = tuple.Item1;
                var col = tuple.Item2;
                var val = tuple.Item3;

                if (val != 0)
                    buf[rw] = true;
            }

            for (var i = 0; i < buf.Length; i++)
                if (!buf[i])
                    lst.Add(i);

            return lst.ToArray();
        }

        public static bool IsIsotropicMaterial(AnisotropicMaterialInfo inf)
        {
            var arr1 = new double[] { inf.Ex, inf.Ey, inf.Ez };
            var arr2 = new double[]
            {
                inf.NuXy, inf.NuYx,
                inf.NuXz, inf.NuZx,
                inf.NuZy, inf.NuYz
            };

            return arr1.Distinct().Count() == 1 && arr2.Distinct().Count() == 1;
        }

        public static int EmptyColumnCount(this CCS matrix)
        {
            var buf = new bool[matrix.RowCount];

            matrix.EnumerateMembers((row, col, val) =>
            {
                buf[col] = true;
            });

            return buf.Count(ii => !ii);
        }

        public static CCS GenerateP_Force(Model target, LoadCase loadCase)
        {
            throw new NotImplementedException();
        }

        public static CCS GenerateS_r(Model target, LoadCase loadCase)
        {
            throw new NotImplementedException();
        }

        public static CCS GenerateS_f(Model target, LoadCase loadCase)
        {
            throw new NotImplementedException();
        }

        public static void AddToSelf(this double[] vector,double[] addition,double coef=1)
        {
            if (vector.Length != addition.Length)
                throw new Exception();

            for(var i = 0;i<vector.Length;i++)
            {
                vector[i] += addition[i] * coef;
            }
        }
    }
}