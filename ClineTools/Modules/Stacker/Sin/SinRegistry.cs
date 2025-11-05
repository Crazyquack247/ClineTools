using System;
using System.Collections.Generic;

namespace ClineTools.Modules.Stacker.Sin
{
    public sealed class SinRegistry
    {
        private readonly List<ISinDecoder> _decoders = new List<ISinDecoder>();

        public void Register(ISinDecoder decoder)
        {
            if (decoder == null) throw new ArgumentNullException(nameof(decoder));
            _decoders.Add(decoder);
        }

        public ISinDecoder Resolve(string rawSin)
        {
            string norm = SinFormats.Normalize(rawSin);
            foreach (var d in _decoders)
                if (d.CanHandle(norm)) return d;
            return null;
        }

        public object DecodeCardOrThrow(string rawSin)
        {
            string norm = SinFormats.Normalize(rawSin);
            foreach (var d in _decoders)
            {
                if (!d.CanHandle(norm)) continue;
                return d.DecodeToCard(norm);
            }
            throw new ArgumentException("No decoder matches this SIN format.");
        }
    }
}