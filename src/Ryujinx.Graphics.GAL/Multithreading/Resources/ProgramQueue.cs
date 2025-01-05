using Ryujinx.Graphics.GAL.Multithreading.Resources.Programs;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Ryujinx.Graphics.GAL.Multithreading.Resources
{
    /// <summary>
    /// A structure handling multithreaded compilation for programs.
    /// </summary>
    class ProgramQueue
    {
        private const int MaxConcurrentCompilations = 8;

        private readonly IRenderer _renderer;

        private readonly Queue<IProgramRequest> _toCompile;
        private readonly List<ThreadedProgram> _inProgress;

        public ProgramQueue(IRenderer renderer)
        {
            _renderer = renderer;

            _toCompile = new Queue<IProgramRequest>();
            _inProgress = [];
        }

        public void Add(IProgramRequest request)
        {
            lock (_toCompile)
            {
                _toCompile.Enqueue(request);
            }
        }

        public void ProcessQueue()
        {
            for (int i = 0; i < _inProgress.Count; i++)
            {
                ThreadedProgram program = _inProgress[i];

                ProgramLinkStatus status = program.Base.CheckProgramLink(false);

                if (status != ProgramLinkStatus.Incomplete)
                {
                    program.Compiled = true;
                    _inProgress.RemoveAt(i--);
                }
            }

            int freeSpace = MaxConcurrentCompilations - _inProgress.Count;

            for (int i = 0; i < freeSpace; i++)
            {
                // Begin compilation of some programs in the compile queue.
                IProgramRequest program;

                lock (_toCompile)
                {
                    if (!_toCompile.TryDequeue(out program))
                    {
                        break;
                    }
                }

                if (program.Threaded.Base != null)
                {
                    ProgramLinkStatus status = program.Threaded.Base.CheckProgramLink(false);

                    if (status != ProgramLinkStatus.Incomplete)
                    {
                        // This program is already compiled. Keep going through the queue.
                        program.Threaded.Compiled = true;
                        i--;
                        continue;
                    }
                }
                else
                {
                    program.Threaded.Base = program.Create(_renderer);
                }

                _inProgress.Add(program.Threaded);
            }
        }

        /// <summary>
        /// Process the queue until the given program has finished compiling.
        /// This will begin compilation of other programs on the queue as well.
        /// </summary>
        /// <param name="program">The program to wait for</param>
        public void WaitForProgram(ThreadedProgram program)
        {
            Span<SpinWait> spinWait = stackalloc SpinWait[1];

            while (!program.Compiled)
            {
                ProcessQueue();

                if (!program.Compiled)
                {
                    spinWait[0].SpinOnce(-1);
                }
            }
        }
    }
}
