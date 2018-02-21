using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;

namespace BriefFiniteElementNet
{
    /// <summary>
    /// Represents a dense real matrix
    /// </summary>
    [DebuggerDisplay("Matrix {RowCount} x {ColumnCount}")]
    [Serializable]
    public class Matrix : ISerializable, IEnumerable<double>, ICloneable
    {
        public MatrixIndexModes IndexMode { get; set; } = MatrixIndexModes.ZeroBasedIndex;
        public int GetIndexFromOneBased(int oneBasedIndex)
        {
            if (IndexMode == MatrixIndexModes.OneBasedIndex)
            {
                return oneBasedIndex;
            }
            else
            {
                return oneBasedIndex - 1;
            }
        }
        public int GetIndexFromZeroBased(int zeroBasedIndex)
        {
            if (IndexMode == MatrixIndexModes.OneBasedIndex)
            {
                return zeroBasedIndex + 1;
            }
            else
            {
                return zeroBasedIndex;
            }
        }
        public int GetZeroBasedIndex(int index)
        {
            if (IndexMode == MatrixIndexModes.OneBasedIndex)
            {
                return index - 1;
            }
            else
            {
                return index;
            }
        }
        public int GetOneBasedIndex(int index)
        {
            if (IndexMode == MatrixIndexModes.OneBasedIndex)
            {
                return index;
            }
            else
            {
                return index + 1;
            }
        }

        /// <summary>
        /// flattens a zero-based row/column index assuming rows are placed next to each other
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public int FlattenIndexRowWise(int row, int column)
        {
            row = GetZeroBasedIndex(row);
            column = GetZeroBasedIndex(column);
            return GetIndexFromZeroBased(row * ColumnCount + column);
        }
        /// <summary>
        /// flattens a zero-based row/column index assuming columns are placed below each other
        /// </summary>
        /// <param name="row"></param>
        /// <param name="column"></param>
        /// <returns></returns>
        public int FlattenIndexColumnWise(int row, int column)
        {
            row = GetZeroBasedIndex(row);
            column = GetZeroBasedIndex(column);
            return GetIndexFromZeroBased(column * RowCount + row);
        }

        public void DeFlattenIndexColumnWise(int index, out int row, out int column)
        {
            index = GetZeroBasedIndex(index);
            row = GetIndexFromZeroBased(index % RowCount);
            column = GetIndexFromZeroBased(index / RowCount);
        }
        public void DeFlattenIndexRowWise(int index, out int row, out int column)
        {
            index = GetZeroBasedIndex(index);
            row = GetIndexFromZeroBased(index / ColumnCount);
            column = GetIndexFromZeroBased(index % ColumnCount);
        }

        public static Matrix Repeat(Matrix mtx, int ni, int nj)
        {
            var r = mtx.RowCount;
            var c = mtx.ColumnCount;

            var buf = new Matrix(r * ni, c * nj);

            for (var i = 0; i < ni; i++)
                for (var j = 0; j < nj; j++)
                    for (var ii = 0; ii < mtx.RowCount; ii++)
                        for (var jj = 0; jj < mtx.ColumnCount; jj++)
                            buf[i * r + ii, j * c + jj] = mtx[ii, jj];

            return buf;
        }

        public static Matrix DiagonallyRepeat(Matrix mtx, int n)
        {
            var r = mtx.RowCount;
            var c = mtx.ColumnCount;

            var buf = new Matrix(r * n, c * n);

            for (var i = 0; i < n; i++)
                //for (var j = 0; j < n; j++)
                for (var ii = 0; ii < mtx.RowCount; ii++)
                    for (var jj = 0; jj < mtx.ColumnCount; jj++)
                        buf[i * r + ii, i * c + jj] = mtx[ii, jj];

            return buf;
        }

        /// <summary>
        /// Horizontally join the two matrices.
        /// Matrix 1 left,  2 right side of it
        /// </summary>
        /// <param name="m1">The m1.</param>
        /// <param name="m2">The m2.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Matrix HorizontalConcat(Matrix m1, Matrix m2)
        {
            if (m1.RowCount != m2.RowCount)
                throw new Exception();

            var buf = new Matrix(m1.RowCount, m1.ColumnCount + m2.ColumnCount);

            for (var i = 0; i < m1.RowCount; i++)
            {
                for (var j = 0; j < m1.ColumnCount; j++)
                {
                    buf[i, j] = m1[i, j];
                }
            }

            for (var i = 0; i < m2.RowCount; i++)
            {
                for (var j = 0; j < m2.ColumnCount; j++)
                {
                    buf[i, j + m1.ColumnCount] = m2[i, j];
                }
            }

            return buf;
        }

        public static Matrix HorizontalConcat(params Matrix[] mtx)
        {

            var rwCnt = mtx.First().RowCount;

            if (mtx.Any(i => i.RowCount != rwCnt))
                //if (m1.RowCount != m2.RowCount)
                throw new Exception();

            var buf = new Matrix(rwCnt, mtx.Sum(i => i.ColumnCount));

            var cnt = 0;

            for (var ii = 0; ii < mtx.Length; ii++)
            {
                var m = mtx[ii];

                for (var i = 0; i < m.RowCount; i++)
                {
                    for (var j = 0; j < m.ColumnCount; j++)
                    {
                        buf[i, j + cnt] = m[i, j];
                    }
                }

                cnt += m.ColumnCount;
            }

            return buf;
        }

        /// <summary>
        /// Vertically join the two matrices.
        /// Matrix 1 on top, 2 below it
        /// </summary>
        /// <param name="m1">The m1.</param>
        /// <param name="m2">The m2.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static Matrix VerticalConcat(Matrix m1, Matrix m2)
        {
            if (m1.ColumnCount != m2.ColumnCount)
                throw new Exception();

            var buf = new Matrix(m1.RowCount + m2.RowCount, m1.ColumnCount);

            for (var i = 0; i < m1.RowCount; i++)
            {
                for (var j = 0; j < m1.ColumnCount; j++)
                {
                    buf[i, j] = m1[i, j];
                }
            }

            for (var i = 0; i < m2.RowCount; i++)
            {
                for (var j = 0; j < m2.ColumnCount; j++)
                {
                    buf[i + m1.RowCount, j] = m2[i, j];
                }
            }

            return buf;
        }

        /// <summary>
        /// Creates a matrix from rowcount, colcount and core array
        /// </summary>
        /// <param name="rows">The rows.</param>
        /// <param name="cols">The cols.</param>
        /// <param name="coreArr">The core arr.</param>
        /// <remarks>
        /// Unlike constructor, do not clones the <see cref="coreArr"/> for better performance</remarks>
        /// <returns></returns>
        public static Matrix FromRowColCoreArray(int rows, int cols, double[] coreArr)
        {
            var cor = new double[rows, cols];
            for (int i = 0; i < cor.GetLength(0); i++)
            {
                for (int j = 0; j < cor.GetLength(1); j++)
                {
                    var flat = cols * i + j;
                    cor[i, j] = coreArr[flat];
                }
            }
            return new Matrix() { CoreArray = cor };
        }

        public static Matrix RandomMatrix(int m, int n)
        {
            var buf = new Matrix(m, n);

            var rnd = new Random(0);

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    buf.CoreArray[i, j] = rnd.NextDouble() * 100;
                }
            }
            return buf;
        }

        public static Matrix CholeskyDecomposeSymmetric(Matrix a)
        {
            if (a.RowCount != a.ColumnCount)
                throw new InvalidOperationException();

            var n = a.ColumnCount;

            var l = new Matrix(n, n);

            for (var i = 0; i < n; i++)
            {
                var lii = a[i, i];

                for (int t = 0; t < i; t++)
                {
                    lii -= l[i, t] * l[i, t];
                }

                lii = System.Math.Sqrt(lii);
                l[i, i] = lii;


                for (var j = i + 1; j < n; j++)
                {
                    var lji = a[i, j];

                    for (int t = 0; t < i; t++)
                    {
                        lji -= l[i, t] * l[j, t];
                    }

                    lji = lji / lii;

                    l[j, i] = lji;
                }
            }

            return l;
        }

        private double[,] m_CoreArray;
        public double[,] CoreArray
        {
            get
            {
                return m_CoreArray;
            }
            internal set
            {
                m_CoreArray = value;
                RowCount = value.GetLength(0);
                ColumnCount = value.GetLength(1);
            }
        }


        /// <summary>
        /// Number of rows of the matrix.
        /// </summary>
        public int RowCount
        {
            get; private set;
        }

        /// <summary>
        /// Number of columns of the matrix.
        /// </summary>
        public int ColumnCount
        {
            get; private set;
        }

        #region Constructors

        /// <summary>
        /// Prevents a default instance of the <see cref="Matrix"/> class from being created.
        /// </summary>
        private Matrix()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> class.
        /// </summary>
        /// <param name="m">The row.</param>
        /// <param name="n">The columns.</param>
        /// <exception cref="System.ArgumentException">
        /// row
        /// or
        /// n
        /// </exception>
        public Matrix(int m, int n, double? defaultValue = null)
        {

            if (m <= 0)
                throw new ArgumentException("m");

            if (n <= 0)
                throw new ArgumentException("n");

            CoreArray = new double[m, n];
            if (defaultValue.HasValue)
            {
                for (int i = 0; i < m; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        CoreArray[i, j] = defaultValue.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new square matrix
        /// </summary>
        /// <param name="n">The matrix dimension.</param>
        public Matrix(int n, double? defaultValue = null)
        {
            if (n <= 0)
                throw new ArgumentException("n");

            CoreArray = new double[n, n];
            if (defaultValue.HasValue)
            {
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        CoreArray[i, j] = defaultValue.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> class with a 2-d double array.
        /// </summary>
        /// <param name="vals">The values.</param>
        public Matrix(double[,] vals, bool CloneToNewArray = true)
        {
            var rows = vals.GetLength(0);
            var cols = vals.GetLength(1);
            if (CloneToNewArray)
            {
                CoreArray = (double[,])vals.Clone();
            }
            else
            {
                CoreArray = vals;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> class with a 2-d double array.
        /// </summary>
        /// <param name="vals">The values.</param>
        public Matrix(double[][] vals)
        {
            var rows = vals.Length;
            var cols = vals.Max(i => i.Length);
            CoreArray = new double[rows, cols];

            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    if (vals[i].Length > j)
                        CoreArray[i, j] = vals[i][j];
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Matrix"/> class as a column vector.
        /// </summary>
        /// <param name="vals">The vals.</param>
        public Matrix(double[] vals)
        {
            CoreArray = new double[vals.Length, 1];
            for (int i = 0; i < vals.Length; i++)
            {
                CoreArray[i, 0] = vals[i];
            }
        }

        public Matrix(int rows, int cols, double[] coreArray)
        {

            if (rows <= 0)
                throw new ArgumentException("rows");

            if (cols <= 0)
                throw new ArgumentException("cols");

            CoreArray = new double[rows, cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    CoreArray[i, j] = coreArray[FlattenIndexRowWise(i, j)];
                }
            }
        }

        #endregion

        /// <summary>
        /// Gets or sets the specified member.
        /// </summary>
        /// <value>
        /// The <see cref="System.Double"/>.
        /// </value>
        /// <param name="row">The row (zero based).</param>
        /// <param name="column">The column (zero based).</param>
        /// <returns></returns>
        [IndexerName("TheMember")]
        public double this[int row, int column]
        {
            get
            {
                return GetMember(row, column);
            }

            set
            {
                SetMember(row, column, value);
            }
        }
        [IndexerName("TheMember")]
        public double this[int FlatIndex]
        {
            get
            {
                DeFlattenIndexColumnWise(FlatIndex, out var i, out var j);
                return GetMember(i, j);
            }
            set
            {
                DeFlattenIndexColumnWise(FlatIndex, out var i, out var j);
                SetMember(i, j, value);
            }
        }
        /// <summary>
        /// sums all cells in every column and puts the sum in the respective column
        /// </summary>
        /// <returns></returns>
        public Matrix SumByColumn()
        {
            var final = new Matrix(1, ColumnCount);
            for (int j = 0; j < ColumnCount; j++)
            {
                double sum = 0;
                for (int i = 0; i < RowCount; i++)
                {
                    sum += CoreArray[i, j];
                }
                final.CoreArray[0, j] = sum;
            }
            return final;
        }

        /// <summary>
        /// sums all cells in every column and puts the sum in the respective column
        /// </summary>
        /// <returns></returns>
        public double[] SumByColumnArray()
        {

            var final = new double[ColumnCount];
            for (int j = 0; j < ColumnCount; j++)
            {
                double sum = 0;
                for (int i = 0; i < RowCount; i++)
                {
                    sum += CoreArray[i, j];
                }
                final[j] = sum;
            }
            return final;
        }

        public static Matrix Multiply(Matrix m1, Matrix m2)
        {
            if (m1.ColumnCount != m2.RowCount)
                throw new InvalidOperationException("No consistent dimensions");

            var res = new Matrix(m1.RowCount, m2.ColumnCount);

            for (int i = 0; i < m1.RowCount; i++)
                for (int j = 0; j < m2.ColumnCount; j++)
                    for (int k = 0; k < m1.ColumnCount; k++)
                    {
                        res.CoreArray[i, j] += m1.CoreArray[i, k] * m2.CoreArray[k, j];
                    }
            return res;
        }


        /// <summary>
        /// Adds the specified matrix to current matrix.
        /// </summary>
        /// <param name="that">The that.</param>
        /// <exception cref="System.InvalidOperationException">No consistent dimensions</exception>
        public void Add(Matrix that)
        {
            if (this.ColumnCount != that.ColumnCount || this.RowCount != that.RowCount)
                throw new InvalidOperationException("No consistent dimensions");

            for (var i = 0; i < RowCount; i++)
                for (int j = 0; j < ColumnCount; j++)
                {
                    CoreArray[i, j] += that.CoreArray[i, j];
                }

        }

        public static void Multiply(Matrix m1, Matrix m2, Matrix result)
        {
            if (m1.ColumnCount != m2.RowCount)
                throw new InvalidOperationException("No consistent dimensions");

            var res = result;

            if (res.RowCount != m1.RowCount || res.ColumnCount != m2.ColumnCount)
            {
                throw new Exception("result dimension mismatch");
            }

            for (var i = 0; i < m1.RowCount; i++)
                for (var j = 0; j < m2.ColumnCount; j++)
                {
                    var t = 0.0;
                    for (var k = 0; k < m1.ColumnCount; k++)
                    {
                        t += m1[i, k] * m2[k, j];
                    }
                    res[i, j] = t;
                }
        }


        /// <summary>
        /// calculates the m1.transpose * m2 and stores the value into result.
        /// </summary>
        /// <param name="m1">The m1.</param>
        /// <param name="m2">The m2.</param>
        /// <param name="result">The result.</param>
        /// <returns></returns>
        public static void TransposeMultiply(Matrix m1, Matrix m2, Matrix result)
        {
            if (m1.RowCount != m2.RowCount)
                throw new InvalidOperationException("No consistent dimensions");

            var res = result;

            if (res.RowCount != m1.ColumnCount || res.ColumnCount != m2.ColumnCount)
            {
                throw new Exception("result dimension mismatch");
            }

            var a = m1;
            var b = m2;


            var a_arr = a.CoreArray;
            var b_arr = b.CoreArray;

            var vecLength = a.RowCount;

            for (var i = 0; i < a.ColumnCount; i++)
                for (var j = 0; j < b.ColumnCount; j++)
                {
                    var t = 0.0;
                    for (var k = 0; k < vecLength; k++)
                        t += a_arr[k, i] * b_arr[k, j];
                    res[i, j] = t;
                }

        }

        /// <summary>
        /// Multiplies the specified <see cref="Matrix"/> with specified Vector <see cref="vec"/>.
        /// </summary>
        /// <param name="m">The m.</param>
        /// <param name="vec">The vec.</param>
        /// <returns></returns>
        /// <exception cref="BriefFiniteElementNet.MatrixException"></exception>
        public static double[] Multiply(Matrix m, double[] vec)
        {
            if (m.ColumnCount != vec.Length)
                throw new MatrixException();

            var c = m.ColumnCount;
            var r = m.RowCount;

            var buf = new double[vec.Length];

            for (var i = 0; i < r; i++)
            {
                var tmp = 0.0;
                for (var j = 0; j < c; j++)
                {
                    tmp += m.CoreArray[i, j] * vec[j];
                }

                buf[i] = tmp;
            }

            return buf;
        }

        #region Dynamic Functions

        /// <summary>
        /// Sets the member at defined row and column to defined value.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="column">The column.</param>
        /// <param name="value">The value.</param>
        public void SetMember(int row, int column, double value)
        {
            row = GetZeroBasedIndex(row);
            column = GetZeroBasedIndex(column);
            MatrixException.ThrowIf(row < 0 || column < 0,
                   "Invalid column or row specified");
            UnSafeSetMember(row, column, value);
        }
        private void UnSafeSetMember(int zerobased_row, int zerobased_column, double value)
        {
            bool isResizeRows = zerobased_row >= RowCount;
            bool isResizeColumns = zerobased_column >= ColumnCount;

            if (isResizeRows || isResizeColumns)
                ResizeCoreArray(isResizeRows ? zerobased_row + 1 : RowCount, isResizeColumns ? zerobased_column + 1 : ColumnCount);
            CoreArray[zerobased_row, zerobased_column] = value;
        }

        /// <summary>
        /// Gets the member at defined row and column.
        /// </summary>
        /// <param name="row">The row.</param>
        /// <param name="column">The column.</param>
        /// <returns></returns>
        public double GetMember(int row, int column)
        {
            row = GetZeroBasedIndex(row);
            column = GetZeroBasedIndex(column);
            MatrixException.ThrowIf(row < 0 || column < 0,
                   "Invalid column or row specified");
            return UnSafeGetMember(row, column);
        }
        private double UnSafeGetMember(int zerobased_row, int zerobased_column)
        {
            bool isResizeRows = zerobased_row >= RowCount;
            bool isResizeColumns = zerobased_column >= ColumnCount;
            if (isResizeRows || isResizeColumns)
                ResizeCoreArray(isResizeRows ? zerobased_row + 1 : RowCount, isResizeColumns ? zerobased_column + 1 : ColumnCount);
            return CoreArray[zerobased_row, zerobased_column];
        }

        private void ResizeCoreArray(int newR, int newC)
        {
            double[,] old = CoreArray;
            var oldrc = CoreArray.GetLength(0);
            var oldcc = CoreArray.GetLength(1);
            CoreArray = new double[newR, newC];
            for (int i = 0; i < oldrc; i++)
            {
                for (int j = 0; j < oldcc; j++)
                {
                    CoreArray[i, j] = old[i, j];
                }
            }
        }

        /// <summary>
        /// Substitutes the defined row with defined values.
        /// </summary>
        /// <param name="i">The i.</param>
        /// <param name="values">The values.</param>
        public void SetRow(int i, params double[] values)
        {
            i = GetZeroBasedIndex(i);
            bool isResizeRows = false, isResizeColumns = false;

            isResizeRows = i >= RowCount;
            isResizeColumns = values.Length > ColumnCount;

            if (isResizeRows || isResizeColumns)
                ResizeCoreArray(isResizeRows ? i + 1 : RowCount, isResizeColumns ? values.Length : ColumnCount);

            for (int j = 0; j < values.Length; j++)
            {
                CoreArray[i, j] = values[j];
            }
        }

        ~Matrix()
        {
            if (UsePool)
                MatrixPool.Free(this);
        }

        /// <summary>
        /// Gets or sets a value indicating whether pool is used for this object or not.
        /// </summary>
        /// <value>
        ///   <c>true</c> if [use pool]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// If pool used for this object, on distruction corearray will return to pool
        /// </remarks>
        public bool UsePool { get; set; }


        /// <summary>
        /// Substitutes the defined column with defined values.
        /// </summary>
        /// <param name="j">The j.</param>
        /// <param name="values">The values.</param>
        public void SetColumn(int j, params double[] values)
        {
            j = GetZeroBasedIndex(j);
            bool isResizeRows = false, isResizeColumns = false;

            isResizeColumns = j >= RowCount;
            isResizeRows = values.Length > ColumnCount;

            if (isResizeRows || isResizeColumns)
                ResizeCoreArray(isResizeRows ? values.Length : RowCount, isResizeColumns ? j + 1 : ColumnCount);

            for (int i = 0; i < values.Length; i++)
            {
                this.CoreArray[i, j] = values[i];
            }
        }

        /// <summary>
        /// Provides a shallow copy of this matrix in O(row).
        /// </summary>
        /// <returns></returns>
        public Matrix Clone()
        {
            var buf = new Matrix(RowCount, ColumnCount)
            {
                CoreArray = (double[,])CoreArray.Clone()
            };
            return buf;
        }

        /// <summary>
        /// Swaps each matrix entry A[i, j] with A[j, i].
        /// </summary>
        /// <returns>A transposed matrix.</returns>
        public Matrix Transpose()
        {
            var buf = new Matrix(ColumnCount, RowCount);
            for (int row = 0; row < RowCount; row++)
                for (int column = 0; column < ColumnCount; column++)
                    //newMatrix[column*this.RowCount + row] = this.CoreArray[row*this.RowCount + column];
                    buf.CoreArray[column, row] = CoreArray[row, column];
            return buf;
        }

        public static void CopyTo(Matrix source, Matrix destination)
        {
            if (source.RowCount != destination.RowCount || source.ColumnCount != destination.ColumnCount)
                throw new NotImplementedException();

            Array.Copy(source.CoreArray, destination.CoreArray, destination.CoreArray.Length);
        }

        public static void TransposeCopyTo(Matrix source, Matrix destination)
        {
            if (source.RowCount != destination.ColumnCount || source.RowCount != destination.ColumnCount)
                throw new NotImplementedException();

            for (var i = 0; i < source.RowCount; i++)
                for (var j = 0; j < source.ColumnCount; j++)
                    destination.CoreArray[j, i] = source.CoreArray[i, j];
        }

        #region Equality Members

        protected bool Equals(Matrix other)
        {
            if (other.RowCount != this.RowCount)
                return false;

            if (other.ColumnCount != this.ColumnCount)
                return false;

            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    if (!MathUtil.Equals(CoreArray[i, j], other.CoreArray[i, j]))
                        return false;
                }
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj is Matrix m)
            {
                return Equals(m);
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Checks

        /// <summary>
        /// Checks if number of rows equals number of columns.
        /// </summary>
        /// <returns>True iff matrix is n by n.</returns>
        public bool IsSquare()
        {
            return ColumnCount == this.RowCount;
        }

        public bool IsSymmetric()
        {
            for (int i = 0; i < RowCount; i++)
                for (int j = i + 1; j < ColumnCount; j++)
                    if (!MathUtil.Equals(this.CoreArray[i, j], this.CoreArray[j, i]))
                    {
                        return false;
                    }

            return true;
        }

        /// <summary>
        /// Checks if A[i, j] == 0 for i < j.
        /// </summary>
        /// <returns>True iff matrix is upper trapeze.</returns>
        public bool IsUpperTrapeze()
        {
            for (int j = 0; j < ColumnCount; j++)
                for (int i = j + 1; i < RowCount; i++)
                    if (!MathUtil.Equals(CoreArray[i, j], 0d))
                        return false;

            return true;
        }

        /// <summary>
        /// Checks if A[i, j] == 0 for i > j.
        /// </summary>
        /// <returns>True iff matrix is lower trapeze.</returns>
        public bool IsLowerTrapeze()
        {
            for (int i = 0; i < RowCount; i++)
                for (int j = i + 1; j < ColumnCount; j++)
                    if (!MathUtil.Equals(CoreArray[i, j], 0))
                        return false;

            return true;
        }

        /// <summary>
        /// Checks if matrix is lower or upper trapeze.
        /// </summary>
        /// <returns>True if matrix is trapeze.</returns>
        public bool IsTrapeze()
        {
            return (IsUpperTrapeze() || IsLowerTrapeze());
        }

        /// <summary>
        /// Checks if matrix is trapeze and square.
        /// </summary>
        /// <returns>True iff matrix is triangular.</returns>
        public bool IsTriangular()
        {
            return (this.IsLowerTriangular() || this.IsUpperTriangular());
        }

        /// <summary>
        /// Checks if matrix is square and upper trapeze.
        /// </summary>
        /// <returns>True iff matrix is upper triangular.</returns>
        public bool IsUpperTriangular()
        {
            return (IsSquare() && this.IsUpperTrapeze());
        }

        /// <summary>
        /// Checks if matrix is square and lower trapeze.
        /// </summary>
        /// <returns>True iff matrix is lower triangular.</returns>
        public bool IsLowerTriangular()
        {
            return (IsSquare() && IsLowerTrapeze());
        }

        #endregion

        /// <summary>
        /// Swaps rows at specified indices. The latter do not have to be ordered.
        /// When equal, nothing is done.
        /// </summary>
        /// <param name="i1">One-based index of first row.</param>
        /// <param name="i2">One-based index of second row.</param>        
        public void SwapRows(int i1, int i2)
        {
            i1 = GetZeroBasedIndex(i1);
            i2 = GetZeroBasedIndex(i2);
            if (i1 < 0 || i1 >= RowCount || i2 < 0 || i2 >= RowCount)
                throw new ArgumentException("Indices must be positive and <= number of rows.");

            if (i1 == i2)
                return;

            for (int j = 0; j < ColumnCount; j++)
            {
                var tmp = CoreArray[i1, j];

                CoreArray[i1, j] = CoreArray[i2, j];
                CoreArray[i2, j] = tmp;
            }
        }

        /// <summary>
        /// Swaps columns at specified indices. The latter do not have to be ordered.
        /// When equal, nothing is done.
        /// </summary>
        /// <param name="j1">One-based index of first col.</param>
        /// <param name="j2">One-based index of second col.</param>       
        public void SwapColumns(int j1, int j2)
        {
            j1 = GetZeroBasedIndex(j1);
            j2 = GetZeroBasedIndex(j2);
            if (j1 <= 0 || j1 > ColumnCount || j2 <= 0 || j2 > ColumnCount)
                throw new ArgumentException("Indices must be positive and <= number of cols.");

            if (j1 == j2)
                return;

            var j1Col = this.ExtractColumn(j1).CoreArray;
            var j2Col = this.ExtractColumn(j2).CoreArray;

            for (int i = 0; i < RowCount; i++)
            {
                var tmp = CoreArray[i, j1];

                CoreArray[i, j1] = CoreArray[i, j2];
                CoreArray[i, j2] = tmp;
            }
        }

        /// <summary>
        /// Retrieves row vector at specfifed index
        /// </summary>
        /// <param name="i">One-based index at which to extract.</param>
        /// <returns>Row vector.</returns>
        public Matrix ExtractRow(int i)
        {
            i = GetZeroBasedIndex(i);
            if (i >= this.RowCount || i < 0)
                throw new ArgumentOutOfRangeException("i");

            var mtx = new Matrix(1, this.ColumnCount);


            for (int j = 0; j < this.ColumnCount; j++)
            {
                mtx.CoreArray[0, j] = CoreArray[i, j];
            }

            return mtx;
        }

        /// <summary>
        /// Retrieves row vector at specfifed index
        /// </summary>
        /// <param name="i">row index at which to extract.</param>
        /// <returns>Row vector.</returns>
        public double[] ExtractRowArray(int i)
        {
            i = GetZeroBasedIndex(i);
            if (i >= RowCount || i < 0)
                throw new ArgumentOutOfRangeException("i");

            var mtx = new double[ColumnCount];


            for (int j = 0; j < ColumnCount; j++)
            {
                mtx[j] = CoreArray[i, j];
            }

            return mtx;
        }

        /// <summary>
        /// Retrieves column vector at specfifed index and deletes it from matrix.
        /// </summary>
        /// <param name="j">One-based index at which to extract.</param>
        /// <returns>Row vector.</returns>
        public Matrix ExtractColumn(int j)
        {
            j = GetZeroBasedIndex(j);
            if (j >= ColumnCount || j < 0)
                throw new ArgumentOutOfRangeException("j");

            var mtx = new Matrix(RowCount, 1);


            for (int i = 0; i < RowCount; i++)
            {
                mtx.CoreArray[i, 0] = CoreArray[i, j];
            }

            return mtx;
        }
        /// <summary>
        /// Retrieves column vector at specfifed index and deletes it from matrix.
        /// </summary>
        /// <param name="j">One-based index at which to extract.</param>
        /// <returns>Row vector.</returns>
        public double[] ExtractColumnArray(int j)
        {
            j = GetZeroBasedIndex(j);
            if (j >= ColumnCount || j < 0)
                throw new ArgumentOutOfRangeException("j");

            var mtx = new double[RowCount];


            for (int i = 0; i < RowCount; i++)
            {
                mtx[i] = CoreArray[i, j];
            }

            return mtx;
        }

        /// <summary>
        /// Gets the determinant of matrix
        /// </summary>
        /// <returns></returns>
        public double Determinant()
        {
            if (!IsSquare())
                throw new InvalidOperationException();

            var clone = Clone();

            var n = RowCount;

            var sign = 1.0;

            var epsi1on = 1e-10 * clone.Min(x => Math.Abs(x));
            if (epsi1on == 0) epsi1on = 1e-9;
            for (var i = 0; i < n - 1; i++)
            {
                if (Math.Abs(clone[i, i]) < epsi1on)
                {
                    var firstNonZero = -1;

                    for (var k = i + 1; k < n; k++)
                        if (Math.Abs(clone[k, i]) > epsi1on)
                            firstNonZero = k;

                    if (firstNonZero == -1)
                        throw new OperationCanceledException();
                    else
                    {
                        clone.SwapRows(firstNonZero, i);
                        sign = -sign;
                    }
                }


                for (var j = i + 1; j < n; j++)
                {
                    var alfa = (clone.CoreArray[i, j] / clone.CoreArray[i, i]);

                    for (var k = i; k < n; k++)
                    {
                        clone.CoreArray[k, j] -= alfa * clone.CoreArray[k, i];
                    }
                }
            }

            var buf = sign;

            var arr = new double[n];

            for (var i = 0; i < n; i++)
                arr[i] = clone.CoreArray[i, i];

            Array.Sort(arr);

            for (var i = 0; i < n; i++)
                buf = buf * arr[n - i - 1];

            return buf;
        }

        /// <summary>
        /// Gets the inverse of matrix
        /// </summary>
        /// <returns></returns>
        public Matrix Inverse()
        {
            if (!IsSquare())
                throw new InvalidOperationException();
            var n = RowCount;
            var clone = Clone();
            var eye = Eye(n);

            var epsi1on = 1e-10 * clone.Min(x => Math.Abs(x));

            if (epsi1on == 0)
                epsi1on = 1e-9;

            /**/

            var perm = new List<int>();

            for (var j = 0; j < n - 1; j++)
            {
                for (var i = j + 1; i < n; i++)
                {
                    if (System.Math.Abs(clone[j, j]) < epsi1on)
                    {
                        var firstNonZero = -1;

                        for (var k = j + 1; k < n; k++)
                            if (System.Math.Abs(clone[k, j]) > epsi1on)
                                firstNonZero = k;

                        if (firstNonZero == -1)
                            throw new OperationCanceledException();
                        else
                        {
                            clone.SwapRows(firstNonZero, j);
                            eye.SwapRows(firstNonZero, j);

                            perm.Add(j);
                            perm.Add(firstNonZero);
                        }
                    }

                    var alfa = clone[i, j] / clone[j, j];

                    for (var k = 0; k < n; k++)
                    {
                        clone[i, k] -= alfa * clone[j, k];
                        eye[i, k] -= alfa * eye[j, k];
                    }
                }
            }

            /**/

            for (var j = n - 1; j > 0; j--)
            {
                for (var i = j - 1; i >= 0; i--)
                {
                    if (System.Math.Abs(clone[j, j]) < epsi1on)
                    {
                        var firstNonZero = -1;

                        for (var k = j - 1; k >= 0; k--)
                            if (System.Math.Abs(clone[k, j]) > epsi1on)
                                firstNonZero = k;

                        if (firstNonZero == -1)
                            throw new OperationCanceledException();
                        else
                        {
                            clone.SwapRows(firstNonZero, j);
                            eye.SwapRows(firstNonZero, j);

                            perm.Add(j);
                            perm.Add(firstNonZero);
                        }
                    }

                    var alfa = clone[i, j] / clone[j, j];

                    for (var k = n - 1; k >= 0; k--)
                    {
                        clone[i, k] -= alfa * clone[j, k];
                        eye[i, k] -= alfa * eye[j, k];
                    }
                }
            }

            /**/

            for (var i = 0; i < n; i++)
            {
                var alfa = 1 / clone[i, i];

                for (var j = 0; j < n; j++)
                {
                    clone[i, j] *= alfa;
                    eye[i, j] *= alfa;
                }
            }

            /**/

            return eye;
        }


        public Matrix Inverse2()
        {
            if (!IsSquare())
                throw new InvalidOperationException();

            //seems working good!
            var n = RowCount;
            var clone = Clone();
            var eye = Eye(n);

            var epsi1on = 1e-10 * clone.Min(x => Math.Abs(x));

            if (epsi1on == 0)
                epsi1on = 1e-9;

            /**/

            var perm = new List<int>();

            var clonea = clone.CoreArray;
            var eyea = eye.CoreArray;

            for (var j = 0; j < n - 1; j++)
            {
                for (var i = j + 1; i < n; i++)
                {
                    if (System.Math.Abs(clonea[j, j]) < epsi1on)
                    {
                        var firstNonZero = -1;

                        for (var k = j + 1; k < n; k++)
                            if (System.Math.Abs(clonea[k, j]) > epsi1on)
                                firstNonZero = k;

                        if (firstNonZero == -1)
                            throw new OperationCanceledException();
                        else
                        {
                            clone.SwapRows(firstNonZero, j);
                            eye.SwapRows(firstNonZero, j);

                            perm.Add(j);
                            perm.Add(firstNonZero);
                        }
                    }

                    var alfa = clonea[i, j] / clonea[i, j];

                    for (var k = 0; k < n; k++)
                    {
                        clonea[i, k] -= alfa * clonea[j, k];
                        eyea[i, k] -= alfa * eyea[j, k];
                    }
                }
            }

            /**/

            for (var j = n - 1; j > 0; j--)
            {
                for (var i = j - 1; i >= 0; i--)
                {
                    if (System.Math.Abs(clonea[j, j]) < epsi1on)
                    {
                        var firstNonZero = -1;

                        for (var k = j - 1; k >= 0; k--)
                            if (System.Math.Abs(clonea[k, j]) > epsi1on)
                                firstNonZero = k;

                        if (firstNonZero == -1)
                            throw new OperationCanceledException();
                        else
                        {
                            clone.SwapRows(firstNonZero, j);
                            eye.SwapRows(firstNonZero, j);

                            perm.Add(j);
                            perm.Add(firstNonZero);
                        }
                    }

                    var alfa = clonea[i, j] / clonea[j, j];

                    for (var k = n - 1; k >= 0; k--)
                    {
                        clonea[i, k] -= alfa * clonea[j, k];
                        eyea[i, k] -= alfa * eyea[j, k];
                    }
                }
            }

            /**/

            for (var i = 0; i < n; i++)
            {
                var alfa = 1 / clonea[i, i];

                for (var j = 0; j < n; j++)
                {
                    clonea[i, j] *= alfa;
                    eyea[i, j] *= alfa;
                }
            }

            /**/

            return eye;
        }

        #endregion

        #region Static Methods

        public static double[,] To2DDoubleArray(Matrix mtx)
        {
            var buf = new double[mtx.RowCount, mtx.ColumnCount];

            for (int i = 0; i < mtx.RowCount; i++)
                for (int j = 0; j < mtx.ColumnCount; j++)
                    buf[i, j] = mtx.CoreArray[i, j];

            return buf;
        }

        public static double[][] ToJaggedArray(Matrix mtx)
        {
            var buf = new double[mtx.RowCount][];

            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = new double[mtx.ColumnCount];
                for (int j = 0; j < mtx.ColumnCount; j++)
                    buf[i][j] = mtx[i, j];
            }
            return buf;
        }

        /// <summary>
        /// Creates a new Identity matrix.
        /// </summary>
        /// <param name="n">The n.</param>
        /// <returns></returns>
        public static Matrix Eye(int n)
        {
            var buf = new Matrix(n, n);

            for (int i = 0; i < n; i++)
                buf.CoreArray[i, i] = 1.0;

            return buf;
        }

        /// <summary>
        /// Creates m by n matrix filled with zeros.
        /// </summary>
        /// <param name="m">Number of rows.</param>
        /// <param name="n">Number of columns.</param>
        /// <returns>row by n matrix filled with zeros.</returns>
        public static Matrix Zeros(int m, int n)
        {
            return new Matrix(m, n);
        }

        /// <summary>
        /// Creates n by n matrix filled with zeros.
        /// </summary>       
        /// <param name="n">Number of rows and columns, resp.</param>
        /// <returns>n by n matrix filled with zeros.</returns>
        public static Matrix Zeros(int n)
        {
            return new Matrix(n);
        }

        /// <summary>
        /// Creates row by n matrix filled with ones.
        /// </summary>
        /// <param name="m">Number of rows.</param>
        /// <param name="n">Number of columns.</param>
        /// <returns>row by n matrix filled with zeros.</returns>
        public static Matrix Ones(int m, int n)
        {
            return new Matrix(m, n, 1);
        }

        /// <summary>
        /// Creates n by n matrix filled with ones.
        /// </summary>        
        /// <param name="n">Number of columns/rows.</param>
        /// <returns>n by n matrix filled with ones.</returns>        
        public static Matrix Ones(int n)
        {
            return Ones(n, n);
        }

        /// <summary>
        /// Computes product of main diagonal entries.
        /// </summary>
        /// <returns>Product of diagonal elements</returns>
        public double DiagProd()
        {
            var buf = 1.0;
            int dim = Math.Min(this.RowCount, this.ColumnCount);

            for (int i = 0; i < dim; i++)
            {
                buf *= this.CoreArray[i, i];
            }

            return buf;
        }

        /// <summary>
        /// Checks if matrix is n by one or one by n.
        /// </summary>
        /// <returns>Length, if vector; zero else.</returns>
        public int VectorLength()
        {
            if (ColumnCount > 1 && RowCount > 1)
                return 0;

            return Math.Max(ColumnCount, RowCount);
        }

        /// <summary>
        /// Generates diagonal matrix
        /// </summary>
        /// <param name="diag_vector">column vector containing the diag elements</param>
        /// <returns></returns>
        public static Matrix Diag(Matrix diag_vector)
        {
            int dim = diag_vector.VectorLength();

            if (dim == 0)
                throw new ArgumentException("diag_vector must be 1xN or Nx1");

            var M = new Matrix(dim, dim);

            for (int i = 0; i < dim; i++)
                M.CoreArray[i, i] = diag_vector.CoreArray[i, 0];

            return M;
        }

        /// <summary>
        /// Creates n by n identity matrix.
        /// </summary>
        /// <param name="n">Number of rows and columns respectively.</param>
        /// <returns>n by n identity matrix.</returns>
        public static Matrix Identity(int n)
        {
            return Eye(n);
        }

        #region Operators

        public static Matrix operator *(
            Matrix m1, Matrix m2)
        {
            return Matrix.Multiply(m1, m2);
        }

        public static Matrix operator *(
            Matrix m1, double[] vec)
        {
            return Multiply(m1, new Matrix(vec)); ;
        }

        public static Matrix operator *(
            double coeff, Matrix mat)
        {
            var newMat = new double[mat.RowCount, mat.ColumnCount];

            for (int i = 0; i < mat.RowCount; i++)
            {
                for (int j = 0; j < mat.ColumnCount; j++)
                {
                    newMat[i, j] = coeff * mat[i, j];
                }
            }

            return new Matrix() { CoreArray = newMat };
        }

        public static Matrix operator -(
            Matrix mat)
        {
            var buf = new Matrix(mat.RowCount, mat.ColumnCount);
            for (int i = 0; i < mat.RowCount; i++)
            {
                for (int j = 0; j < mat.ColumnCount; j++)
                {
                    buf.CoreArray[i, j] = -mat.CoreArray[i, j];
                }
            }
            return buf;
        }

        public static Matrix operator +(
            Matrix mat1, Matrix mat2)
        {
            MatrixException.ThrowIf(mat1.RowCount != mat2.RowCount || mat1.ColumnCount != mat2.ColumnCount,
                "Inconsistent matrix sizes");

            var buf = new Matrix(mat1.RowCount, mat1.ColumnCount);
            for (int i = 0; i < mat1.RowCount; i++)
            {
                for (int j = 0; j < mat1.ColumnCount; j++)
                {
                    buf.CoreArray[i, j] = mat1.CoreArray[i, j] + mat2.CoreArray[i, j];
                }
            }

            return buf;
        }

        public static Matrix operator -(
            Matrix mat1, Matrix mat2)
        {
            MatrixException.ThrowIf(mat1.RowCount != mat2.RowCount || mat1.ColumnCount != mat2.ColumnCount,
                "Inconsistent matrix sizes");

            var buf = new Matrix(mat1.RowCount, mat1.ColumnCount);
            for (int i = 0; i < mat1.RowCount; i++)
            {
                for (int j = 0; j < mat1.ColumnCount; j++)
                {
                    buf.CoreArray[i, j] = mat1.CoreArray[i, j] - mat2.CoreArray[i, j];
                }
            }

            return buf;
        }

        public static bool operator ==(Matrix left, Matrix right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Matrix left, Matrix right)
        {
            return !Equals(left, right);
        }

        #endregion

        #endregion

        public override string ToString()
        {
            var sb = new StringBuilder();

            var mtx = this;

            var epsi1on = mtx.Min(i => Math.Abs(i)) * 1e-9;

            if (epsi1on == 0)
                epsi1on = 1e-9;

            for (var i = 0; i < mtx.RowCount; i++)
            {
                for (var j = 0; j < mtx.ColumnCount; j++)
                {
                    if (Math.Abs(mtx[i, j]) < epsi1on)
                        sb.AppendFormat("0\t", mtx[i, j]);
                    else
                        sb.AppendFormat("{0:0.00}\t", mtx[i, j]);
                }
                sb.AppendLine();
            }

            return sb.ToString();
        }



        public void RemoveRow(int m)
        {
            m = GetZeroBasedIndex(m);
            var newArr = new double[RowCount - 1, ColumnCount];
            for (int i = 0; i < RowCount; i++)
            {
                if (i == m)
                {
                    continue;
                }
                for (int j = 0; j < ColumnCount; j++)
                {
                    var og = CoreArray[i, j];
                    if (i < m)
                    {
                        newArr[i, j] = og;
                    }
                    else
                    {
                        //i>m
                        newArr[i - 1, j] = og;
                    }
                }
            }
            CoreArray = newArr;
        }

        public void FillRow(int rowNum, params double[] values)
        {
            SetRow(rowNum, values);
        }

        public static Matrix DotDivide(Matrix a1, Matrix a2)
        {
            if (a1.RowCount != a2.RowCount || a1.ColumnCount != a2.ColumnCount)
                throw new Exception();

            var buf = new Matrix(a1.RowCount, a1.ColumnCount);

            for (int i = 0; i < a1.RowCount; i++)
            {
                for (int j = 0; j < a1.ColumnCount; j++)
                {
                    buf.CoreArray[i, j] = a1.CoreArray[i, j] / a2.CoreArray[i, j];
                }
            }
            return buf;
        }

        public static Matrix DotMultiply(Matrix a1, Matrix a2)
        {
            if (a1.RowCount != a2.RowCount || a1.ColumnCount != a2.ColumnCount)
                throw new Exception();

            var buf = new Matrix(a1.RowCount, a1.ColumnCount);
            for (int i = 0; i < buf.RowCount; i++)
            {
                for (int j = 0; j < buf.ColumnCount; j++)
                {
                    buf.CoreArray[i, j] = a1.CoreArray[i, j] * a2.CoreArray[i, j];
                }
            }
            return buf;
        }

        public static double DotProduct(Matrix a1, Matrix a2)
        {
            if (a1.RowCount != a2.RowCount || a1.ColumnCount != a2.ColumnCount)
                throw new Exception();

            var buf = 0d;

            for (int i = 0; i < a1.RowCount; i++)
            {
                for (int j = 0; j < a1.ColumnCount; j++)
                {
                    buf += a1.CoreArray[i, j] * a2.CoreArray[i, j];
                }
            }
            return buf;
        }

        public void FillColumn(int colNum, params double[] values)
        {
            SetColumn(colNum, values);
        }

        public void RemoveColumn(int n)
        {
            n = GetZeroBasedIndex(n);
            var newArr = new double[RowCount, ColumnCount - 1];
            for (int j = 0; j < ColumnCount; j++)
            {
                if (j == n)
                {
                    continue;
                }
                for (int i = 0; i < RowCount; i++)
                {
                    var og = CoreArray[i, j];
                    if (j < n)
                    {
                        newArr[i, j] = og;
                    }
                    else
                    {
                        //i>m
                        newArr[i, j - 1] = og;
                    }
                }
            }
            CoreArray = newArr;
        }
        public void FillMatrix(double val)
        {
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    CoreArray[i, j] = val;
                }
            }
        }
        public void SetRowToValue(int i, double val)
        {
            i = GetZeroBasedIndex(i);
            for (int j = 0; j < ColumnCount; j++)
            {
                CoreArray[i, j] = val;
            }
        }

        public void SetColumnToValue(int j, double val)
        {
            j = GetZeroBasedIndex(j);
            for (int i = 0; i < RowCount; i++)
            {
                this[i, j] = val;
            }
        }

        #region Serialization stuff

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("rowCount", RowCount);
            info.AddValue("columnCount", ColumnCount);
            info.AddValue("coreArray", CoreArray);
        }

        protected Matrix(
            SerializationInfo info,
            StreamingContext context)
        {
            this.RowCount = (int)info.GetValue("rowCount", typeof(int));
            this.ColumnCount = (int)info.GetValue("columnCount", typeof(int));
            this.CoreArray = (double[,])info.GetValue("coreArray", typeof(double[,]));
        }

        #endregion

        /// <summary>
        /// Clears this instance. Turn all members to 0...
        /// </summary>
        public void Clear()
        {
            FillMatrix(0d);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return CoreArray.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<double> GetEnumerator()
        {
            var linear = new List<double>(RowCount * ColumnCount);
            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    linear.Add(CoreArray[i, j]);
                }
            }
            return linear.GetEnumerator();
        }

        /// <summary>
        /// Fills the matrix in more straight way!
        /// it is assumed that vals are columns and mappind 2d array to 1d is by placing matrix columns under each other
        /// </summary>
        /// <param name="vals"></param>
        public void FillWith(params double[] vals)
        {
            Array.Copy(vals, CoreArray, Math.Max(vals.Length, CoreArray.Length));
        }
        /// <summary>
        /// Fills the matrix rowise (all rows are beside each other).
        /// </summary>
        /// <param name="members">The members.</param>
        /// <exception cref="System.Exception"></exception>
        public void FillMatrixRowise(params double[] members)
        {
            if (members.Length != this.CoreArray.Length)
                throw new Exception();

            for (int i = 0; i < this.RowCount; i++)
            {
                for (int j = 0; j < this.ColumnCount; j++)
                {
                    //column * this.rowCount + row
                    this[i, j] = members[FlattenIndexRowWise(i, j)];
                }
            }
        }

        /// <summary>
        /// Fills the matrix col wise (all columns are below each other)
        /// </summary>
        /// <param name="members">The members.</param>
        /// <exception cref="System.Exception"></exception>
        public void FillMatrixColWise(params double[] members)
        {
            if (members.Length != this.CoreArray.Length)
                throw new Exception();

            for (int i = 0; i < RowCount; i++)
            {
                for (int j = 0; j < ColumnCount; j++)
                {
                    this[i, j] = members[FlattenIndexColumnWise(i, j)];
                }
            }
        }

        #region nonzero Pattern

        /// <summary>
        /// The nonzero pattern for each column
        /// </summary>
        [NonSerialized]
        internal List<int>[] ColumnNonzeros;

        /// <summary>
        /// The nonzero pattern for each row
        /// </summary>
        [NonSerialized]
        internal List<int>[] RowNonzeros;


        public void UpdateNonzeroPattern()
        {
            #region row nonzeros

            if (RowNonzeros == null)
                RowNonzeros = new List<int>[RowCount];

            for (int i = 0; i < RowCount; i++)
            {
                if (RowNonzeros[i] == null)
                    RowNonzeros[i] = new List<int>();
                else
                    RowNonzeros[i].Clear();


                for (int j = 0; j < ColumnCount; j++)
                {
                    if (!CoreArray[i, j].Equals(0d))
                        RowNonzeros[i].Add(j);
                }
            }

            #endregion

            #region col nonzeros

            if (ColumnNonzeros == null)
                ColumnNonzeros = new List<int>[ColumnCount];

            for (int j = 0; j < ColumnCount; j++)
            {
                if (ColumnNonzeros[j] == null)
                    ColumnNonzeros[j] = new List<int>();
                else
                    ColumnNonzeros[j].Clear();


                for (int i = 0; i < RowCount; i++)
                {
                    if (!CoreArray[i, j].Equals(0d))
                        ColumnNonzeros[j].Add(i);
                }
            }

            #endregion
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        #endregion
    }
}
