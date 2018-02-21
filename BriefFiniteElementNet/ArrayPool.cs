///Copyright http://geekswithblogs.net/
///

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BriefFiniteElementNet
{
    public class ArrayPool<T>
    {
        private readonly Dictionary<int, Stack<Array>> _pool = new Dictionary<int, Stack<Array>>();

        public readonly T[] Empty = new T[0];
        public readonly T[,] Empty2D = new T[0, 0];
        public virtual void Clear()
        {
            _pool.Clear();
        }

        internal virtual T[] Allocate(int size)
        {
            if (size < 0) throw new ArgumentOutOfRangeException("size", "Must be positive.");

            if (size == 0) return Empty;


            var res = _pool.TryGetValue(size, out Stack<Array> candidates) && candidates.Count > 0 ? candidates.Pop() : new T[size];
            return (T[])res;
        }

        internal virtual void Free(Array array)
        {
            if (array == null) throw new ArgumentNullException("array");
            if (array.Length == 0) return;


            if (!_pool.TryGetValue(array.Length, out Stack<Array> candidates))
                _pool.Add(array.Length, candidates = new Stack<Array>());

            if (candidates.Count < MaxQLength)
                if (!candidates.Contains(array))
                {
                    candidates.Push(array);
                }
        }

        private readonly int MaxQLength = 20;
    }

    public class ConcurrentArrayPool<T> : ArrayPool<T>
    {
        internal override T[] Allocate(int size)
        {
            lock (this)
            {
                return base.Allocate(size);
            }
        }


        internal override void Free(Array array)
        {
            lock (this)
            {
                base.Free(array);
            }
        }

        public override void Clear()
        {
            lock (this)
            {
                base.Clear();
            }
        }
    }

    public static class MatrixPool
    {
        private static ConcurrentArrayPool<double> Pool = new ConcurrentArrayPool<double>();

        public static Matrix Allocate(int rows, int columns)
        {
            var arr = Pool.Allocate(rows * columns);
            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = 0;
            }
            var buf = new Matrix(rows, columns, arr)
            {
                UsePool = true
            };

            return buf;
        }

        public static void Free(params Matrix[] matrices)
        {
            foreach (var mtx in matrices)
            {
                if (mtx.CoreArray == null)
                    continue;

                Pool.Free(mtx.CoreArray);
                mtx.CoreArray = null;
            }
        }
    }
}
