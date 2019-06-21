using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace MaximEmmBots.Extensions
{
    internal static class GoogleAttachmentExtensions
    {
        [SecurityCritical]
        internal static string ToBase64Url(this string input)
        {
            var readOnlyInput = input.AsMemory();
            var writeableInput = Unsafe.As<ReadOnlyMemory<char>, Memory<char>>(ref readOnlyInput);
            foreach (ref var elem in writeableInput.Span)
            {
                if (elem == '-')
                    elem = '+';
                if (elem == '_')
                    elem = '/';
            }
            return input;
        }

        internal static string ToFileName(this string input)
        {
            var readOnlyInput = input.AsMemory();
            var writeableInput = Unsafe.As<ReadOnlyMemory<char>, Memory<char>>(ref readOnlyInput);
            foreach (ref var elem in writeableInput.Span)
            {
                if (elem == '/')
                    elem = '_';
            }
            return input;
        }
    }
}