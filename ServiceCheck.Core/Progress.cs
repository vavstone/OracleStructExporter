using System;
using System.Threading;

namespace ServiceCheck.Core
{
    public interface IProgress<in T>
    {
        void Report(T value);
    }

    public class Progress<T> : IProgress<T>
    {
        private readonly Action<T> _handler;
        private readonly SynchronizationContext _context;

        public Progress(Action<T> handler)
        {
            _handler = handler;
            _context = SynchronizationContext.Current;
        }

        public void Report(T value)
        {
            if (_context != null)
                _context.Post(state => _handler((T)state), value);
            else
                _handler(value);
        }
    }
}
