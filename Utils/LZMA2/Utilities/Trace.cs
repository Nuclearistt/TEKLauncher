using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Globalization;
using D = System.Diagnostics;

namespace ManagedLzma.LZMA
{
    namespace Master
    {
        partial class LZMA
        {
            private static void TR(string str, int arg)
            {
            }

            private static void TR(string str, uint arg)
            {
            }

            private static void TRS(string str1, string str2)
            {
            }

            private static SRes TSZ(string kind)
            {
                return SZ_OK;
            }
        }
    }

    internal static class Trace
    {

        #region Public Methods

        public static void InitSession(Guid id)
        {
            throw new NotSupportedException();
        }

        public static void StopSession()
        {
            throw new NotSupportedException();
        }

        public static void MatchObjectCreate(object obj, string arg)
        {
        }

        public static void MatchObjectDestroy(object obj, string arg)
        {
        }

        public static void MatchObjectWait(object obj, string arg)
        {
        }

        public static int MatchStatusCode(string arg)
        {
            throw new NotSupportedException();
        }

        public static void Match(int arg1, int arg2 = 0)
        {
            throw new NotSupportedException();
        }

        public static void Match(string arg1, int arg2 = 0)
        {
            throw new NotSupportedException();
        }

        public static void Match(string arg1, uint arg2)
        {
            throw new NotSupportedException();
        }

        public static void Match(string arg1, string arg2)
        {
            throw new NotSupportedException();
        }

        #endregion

        internal static void AllocSmallObject(string type, Master.LZMA.ISzAlloc alloc)
        {
        }
    }
}
